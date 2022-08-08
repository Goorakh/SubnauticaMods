using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityModdingUtility;
using UnityModdingUtility.Extensions;

namespace GRandomizer.RandomizerControllers
{
    public static class SpriteRandomizer
    {
        public enum Mode : byte
        {
            Off,
            SameCategory,
            Random
        }

        static Mode mode
        {
            get
            {
                return Mode.Off;
                //return Mod.Config.SpriteRandomizerMode;
            }
        }

        static bool IsEnabled() => mode > Mode.Off;

        static void Reset()
        {
            _spriteReplacements.Reset();
        }

        public static void Serialize(BinaryWriter writer)
        {
            if (writer.WriteAndReturn(_spriteReplacements.IsInitialized))
            {
                writer.Write(_spriteReplacements.Get);
            }
        }

        public static void Deserialize(VersionedBinaryReader reader)
        {
            if (reader.ReadBoolean()) // _toolNameReplacements.IsInitialized
            {
                _spriteReplacements.SetValue(reader.ReadReplacementDictionary<SpriteIdentifier>());
            }
        }

        static readonly InitializeOnAccess<SpriteIdentifier[]> _allSprites = new InitializeOnAccess<SpriteIdentifier[]>(() =>
        {
            IEnumerable<SpriteIdentifier> getSprites()
            {
                foreach (KeyValuePair<SpriteManager.Group, Dictionary<string, Atlas.Sprite>> spritesPair in SpriteManager.groups)
                {
                    SpriteManager.Group spriteGroup = spritesPair.Key;

                    foreach (string spriteName in spritesPair.Value.Keys)
                    {
                        Utils.DebugLog($"(1) Adding {spriteGroup} {spriteName}");
                        yield return new SpriteIdentifier(spriteGroup, spriteName);
                    }
                }

                foreach (SpriteManager.Group group in (SpriteManager.Group[])Enum.GetValues(typeof(SpriteManager.Group)))
                {
                    if (SpriteManager.mapping.TryGetValue(group, out string atlasName))
                    {
                        Atlas atlas = Atlas.GetAtlas(atlasName);
                        if (atlas.Exists())
                        {
                            foreach (string spriteName in atlas.nameToSprite.Keys)
                            {
                                Utils.DebugLog($"(2) Adding {group} {spriteName}");
                                yield return new SpriteIdentifier(group, spriteName);
                            }
                        }
                    }
                }
            }

            return getSprites().Distinct().ToArray();
        });

        static readonly InitializeOnAccess<ReplacementDictionary<SpriteIdentifier>> _spriteReplacements = new InitializeOnAccess<ReplacementDictionary<SpriteIdentifier>>(() =>
        {
            switch (mode)
            {
                case Mode.SameCategory:
                    return new ReplacementDictionary<SpriteIdentifier>((from sprite in _allSprites.Get
                                                                        group sprite by sprite.Group into gr
                                                                        from replacementPair in gr.ToRandomizedReplacementDictionary(SpriteIdentifier.EqualityComparer.Instance)
                                                                        select replacementPair).ToDictionary(SpriteIdentifier.EqualityComparer.Instance));
                case Mode.Random:
                    return _allSprites.Get.ToRandomizedReplacementDictionary(SpriteIdentifier.EqualityComparer.Instance);
                default:
                    throw new NotImplementedException($"{mode} is not implemented");
            }
        });

        [HarmonyPatch]
        static class SpriteManager_GetWithNoDefault_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => SpriteManager.GetWithNoDefault(default, default));
            }

            public static bool IsRunning = false;

            static void Prefix(ref SpriteManager.Group group, ref string name)
            {
                IsRunning = true;

                if (IsEnabled())
                {
                    if (_spriteReplacements.Get.TryGetReplacement(new SpriteIdentifier(group, name), out SpriteIdentifier replacement))
                    {
                        group = replacement.Group;
                        name = replacement.Name;
                    }
                }
            }

            static void Postfix()
            {
                IsRunning = false;
            }
        }

        //[HarmonyPatch]
        //static class SpriteManager_GetFromResources_Patch
        //{
        //
        //}
    }
}

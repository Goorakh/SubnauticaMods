using GRandomizer.Util.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace GRandomizer.Util.Lifepod
{
    public abstract class LifepodModelInfo
    {
        protected const string LIFEPOD_NAME = "LIFEPOD 5";

        static Dictionary<LifepodModelType, LifepodModelInfo> _modelInfoByType;
        public static void InitializeModelInfoByTypeDictionary()
        {
            if (_modelInfoByType != null)
                return;

            _modelInfoByType = (from type in TypeCollection.GetAllTypes(TypeFlags.ThisAssembly | TypeFlags.Class)
                                where !type.IsAbstract && typeof(LifepodModelInfo).IsAssignableFrom(type)
                                let attr = type.GetCustomAttribute<LifepodModelTypeAttribute>()
                                where attr != null
                                select new KeyValuePair<LifepodModelType, LifepodModelInfo>(attr.Type, (LifepodModelInfo)Activator.CreateInstance(type, attr.Type))).ToDictionary();
        }

        public static LifepodModelInfo GetRandomModelInfo()
        {
            return _modelInfoByType.Values.GetRandomOrDefault();
        }

        public static LifepodModelInfo GetByType(LifepodModelType lifepodModelType)
        {
            if (_modelInfoByType.TryGetValue(lifepodModelType, out LifepodModelInfo modelInfo))
            {
                return modelInfo;
            }

            throw new KeyNotFoundException(string.Format(nameof(LifepodModelType) + ".{0} is not implemented", lifepodModelType));
        }

        public static void ResetInstances()
        {
            _modelInfoByType.Values.ForEach(lmi => lmi.reset());
        }

        [Flags]
        public enum InteriorObjectFlags : byte
        {
            None,
            FireExtinguisher = 1 << 0,
            SeatL = 1 << 1,
            Storage = 1 << 2,
            Ladder = 1 << 3,
            FlyingPanel = 1 << 4
        }

        public readonly LifepodModelType Type;

        protected Dictionary<string, bool> _overrideIntroLifepodDirectorActiveObjectStates = new Dictionary<string, bool>();

        protected string _modelObjectPrefabIdentifier;

        public LifepodModelInfo(LifepodModelType type)
        {
            Type = type;
        }

        public virtual bool DisableTutorial => false;

        public virtual InteriorObjectFlags ShowInteriorObjects => DisableTutorial ? InteriorObjectFlags.None : InteriorObjectFlags.FireExtinguisher;

        public virtual FakeParentData FakeParentData => null;

        public GameObject ModelObject { get; private set; }

        public bool LoadedFromSaveFile { get; private set; } = false;

        protected virtual void reset()
        {
            _overrideIntroLifepodDirectorActiveObjectStates.Clear();
            ModelObject = null;
            LoadedFromSaveFile = false;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.WriteGeneric(Type);
            writer.WriteGeneric(_modelObjectPrefabIdentifier);
        }

        protected virtual void deserialize(BinaryReader reader, ushort version)
        {
            // LifepodModelType is already read at this point

            _modelObjectPrefabIdentifier = reader.ReadGeneric<string>();
        }

        public static LifepodModelInfo Deserialize(BinaryReader reader, ushort version)
        {
            LifepodModelInfo modelInfo = GetByType(reader.ReadGeneric<LifepodModelType>());
            modelInfo.LoadedFromSaveFile = true;
            modelInfo.deserialize(reader, version);
            return modelInfo;
        }

        public void FindLifepodModelAfterLoad()
        {
            if (_modelObjectPrefabIdentifier == null)
            {
                Utils.LogError($"Could not locate lifepod model object for {GetType().Name}: null ID");
                return;
            }

            if (UniqueIdentifier.TryGetIdentifier(_modelObjectPrefabIdentifier, out UniqueIdentifier identifier))
            {
#if VERBOSE
                Utils.DebugLog($"Found lifepod model object with id {_modelObjectPrefabIdentifier} for {GetType().Name}: {identifier.gameObject}");
#endif
                ModelObject = identifier.gameObject;
                prepareModel();
            }
            else
            {
                Utils.LogError($"Could not locate lifepod model object with id {_modelObjectPrefabIdentifier} for {GetType().Name}");
            }
        }

        public virtual void OnLifepodPositioned()
        {
            if (ModelObject.Exists())
            {
                updateModelTransform();
            }
            else
            {
                Utils.LogError($"No model object!");
            }
        }

        protected abstract void spawnModel(Action<LifepodModelData> onComplete);

        public void Replace()
        {
            if (!LoadedFromSaveFile)
            {
                spawnModel(modelData =>
                {
                    ModelObject = modelData.MainModel;
                    ModelObject.RegisterLargeWorldEntityOnceStreamerInitialized();

                    UniqueIdentifier identifier = ModelObject.GetComponent<UniqueIdentifier>();
                    if (identifier.Exists())
                    {
                        _modelObjectPrefabIdentifier = identifier.Id;
                    }

                    if (GameModeUtils.RequiresSurvival())
                    {
                        if (modelData.Radio.Exists())
                        {
                            LiveMixin radioLiveMixin = modelData.Radio.liveMixin;
                            if (radioLiveMixin.Exists() && radioLiveMixin.IsFullHealth())
                            {
                                radioLiveMixin.TakeDamage(80f);
                            }
                        }

                        LiveMixin playerLiveMixin = Player.main.GetComponent<LiveMixin>();
                        if (playerLiveMixin.Exists() && playerLiveMixin.IsFullHealth())
                        {
                            playerLiveMixin.TakeDamage(20f, default(Vector3), DamageType.Normal, null);
                        }
                    }

                    prepareModel();
                    prepareForIntro();
                });
            }
        }

        protected virtual void prepareForIntro()
        {
            EscapePod escapePod = EscapePod.main;

            escapePod.gameObject.DisableAllCollidersOfType<Collider>();

            escapePod.transform.TryDisableChild("models/Life_Pod_damaged_LOD1");
            escapePod.transform.TryDisableChild("models/Life_Pod_damaged_03/root/UISpawn");
            escapePod.transform.TryDisableChild("ModulesRoot");

            Transform interiorModelRoot = escapePod.transform.Find("models/Life_Pod_damaged_03/lifepod_damaged_03_geo");
            if (interiorModelRoot.Exists())
            {
                InteriorObjectFlags interiorFlags = ShowInteriorObjects;
                if (interiorFlags == InteriorObjectFlags.None)
                {
                    interiorModelRoot.gameObject.SetActive(false);
                }
                else
                {
                    List<string> showChildren = new List<string>();

                    if ((interiorFlags & InteriorObjectFlags.FireExtinguisher) != 0)
                        showChildren.Add("fire_extinguisher_01_tp");

                    if ((interiorFlags & InteriorObjectFlags.SeatL) != 0)
                        showChildren.Add("life_pod_seat_01_L");

                    if ((interiorFlags & InteriorObjectFlags.Storage) != 0)
                    {
                        showChildren.Add("life_pod_storage_01");
                        showChildren.Add("life_pod_storage_01_door");
                    }

                    if ((interiorFlags & InteriorObjectFlags.FlyingPanel) != 0)
                        showChildren.Add("life_pod_wall_panel_01_door");

                    interiorModelRoot.DisableAllChildrenExcept(showChildren.ToArray());

                    _overrideIntroLifepodDirectorActiveObjectStates["models/Life_Pod_damaged_03/lifepod_damaged_03_geo/life_pod_aid_box_01"] = false;
                    _overrideIntroLifepodDirectorActiveObjectStates["models/Life_Pod_damaged_03/lifepod_damaged_03_geo/life_pod_aid_box_01_base"] = false;
                }
            }
        }

        protected virtual void prepareModel()
        {
        }

        protected virtual void updateModelTransform()
        {
            EscapePod escapePod = EscapePod.main;
            FakeParentData fakeParentData = FakeParentData;
            if (fakeParentData != null)
            {
                ModelObject.transform.rotation = escapePod.transform.rotation * fakeParentData.LocalRotation;
                ModelObject.transform.position = escapePod.transform.TransformPoint(fakeParentData.LocalPosition);
            }
            else if (!ModelObject.transform.IsChildOf(escapePod.transform))
            {
                ModelObject.transform.position = escapePod.transform.position;
                ModelObject.transform.rotation = escapePod.transform.rotation;
            }
        }

        public virtual void EndIntro(bool skipped)
        {
            if (DisableTutorial || skipped)
            {
                EscapePod.main.gameObject.SetActive(false);

                if (!skipped)
                {
                    GlobalObject.Schedule(IntroLifepodDirector.main.SetHudToActive, 30f);
                    GlobalObject.Schedule(IntroLifepodDirector.main.OpenPDA, 4.1f);
                    GlobalObject.Schedule(IntroLifepodDirector.main.ResetFirstUse, 8f);
                }

                cleanup();
            }
        }

        public virtual void TutorialFinished()
        {
            if (!DisableTutorial)
            {
                EscapePod.main.gameObject.SetActive(false);
                cleanup();
            }
        }

        protected virtual void cleanup()
        {
        }

        protected virtual GameObject spawnStaticBuildable(TechType type, Transform parent, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
        {
            GameObject obj = CraftData.InstantiateFromPrefab(type);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
            obj.transform.localEulerAngles = localEuler;
            obj.transform.localScale = localScale;

            Constructable constructable = obj.GetComponent<Constructable>();
            if (constructable.Exists())
                constructable.deconstructionAllowed = false;

            if (type == TechType.MedicalCabinet)
            {
                obj.GetComponent<MedicalCabinet>().ForceSpawnMedKit();
            }

            return obj;
        }

        protected GameObject spawnStaticBuildable(TechType type, Transform parent, Vector3 localPos, Vector3 localEuler)
        {
            return spawnStaticBuildable(type, parent, localPos, localEuler, Vector3.one);
        }

        public virtual Vector3 GetOverrideLifepodPosition(Vector3 originalPos)
        {
            return originalPos;
        }

        public virtual void RespawnPlayer(Player player)
        {
            Utils.LogError($"Not implemented for {GetType().Name}");
        }

        public bool TryGetIntroLifepodDirectorActiveObjectOverrideState(string relativePath, out bool state)
        {
            return _overrideIntroLifepodDirectorActiveObjectStates.TryGetValue(relativePath, out state);
        }
    }
}

using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.RandomizerControllers.Callbacks
{
    static class RandomizerControllerCallbacks
    {
        static readonly InitializeOnAccess<Type[]> _randomizerControllerTypes = new InitializeOnAccess<Type[]>(() =>
        {
            return TypeCollection.GetAllTypes(TypeFlags.ThisAssembly | TypeFlags.Class).Where(t => t.GetCustomAttribute<RandomizerControllerAttribute>() != null).ToArray();
        });

        static void invoke(string methodName, params object[] parameters)
        {
            foreach (Type randomizerControllerType in _randomizerControllerTypes.Get)
            {
                AccessTools.DeclaredMethod(randomizerControllerType, methodName)?.Invoke(null, parameters);
            }
        }

        public static void Reset()
        {
            invoke("Reset");
        }

        public static void Initialize()
        {
            invoke("Initialize");
        }
    }
}

﻿using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class SpawnLocationRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomSpawnLocation;
        }

        [HarmonyPatch]
        static class RandomStart_IsStartPointValid_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<RandomStart>(_ => _.IsStartPointValid(default, default));
            }

            static bool Prefix(ref bool __result, Vector3 point)
            {
                if (IsEnabled())
                {
                    __result = point.magnitude < Mod.Config.MaxSpawnRadius;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}

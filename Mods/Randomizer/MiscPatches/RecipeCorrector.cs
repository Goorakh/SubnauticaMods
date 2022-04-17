using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.MiscPatches
{
    static class RecipeCorrector
    {
        static readonly InitializeOnAccess<TechType[]> _ingredientsToCorrect = new InitializeOnAccess<TechType[]>(() =>
        {
            return CraftData.harvestOutputList.Values
                            .AddItem(TechType.CreepvineSeedCluster) // TODO: Don't hardcode these
                            .AddItem(TechType.StalkerTooth)
                            .ToArray();
        });

        // Note: Null value is used as a 'use original' sign, instead of using a separate dictionary/collection for tracking that
        static readonly Dictionary<IIngredient, RandomizedIngredient> _ingredientReplacements = new Dictionary<IIngredient, RandomizedIngredient>();

        static IIngredient tryGetCorrectedIngredient(IIngredient original)
        {
            if (!LootRandomizer.IsEnabled())
                return original;

            if (_ingredientReplacements.TryGetValue(original, out RandomizedIngredient replacement))
                return replacement ?? original;

            if (_ingredientsToCorrect.Get.Contains(original.techType))
            {
                return _ingredientReplacements[original] = new RandomizedIngredient(original.techType, original.amount);
            }
            else
            {
                _ingredientReplacements[original] = null;
                return original;
            }
        }

        [HarmonyPatch]
        static class TechData_GetIngredient_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<CraftData.TechData>(_ => _.GetIngredient(default));
                yield return SymbolExtensions.GetMethodInfo<SMLHelper.V2.Crafting.TechData>(_ => _.GetIngredient(default));
            }

            static IIngredient Postfix(IIngredient __result)
            {
                return tryGetCorrectedIngredient(__result);
            }
        }

        class RandomizedIngredient : IIngredient
        {
            readonly TechType _originalTechType;
            public TechType techType => LootRandomizer.TryGetItemReplacement(_originalTechType);

            public int amount { get; }

            public RandomizedIngredient(TechType originalTechType, int amount)
            {
                _originalTechType = originalTechType;
                this.amount = amount;
            }
        }
    }
}

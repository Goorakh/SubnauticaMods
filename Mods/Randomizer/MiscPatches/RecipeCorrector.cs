using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GRandomizer.MiscPatches
{
    static class RecipeCorrector
    {
        static readonly InitializeOnAccess<HashSet<TechType>> _ingredientsToCorrect = new InitializeOnAccess<HashSet<TechType>>(() =>
        {
            return CraftData.harvestOutputList.Values
                            .AddItem(TechType.CreepvineSeedCluster) // TODO: Don't hardcode these
                            .AddItem(TechType.StalkerTooth)
                            .ToHashSet();
        });

        // Note: Null value is used as a 'use original' sign, instead of using a separate dictionary/collection for tracking that
        static readonly InitializeOnAccessDictionary<IIngredient, RandomizedIngredient> _ingredientReplacements = new InitializeOnAccessDictionary<IIngredient, RandomizedIngredient>(key =>
        {
            if (_ingredientsToCorrect.Get.Contains(key.techType))
            {
                return new RandomizedIngredient(key.techType, key.amount);
            }
            else
            {
                return null;
            }
        });

        static IIngredient tryGetCorrectedIngredient(IIngredient original)
        {
            return LootRandomizer.IsEnabled() ? _ingredientReplacements[original] ?? original : original;
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

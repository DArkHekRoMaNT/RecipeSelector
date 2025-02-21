using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.Common;
using System;

namespace RecipeSelector
{
    [HarmonyPatch(typeof(InventoryCraftingGrid))]
    [HarmonyPatch("FindMatchingRecipe")]
    public static class Patch_InventoryCraftingGrid_FindMatchingRecipe
    {
        private static RecipeSelector? _recipeSelector;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var foundMatchMethod = AccessTools.Method(typeof(InventoryCraftingGrid), "FoundMatch");

            var lastMatchingRecipeIndex = generator.DeclareLocal(typeof(GridRecipe)).LocalIndex;
            yield return CodeInstruction.LoadArgument(0);
            yield return CodeInstruction.LoadField(typeof(InventoryCraftingGrid), nameof(InventoryCraftingGrid.MatchingRecipe));
            yield return CodeInstruction.StoreLocal(lastMatchingRecipeIndex);

            var recipeListIndex = generator.DeclareLocal(typeof(List<GridRecipe>)).LocalIndex;
            yield return new CodeInstruction(OpCodes.Newobj, typeof(List<GridRecipe>).GetConstructor(Type.EmptyTypes));
            yield return CodeInstruction.StoreLocal(recipeListIndex);

            var codes = instructions.ToArray();
            for (int i = 0; i < codes.Length - 3; i++)
            {
                if (codes[i + 2].Calls(foundMatchMethod))
                {
                    yield return CodeInstruction.LoadLocal(recipeListIndex);
                    yield return codes[i + 1]; // recipe
                    yield return CodeInstruction.Call(typeof(List<GridRecipe>), nameof(List<GridRecipe>.Add));

                    i += 3; // Skip FoundMatch method and return
                    continue;
                }

                yield return codes[i];
            }

            var endLabel = generator.DefineLabel();

            yield return CodeInstruction.LoadLocal(recipeListIndex);
            yield return CodeInstruction.Call(typeof(List<GridRecipe>), "get_Count");
            yield return new CodeInstruction(OpCodes.Ldc_I4_0);
            yield return new CodeInstruction(OpCodes.Cgt);
            yield return new CodeInstruction(OpCodes.Brfalse_S, endLabel);

            yield return CodeInstruction.LoadArgument(0); // this
            yield return CodeInstruction.LoadLocal(lastMatchingRecipeIndex);
            yield return CodeInstruction.LoadLocal(recipeListIndex);
            yield return CodeInstruction.LoadArgument(0); // this
            yield return CodeInstruction.LoadField(typeof(InventoryCraftingGrid), nameof(InventoryCraftingGrid.Api));
            yield return CodeInstruction.LoadArgument(0); // this
            yield return CodeInstruction.Call(typeof(InventoryCraftingGrid), "get_Player");
            yield return CodeInstruction.Call(typeof(Patch_InventoryCraftingGrid_FindMatchingRecipe), nameof(Inject));
            yield return new CodeInstruction(OpCodes.Call, foundMatchMethod);

            codes[^3].labels.Add(endLabel);
            yield return codes[^3];
            yield return codes[^2];
            yield return codes[^1];
        }

        private static GridRecipe Inject(GridRecipe currentMatchingRecipe, List<GridRecipe> recipes, ICoreAPI api, IPlayer player)
        {
            if (recipes.Count == 1)
            {
                return recipes[0];
            }

            _recipeSelector ??= api.ModLoader.GetModSystem<RecipeSelector>();

            bool HasRecipe(GridRecipe recipe)
            {
                return recipes.Any(x => x.Output.ResolvedItemstack.Equals(api.World, recipe.Output.ResolvedItemstack));
            }

            if (_recipeSelector.ConsumeNextPressed(player) && currentMatchingRecipe != null && HasRecipe(currentMatchingRecipe))
            {
                for (int i = 0; i < recipes.Count; i++)
                {
                    if (recipes[i] == currentMatchingRecipe)
                    {
                        var nextRecipe = recipes[i + 1 < recipes.Count ? i + 1 : 0];
                        _recipeSelector.OnRecipeSelected(player, nextRecipe);
                        return nextRecipe;
                    }
                }
            }

            if (_recipeSelector.TryGetLastCraftedRecipe(player, out var lastCraftedRecipe) && HasRecipe(lastCraftedRecipe))
            {
                return lastCraftedRecipe;
            }

            if (currentMatchingRecipe != null && HasRecipe(currentMatchingRecipe))
            {
                return currentMatchingRecipe;
            }
            
            return recipes[0];
        }
    }
}

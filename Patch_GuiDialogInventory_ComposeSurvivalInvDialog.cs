using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.API.Config;

namespace RecipeSelector
{
    [HarmonyPatch(typeof(GuiDialogInventory))]
    [HarmonyPatch("ComposeSurvivalInvDialog")]
    public static class Patch_GuiDialogInventory_ComposeSurvivalInvDialog
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var composeMethod = AccessTools.Method(typeof(GuiComposer), nameof(GuiComposer.Compose));
            var injectMethod = AccessTools.Method(typeof(Patch_GuiDialogInventory_ComposeSurvivalInvDialog), nameof(Inject));

            var codes = instructions.ToArray();
            for (int i = 0; i < codes.Length - 2; i++)
            {
                if (codes[i + 1].Calls(composeMethod))
                {
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadField(typeof(GuiDialogInventory), "craftingInv");
                    yield return new CodeInstruction(OpCodes.Call, injectMethod);
                }
                yield return codes[i];
            }
            yield return codes[^2];
            yield return codes[^1];
        }

        private static GuiComposer Inject(GuiComposer composer, IInventory inv)
        {
            bool OnClick()
            {
                (inv as InventoryCraftingGrid)?.Api.ModLoader.GetModSystem<RecipeSelector>().Next();
                return true;
            }

            composer.AddButton(Lang.Get("Next"), OnClick, ElementBounds.Fixed(412, 280, 60, 30), CairoFont.WhiteSmallText(), EnumButtonStyle.Small, "buttonNext");
            composer.GetButton("buttonNext").SetOrientation(EnumTextOrientation.Center);
            return composer;
        }
    }
}

using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RecipeSelector
{
    public class VanillaPatches : ModSystem
    {
        private string PatchCode { get; } = typeof(VanillaPatches).AssemblyQualifiedName!;
        private Harmony _harmonyInstance = null!;

        public override void StartPre(ICoreAPI api)
        {
            if (api is ICoreClientAPI capi)
            {
                // Prevent double patches in singleplayer
                if (!capi.IsSinglePlayer)
                {
                    PatchAll();
                }
            }
            else
            {
                PatchAll();
            }
        }

        private void PatchAll()
        {
            _harmonyInstance = new Harmony(PatchCode);
            _harmonyInstance.PatchAll();
            var patchedMethods = _harmonyInstance.GetPatchedMethods();
            Mod.Logger.Notification($"Harmony patched: {string.Join(",", patchedMethods)}");
        }

        public override void Dispose()
        {
            _harmonyInstance?.UnpatchAll(PatchCode);
        }
    }
}

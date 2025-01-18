using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using ProtoBuf;

namespace RecipeSelector
{
    public class RecipeSelector : ModSystem
    {
        [ProtoContract]
        private class NextRecipe { }

        private IClientNetworkChannel? _channel;

        public override void StartClientSide(ICoreClientAPI api)
        {
            _channel = api.Network
                .RegisterChannel("recipeselector")
                .RegisterMessageType<NextRecipe>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Network
                .RegisterChannel("recipeselector")
                .RegisterMessageType<NextRecipe>()
                .SetMessageHandler<NextRecipe>(SelectNextRecipe);
        }

        public void Next()
        {
            _channel?.SendPacket(new NextRecipe());
        }

        private void SelectNextRecipe(IServerPlayer fromPlayer, NextRecipe packet)
        {
            var invId = fromPlayer.InventoryManager.GetInventoryName(GlobalConstants.craftingInvClassName);
            var inv = fromPlayer.InventoryManager.GetInventory(invId) as InventoryCraftingGrid;
            AccessTools.Method(typeof(InventoryCraftingGrid), "FindMatchingRecipe").Invoke(inv, []);
        }
    }
}

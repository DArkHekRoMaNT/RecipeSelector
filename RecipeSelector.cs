using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using ProtoBuf;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RecipeSelector
{
    public class RecipeSelector : ModSystem
    {
        [ProtoContract] private record NextRecipePacket();

        private IClientNetworkChannel? _clientChannel;

        private readonly HashSet<string> _nextPressedPlayers = [];
        private readonly Dictionary<string, GridRecipe> _lastCraftedRecipeByPlayer = [];

        public override void StartClientSide(ICoreClientAPI api)
        {
            _clientChannel = api.Network
                .RegisterChannel("recipeselector")
                .RegisterMessageType<NextRecipePacket>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Network
                .RegisterChannel("recipeselector")
                .RegisterMessageType<NextRecipePacket>()
                .SetMessageHandler<NextRecipePacket>(OnNextRecipePacketReceived);
        }

        public void OnNextClicked(IClientPlayer player)
        {
            _nextPressedPlayers.Add(player.PlayerUID);
            _clientChannel?.SendPacket(new NextRecipePacket());
        }

        private void OnNextRecipePacketReceived(IServerPlayer fromPlayer, NextRecipePacket packet)
        {
            _nextPressedPlayers.Add(fromPlayer.PlayerUID);
            UpdateRecipe(fromPlayer);
        }

        public bool ConsumeNextPressed(IPlayer player)
        {
            if (_nextPressedPlayers.Contains(player.PlayerUID))
            {
                _nextPressedPlayers.Remove(player.PlayerUID);
                return true;
            }

            return false;
        }

        public void OnRecipeSelected(IPlayer player, GridRecipe recipe)
        {
            if (_lastCraftedRecipeByPlayer.ContainsKey(player.PlayerUID))
            {
                _lastCraftedRecipeByPlayer[player.PlayerUID] = recipe;
            }
            else
            {
                _lastCraftedRecipeByPlayer.Add(player.PlayerUID, recipe);
            }

        }

        public bool TryGetLastCraftedRecipe(IPlayer player, [NotNullWhen(true)] out GridRecipe? lastCraftedRecipe)
        {
            return _lastCraftedRecipeByPlayer.TryGetValue(player.PlayerUID, out lastCraftedRecipe);
        }

        private static void UpdateRecipe(IPlayer player)
        {
            var invId = player.InventoryManager.GetInventoryName(GlobalConstants.craftingInvClassName);
            var inv = player.InventoryManager.GetInventory(invId) as InventoryCraftingGrid;
            AccessTools.Method(typeof(InventoryCraftingGrid), "FindMatchingRecipe").Invoke(inv, []);
        }
    }
}

using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace vsstickydirtmod
{
    public class StickyDirtModSystem : ModSystem
    {
        private static ICoreAPI? _api;
        public static ICoreAPI api => _api ?? throw new ArgumentNullException(nameof(api));
        [ThreadStatic]
        public static IServerPlayer? lastPlayer;
        [ThreadStatic]
        public static BlockPos? lastPos;
        [ThreadStatic]
        public static ItemStack? lastTool;
        public Harmony? harmony;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            _api = api;
            //api.RegisterBlockEntityClass("DelayedFall", typeof(BlockEntityDelayedFall));
            //api.RegisterBlockEntityBehaviorClass("DelayedFall", typeof(BEBehaviorDelayedFall));

            if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll(); // Applies all harmony patches
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("vsstickydirtmod:hello"));
            api.Event.BreakBlock += Event_BreakBlock;
            api.Event.DidBreakBlock += Event_DidBreakBlock;
        }

        private void Event_DidBreakBlock(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            api.Logger.Notification($"Player {byPlayer} did break block at {blockSel.Position}");
        }

        private void Event_BreakBlock(IServerPlayer byPlayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
        {
            api.Logger.Notification($"Player {byPlayer} will break block at {blockSel.Position} using {byPlayer.Entity.ActiveHandItemSlot.Itemstack}");
            lastPlayer = byPlayer;
            lastPos = blockSel.Position;
            lastTool = byPlayer.Entity.ActiveHandItemSlot.Itemstack;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("vsstickydirtmod:hello"));
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            harmony = null;
        }
    }
}
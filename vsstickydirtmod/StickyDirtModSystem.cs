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
        public Harmony? harmony;

        public override void Start(ICoreAPI api)
        {
            _api = api;

            if (!Harmony.HasAnyPatches(Mod.Info.ModID)) {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAll(); // Applies all harmony patches
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(Mod.Info.ModID);
            harmony = null;
        }
    }
}
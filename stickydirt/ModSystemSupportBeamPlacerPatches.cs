using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace stickydirt
{
    [HarmonyPatch(typeof(ModSystemSupportBeamPlacer))]
    public static class ModSystemSupportBeamPlacerPatches
    {
        public static ICoreAPI api => StickyDirtModSystem.api;

        // Fix bug in VS 1.19.7 that causes support beams to be unchangeable if the world was loaded with them in place
        [HarmonyPostfix, HarmonyPatch(nameof(ModSystemSupportBeamPlacer.GetSbData), new Type[] { typeof(int), typeof(int), typeof(int) })]
        public static void After__GetSbData(int chunkx, int chunky, int chunkz, SupportBeamsData __result)
        {
            var chunk = api.World.BlockAccessor.GetChunk(chunkx, chunky, chunkz);
            if (chunk == null) return;

            chunk.LiveModData.TryAdd("supportbeams", __result);
        }
    }
}
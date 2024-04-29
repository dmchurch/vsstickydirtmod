using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;
using Vintagestory.Server;

namespace vsstickydirtmod
{
    //[HarmonyPatch(typeof(ServerMain))]
    //public static class ServerMainPatches
    //{
    //    public static ICoreAPI api => StickyDirtModSystem.api;

    //    // This is, sadly, the only place we can effectively intercept falling blocks before they spawn
    //    public static bool Before__SpawnEntity(ServerMain __instance, Entity entity)
    //    {
    //        if (entity is not EntityBlockFalling ebf) return true;
    //        if (BEBehaviorDelayedFall.InterceptFallingBlock(__instance, ebf)) return false;
    //        return true;
    //    }
    //}
}
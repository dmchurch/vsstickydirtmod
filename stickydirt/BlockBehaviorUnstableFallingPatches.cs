using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace stickydirt
{

    [HarmonyPatch(typeof(BlockBehaviorUnstableFalling))]
    public static class BlockBehaviorUnstableFallingPatches
    {
        public static ICoreAPI api => StickyDirtModSystem.api;
        public static float MaxDropDelayHours = 0.1f;
        public static int SupportToPreventDrop = 7;
        // in the order NESWUD, just like BlockFacing.ALL_FACES
        public static int[] SupportWeights = { 2, 2, 2, 2, 2, 2 }; // 3 walls + a block (or vegetation) on top is enough
        public static int[] SupportBeamWeights = { 3, 3, 3, 3, 0, 6 }; // 1 support beam + 2 attached walls is enough, 2 support beams alone is not
        public static int RootSupport = 3; // Support when the above block is a BlockPlant

        [HarmonyPrefix, HarmonyPatch("TryFalling")]
        public static bool Before__TryFalling(BlockBehaviorUnstableFalling __instance, float ___fallSidewaysChance, ref bool __result, IWorldAccessor world, BlockPos pos, ref EnumHandling handling, ref string failureCode)
        {
            if (world.Side != EnumAppSide.Server) return true;

            if (!__instance.fallSideways || ___fallSidewaysChance > 0.5 || __instance.block.BlockMaterial != EnumBlockMaterial.Soil) {
                //api.Logger.Notification($"Block {__instance.block} is not an unstable dirt block");
                return true;
            }
            //api.Logger.Debug($"TryFalling at {pos} ({__instance.block})");
            if (__instance.IsReplacableBeneath(world, pos) || __instance.IsReplacableBeneathAndSideways(world, pos)) {
                // This is at risk of falling if we pass the call on, check its support
                int support = __instance.SupportCount(world, pos);
                //api.Logger.Debug($"  Block at {pos} has risk of falling, support count {support}");

                if (support >= SupportToPreventDrop) {
                    //api.Logger.Debug($"    Block at {pos} immune to drop");
                    handling = EnumHandling.PassThrough;
                    __result = false;
                    return false;
                }
            }
            return true;
        }

        // How much support does this block have?
        public static int SupportCount(this BlockBehaviorUnstableFalling self, IWorldAccessor worldAccessor, BlockPos pos)
        {
            IBlockAccessor blockAccessor = worldAccessor.BlockAccessor;
            int support = 0;
            BlockPos tmpPos = pos.Copy();
            Vec3d faceVec = pos.ToVec3d();
            Vec3d beamVec = pos.ToVec3d();

            ModSystemSupportBeamPlacer sbp = api.ModLoader.GetModSystem<ModSystemSupportBeamPlacer>();
            SupportBeamsData? sbd = sbp?.GetSbData(pos);

            foreach ((BlockFacing face, int i) in BlockFacing.ALLFACES.WithIndex())
            {
                tmpPos.Set(pos).Add(face);
                Block block = blockAccessor.GetBlock(tmpPos);

                // If this is a plant and it's on top, provide support based on roots
                if (face == BlockFacing.UP && block is BlockPlant) {
                    support += RootSupport;
                    continue;
                }

                // If this is a non-SupportBeam, solid, attachable block on this side, provide standard support values
                if (!(block is BlockSupportBeam)
                    && block.Replaceable <= 6000
                    && block.SideIsSolid(blockAccessor, tmpPos, face.Opposite.Index)
                    && block.CanAttachBlockAt(blockAccessor, self.block, tmpPos, face.Opposite)) {
                    support += SupportWeights[i];
                    continue;
                }

                // center of adjoining face in that direction
                faceVec.Set(face.Normald).Scale(0.5).Add(0.5, 0.5, 0.5).Add(pos);

                foreach ((Vec3d start, Vec3d end) in sbd?.Beams ?? Enumerable.Empty<StartEnd>()) {
                    bool startMatches = start.WithinFace(face, faceVec);
                    bool endMatches = end.WithinFace(face, faceVec);
                    if (startMatches != endMatches) { // must have exactly one end embedded in this face
                        Vec3d nearSide = startMatches ? start : end;
                        Vec3d farSide = startMatches ? end : start;
                        double supportRatio = beamVec.Set(nearSide).Sub(farSide).FaceSupport(face);
                        //api.Logger.Debug($"Beam from {start} to {end} within {face} face ({face.Normald}) at {faceVec} generates {supportRatio} support");
                        if (supportRatio >= 0.5) {
                            support += SupportBeamWeights[i];
                            break;
                        }
                    }
                }

            }
            return support;
        }

        public static double FaceSupport(this Vec3d supportVec, BlockFacing face)
        {
            double supportLengthSq = supportVec.LengthSq();
            double normalComponent = supportVec.Dot(face.Opposite.Normald);
            if (normalComponent < 0) return 0; // pointing away, no support
            // For a beam that points directly at the face, normalComponent == supportLength.
            // A beam that is 45° off has normalComponent == supportLength / Sqrt(2), so normalComponent^2 / supportLengthSq == 0.5
            return normalComponent * normalComponent / supportLengthSq;
        }

        public static bool WithinFace(this Vec3d point, BlockFacing face, Vec3d faceVec)
        {
            double normalDistance = Math.Abs(
                face.Axis switch {
                    EnumAxis.X => faceVec.X - point.X,
                    EnumAxis.Y => faceVec.Y - point.Y,
                    EnumAxis.Z => faceVec.Z - point.Z,
                    _ => throw new ArgumentOutOfRangeException(nameof(face)),
                });
            if (normalDistance > 0.25) return false;

            double planarDistance = Math.Max(
                Math.Abs(face.Axis switch
                {
                    EnumAxis.X => faceVec.Z - point.Z,
                    EnumAxis.Y => faceVec.X - point.X,
                    EnumAxis.Z => faceVec.Y - point.Y,
                    _ => throw new ArgumentOutOfRangeException(nameof(face)),
                }),
                Math.Abs(face.Axis switch
                {
                    EnumAxis.X => faceVec.Y - point.Y,
                    EnumAxis.Y => faceVec.Z - point.Z,
                    EnumAxis.Z => faceVec.X - point.X,
                    _ => throw new ArgumentOutOfRangeException(nameof(face)),
                }));
            if (planarDistance > 0.6) return false;

            return true;
        }

        public static void Deconstruct(this StartEnd self, out Vec3d start, out Vec3d end) => (start, end) = (self.Start, self.End);

        public static double ManhattanDistanceTo(this Vec3d start, Vec3d end) => Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) + Math.Abs(end.Z - start.Z);

        public static bool IsReplacableBeneath(this BlockBehaviorUnstableFalling _self, IWorldAccessor world, BlockPos pos)
        {
            Block bottomBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
            return bottomBlock.Replaceable > 6000;
        }
        public static bool IsReplacableBeneathAndSideways(this BlockBehaviorUnstableFalling _self, IWorldAccessor world, BlockPos pos)
        {
            BlockPos nPos = pos.Copy();
            foreach (BlockFacing face in BlockFacing.HORIZONTALS) {
                nPos.Set(pos).Add(face);
                Block nBlock = world.BlockAccessor.GetBlock(nPos);
                if (nBlock.Replaceable >= 6000) {
                    nPos.Down();
                    nBlock = world.BlockAccessor.GetBlock(nPos);
                    if (nBlock.Replaceable >= 6000) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static IEnumerable<(T value, int index)> WithIndex<T>(this IEnumerable<T> self) => self.Select((value, i) => (value, i));
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace vsstickydirtmod
{
    public class BlockEntityDelayedFall : BlockEntity
    {
    }

    public class BEBehaviorDelayedFall : BlockEntityBehavior
    {
        //public bool hasInterceptedBlock;

        //private bool canFallSideways;
        //private float dustIntensity;
        //private AssetLocation? fallSound;
        //private float impactDamageMul;

        public double dropDeadlineTotalHours;
        private long listener;
        private bool removed;

        public double HoursToDeadline => dropDeadlineTotalHours - Api.World.Calendar.TotalHours;

        //private static AccessTools.FieldRef<EntityBlockFalling, AssetLocation> fallSound__ = AccessTools.FieldRefAccess<EntityBlockFalling, AssetLocation>("fallSound");
        //private static AccessTools.FieldRef<EntityBlockFalling, float> impactDamageMul__ = AccessTools.FieldRefAccess<EntityBlockFalling, float>("impactDamageMul");

        public BEBehaviorDelayedFall(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (dropDeadlineTotalHours > 0) {
                RegisterDelayedCallback();
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            //hasInterceptedBlock = tree.GetBool("hasInterceptedBlock");
            dropDeadlineTotalHours = tree.GetDouble("dropDeadlineTotalHours");
            //canFallSideways = tree.GetBool("canFallSideways");
            //dustIntensity = tree.GetFloat("dustIntensity");
            //impactDamageMul = tree.GetFloat("impactDamageMul");
            //string sound = tree.GetString("fallSound");
            //fallSound = sound != null ? AssetLocation.Create(sound) : null;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            //tree.SetBool("hasInterceptedBlock", hasInterceptedBlock);
            tree.SetDouble("dropDeadlineTotalHours", dropDeadlineTotalHours);
            //tree.SetBool("canFallSideways", canFallSideways);
            //tree.SetFloat("dustIntensity", dustIntensity);
            //tree.SetFloat("impactDamageMul", impactDamageMul);
            //if (fallSound != null) {
            //    tree.SetString("fallSound", fallSound.ToShortString());
            //} else {
            //    tree.RemoveAttribute("fallSound");
            //}
        }

        public void DelayFall(double delayHours)
        {
            double targetDeadline = Api.World.Calendar.TotalHours + delayHours;
            if (dropDeadlineTotalHours < targetDeadline) {
                dropDeadlineTotalHours = targetDeadline;
                RegisterDelayedCallback(CheckFallDelay, delayHours);
            }
        }

        public void Stabilize()
        {
            UnregisterDelayedCallback();
            Remove();
        }

        public void RegisterDelayedCallback()
        {
            RegisterDelayedCallback(CheckFallDelay, HoursToDeadline);
        }

        private void RegisterDelayedCallback(Action<float> OnDelayedCallbackTick, double delayHours)
        {
            if (listener != 0) {
                Blockentity.UnregisterDelayedCallback(listener);
            }
            listener = Blockentity.RegisterDelayedCallback(OnDelayedCallbackTick, DelayMillisecondsFor(delayHours));
        }

        public int DelayMillisecondsFor(double delayHours) => Math.Max(0, (int)Math.Ceiling(delayHours * 3600_000 / Api.World.Calendar.SpeedOfTime / Api.World.Calendar.CalendarSpeedMul));

        private void UnregisterDelayedCallback()
        {
            if (listener != 0) {
                Blockentity.UnregisterDelayedCallback(listener);
                listener = 0;
            }
        }

        public void Remove()
        {
            if (removed) return;
            // Remove self from be
            BlockEntity be = Blockentity;
            be.Behaviors.Remove(this);

            // If be is a BEDF and is now empty, remove it entirely
            if (be is BlockEntityDelayedFall && be.Behaviors.Count == 0) {
                if (Api.World.BlockAccessor.GetBlockEntity(be.Pos) != be) {
                    Api.Logger.Error($"About to remove BlockEntityDelayedFall {be}, but it is not the current BE!");
                }
                else {
                    Api.World.BlockAccessor.RemoveBlockEntity(be.Pos);
                }
            }
            removed = true;
        }

        private void CheckFallDelay(float t)
        {
            double timeToDeadline = HoursToDeadline;
            if (timeToDeadline > 0) {
                RegisterDelayedCallback(CheckFallDelay, (float)timeToDeadline);
                return;
            }

            UnregisterDelayedCallback();

            // Try to fall
            EnumHandling handling = default;
            Block?.GetBehavior<BlockBehaviorUnstableFalling>()?.OnNeighbourBlockChange(Api.World, Pos, Pos, ref handling); // this will trigger the fall if appropriate, possibly calling Remove()

            // If we didn't get removed above, do so now
            Remove();
        }

        //public bool InterceptBlock(IWorldAccessor world, EntityBlockFalling ebf)
        //{
        //    if (HoursToDeadline > 0) {
        //        //canFallSideways = ebf.WatchedAttributes.GetBool("canFallSideways");
        //        //dustIntensity = ebf.WatchedAttributes.GetFloat("dustIntensity");
        //        //fallSound = fallSound__(ebf);
        //        //impactDamageMul = impactDamageMul__(ebf);
        //        hasInterceptedBlock = true;
        //        RegisterDelayedCallback();
        //        return true;
        //    }
        //    return false;
        //}

        //public static bool InterceptFallingBlock(IWorldAccessor world, EntityBlockFalling ebf)
        //{
        //    BlockPos pos = ebf.initialPos;
        //    BEBehaviorDelayedFall? bebdh = world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDelayedFall>();
        //    return bebdh?.InterceptBlock(world, ebf) ?? false;
        //}
    }
}
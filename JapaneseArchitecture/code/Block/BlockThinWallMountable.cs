using JapaneseArchitecture.code.BlockEntities;
using JapaneseArchitecture.code.ThinWall;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace JapaneseArchitecture.code.Blocks {
    public class BlockThinWallMountable : Vintagestory.API.Common.Block, IMultiBlockColSelBoxes {
        public Cuboidf[] MBGetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
            Block masterBlock = blockAccessor.GetBlock(pos.AddCopy(offset));
            return masterBlock?.CollisionBoxes;
        }

        public Cuboidf[] MBGetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos, Vec3i offset) {
            Block masterBlock = blockAccessor.GetBlock(pos.AddCopy(offset));
            return masterBlock?.SelectionBoxes;
        }

        public override void OnLoaded(ICoreAPI api) {
            base.OnLoaded(api);
            ThinWallFaceData.ApplySolidFaces(this);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) {
            BEThinWallMountable be = ResolveBlockEntity(world.BlockAccessor, blockSel.Position, out _, out int verticalOffset);
            if (be == null) {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            ItemSlot activeSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            bool hasSupportedHeldLight = activeSlot?.Empty == false && be.CanAccept(activeSlot.Itemstack);
            bool sneaking = byPlayer?.Entity?.Controls?.Sneak == true;
            int slotIndex = ThinWallFaceData.GetSlotIndex(this, blockSel.Face, verticalOffset, blockSel.HitPosition);
            if (slotIndex < 0) {
                return hasSupportedHeldLight;
            }

            if (be.HasMountedLight(slotIndex)) {
                if (sneaking || !hasSupportedHeldLight) {
                    return be.TryTakeMountedLight(slotIndex, byPlayer);
                }

                return true;
            }

            if (!hasSupportedHeldLight) {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            return be.TryMount(slotIndex, activeSlot, byPlayer, blockSel.Face);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
            BEThinWallMountable be = ResolveBlockEntity(world.BlockAccessor, pos, out _, out _);
            be?.DropMountedLights();
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null) {
            if (stack != null || blockAccessor == null || pos == null) {
                return base.GetLightHsv(blockAccessor, pos, stack);
            }

            BEThinWallMountable be = blockAccessor.GetBlockEntity(pos) as BEThinWallMountable;
            byte[] lightHsv = be?.MountedLightHsv;
            if (lightHsv != null) {
                return lightHsv;
            }

            return base.GetLightHsv(blockAccessor, pos, stack);
        }

        static BEThinWallMountable ResolveBlockEntity(IBlockAccessor blockAccessor, BlockPos pos, out BlockPos rootPos, out int verticalOffset) {
            rootPos = pos.Copy();
            verticalOffset = 0;

            BEThinWallMountable be = blockAccessor.GetBlockEntity(pos) as BEThinWallMountable;
            if (be != null) {
                return be;
            }

            if (blockAccessor.GetBlock(pos) is BlockMultiblock multiblock) {
                rootPos = pos.AddCopy(multiblock.OffsetInv);
                verticalOffset = pos.Y - rootPos.Y;
                return blockAccessor.GetBlockEntity(rootPos) as BEThinWallMountable;
            }

            return null;
        }
    }
}

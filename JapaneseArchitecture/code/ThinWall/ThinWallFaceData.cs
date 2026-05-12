using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace JapaneseArchitecture.code.ThinWall {
    public static class ThinWallFaceData {
        public const float WallThickness = 2f / 16f;

        public static bool TryResolveThinWall(IBlockAccessor blockAccessor, BlockPos pos, out Block thinWallBlock, out BlockPos rootPos, out int verticalOffset) {
            thinWallBlock = null;
            rootPos = pos?.Copy();
            verticalOffset = 0;

            if (blockAccessor == null || pos == null) {
                return false;
            }

            Block block = blockAccessor.GetBlock(pos);
            if (IsThinWallBlock(block)) {
                thinWallBlock = block;
                return true;
            }

            if (block is BlockMultiblock multiblock) {
                rootPos = pos.AddCopy(multiblock.OffsetInv);
                verticalOffset = pos.Y - rootPos.Y;
                Block rootBlock = blockAccessor.GetBlock(rootPos);
                if (IsThinWallBlock(rootBlock)) {
                    thinWallBlock = rootBlock;
                    return true;
                }
            }

            return false;
        }

        public static BlockFacing GetWallFacing(Block block) {
            if (block?.Variant == null || !block.Variant.TryGetValue("side", out string sideCode)) {
                return null;
            }

            return BlockFacing.FromCode(sideCode);
        }

        public static void ApplySolidFaces(Block block) {
            BlockFacing wallFacing = GetWallFacing(block);
            if (block?.SideSolid == null || wallFacing == null) {
                return;
            }

            block.SideSolid[wallFacing.Index] = true;
            block.SideSolid[wallFacing.Opposite.Index] = true;
        }

        public static int GetSlotIndex(Block block, BlockFacing clickedFace, int verticalOffset, Vec3d hitPosition) {
            BlockFacing wallFacing = GetWallFacing(block);
            if (wallFacing == null) {
                return -1;
            }

            int sideIndex;
            if (clickedFace == wallFacing) {
                sideIndex = 0;
            } else if (clickedFace == wallFacing.Opposite) {
                sideIndex = 1;
            } else {
                sideIndex = GetSideIndexFromHitPosition(wallFacing, hitPosition);
            }

            double hitY = hitPosition?.Y ?? 0;
            int heightIndex = verticalOffset > 0 || hitY < 0.5 ? 1 : 0;
            return sideIndex * 2 + heightIndex;
        }

        public static Vec3f GetMountOffset(Block block, int slotIndex) {
            BlockFacing wallFacing = GetWallFacing(block);
            if (wallFacing == null) {
                return new Vec3f();
            }

            bool isInnerSide = slotIndex / 2 == 1;
            bool isUpper = (slotIndex % 2) == 1;

            float inset = isInnerSide ? -WallThickness : 1f;
            return new Vec3f(
                wallFacing.Normali.X * inset,
                isUpper ? 1f : 0f,
                wallFacing.Normali.Z * inset
            );
        }

        static int GetSideIndexFromHitPosition(BlockFacing wallFacing, Vec3d hitPosition) {
            if (hitPosition == null) {
                return 0;
            }

            if (wallFacing.Axis == EnumAxis.Z) {
                bool onFacingSide = wallFacing == BlockFacing.NORTH ? hitPosition.Z < 0.5 : hitPosition.Z > 0.5;
                return onFacingSide ? 0 : 1;
            }

            bool onXAxisFacingSide = wallFacing == BlockFacing.WEST ? hitPosition.X < 0.5 : hitPosition.X > 0.5;
            return onXAxisFacingSide ? 0 : 1;
        }

        static bool IsThinWallBlock(Block block) {
            return block?.Class == "ThinWallMountable" && block.EntityClass == "ThinWallMountable";
        }
    }
}

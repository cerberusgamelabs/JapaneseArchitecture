using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace JapaneseArchitecture.code.BlockBehavior {
    public class BlockBehaviorThinWallAttachable : StrongBlockBehavior, IMultiBlockBlockProperties {
        readonly HashSet<string> attachmentFaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ICoreAPI api;

        public BlockBehaviorThinWallAttachable(Block block) : base(block) {
        }

        public override void OnLoaded(ICoreAPI api) {
            this.api = api;
            base.OnLoaded(api);
        }

        public override void Initialize(JsonObject properties) {
            base.Initialize(properties);

            attachmentFaces.Clear();

            string[] configuredFaces = properties?["attachmentFaces"].AsArray<string>(new string[] { "front", "back" });
            if (configuredFaces == null || configuredFaces.Length == 0) {
                configuredFaces = new string[] { "front", "back" };
            }

            foreach (string face in configuredFaces) {
                if (!string.IsNullOrWhiteSpace(face)) {
                    attachmentFaces.Add(face);
                }
            }
        }

        public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null) {
            return ResolveCanAttach(blockFace, ref handling)
                ? true
                : base.CanAttachBlockAt(world, block, pos, blockFace, ref handling, attachmentArea);
        }

        public bool MBCanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea, Vec3i offsetInv) {
            EnumHandling handling = EnumHandling.PassThrough;
            return ResolveCanAttach(blockFace, ref handling);
        }

        public JsonObject MBGetAttributes(IBlockAccessor blockAccessor, BlockPos pos) {
            return null;
        }

        public float MBGetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, Vec3i offset) {
            return 0f;
        }

        public int MBGetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, Vec3i offset) {
            if (api == null) return 0;
            BlockPos masterPos = pos.AddCopy(offset);
            Block masterBlock = api.World.BlockAccessor.GetBlock(masterPos);
            if (masterBlock == null) return 0;
            return masterBlock.GetRetention(masterPos, facing, type);
        }

        BlockFacing GetWallFacing() {
            if (this.block?.Variant == null || !this.block.Variant.TryGetValue("side", out string sideCode)) {
                return null;
            }

            return BlockFacing.FromCode(sideCode);
        }

        bool ResolveCanAttach(BlockFacing blockFace, ref EnumHandling handling) {
            BlockFacing wallFacing = GetWallFacing();
            if (wallFacing == null) {
                return false;
            }

            if (attachmentFaces.Contains("front") && blockFace == wallFacing) {
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            if (attachmentFaces.Contains("back") && blockFace == wallFacing.Opposite) {
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            return false;
        }
    }
}

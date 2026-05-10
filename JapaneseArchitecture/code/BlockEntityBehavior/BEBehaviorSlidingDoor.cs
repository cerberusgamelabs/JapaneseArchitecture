using JapaneseArchitecture.code.BlockBehavior;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace JapaneseArchitecture.code.BlockEntityBehavior {

    public class BEBehaviorSlidingDoor : BEBehaviorAnimatable, IInteractable, IRotatable {
        public float RotateYRad;
        protected bool opened;
        protected bool invertHandles;
        protected MeshData mesh;
        protected Cuboidf[] boxesClosed, boxesOpened;

        public BlockFacing facingWhenClosed { get { return BlockFacing.HorizontalFromYaw(RotateYRad); } }
        public BlockFacing facingWhenOpened { get { return invertHandles ? facingWhenClosed.GetCCW() : facingWhenClosed.GetCW(); } }

        /// <summary>
        /// A rather counter-intuitive property, setting this actually sets up an internal Vec3i giving the offset to the Pos of the supplied door
        /// </summary>
        BEBehaviorSlidingDoor leftDoor {
            get { return leftDoorOffset == null ? null : BlockBehaviorSlidingDoor.getDoorAt(Api.World, Pos.AddCopy(leftDoorOffset)); }
            set { leftDoorOffset = value == null ? null : value.Pos.SubCopy(Pos).ToVec3i(); }
        }
        /// <summary>
        /// A rather counter-intuitive property, setting this actually sets up an internal Vec3i giving the offset to the Pos of the supplied door
        /// </summary>
        BEBehaviorSlidingDoor rightDoor {
            get { return rightDoorOffset == null ? null : BlockBehaviorSlidingDoor.getDoorAt(Api.World, Pos.AddCopy(rightDoorOffset)); }
            set { rightDoorOffset = value == null ? null : value.Pos.SubCopy(Pos).ToVec3i(); }
        }

        protected Vec3i leftDoorOffset;
        protected Vec3i rightDoorOffset;

        protected BlockBehaviorSlidingDoor doorBh;

        public Cuboidf[] ColSelBoxes => opened ? boxesOpened : boxesClosed;
        public bool Opened => opened;
        public bool InvertHandles => invertHandles;

        public Vec3i? OpenInteractionProxyOffset => GetOpenInteractionProxyOffset();

        public BEBehaviorSlidingDoor(BlockEntity blockentity) : base(blockentity) {
            boxesClosed = blockentity.Block.CollisionBoxes;

            doorBh = blockentity.Block.GetBehavior<BlockBehaviorSlidingDoor>();
        }

        public override void Initialize(ICoreAPI api, JsonObject properties) {
            base.Initialize(api, properties);

            SetupRotationsAndColSelBoxes(false);
            UpdateOpenInteractionProxyBlocks();

            if (opened && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened")) {
                ToggleDoorWing(true);
            }
        }


        public BlockPos getAdjacentPosition(int right, int back = 0, int up = 0) {
            return Blockentity.Pos.AddCopy(getAdjacentOffset(right, back, up, RotateYRad, invertHandles));
        }

        public Vec3i getAdjacentOffset(int right, int back = 0, int up = 0) {
            return getAdjacentOffset(right, back, up, RotateYRad, invertHandles);
        }

        public static Vec3i getAdjacentOffset(int right, int back, int up, float rotateYRad, bool invertHandles) {
            if (invertHandles) right = -right;
            return new Vec3i(
                right * (int)Math.Round(Math.Sin(rotateYRad + GameMath.PIHALF)) - back * (int)Math.Round(Math.Sin(rotateYRad)),
                up,
                right * (int)Math.Round(Math.Cos(rotateYRad + GameMath.PIHALF)) - back * (int)Math.Round(Math.Cos(rotateYRad))
            );
        }

        internal void SetupRotationsAndColSelBoxes(bool initalSetup) {
            int width = doorBh.width;
            if (initalSetup) {
                BlockPos leftPos = Blockentity.Pos.AddCopy(width * (int)Math.Round(Math.Sin(RotateYRad - GameMath.PIHALF)), 0, width * (int)Math.Round(Math.Cos(RotateYRad - GameMath.PIHALF)));
                leftDoor = BlockBehaviorSlidingDoor.getDoorAt(Api.World, leftPos);
            }

            if (leftDoor != null && !leftDoor.invertHandles && invertHandles) {
                leftDoor.rightDoor = this;
            }

            if (initalSetup) {
                if (leftDoor != null && !leftDoor.invertHandles && leftDoor.facingWhenClosed == facingWhenClosed) {
                    invertHandles = true;
                    leftDoor.rightDoor = this;
                    Blockentity.MarkDirty(true);
                }

                BlockPos rightPos = Blockentity.Pos.AddCopy(width * (int)Math.Round(Math.Sin(RotateYRad + GameMath.PIHALF)), 0, width * (int)Math.Round(Math.Cos(RotateYRad + GameMath.PIHALF)));
                var rightDoor = BlockBehaviorSlidingDoor.getDoorAt(Api.World, rightPos);

                if (leftDoor == null && rightDoor != null && !rightDoor.invertHandles && rightDoor.rightDoor?.invertHandles != true && rightDoor.facingWhenClosed == facingWhenClosed) {
                    if (Api.Side == EnumAppSide.Server) {
                        if (rightDoor.doorBh.width > 1) {
                            Api.World.BlockAccessor.SetBlock(0, rightDoor.Blockentity.Pos);
                            BlockPos rightDoorPos = Blockentity.Pos.AddCopy((rightDoor.doorBh.width + width - 1) * (int)Math.Round(Math.Sin(RotateYRad + GameMath.PIHALF)), 0, (rightDoor.doorBh.width + width - 1) * (int)Math.Round(Math.Cos(RotateYRad + GameMath.PIHALF)));
                            Api.World.BlockAccessor.SetBlock(rightDoor.Block.Id, rightDoorPos);
                            rightDoor = Block.GetBEBehavior<BEBehaviorSlidingDoor>(rightDoorPos);
                            rightDoor.RotateYRad = RotateYRad;
                            rightDoor.invertHandles = true;
                            rightDoor.doorBh.placeMultiblockParts(Api.World, rightDoorPos);
                            this.rightDoor = rightDoor;
                            rightDoor.SetupRotationsAndColSelBoxes(true);
                            rightDoor.leftDoor = this;
                            rightDoor.Blockentity.MarkDirty(true);
                        } else {
                            rightDoor.invertHandles = true;
                            this.rightDoor = rightDoor;
                            rightDoor.leftDoor = this;
                            rightDoor.Blockentity.MarkDirty(true);
                        }
                    }
                }
            }

            if (Api.Side == EnumAppSide.Client) {
                if (doorBh.animatableOrigMesh == null) {
                    string animkey = Block.Shape.ToString();
                    doorBh.animatableOrigMesh = animUtil.CreateMesh(animkey, null, out Shape shape, null);
                    doorBh.animatableShape = shape;
                    doorBh.animatableDictKey = animkey;
                }
                if (doorBh.animatableOrigMesh != null) {
                    animUtil.InitializeAnimator(doorBh.animatableDictKey, doorBh.animatableOrigMesh, doorBh.animatableShape, null);
                    UpdateMeshAndAnimations();
                }
            }

            UpdateHitBoxes();
        }

        protected virtual void UpdateMeshAndAnimations() {
            if (doorBh.animatableOrigMesh == null || animUtil == null) {
                return;
            }

            mesh = doorBh.animatableOrigMesh.Clone();
            if (RotateYRad != 0) {
                float rot = invertHandles ? -RotateYRad : RotateYRad;
                mesh = mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, rot, 0);
                animUtil.renderer.rotationDeg.Y = rot * GameMath.RAD2DEG;
            }

            if (invertHandles) {
                // We need a full matrix transform for this to update the normals as well
                Matrixf matf = new Matrixf();
                matf.Translate(0.5f, 0.5f, 0.5f).Scale(-1, 1, 1).Translate(-0.5f, -0.5f, -0.5f);
                mesh.MatrixTransform(matf.Values);

                animUtil.renderer.backfaceCulling = false;
                animUtil.renderer.ScaleX = -1;
            }
        }

        protected virtual void UpdateHitBoxes() {
            var boxesopened = new Cuboidf[boxesClosed.Length];
            boxesClosed = Blockentity.Block.CollisionBoxes;
            //float xOffset = Blockentity.Block.Attributes?["xOffset"].AsFloat(0.8125f) ?? 0.8125f;
            //float zOffset = Blockentity.Block.Attributes?["zOffset"].AsFloat(-0.109375f) ?? -0.109375f;
            float xOffset = 0.8125f;
            float zOffset = -0.109375f;
            //float dirX = 0f;
            //float dirZ = 0f;
            if (RotateYRad != 0) {
                var boxesC = new Cuboidf[boxesClosed.Length];
                for (int i = 0; i < boxesClosed.Length; i++) {
                    boxesopened[i] = boxesClosed[i].OffsetCopy(invertHandles ? xOffset : -xOffset, 0, zOffset).RotatedCopy(0, RotateYRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));
                    boxesC[i] = boxesClosed[i].RotatedCopy(0, RotateYRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));
                }

                boxesClosed = boxesC;
            } else {
                for (int i = 0; i < boxesClosed.Length; i++) {
                    boxesopened[i] = boxesClosed[i].OffsetCopy(invertHandles ? xOffset : -xOffset, 0, zOffset);
                }
            }

            boxesOpened = boxesopened;
        }

        private Vec3i? GetOpenInteractionProxyOffset() {
            if (boxesClosed == null || boxesOpened == null || boxesClosed.Length == 0 || boxesOpened.Length == 0) {
                return null;
            }

            float offsetX = 0;
            float offsetZ = 0;
            int count = Math.Min(boxesClosed.Length, boxesOpened.Length);
            for (int i = 0; i < count; i++) {
                offsetX += ((boxesOpened[i].X1 + boxesOpened[i].X2) - (boxesClosed[i].X1 + boxesClosed[i].X2)) / 2f;
                offsetZ += ((boxesOpened[i].Z1 + boxesOpened[i].Z2) - (boxesClosed[i].Z1 + boxesClosed[i].Z2)) / 2f;
            }

            int proxyX = (int)Math.Round(offsetX / count);
            int proxyZ = (int)Math.Round(offsetZ / count);

            if (proxyX == 0 && proxyZ == 0) {
                return null;
            }

            return new Vec3i(proxyX, 0, proxyZ);
        }

        public virtual void OnBlockPlaced(ItemStack byItemStack, IPlayer byPlayer, BlockSelection blockSel) {
            if (byItemStack == null) return; // Placed by worldgen

            RotateYRad = getRotateYRad(byPlayer, blockSel);
            SetupRotationsAndColSelBoxes(true);
        }

        public static float getRotateYRad(IPlayer byPlayer, BlockSelection blockSel) {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float deg90 = GameMath.PIHALF;
            return (int)Math.Round(angleHor / deg90) * deg90;
        }

        public bool IsSideSolid(BlockFacing facing) {
            return !opened && facing == facingWhenClosed || opened && facing == facingWhenOpened;
        }

        public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling) {
            if (!doorBh.handopenable && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
                (Api as ICoreClientAPI).TriggerIngameError(this, "nothandopenable", Lang.Get("This door cannot be opened by hand."));
                return true;
            }

            ToggleDoorState(byPlayer, !opened);
            handling = EnumHandling.PreventDefault;
            return true;
        }

        public void ToggleDoorState(IPlayer byPlayer, bool opened) {
            float breakChance = Block.Attributes["breakOnTriggerChance"].AsFloat(0);
            if (Api.Side == EnumAppSide.Server && Api.World.Rand.NextDouble() < breakChance && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) {
                Api.World.BlockAccessor.BreakBlock(Pos, byPlayer);
                Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), Pos, 0, null);
                return;
            }

            if (opened && !this.opened && IsOpenPathBlockedForAnyWing()) {
                if (Api.Side == EnumAppSide.Client && byPlayer != null) {
                    (Api as ICoreClientAPI)?.TriggerIngameError(this, "doorblocked", Lang.Get("This door is blocked."));
                }
                return;
            }

            this.opened = opened;
            ToggleDoorWing(opened);

            var be = Blockentity;
            float pitch = opened ? 1.1f : 0.9f;

            var bh = Blockentity.Block.GetBehavior<BlockBehaviorSlidingDoor>();
            var sound = opened ? bh?.OpenSound : bh?.CloseSound;

            Api.World.PlaySoundAt(sound, be.Pos.X + 0.5f, be.Pos.InternalY + 0.5f, be.Pos.Z + 0.5f, byPlayer, EnumSoundType.Sound, pitch);

            if (leftDoor != null && invertHandles) {
                leftDoor.ToggleDoorWing(opened);
                leftDoor.UpdateOpenInteractionProxyBlocks();
                leftDoor.UpdateNeighbors();
            } else if (rightDoor != null) {
                rightDoor.ToggleDoorWing(opened);
                rightDoor.UpdateOpenInteractionProxyBlocks();
                rightDoor.UpdateNeighbors();
            }

            be.MarkDirty(true);
            UpdateOpenInteractionProxyBlocks();

            UpdateNeighbors();
        }

        private bool IsOpenPathBlockedForAnyWing() {
            if (IsOpenPathBlocked(this)) {
                return true;
            }

            if (leftDoor != null && invertHandles && IsOpenPathBlocked(leftDoor)) {
                return true;
            }

            if (rightDoor != null && IsOpenPathBlocked(rightDoor)) {
                return true;
            }

            return false;
        }

        private bool IsOpenPathBlocked(BEBehaviorSlidingDoor door) {
            if (door?.Api?.World == null || door.boxesClosed == null || door.boxesOpened == null) {
                return false;
            }

            HashSet<Vec3i> closedOffsets = GetOccupiedOffsets(door.boxesClosed);
            HashSet<Vec3i> openedOffsets = GetOccupiedOffsets(door.boxesOpened);

            foreach (Vec3i offset in openedOffsets) {
                if (closedOffsets.Contains(offset)) {
                    continue;
                }

                for (int y = 0; y < door.doorBh.height; y++) {
                    BlockPos checkPos = door.Pos.AddCopy(offset.X, y, offset.Z);
                    Block existingBlock = door.Api.World.BlockAccessor.GetBlock(checkPos, BlockLayersAccess.Solid);
                    if (existingBlock.Id == 0) {
                        continue;
                    }

                    if (existingBlock is BlockMultiblock existingMb) {
                        BlockPos rootPos = checkPos.AddCopy(existingMb.OffsetInv);
                        if (rootPos == door.Pos || door.leftDoor != null && rootPos == door.leftDoor.Pos || door.rightDoor != null && rootPos == door.rightDoor.Pos) {
                            continue;
                        }
                    }

                    if (existingBlock.IsReplacableBy(door.Block)) {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private HashSet<Vec3i> GetOccupiedOffsets(Cuboidf[] boxes) {
            HashSet<Vec3i> offsets = new HashSet<Vec3i>(new Vec3iComparer());
            const float epsilon = 0.0001f;

            foreach (Cuboidf box in boxes) {
                int minX = (int)Math.Floor(box.X1 + epsilon);
                int maxX = (int)Math.Ceiling(box.X2 - epsilon) - 1;
                int minZ = (int)Math.Floor(box.Z1 + epsilon);
                int maxZ = (int)Math.Ceiling(box.Z2 - epsilon) - 1;

                for (int x = minX; x <= maxX; x++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        offsets.Add(new Vec3i(x, 0, z));
                    }
                }
            }

            if (offsets.Count == 0) {
                offsets.Add(new Vec3i());
            }

            return offsets;
        }

        internal void UpdateOpenInteractionProxyBlocks(bool clearOnly = false) {
            if (Api?.Side != EnumAppSide.Server) {
                return;
            }

            Vec3i? proxyOffset = clearOnly ? null : OpenInteractionProxyOffset;

            for (int y = 0; y < doorBh.height; y++) {
                if (proxyOffset != null) {
                    BlockPos proxyPos = Pos.AddCopy(proxyOffset.X, y, proxyOffset.Z);
                    Block existingBlock = Api.World.BlockAccessor.GetBlock(proxyPos);
                    BlockMultiblock existingMb = existingBlock as BlockMultiblock;

                    bool canPlaceProxy = existingBlock.Id == 0 || existingMb != null && proxyPos.AddCopy(existingMb.OffsetInv) == Pos;
                    if (canPlaceProxy) {
                        int dx = proxyOffset.X;
                        int dy = y;
                        int dz = proxyOffset.Z;

                        string sdx = (dx < 0 ? "n" : dx > 0 ? "p" : "") + Math.Abs(dx);
                        string sdy = (dy < 0 ? "n" : dy > 0 ? "p" : "") + Math.Abs(dy);
                        string sdz = (dz < 0 ? "n" : dz > 0 ? "p" : "") + Math.Abs(dz);

                        AssetLocation loc = new AssetLocation("multiblock-monolithic-" + sdx + "-" + sdy + "-" + sdz);
                        Block proxyBlock = Api.World.GetBlock(loc);
                        if (proxyBlock != null && proxyBlock.Id != 0) {
                            Api.World.BlockAccessor.SetBlock(proxyBlock.Id, proxyPos);
                        }
                    }
                }

                foreach (Vec3i staleOffset in GetLegacyOpenInteractionProxyOffsets(proxyOffset)) {
                    BlockPos stalePos = Pos.AddCopy(staleOffset.X, y, staleOffset.Z);
                    Block staleBlock = Api.World.BlockAccessor.GetBlock(stalePos);
                    if (staleBlock is BlockMultiblock staleMb && stalePos.AddCopy(staleMb.OffsetInv) == Pos) {
                        Api.World.BlockAccessor.SetBlock(0, stalePos);
                    }
                }
            }
        }

        private Vec3i[] GetLegacyOpenInteractionProxyOffsets(Vec3i? activeOffset) {
            Vec3i[] candidates = new[] {
                new Vec3i(-1, 0, 0),
                new Vec3i(1, 0, 0),
                new Vec3i(0, 0, -1),
                new Vec3i(0, 0, 1)
            };

            int count = 0;
            Vec3i[] staleOffsets = new Vec3i[candidates.Length];
            for (int i = 0; i < candidates.Length; i++) {
                if (activeOffset != null && candidates[i].X == activeOffset.X && candidates[i].Z == activeOffset.Z) {
                    continue;
                }

                staleOffsets[count++] = candidates[i];
            }

            Array.Resize(ref staleOffsets, count);
            return staleOffsets;
        }

        private void UpdateNeighbors() {
            if (Api.Side == EnumAppSide.Server) {
                BlockPos tempPos = new BlockPos(Pos.dimension);
                tempPos.dimension = Pos.dimension;
                for (int y = 0; y < doorBh.height; y++) {
                    tempPos.Set(Pos).Add(0, y, 0);
                    BlockFacing sideMove = BlockFacing.ALLFACES[Opened ? facingWhenClosed.HorizontalAngleIndex : facingWhenOpened.HorizontalAngleIndex];

                    for (int x = 0; x < doorBh.width; x++) {
                        Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(tempPos);
                        tempPos.Add(sideMove);
                    }
                }
            }
        }

        private void ToggleDoorWing(bool opened) {
            this.opened = opened;
            if (!opened) {
                animUtil.StopAnimation("opened");
            } else {
                float easingSpeed = Blockentity.Block.Attributes?["easingSpeed"].AsFloat(10) ?? 10;
                animUtil.StartAnimation(new AnimationMetaData() { Animation = "opened", Code = "opened", EaseInSpeed = easingSpeed, EaseOutSpeed = easingSpeed });
            }
            Blockentity.MarkDirty();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
            if (mesh == null) {
                UpdateMeshAndAnimations();
            }

            bool skipMesh = base.OnTesselation(mesher, tessThreadTesselator);
            if (!skipMesh && mesh != null) {
                mesher.AddMeshData(mesh);
            }
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            bool beforeOpened = opened;

            RotateYRad = tree.GetFloat("rotateYRad");
            opened = tree.GetBool("opened");
            invertHandles = tree.GetBool("invertHandles");
            leftDoorOffset = tree.GetVec3i("leftDoorPos");
            rightDoorOffset = tree.GetVec3i("rightDoorPos");

            if (opened != beforeOpened && animUtil != null) ToggleDoorWing(opened);
            if (Api != null && Api.Side is EnumAppSide.Client) {
                UpdateMeshAndAnimations();
                if (opened && !beforeOpened && animUtil != null && !animUtil.activeAnimationsByAnimCode.ContainsKey("opened")) {
                    ToggleDoorWing(true);
                }
                UpdateHitBoxes();
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);

            tree.SetFloat("rotateYRad", RotateYRad);
            tree.SetBool("opened", opened);
            tree.SetBool("invertHandles", invertHandles);
            if (leftDoorOffset != null) tree.SetVec3i("leftDoorPos", leftDoorOffset);
            if (rightDoorOffset != null) tree.SetVec3i("rightDoorPos", rightDoorOffset);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
            if (Api is ICoreClientAPI capi) {
                if (capi.Settings.Bool["extendedDebugInfo"] == true) {
                    dsc.AppendLine("" + facingWhenClosed + (invertHandles ? "-inv " : " ") + (opened ? "open" : "closed"));
                    dsc.AppendLine("" + doorBh.height + "x" + doorBh.width + (leftDoorOffset != null ? " leftdoor at:" + leftDoorOffset : " ") + (rightDoorOffset != null ? " rightdoor at:" + rightDoorOffset : " "));
                    EnumHandling h = EnumHandling.PassThrough;
                    if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.NORTH, Pos, ref h) > 0) dsc.AppendLine("Barrier to liquid on side: North");
                    if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.EAST, Pos, ref h) > 0) dsc.AppendLine("Barrier to liquid on side: East");
                    if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.SOUTH, Pos, ref h) > 0) dsc.AppendLine("Barrier to liquid on side: South");
                    if (doorBh.GetLiquidBarrierHeightOnSide(BlockFacing.WEST, Pos, ref h) > 0) dsc.AppendLine("Barrier to liquid on side: West");
                }
            }
        }

        public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis) {
            RotateYRad = tree.GetFloat("rotateYRad");
            RotateYRad = (RotateYRad - degreeRotation * GameMath.DEG2RAD) % GameMath.TWOPI;
            tree.SetFloat("rotateYRad", RotateYRad);
        }

        private class Vec3iComparer : IEqualityComparer<Vec3i> {
            public bool Equals(Vec3i? a, Vec3i? b) {
                return a != null && b != null && a.X == b.X && a.Y == b.Y && a.Z == b.Z;
            }

            public int GetHashCode(Vec3i value) {
                return HashCode.Combine(value.X, value.Y, value.Z);
            }
        }
    }
}

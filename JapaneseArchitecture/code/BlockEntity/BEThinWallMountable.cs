using System;
using JapaneseArchitecture.code.ThinWall;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace JapaneseArchitecture.code.BlockEntities {
    public class BEThinWallMountable : Vintagestory.API.Common.BlockEntity {
        const string OriginalCodeAttribute = "jaOriginalCode";
        const string OriginalStackAttribute = "jaOriginalStack";

        readonly ItemStack[] mountedLightStacks = new ItemStack[4];
        readonly ItemStack[] mountedSourceStacks = new ItemStack[4];
        readonly byte[][] mountedLightHsVs = new byte[4][];

        public byte[] MountedLightHsv => GetCombinedLightHsv();

        public bool CanAccept(ItemStack stack) {
            if (stack?.Collectible is not Vintagestory.API.Common.Block) {
                return false;
            }

            string path = stack.Collectible.Code?.Path ?? "";
            return path.StartsWith("torch-", StringComparison.Ordinal)
                || path.StartsWith("lantern-", StringComparison.Ordinal)
                || path.StartsWith("oillamp-", StringComparison.Ordinal);
        }

        public bool HasMountedLight(int slotIndex) {
            return slotIndex >= 0 && slotIndex < mountedLightStacks.Length && mountedLightStacks[slotIndex] != null;
        }

        public bool TryMount(int slotIndex, ItemSlot sourceSlot, IPlayer byPlayer, BlockFacing clickedFace) {
            if (sourceSlot?.Empty != false || HasMountedLight(slotIndex) || !CanAccept(sourceSlot.Itemstack)) {
                return false;
            }

            ItemStack oneStack = sourceSlot.Itemstack.Clone();
            oneStack.StackSize = 1;

            ItemStack orientedStack = CreateMountedStack(oneStack, clickedFace);
            if (orientedStack == null) {
                return false;
            }

            sourceSlot.TakeOut(1);
            sourceSlot.MarkDirty();

            mountedLightStacks[slotIndex] = orientedStack;
            mountedSourceStacks[slotIndex] = SanitizeCanonicalSourceStack(oneStack);
            LogTorchStack("Mount source", oneStack);
            LogTorchStack("Mount stored source", mountedSourceStacks[slotIndex]);
            LogTorchStack("Mount oriented", orientedStack);
            RefreshMountedLightState();
            return true;
        }

        public bool TryTakeMountedLight(int slotIndex, IPlayer byPlayer) {
            if (!HasMountedLight(slotIndex)) {
                return false;
            }

            ItemStack stack = GetSourceStack(slotIndex);
            LogTorchStack("Take restored", stack);
            mountedLightStacks[slotIndex] = null;
            mountedSourceStacks[slotIndex] = null;
            RefreshMountedLightState();

            if (byPlayer?.InventoryManager?.TryGiveItemstack(stack, true) != true) {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            return true;
        }

        public void DropMountedLights() {
            if (Api?.World == null) {
                return;
            }

            for (int i = 0; i < mountedLightStacks.Length; i++) {
                if (mountedLightStacks[i] != null) {
                    ItemStack stack = GetSourceStack(i);
                    LogTorchStack("Drop restored", stack);
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5 + (i % 2), 0.5));
                    mountedLightStacks[i] = null;
                    mountedSourceStacks[i] = null;
                    mountedLightHsVs[i] = null;
                }
            }
        }

        public override void Initialize(ICoreAPI api) {
            base.Initialize(api);
            RefreshMountedLightState(false);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
            base.FromTreeAttributes(tree, worldAccessForResolve);

            for (int i = 0; i < mountedLightStacks.Length; i++) {
                mountedLightStacks[i] = tree.GetItemstack("mountedLight" + i, null);
                mountedLightStacks[i]?.ResolveBlockOrItem(worldAccessForResolve);
                mountedSourceStacks[i] = tree.GetItemstack("mountedSource" + i, null);
                mountedSourceStacks[i]?.ResolveBlockOrItem(worldAccessForResolve);
                mountedSourceStacks[i] = SanitizeCanonicalSourceStack(mountedSourceStacks[i]);
                mountedLightHsVs[i] = mountedLightStacks[i]?.Collectible?.GetLightHsv(worldAccessForResolve.BlockAccessor, Pos, mountedLightStacks[i]);
            }

            if (Api?.Side == EnumAppSide.Client) {
                Api.World.BlockAccessor.MarkBlockDirty(Pos);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree) {
            base.ToTreeAttributes(tree);
            for (int i = 0; i < mountedLightStacks.Length; i++) {
                tree.SetItemstack("mountedLight" + i, mountedLightStacks[i]);
                tree.SetItemstack("mountedSource" + i, mountedSourceStacks[i]);
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) {
            for (int i = 0; i < mountedLightStacks.Length; i++) {
                if (mountedLightStacks[i]?.Collectible is Vintagestory.API.Common.Block mountedBlock) {
                    tessThreadTesselator.TesselateBlock(mountedBlock, out MeshData mesh);
                    if (mesh != null) {
                        Vec3f offset = GetMountOffset(i);
                        mesh.Translate(offset.X, offset.Y, offset.Z);
                        mesher.AddMeshData(mesh);
                    }
                }
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        void RefreshMountedLightState(bool markDirty = true) {
            for (int i = 0; i < mountedLightStacks.Length; i++) {
                mountedLightHsVs[i] = mountedLightStacks[i]?.Collectible?.GetLightHsv(Api?.World?.BlockAccessor, Pos, mountedLightStacks[i]);
            }

            if (Api?.World == null || !markDirty) {
                return;
            }

            MarkDirty(true);
            Api.World.BlockAccessor.MarkBlockDirty(Pos);
            Api.World.BlockAccessor.MarkBlockModified(Pos);
        }

        ItemStack CreateMountedStack(ItemStack sourceStack, BlockFacing clickedFace) {
            if (clickedFace == null) {
                return null;
            }

            AssetLocation code = ReplaceLastCodePart(sourceStack.Collectible.Code, clickedFace.Code);
            Vintagestory.API.Common.Block mountedBlock = Api.World.GetBlock(code);
            if (mountedBlock == null || mountedBlock.Id == 0) {
                return null;
            }

            ItemStack mountedStack = new ItemStack(mountedBlock, 1);
            mountedStack.Attributes = sourceStack.Attributes?.Clone() ?? new TreeAttribute();
            mountedStack.Attributes.SetString(OriginalCodeAttribute, sourceStack.Collectible.Code.ToShortString());
            mountedStack.Attributes.SetItemstack(OriginalStackAttribute, sourceStack.Clone());

            return mountedStack;
        }

        ItemStack RestoreOriginalStack(ItemStack mountedStack) {
            ItemStack restoredStack = mountedStack.Clone();
            ItemStack sourceStack = restoredStack.Attributes?.GetItemstack(OriginalStackAttribute);
            sourceStack?.ResolveBlockOrItem(Api.World);

            string originalCode = restoredStack.Attributes?.GetString(OriginalCodeAttribute);
            if (string.IsNullOrEmpty(originalCode) && sourceStack != null) {
                originalCode = sourceStack.Collectible?.Code?.ToShortString();
            }

            if (string.IsNullOrEmpty(originalCode)) {
                return restoredStack;
            }

            originalCode = NormalizeRestoredCode(originalCode);

            CollectibleObject originalCollectible = Api?.World?.GetBlock(new AssetLocation(originalCode));
            originalCollectible ??= Api?.World?.GetItem(new AssetLocation(originalCode));
            if (originalCollectible == null) {
                return restoredStack;
            }

            int stackSize = sourceStack?.StackSize ?? restoredStack.StackSize;
            ItemStack normalizedStack = new ItemStack(originalCollectible, stackSize);

            ITreeAttribute sourceAttributes = sourceStack?.Attributes?.Clone() ?? restoredStack.Attributes?.Clone();
            if (sourceAttributes != null) {
                sourceAttributes.RemoveAttribute(OriginalCodeAttribute);
                sourceAttributes.RemoveAttribute(OriginalStackAttribute);
                normalizedStack.Attributes = sourceAttributes;
            }

            return normalizedStack;
        }

        ItemStack GetSourceStack(int slotIndex) {
            if (slotIndex >= 0 && slotIndex < mountedSourceStacks.Length && mountedSourceStacks[slotIndex] != null) {
                return SanitizeCanonicalSourceStack(mountedSourceStacks[slotIndex]);
            }

            return RestoreOriginalStack(mountedLightStacks[slotIndex]);
        }

        ItemStack SanitizeCanonicalSourceStack(ItemStack sourceStack) {
            if (sourceStack == null) {
                return null;
            }

            CollectibleObject originalCollectible = ResolveCanonicalCollectible(sourceStack);
            if (originalCollectible == null) {
                return sourceStack.Clone();
            }

            ItemStack sanitizedStack = new ItemStack(originalCollectible, sourceStack.StackSize);
            ITreeAttribute sourceAttributes = sourceStack.Attributes?.Clone();
            string resolvedCode = originalCollectible.Code?.ToShortString() ?? "";

            if (sourceAttributes != null) {
                sourceAttributes.RemoveAttribute(OriginalCodeAttribute);
                sourceAttributes.RemoveAttribute(OriginalStackAttribute);

                if (!resolvedCode.StartsWith("torch-", StringComparison.Ordinal)) {
                    sanitizedStack.Attributes = sourceAttributes;
                }
            }

            return sanitizedStack;
        }

        CollectibleObject ResolveCanonicalCollectible(ItemStack sourceStack) {
            string originalCode = sourceStack?.Collectible?.Code?.ToShortString();
            if (string.IsNullOrEmpty(originalCode)) {
                return null;
            }

            if (sourceStack.Collectible is Vintagestory.GameContent.BlockTorch && originalCode.StartsWith("torch-", StringComparison.Ordinal)) {
                AssetLocation upCode = ReplaceLastCodePart(sourceStack.Collectible.Code, "up");
                CollectibleObject upCollectible = Api?.World?.GetBlock(upCode);
                if (upCollectible != null) {
                    return upCollectible;
                }
            }

            originalCode = NormalizeRestoredCode(originalCode);

            CollectibleObject originalCollectible = Api?.World?.GetBlock(new AssetLocation(originalCode));
            originalCollectible ??= Api?.World?.GetItem(new AssetLocation(originalCode));
            return originalCollectible;
        }

        void LogTorchStack(string label, ItemStack stack) {
            string code = stack?.Collectible?.Code?.ToShortString() ?? "<null>";
            if (
                !code.StartsWith("torch-", StringComparison.Ordinal) &&
                !code.StartsWith("lantern-", StringComparison.Ordinal) &&
                !code.StartsWith("oillamp-", StringComparison.Ordinal)
            ) {
                return;
            }

            string collectibleType = stack?.Collectible?.GetType().FullName ?? "<null>";
            string attributes = stack?.Attributes?.ToJsonToken()?.ToString() ?? "<null>";
            Api?.Logger?.Warning("[JapaneseArchitecture] {0}: code={1}, collectibleType={2}, stackSize={3}, attrs={4}", label, code, collectibleType, stack?.StackSize ?? 0, attributes);
        }

        Vec3f GetMountOffset(int slotIndex) {
            return ThinWallFaceData.GetMountOffset(Block, slotIndex);
        }

        byte[] GetCombinedLightHsv() {
            byte[] brightest = null;

            for (int i = 0; i < mountedLightHsVs.Length; i++) {
                byte[] hsv = mountedLightHsVs[i];
                if (hsv == null) {
                    continue;
                }

                if (brightest == null || hsv[2] > brightest[2]) {
                    brightest = hsv;
                }
            }

            return brightest;
        }
        static AssetLocation ReplaceLastCodePart(AssetLocation code, string replacement) {
            string[] parts = code.Path.Split('-');
            if (parts.Length == 0) {
                return code;
            }

            parts[parts.Length - 1] = replacement;
            return code.WithPath(string.Join("-", parts));
        }

        static string NormalizeRestoredCode(string code) {
            if (string.IsNullOrEmpty(code)) {
                return code;
            }

            if (
                !code.StartsWith("torch-", StringComparison.Ordinal) &&
                !code.StartsWith("oillamp-", StringComparison.Ordinal) &&
                !code.StartsWith("lantern-", StringComparison.Ordinal)
            ) {
                return code;
            }

            string[] parts = code.Split('-');
            if (parts.Length == 0) {
                return code;
            }

            string lastPart = parts[parts.Length - 1];
            if (lastPart == "north" || lastPart == "east" || lastPart == "south" || lastPart == "west" || lastPart == "down") {
                parts[parts.Length - 1] = "up";
                return string.Join("-", parts);
            }

            return code;
        }
    }
}

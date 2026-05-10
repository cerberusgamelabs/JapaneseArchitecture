using JapaneseArchitecture.code.BlockBehavior;
using JapaneseArchitecture.code.BlockEntities;
using JapaneseArchitecture.code.Blocks;
using JapaneseArchitecture.code.BlockEntityBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace JapaneseArchitecture.code {
    public class JapaneseArchitectureModSystem : ModSystem {

        public override void Start(ICoreAPI api) {
            base.Start(api);
            Mod.Logger.Notification("Japanese Architecture loaded: " + api.Side);
            api.RegisterBlockClass("ThinWallMountable", typeof(BlockThinWallMountable));
            api.RegisterBlockEntityClass("ThinWallMountable", typeof(BEThinWallMountable));
            api.RegisterBlockEntityBehaviorClass("SlidingDoor", typeof(BEBehaviorSlidingDoor));
            api.RegisterBlockBehaviorClass("SlidingDoor", typeof(BlockBehaviorSlidingDoor));
            api.RegisterBlockBehaviorClass("ThinWallAttachable", typeof(BlockBehaviorThinWallAttachable));
        }

        public override void StartServerSide(ICoreServerAPI api) {
            Mod.Logger.Notification("Japanese Architecture loaded server side: " + Lang.Get("japanesearchitecture:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api) {
            Mod.Logger.Notification("Japanese Architecture loaded client side: " + Lang.Get("japanesearchitecture:hello"));
        }
    }
}

using JapaneseArchitecture.code.BlockBehavior;
using JapaneseArchitecture.code.BlockEntityBehavior;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace JapaneseArchitecture.code {
    public class JapaneseArchitectureModSystem : ModSystem {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api) {
            base.Start(api);
            Mod.Logger.Notification("Japanese Architecture loaded: " + api.Side);
            api.RegisterBlockEntityBehaviorClass("SlidingDoor", typeof(BEBehaviorSlidingDoor));
            api.RegisterBlockBehaviorClass("SlidingDoor", typeof(BlockBehaviorSlidingDoor));
        }

        public override void StartServerSide(ICoreServerAPI api) {
            Mod.Logger.Notification("Japanese Architecture loaded server side: " + Lang.Get("japanesearchitecture:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api) {
            Mod.Logger.Notification("Japanese Architecture loaded client side: " + Lang.Get("japanesearchitecture:hello"));
        }

    }
}
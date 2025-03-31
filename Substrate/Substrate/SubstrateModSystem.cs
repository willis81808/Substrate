using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Substrate.Behaviors;
using Substrate.BlockEntities;
using Substrate.Blocks;
using Substrate.Utils;
using Substrate.Utils.CodecPattern;
using Substrate.Utils.CodecPattern.Codecs;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Substrate
{
    public class SubstrateModSystem : ModSystem
    {
        internal static ILogger Logger { get; private set; }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockFruitingBag", typeof(BlockFruitingBag));
            api.RegisterBlockClass("BlockGrowBed", typeof(BlockGrowBed));
            api.RegisterBlockEntityClass("FruitingBag", typeof(BlockEntityFruitingBag));

            api.RegisterBlockClass("BlockSporePaper", typeof(BlockSporePaper));
            api.RegisterBlockEntityClass("SporePaper", typeof(BlockEntitySporePaper));

            api.RegisterCollectibleBehaviorClass("UseInventoryShape", typeof(BehaviorShapeInventory));
            api.RegisterBlockBehaviorClass("BehaviorMushroomGrower", typeof(BehaviorMushroomGrower));

            var harmony = new Harmony(Mod.Info.ModID);
            harmony.PatchAll(typeof(SubstrateModSystem).Assembly);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            // Add UseInventoryShape behavior to all spore harvestable mushrooms
            foreach (var obj in api.World.Collectibles)
            {
                if (obj == null || obj.Code == null) continue;

                if (obj is BlockMushroom { Attributes: not null } bm && bm.Attributes.IsTrue("sporeharvestable"))
                {
                    obj.CollectibleBehaviors = obj.CollectibleBehaviors.Append(new BehaviorShapeInventory(bm));
                }
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Logger = Mod.Logger;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Logger = Mod.Logger;
        }
    }
}

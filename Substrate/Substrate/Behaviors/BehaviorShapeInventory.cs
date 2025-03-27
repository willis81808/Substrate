using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Substrate.Behaviors
{
    internal class BehaviorShapeInventory : CollectibleBehavior, IContainedMeshSource
    {
        public BehaviorShapeInventory(CollectibleObject collObj) : base(collObj) {}

        private ICoreAPI _api;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this._api = api;
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
        {
            var api = _api as ICoreClientAPI;

            var inventoryShape = itemstack.Block.ShapeInventory; // this is not a valid cast
            var shapePath = inventoryShape.Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            var shape = _api.Assets.TryGet(shapePath)?.ToObject<Shape>();

            if (shape == null) return null;

            var containedTextureSource = new ContainedTextureSource(api, targetAtlas, shape.Textures, $"For displayed item {itemstack.Collectible.Code}");
            var tesselator = api.Tesselator;
            var meta = new TesselationMetaData
            {
                TexSource = containedTextureSource
            };

            tesselator.TesselateShape(meta, shape, out var meshData);

            return meshData;
        }

        public string GetMeshCacheKey(ItemStack itemstack)
        {
            return itemstack.Collectible.Code + "-shapeInventory";
        }
    }
}

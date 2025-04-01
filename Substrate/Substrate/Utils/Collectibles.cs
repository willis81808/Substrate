using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Substrate.Utils
{
    internal static class Collectibles
    {
        internal static CollectibleObject Compost(ICoreAPI api) =>
            ObjectCacheUtil.GetOrCreate(
                api,
                "compostCollectible",
                () => api.World.GetItem(AssetLocation.Create("game:compost"))
            );


        internal static CollectibleObject Rot(ICoreAPI api) =>
            ObjectCacheUtil.GetOrCreate(
                api, 
                "rotCollectable", 
                () => api.World.GetItem(AssetLocation.Create("game:rot"))
            );
    }
}

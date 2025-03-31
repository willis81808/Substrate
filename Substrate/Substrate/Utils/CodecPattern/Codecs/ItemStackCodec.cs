using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Substrate.Utils.CodecPattern.Codecs
{
    public class ItemStackCodec
    {
        public static readonly ICodec<ItemStack> ITEM_STACK = RecordCodecBuilder.Create<ItemStack>()
            .Apply(Codec.INT, "id", i => i.Id)
            .Apply(Codec.ASSET_LOCATION, "code", i => i.Collectible.Code)
            .Apply(Codec.INT, "class", i => (int)i.Class)
            .Apply(Codec.INT, "stack_size", i => i.StackSize)
            .Apply(Codec.ListOf(Codec.BYTE), "bytes", i => i.ToBytes().ToList())
            .Build((id, code, itemClass, stackSize, bytes) =>
            {
                var stack = new ItemStack
                {
                    Id = id, 
                    StackSize = stackSize, 
                    Class = (EnumItemClass) itemClass
                };
                using (var memoryStream = new MemoryStream(bytes.ToArray()))
                {
                    using (var binaryReader= new BinaryReader(memoryStream))
                    {
                        stack.FromBytes(binaryReader);
                    }
                }
                var mapping = new Dictionary<int, AssetLocation> { { id, code } };
                //if (stack.FixMapping(mapping, mapping, SubstrateModSystem.Api.World))
                //{
                //    stack.ResolveBlockOrItem(SubstrateModSystem.Api.World);
                //    return stack;
                //}
                return null;
            });
    }
}

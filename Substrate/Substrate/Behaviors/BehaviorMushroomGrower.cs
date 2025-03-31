using Substrate.BlockEntities;
using Substrate.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Substrate.Behaviors
{
    public class BehaviorMushroomGrower : BlockBehavior
    {
        public BehaviorMushroomGrower(Block block) : base(block) { }

        internal float NextRotDrop() => block.Attributes["rotDrop"]?.AsObject<NatFloat>()?.nextFloat() ?? 0;
        internal float NextCompostDrop() => block.Attributes["compostDrop"]?.AsObject<NatFloat>()?.nextFloat() ?? 0;
        internal float NextRefundDrop() => block.Attributes["refundAmount"]?.AsObject<NatFloat>()?.nextFloat() ?? 0;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            return block
                .GetBlockEntity<BlockEntityFruitingBag>(blockSel.Position)?
                .OnInteract(world, byPlayer, blockSel) ?? true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            return block.GetBlockEntity<BlockEntityFruitingBag>(blockSel)
                ?.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }


        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handling)
        {
            var entity = block.GetBlockEntity<BlockEntityFruitingBag>(pos);

            handling = EnumHandling.PreventDefault;

            if (entity.Fertility <= 0)
            {
                return GetRottenDrops(world, entity).ToArray();
            }

            var drops = new [] { new ItemStack(block) };

            if (drops.FirstOrDefault() is not { } stack) return drops;

            if (Math.Abs(entity.Fertility - entity.MaxFertility) > 0.01f)
                stack.Attributes.SetFloat("fertility", entity.Fertility);

            if (entity.RefundItem != null)
                stack.Attributes.SetString("refundItem", entity.RefundItem);

            if (entity.ElapsedColonizeHours > 0)
                stack.Attributes.SetDouble("elapsedColonizeHours", entity.ElapsedColonizeHours);

            if (entity.ColonizeDuration > 0)
                stack.Attributes.SetDouble("colonizeDuration", entity.ColonizeDuration);

            if (entity.InoculatedMushroom != null)
                stack.Attributes.SetString("sporetype", entity.InoculatedMushroom);

            return drops;
        }

        private IEnumerable<ItemStack> GetRottenDrops(IWorldAccessor world, BlockEntityFruitingBag entity)
        {
            var drops = new List<CollectibleObject>();

            var compost = Collectibles.Compost(world.Api);
            var rot = Collectibles.Rot(world.Api);

            var compostDrops = NextCompostDrop();
            for (var i = 0; i < compostDrops; i++)
            {
                drops.Add(compost);
            }

            var rotDrops = NextRotDrop();
            for (var i = 0; i < rotDrops; i++)
            {
                drops.Add(rot);
            }

            if (entity.RefundItem != null && world.Collectibles.FirstOrDefault(c => c.Code == new AssetLocation(entity.RefundItem)) is { } collectible)
            {
                var refundDrops = NextRefundDrop();
                for (var i = 0; i < refundDrops; i++)
                {
                    drops.Add(collectible);
                }
            }

            return drops.Select(c => new ItemStack(c));
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            if (itemStack.Attributes.GetString("sporetype") is { } type)
            {
                sb.Append($" ({Lang.Get($"substrate:mushroom-{type}")})");
            }

            var maxFertility = itemStack.Block.Attributes["maxfert"].AsFloat();
            var fertility = itemStack.Attributes.GetFloat("fertility", maxFertility);
            if (fertility <= 0)
            {
                sb.Insert(0, "Moldy ");
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot.Itemstack.Attributes.GetDouble("elapsedColonizeHours") is var elapsedColonizeHours and > 0 &&
                inSlot.Itemstack.Attributes.GetDouble("colonizeDuration") is var colonizeDuration and > 0 &&
                elapsedColonizeHours < colonizeDuration)
            {
                var remainingHours = colonizeDuration - elapsedColonizeHours;
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-colonizing-interrupted"));
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-colonizing-hours", remainingHours));
            }

            var maxFertility = inSlot.Itemstack.Block.Attributes["maxfert"].AsFloat();
            var fertility = inSlot.Itemstack.Attributes.GetFloat("fertility", maxFertility);
            if (fertility > 0)
            {
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-remaining-fertility", fertility / maxFertility * 100));
            }
        }
    }
}

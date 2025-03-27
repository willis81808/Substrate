using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Substrate.BlockEntities;
using Substrate.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Substrate.Blocks
{
    public class BlockFruitingBag : Block
    {
        internal float NextRotDrop() => Attributes["rotDrop"].AsObject<NatFloat>()?.nextFloat() ?? 0;
        internal float NextCompostDrop() => Attributes["compostDrop"].AsObject<NatFloat>()?.nextFloat() ?? 0;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel) && (world.BlockAccessor
                .GetBlockEntity<BlockEntityFruitingBag>(blockSel.Position)?
                .OnInteract(world, byPlayer, blockSel) ?? true);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            return GetBlockEntity<BlockEntityFruitingBag>(blockSel)
                ?.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var entity = GetBlockEntity<BlockEntityFruitingBag>(pos);

            if (entity.Fertility <= 0)
            {
                return GetRottenDrops(world).ToArray();
            }

            var drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);

            if (drops.FirstOrDefault() is not { } stack) return drops;

            if (Math.Abs(entity.Fertility - entity.MaxFertility) > 0.01f)
                stack.Attributes.SetFloat("fertility", entity.Fertility);

            if (entity.ElapsedColonizeHours > 0)
                stack.Attributes.SetDouble("elapsedColonizeHours", entity.ElapsedColonizeHours);

            if (entity.ColonizeDuration > 0)
                stack.Attributes.SetDouble("colonizeDuration", entity.ColonizeDuration);

            if (entity.InoculatedMushroom != null)
                stack.Attributes.SetString("sporetype", entity.InoculatedMushroom);

            return drops;
        }

        private IEnumerable<ItemStack> GetRottenDrops(IWorldAccessor world)
        {
            var drops = new List<CollectibleObject>();

            var compost = Collectibles.Compost(world.Api);
            var rot = Collectibles.Rot(world.Api);

            for (var i = 0; i < NextCompostDrop(); i++)
            {
                drops.Add(compost);
            }

            for (var i = 0; i < NextRotDrop(); i++)
            {
                drops.Add(rot);
            }

            return drops.Select(c => new ItemStack(c));
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            var baseName = base.GetHeldItemName(itemStack);
            if (itemStack.Attributes.GetString("sporetype") is { } type)
            {
                baseName = $"{baseName} ({Lang.Get($"substrate:mushroom-{type}")})";
            }

            var maxFertility = itemStack.Block.Attributes["maxfert"].AsFloat();
            var fertility = itemStack.Attributes.GetFloat("fertility", maxFertility);
            if (fertility <= 0)
            {
                baseName = $"Moldy {baseName}";
            }

            return baseName;
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

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            var baseName = base.GetPlacedBlockName(world, pos);
            if (GetBlockEntity<BlockEntityFruitingBag>(pos) is { InoculatedMushroom: { } type })
            {
                baseName = $"{baseName} ({Lang.Get($"substrate:mushroom-{type}")})";
            }

            if (GetBlockEntity<BlockEntityFruitingBag>(pos) is { Fertility: <= 0 })
            {
                baseName = $"Moldy {baseName}";
            }

            return baseName;
        }
    }
}

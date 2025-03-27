using Substrate.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Substrate.BlockEntities
{
    public class BlockEntitySporePaper : BlockEntityDisplayCase
    {
        public override string InventoryClassName => "sporepaper";
        public override InventoryBase Inventory => inventory;

        private bool Collecting => Inventory.All(i => i.Itemstack?.Block?.Attributes.IsTrue("sporeharvestable") ?? false);
        private bool Done => !string.IsNullOrWhiteSpace(_sporeType);
        private double CollectDuration => Block.Attributes["sporecollectdays"].AsDouble();
        private double RemainingCollectTime
        {
            get
            {
                var hoursPerDay = Api.World.Calendar.HoursPerDay;
                var elapsedHours = Api.World.Calendar.ElapsedHours - _startCollectingHours;
                return (hoursPerDay * CollectDuration) - elapsedHours;
            }
        }

        private double _startCollectingHours = 0;
        private string _sporeType = null;

        public BlockEntitySporePaper()
        {
            inventory = new InventoryDisplayed(this, 4, "sporepaper-0", null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(Tick, 1000);
            var mushroomVariant = Block?.Variant.Get("mushroom");
            if (mushroomVariant == null) return;
            var mushroomBlock = BlockEntityFruitingBag.GetMushroomBlock(api.World, mushroomVariant, BlockFacing.EAST);
            _sporeType = mushroomBlock.Code.ToString();
        }

        private void Tick(float deltaTime)
        {
            if (!Collecting) return;

            if (RemainingCollectTime <= 0)
            {
                FinishCollection();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble(nameof(_startCollectingHours).ToLower(), _startCollectingHours);
            tree.SetString(nameof(_sporeType).ToLower(), _sporeType);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            _startCollectingHours = tree.GetDouble(nameof(_startCollectingHours).ToLower());
            _sporeType = tree.GetString(nameof(_sporeType).ToLower());
        }

        private void FinishCollection()
        {
            _sporeType = Inventory.FirstNonEmptySlot.Itemstack.Block.Code.ToString();

            var mushroomName = BlockEntityFruitingBag.ExtractMushroomName(_sporeType);
            var block = Api.World.BlockAccessor.GetBlock(new AssetLocation("substrate", $"sporepaperprinted-{mushroomName}-{Block.Variant["side"]}"));
            Api.World.BlockAccessor.SetBlock(block.Id, Pos);
            var newEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntitySporePaper>(Pos);
            for (var i = 0; i < newEntity.Inventory.Count; i++)
            {
                newEntity.Inventory[i].Itemstack = new ItemStack(Collectibles.Rot(Api));
                newEntity.updateMesh(i);
                newEntity.MarkDirty(true);
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            if (Collecting)
            {
                sb.AppendLine(Lang.Get("substrate:sporepaper-collecting-hours", RemainingCollectTime));
            }
            else if (Done)
            {

            }
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] matrices = base.genTransformationMatrices();

            // Check each slot for mushrooms
            for (int i = 0; i < inventory.Count; i++)
            {
                // For mushrooms, use a custom matrix that works better with the inventory mesh
                float x = i % 2 == 0 ? 5f / 16f : 11f / 16f;
                float z = i > 1 ? 11f / 16f : 5f / 16f;

                // Simplify the transformation for mushrooms
                matrices[i] = new Matrixf()
                    .Translate(0.5f, 0.0f, 0.5f)
                    .Translate(x - 0.5f, 0, z - 0.5f)
                    .RotateYDeg(45) // Fixed rotation for better visibility
                    //.Scale(0.75f, 0.75f, 0.75f) // Smaller scale to match inventory item size
                    .Translate(-0.5f, 0.0f, -0.5f)
                    .Values;
            }

            return matrices;
        }

        private bool CanAccept(CollectibleObject obj)
        {
            var requiredId = inventory.FirstNonEmptySlot?.Itemstack.Id;
            if (requiredId == null)
            {
                return obj.Attributes != null && obj.Attributes["sporeharvestable"].AsBool();
            }
            else
            {
                return requiredId == obj.Id;
            }
        }

        internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;

            var colObj = slot.Itemstack?.Collectible;

            if (slot.Empty)
            {
                return TryTake(byPlayer, blockSel);
            }

            if (colObj == null || !CanAccept(colObj) || !TryPut(slot, blockSel)) return false;

            var sound = slot.Itemstack?.Block?.Sounds?.Place;
            Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
            
            return true;
        }

        private bool TryPut(ItemSlot slot, BlockSelection blockSel)
        {
            if (Done) return false;

            int index = blockSel.SelectionBoxIndex;

            if (index < inventory.Count && inventory[index].Empty)
            {
                int moved = slot.TryPutInto(Api.World, inventory[index]);

                if (moved > 0)
                {
                    updateMesh(index);
                    MarkDirty(true);
                    _startCollectingHours = Api.World.Calendar.ElapsedHours;
                }

                return moved > 0;
            }

            return false;
        }

        private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 4 || Done)
            {
                var slot = inventory.FirstNonEmptySlot;
                if (slot != null)
                {
                    var i = inventory.IndexOf(s => s == slot);
                    var stack = inventory.FirstNonEmptySlot?.TakeOut(1);
                    if (stack != null && byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        AssetLocation sound = stack.Block?.Sounds?.Place;
                        Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                    }

                    updateMesh(i);
                    MarkDirty(true);
                }

                return true;
            }

            int index = blockSel.SelectionBoxIndex;

            if (!inventory[index].Empty)
            {
                ItemStack stack = inventory[index].TakeOut(1);
                if (stack != null && byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    AssetLocation sound = stack.Block?.Sounds?.Place;
                    Api.World.PlaySoundAt(sound != null ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }

                if (stack.StackSize > 0)
                {
                    Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }

                updateMesh(index);
                MarkDirty(true);
                return true;
            }

            return false;
        }

        public WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var interaction = new WorldInteraction { MouseButton = EnumMouseButton.Right };
            var slot = selection.SelectionBoxIndex < 4 ? Inventory[selection.SelectionBoxIndex] : Inventory.FirstNonEmptySlot ?? Inventory[0];

            if (slot.Itemstack?.Collectible?.LastCodePart() == "rot" || Done && Inventory.FirstNonEmptySlot != null)
            {
                interaction.ActionLangCode = "substrate:sporepaper-clean-rot";
                return new[] { interaction };
            }

            if (Done) return Array.Empty<WorldInteraction>();

            if (slot.Empty)
            {
                interaction.ActionLangCode = "substrate:sporepaper-insert-mushroom";
                interaction.Itemstacks = CanHarvestSporesStacks(Api);
            }
            else
            {
                interaction.ActionLangCode = "substrate:sporepaper-take-mushroom";
            }

            return new[] { interaction };
        }

        private ItemStack[] CanHarvestSporesStacks(ICoreAPI api)
        {
            var restricted = inventory.FirstNonEmptySlot?.Itemstack;
            return restricted != null ? new[] { restricted } : ObjectCacheUtil.GetOrCreate(api, "canHarvestSpores", () =>
                api.World.Collectibles
                    .Where(c => c is Block { Attributes: not null } b && b.Attributes["sporeharvestable"].AsBool())
                    .SelectMany(c => c.GetHandBookStacks(api as ICoreClientAPI) ?? Enumerable.Empty<ItemStack>())
                    .ToArray()
            );
        }
    }
}

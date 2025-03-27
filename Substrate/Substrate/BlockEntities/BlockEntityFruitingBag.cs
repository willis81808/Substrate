using System;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Substrate.Utils;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Substrate.BlockEntities
{
    public class BlockEntityFruitingBag : BlockEntity
    {
        internal bool Inoculated => !string.IsNullOrWhiteSpace(_inoculatedMushroom);
        internal bool Colonizing => Inoculated && _colonizeDuration > 0 && _elapsedColonizeHours < _colonizeDuration;
        internal float MaxFertility => Block.Attributes["maxfert"].AsFloat();
        internal float GrowChance => Block.Attributes["growchance"].AsFloat();

        internal double ElapsedColonizeHours => _elapsedColonizeHours;
        internal double ColonizeDuration => _colonizeDuration;

        private double _startGrowingHours = 0;
        private double _elapsedColonizeHours = 0;
        private double _nextGrowHours = 0;
        private double _colonizeDuration = 0;
        private double _lastColonizeProgressTimestamp = 0;

        internal string? InoculatedMushroom => string.IsNullOrWhiteSpace(_inoculatedMushroom) ? null : _inoculatedMushroom;
        private string _inoculatedMushroom = string.Empty;

        internal float Fertility
        {
            get => _fertility;
            private set => _fertility = value;
        }
        private float _fertility = -1;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(Tick, 1000);

            if (Fertility >= 0) return;

            Fertility = MaxFertility;
            MarkDirty(true);
        }

        internal float NextFertilityDrain() => Block.Attributes["fertconsumerate"].AsObject<NatFloat>()?.nextFloat() ?? 0;
        internal float NextGrowIncrement() => Block.Attributes["growincrement"].AsObject<NatFloat>()?.nextFloat() ?? 2;
        internal float NextColonizeIncrement() => Block.Attributes["colonizeincrement"].AsObject<NatFloat>()?.nextFloat() ?? 2;

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString(nameof(_inoculatedMushroom).ToLower(), _inoculatedMushroom);
            tree.SetFloat(nameof(_fertility).ToLower(), _fertility);
            tree.SetDouble(nameof(_startGrowingHours).ToLower(), _startGrowingHours);
            tree.SetDouble(nameof(_elapsedColonizeHours).ToLower(), _elapsedColonizeHours);
            tree.SetDouble(nameof(_colonizeDuration).ToLower(), _colonizeDuration);
            tree.SetDouble(nameof(_nextGrowHours).ToLower(), _nextGrowHours);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            _inoculatedMushroom = tree.GetString(nameof(_inoculatedMushroom).ToLower());
            _fertility = tree.GetFloat(nameof(_fertility).ToLower(), MaxFertility);
            _startGrowingHours = tree.GetDouble(nameof(_startGrowingHours).ToLower());
            _elapsedColonizeHours = tree.GetDouble(nameof(_elapsedColonizeHours).ToLower());
            _colonizeDuration = tree.GetDouble(nameof(_colonizeDuration).ToLower());
            _nextGrowHours = tree.GetDouble(nameof(_nextGrowHours).ToLower());
        }

        private void Tick(float deltaTime)
        {
            if (Api.Side != EnumAppSide.Server || !Inoculated) return;

            if (Fertility <= 0)
            {
                _inoculatedMushroom = null;
                _fertility = 0;
                _startGrowingHours = 0;
                _elapsedColonizeHours = 0;
                _colonizeDuration = 0;
                _nextGrowHours = 0;
                MarkDirty();

                return;
            }

            if (Colonizing)
            {
                if (_lastColonizeProgressTimestamp == 0)
                    _lastColonizeProgressTimestamp = Api.World.Calendar.ElapsedHours;

                _elapsedColonizeHours += Api.World.Calendar.ElapsedHours - _lastColonizeProgressTimestamp;
                _lastColonizeProgressTimestamp = Api.World.Calendar.ElapsedHours;

                if (!Colonizing) _nextGrowHours = Api.World.Calendar.ElapsedHours + NextGrowIncrement();
            }
            else if (_nextGrowHours < Api.World.Calendar.ElapsedHours)
            {
                _nextGrowHours = Api.World.Calendar.ElapsedHours + NextGrowIncrement();

                Fertility = Math.Max(0, Fertility - NextFertilityDrain());

                var openFaces = BlockFacing.HORIZONTALS
                    .Select(facing => new
                    {
                        Facing = facing,
                        Block = Api.World.BlockAccessor.GetBlockOnSide(Pos, facing),
                        Position = Pos.Copy().Offset(facing),
                    })
                    .Where(b => b.Block.IsAir())
                    .OrderBy(_ => Api.World.Rand.NextDouble())
                    .ToList();

                if (openFaces.Count > 0)
                {
                    foreach (var selection in openFaces)
                    {
                        if (!(Api.World.Rand.NextDouble() <= GrowChance)) continue;
                        var mushroom = GetMushroomBlock(Api.World, InoculatedMushroom, selection.Facing.Opposite);
                        Api.World.BlockAccessor.SetBlock(mushroom.Id, selection.Position);
                        Api.World.BlockAccessor.MarkBlockDirty(selection.Position);
                        Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(selection.Position);
                    }
                }
                else
                {
                    Api.Logger.Debug("No open faces around mushroom bag!");
                }
            }

            MarkDirty(true);
        }

        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Inoculated || !byPlayer.Entity.Controls.ShiftKey) return false;

            var slot = byPlayer.Entity.RightHandItemSlot;
            if (slot.Empty) return false;

            if (!slot.Itemstack.Collectible.Code.PathStartsWith("sporeprint")) return false;

            _inoculatedMushroom = slot.Itemstack.Collectible.Variant["mushroom"];
            _colonizeDuration = NextColonizeIncrement();

            slot.TakeOut(1);
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/dirt"), byPlayer.Entity, byPlayer, true, 16);
            
            MarkDirty(true);

            return true;
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            if (byItemStack == null) return;

            _fertility = byItemStack.Attributes.GetFloat("fertility", MaxFertility);
            _elapsedColonizeHours = byItemStack.Attributes.GetDouble("elapsedColonizeHours");
            _colonizeDuration = byItemStack.Attributes.GetDouble("colonizeDuration");
            _inoculatedMushroom = byItemStack.Attributes.GetString("sporetype") ?? string.Empty;

            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (Fertility > 0)
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-remaining-fertility", Fertility / MaxFertility * 100));

            if (!Inoculated) return;

            if (Colonizing)
            {
                var remainingHours = ColonizeDuration - ElapsedColonizeHours;
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-colonizing-hours", remainingHours));
            }
            //else if (Fertility > 0)
            //{
            //    var nextGrowAttempt = _nextGrowHours - Api.World.Calendar.ElapsedHours;
            //    dsc.AppendLine($"Growing again in {nextGrowAttempt:F2} hour(s)");
            //}
        }

        public WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (Inoculated || Fertility <= 0) return Array.Empty<WorldInteraction>();

            return  new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "substrate:fruitingbag-insert-substrate",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = SporePaperStacks(Api)
                }
            };
        }

        internal static Block GetMushroomBlock(IWorldAccessor world, string mushroom, BlockFacing direction)
        {
            var asset = new AssetLocation($"game:mushroom-{mushroom}-normal-{direction.Code}");
            return world.GetBlock(asset);
        }

        public static string ExtractMushroomName(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return null;
            }

            // Match anything after "mushroom-" and before the next "-" or end of string
            var match = Regex.Match(code, @"(?:^|:)mushroom-([^-]+)");

            return match.Success ? match.Groups[1].Value : null;
        }

        private static ItemStack[] SporePaperStacks(ICoreAPI api)
        {
            return ObjectCacheUtil.GetOrCreate(api, "allSporePaperStacks", () =>
                api.World.Collectibles
                    .Where(c => c.Code.PathStartsWith("sporeprint"))
                    .SelectMany(c => c.GetHandBookStacks(api as ICoreClientAPI) ?? Enumerable.Empty<ItemStack>())
                    .ToArray()
            );
        }
    }
}

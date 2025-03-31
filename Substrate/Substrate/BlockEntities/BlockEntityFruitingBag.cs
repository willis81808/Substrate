using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using Substrate.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Substrate.Utils;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Substrate.BlockEntities
{
    public class BlockEntityFruitingBag : BlockEntity
    {
        internal bool Inoculated => !string.IsNullOrWhiteSpace(_inoculatedMushroom);
        internal bool Colonizing => Inoculated && _colonizeDuration > 0 && _elapsedColonizeHours < _colonizeDuration;
        internal float MaxFertility => Block.Attributes["maxfert"]?.AsFloat() ?? 1000;
        internal float GrowChance => Block.Attributes["growchance"]?.AsFloat() ?? 0.1f;
        internal string RefundItem => _refundItem ?? Block.Attributes["refundItem"].AsString();

        internal string[] AcceptableMushrooms =>
            Block.Attributes["acceptablemushrooms"].AsArray<string>() ?? Array.Empty<string>();

        internal double ElapsedColonizeHours => _elapsedColonizeHours;
        internal double ColonizeDuration => _colonizeDuration;

        private string _refundItem = null;
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
            tree.SetString(nameof(_refundItem).ToLower(), _refundItem);
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
            _refundItem = tree.GetString(nameof(_refundItem).ToLower());
            _fertility = tree.GetFloat(nameof(_fertility).ToLower(), MaxFertility);
            _startGrowingHours = tree.GetDouble(nameof(_startGrowingHours).ToLower());
            _elapsedColonizeHours = tree.GetDouble(nameof(_elapsedColonizeHours).ToLower());
            _colonizeDuration = tree.GetDouble(nameof(_colonizeDuration).ToLower());
            _nextGrowHours = tree.GetDouble(nameof(_nextGrowHours).ToLower());
        }

        private int GetOpenFaceCount() => GetGrowFaces().Count();

        private struct GrowFace
        {
            public BlockFacing Facing;
            public Block Block;
            public BlockPos Position;
        }

        private IEnumerable<GrowFace> GetGrowFaces()
        {
            switch (Block)
            {
                case BlockGrowBed:
                {
                    var positions = new []
                    {
                        Pos.UpCopy(),
                        Pos.UpCopy().East(),
                        Pos.UpCopy().South(),
                        Pos.UpCopy().East().South()
                    };
                    return positions.Select(pos => new GrowFace
                    {
                        Facing = BlockFacing.UP,
                        Block = Api.World.BlockAccessor.GetBlock(pos),
                        Position = pos
                    }).Where(b => b.Block.IsAir());
                }
                case BlockFruitingBag:
                    return BlockFacing.HORIZONTALS
                        .Select(facing => new GrowFace
                        {
                            Facing = facing,
                            Block = Api.World.BlockAccessor.GetBlockOnSide(Pos, facing),
                            Position = Pos.Copy().Offset(facing),
                        }).Where(b => b.Block.IsAir());
                default:
                    return Array.Empty<GrowFace>();
            }
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

                var openFaces = GetGrowFaces()
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
            }

            MarkDirty(true);
        }

        public bool OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Inoculated || !byPlayer.Entity.Controls.ShiftKey) return false;

            var slot = byPlayer.Entity.RightHandItemSlot;
            if (slot.Empty) return false;

            if (!slot.Itemstack.Collectible.Code.PathStartsWith("sporeprint")) return false;

            var mushroomVariant = slot.Itemstack.Collectible.Variant["mushroom"];
            if (!AcceptableMushrooms.Contains(mushroomVariant))
            {
                if (Api is ICoreClientAPI capi)
                {
                    var requiredBlockKey = Block is BlockFruitingBag ? "block-growbed" : "block-fruitingbag";
                    var mushroomName = Lang.Get($"substrate:mushroom-{mushroomVariant}");
                    var requiredBlockName = Lang.Get($"substrate:{requiredBlockKey}");
                    capi.TriggerIngameError(
                        this, 
                        "incorrect-growth-medium", 
                        Lang.Get("substrate:notice-cannot-grow-mushroom-with-block", mushroomName, requiredBlockName)
                    );
                }
                return false;
            }

            _inoculatedMushroom = mushroomVariant;
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
            _refundItem = byItemStack.Attributes.GetString("refundItem");

            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            if (Fertility > 0)
            {
                if (Inoculated)
                {
                    var mushroomName = Lang.Get($"substrate:mushroom-{InoculatedMushroom}");
                    dsc.AppendLine(Lang.Get("substrate:fruitingbag-inoculated-with", mushroomName));
                }

                dsc.AppendLine(Lang.Get("substrate:fruitingbag-remaining-fertility", Fertility / MaxFertility * 100));
                if (GetOpenFaceCount() == 0)
                    dsc.AppendLine(Lang.Get("substrate:notice-no-available-grow-spots"));
            }
            else
            {
                dsc.AppendLine(Lang.Get("substrate:notice-moldy"));
            }

            if (Colonizing)
            {
                var remainingHours = ColonizeDuration - ElapsedColonizeHours;
                dsc.AppendLine(Lang.Get("substrate:fruitingbag-colonizing-hours", remainingHours));
            }
        }

        public WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (Inoculated || Fertility <= 0) return Array.Empty<WorldInteraction>();

            return  new[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "substrate:fruitingbag-insert-sporeprint",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = SporePaperStacks(Api)
                }
            };
        }

        internal static Block GetMushroomBlock(IWorldAccessor world, string mushroom, BlockFacing direction)
        {
            return world.GetBlock(new AssetLocation($"game:mushroom-{mushroom}-normal-{direction.Code}")) ??
                   world.GetBlock(new AssetLocation($"game:mushroom-{mushroom}-normal"));
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

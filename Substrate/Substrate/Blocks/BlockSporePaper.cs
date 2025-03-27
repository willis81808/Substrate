using Substrate.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Substrate.Blocks
{
    public class BlockSporePaper : Block
    {
        public BlockSporePaper()
        {
            InteractionHelpYOffset = 0.2f;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return GetBlockEntity<BlockEntitySporePaper>(blockSel)?.OnInteract(byPlayer, blockSel) ?? true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            return GetBlockEntity<BlockEntitySporePaper>(blockSel)
                ?.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }
    }
}

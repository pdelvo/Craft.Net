using Craft.Net.Data.Blocks;
namespace Craft.Net.Data.Items
{
    public class RedstoneItem : Item
    {
        public override ushort Id
        {
            get
            {
                return 331;
            }
        }

        public override void OnItemUsed(World world, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            if (world.GetBlock(clickedBlock + clickedSide) == 0)
                world.SetBlock(clickedBlock + clickedSide, new RedstoneWireBlock());
        }
    }
}

using Craft.Net.Data.Blocks;
namespace Craft.Net.Data.Items
{
    
    public class CauldronItem : Item
    {
        public override ushort Id
        {
            get
            {
                return 380;
            }
        }

        public override void OnItemUsed(World world, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            if (world.GetBlock(clickedBlock + clickedSide) == 0)
                world.SetBlock(clickedBlock + clickedSide, new CauldronBlock());
        }
    }
}

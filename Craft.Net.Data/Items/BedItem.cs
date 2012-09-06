using Craft.Net.Data.Blocks;

namespace Craft.Net.Data.Items
{
    
    public class BedItem : Item
    {
        public override ushort Id
        {
            get
            {
                return 355;
            }
        }

        public override void OnItemUsed(World world, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            if (clickedSide != Vector3.Up)
                return;
            Vector3 away = DataUtility.FowardVector(usedBy);
            var near = world.GetBlock(clickedBlock + clickedSide);
            var far = world.GetBlock(clickedBlock + clickedSide + away);
            if (near is AirBlock && far is AirBlock)
            {
                // Place bed
                world.SetBlock(clickedBlock + clickedSide, new BedBlock(BedBlock.Vector3ToBedDirection(away), false));
                world.SetBlock(clickedBlock + clickedSide + away, new BedBlock(BedBlock.Vector3ToBedDirection(away), true));
            }
        }
    }
}

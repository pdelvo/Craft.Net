//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Craft.Net.Data.Blocks;

namespace Craft.Net.Data.Items
{
    
    public class IronDoorItem : Item
    {
        public override ushort Id
        {
            get
            {
                return 330;
            }
        }

        public override void OnItemUsed(World world, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            if (clickedSide != Vector3.Up)
                return;
            Vector3 away = DataUtility.FowardVector(usedBy, true);
            var near = world.GetBlock(clickedBlock + clickedSide);
            var far = world.GetBlock(clickedBlock + clickedSide + Vector3.Up);
            if (near is AirBlock && far is AirBlock)
            {
                // Place door
                world.SetBlock(clickedBlock + clickedSide,
                    new IronDoorBlock(DoorBlock.Vector3ToDoorDirection(away), false));
                world.SetBlock(clickedBlock + clickedSide + Vector3.Up,
                    new IronDoorBlock(DoorBlock.Vector3ToDoorDirection(away), true));
            }
        }
    }
}

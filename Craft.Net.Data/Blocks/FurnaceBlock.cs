﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Craft.Net.Data.Blocks
{
    public class FurnaceBlock : Block
    {
        public override ushort Id
        {
            get { return 61; }
        }

        public override bool OnBlockRightClicked(Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, World world, Entities.Entity usedBy)
        {
            return false;
        }

        public override bool OnBlockPlaced(World world, Vector3 position, Vector3 clickedBlock, Vector3 clickedSide, Vector3 cursorPosition, Entities.Entity usedBy)
        {
            this.Metadata = (byte)DataUtility.DirectionByRotationFlat(usedBy, true);
            return true;
        }
    }
}

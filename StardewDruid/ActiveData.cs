﻿using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace StardewDruid
{
    internal class ActiveData
    {

        public string activeCast = "none";

        public bool activeCharge = false;

        public string activeKey = null;

        public int chargeAmount = 0;

        public int castLevel = 0;

        //public int animateLevel = 0;

        public int chargeLevel = 0;

        public int cycleLevel = 1;

        public bool castComplete = false;

        public int activeDirection = -1;

        public Vector2 activeVector = new(0,0);

        public Dictionary<string, bool> spawnIndex = new();

    }

}

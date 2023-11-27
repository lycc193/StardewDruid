﻿using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace StardewDruid.Cast.Water
{
    internal class Totem : CastHandle
    {

        public int targetIndex { get; set; }


        public Totem(Vector2 target, Rite rite, int TargetIndex)
            : base(target, rite)
        {

            targetIndex = TargetIndex;

            castCost = 0;
        }

        public override void CastEffect()
        {

            int extractionChance = 1;

            if (!riteData.castTask.ContainsKey("masterTotem"))
            {

                Mod.instance.UpdateTask("lessonTotem", 1);

            }
            else
            {
                extractionChance = randomIndex.Next(1, 3);

            }

            for (int i = 0; i < extractionChance; i++)
            {
                Game1.createObjectDebris(targetIndex, (int)targetVector.X, (int)targetVector.Y - 1);

            }

            castFire = true;

            Vector2 boltVector = new(targetVector.X, targetVector.Y - 2);

            ModUtility.AnimateBolt(targetLocation, boltVector);

            return;

        }

    }

}
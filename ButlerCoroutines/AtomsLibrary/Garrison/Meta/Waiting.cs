using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using Styx;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    public class Waiting : Atom
    {
        private static readonly List<WoWPoint> AllyWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(1866.069, 230.9416, 76.63979), //level 2
            new WoWPoint(1866.069, 230.9416, 76.63979) //level 3
        };

        private static readonly List<WoWPoint> HordeWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(5590.288, 4568.919, 136.1698), //level 2
            new WoWPoint(5585.125, 4565.036, 135.9761) //level 3
        };
        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        
        public Waiting()
        {
            var townHallLevel = BuildingsLua.GetTownHallLevel();
            if (townHallLevel < 1)
                townHallLevel = 1;

            var myFactionWaitingPoints = StyxWoW.Me.IsAlliance ? AllyWaitingPoints : HordeWaitingPoints;

            if (myFactionWaitingPoints[townHallLevel - 1] == new WoWPoint())
            {
                throw new NotImplementedException(
                    "This level of garrison is not supported! Please upgrade at least to level 2 the main building.");
            }
            var r = new Random(DateTime.Now.Second);
            var randomX = (float)(r.NextDouble() - 0.5) * 5;
            var randomY = (float)(r.NextDouble() - 0.5) * 5;

            var waitingSpot = StyxWoW.Me.IsAlliance ? TableAlliance : TableHorde;
            waitingSpot.X = waitingSpot.X + randomX;
            waitingSpot.Y = waitingSpot.Y + randomY;

            Dependencies.Add(new MoveTo(waitingSpot, 3));
        }
        public override bool RequirementsMet()
        {
            return true; 
        }

        public override bool IsFulfilled()
        {
            return Dependencies.TrueForAll(d => d.IsFulfilled());
        }

        public async override Task Action()
        {
            GarrisonButler.Log("You Garrison has been taken care of! Waiting for orders...");
            await Coroutine.Sleep(20000);
        }

        public override string Name()
        {
            return "[Waiting]";
        }
    }
}
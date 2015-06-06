#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    public class MoveTo : Atom
    {
        protected WoWPoint Location;
        private readonly float _precision;

        /// <summary>
        /// Location is cached.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="precision"></param>
        public MoveTo(WoWPoint location, float precision = 0.0f)
        {
            if (location == default(WoWPoint))
            {
                Status = new Result(ActionResult.Failed, "Location is equal to default value.");
                GarrisonButler.Diagnostic("Creating MoveTo Failed Location is equal to default value");
                return;
            }
            GarrisonButler.Diagnostic("Creating MoveTo at {0} (p={1})", location.ToString(), precision);

            Location.X = location.X;
            Location.Y = location.Y;
            Location.Z = location.Z;

            if (Math.Abs(precision) < Navigator.PathPrecision)
                _precision = Navigator.PathPrecision;
            else
                _precision = precision;
        }

        /// <summary>
        /// Can the player move to the destination?
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            //return Navigator.CanNavigateWithin(StyxWoW.Me.Location, Location, _precision);
            return true;
        }

        /// <summary>
        /// Should be within interaction range of the player.
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            //GarrisonButler.Diagnostic("Checking IsFulfilled for MoveTo at {0} (p={1})", Location.ToString(), _precision);
            return StyxWoW.Me.Location.Distance(Location) <= _precision;
        }


        /// <summary>
        /// Will start the navigation system and move within interaction range of the NPC.
        /// </summary>
        /// <returns></returns>
        public override async Task Action()
        {
            //GarrisonButler.Diagnostic("Running Action for MoveTo at {0} (p={1})", Location.ToString(), _precision);
            var lastMoveResult = Navigator.MoveTo(Location);
            Navigator.GetRunStatusFromMoveResult(lastMoveResult);
            switch (lastMoveResult)
            {
                case MoveResult.UnstuckAttempt:
                    GarrisonButler.Diagnostic("MoveResult: UnstuckAttempt.");
                    Status = new Result(ActionResult.Running, "MoveResult: UnstuckAttempt.");
                    return;

                case MoveResult.Failed:
                    GarrisonButler.Diagnostic("MoveResult: Failed.");
                    Status = new Result(ActionResult.Failed, "MoveResult: Failed.");
                    return;

                case MoveResult.ReachedDestination:
                    GarrisonButler.Diagnostic("MoveResult: ReachedDestination.");
                    Status = new Result(ActionResult.Done);
                    return;

                case MoveResult.Moved:
                    //GarrisonButler.Diagnostic("MoveResult: Moved.");
                    Status = new Result(ActionResult.Running);
                    return;

                case MoveResult.PathGenerationFailed:
                    GarrisonButler.Diagnostic("MoveResult: PathGenerationFailed.");
                    Status = new Result(ActionResult.Failed, "MoveResult: PathGenerationFailed.");
                    return;

                case MoveResult.PathGenerated:
                    GarrisonButler.Diagnostic("MoveResult: PathGenerated.");
                    Status = new Result(ActionResult.Running, "MoveResult: PathGenerated");
                    return;
            }

            GarrisonButler.Diagnostic("MoveResult: " + Enum.GetName(typeof(MoveResult), lastMoveResult));
            Status = new Result(ActionResult.Running, "MoveResult: " + Enum.GetName(typeof (MoveResult), lastMoveResult));
        }

        public override string Name()
        {
            return "[MoveTo|" + Location + "]";
        }
    }
}
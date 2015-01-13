﻿#region

using System.Threading.Tasks;
using GarrisonButler.Coroutines;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.Pathing;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static string _oldDestMessage;
        private static MoveResult _lastMoveResult;

// ReSharper disable once CSharpWarnings::CS1998
        public static async Task<ActionResult> MoveTo(WoWPoint destination, string destinationMessage = null)
        {
            if (destinationMessage != null && destinationMessage != _oldDestMessage)
            {
                _oldDestMessage = destinationMessage;
                GarrisonButler.Log(destinationMessage);
            }

            _lastMoveResult = Navigator.MoveTo(destination);

            Navigator.GetRunStatusFromMoveResult(_lastMoveResult);
            switch (_lastMoveResult)
            {
                case MoveResult.UnstuckAttempt:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: UnstuckAttempt.");
                    //await Buddy.Coroutines.Coroutine.Sleep(1000);
                    break;

                case MoveResult.Failed:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: Failed.");
                    return ActionResult.Failed;

                case MoveResult.ReachedDestination:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination.");
                    return ActionResult.Done;

                case MoveResult.Moved:
                    return ActionResult.Running;
            }
            return ActionResult.Running;
        }

        public static async Task<ActionResult> MoveToInteract(WoWObject woWObject)
        {
            if (!woWObject.WithinInteractRange)
                return await MoveTo(woWObject.Location, "[Navigation] Moving to interact with " + woWObject.SafeName);
            GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination to interact with " +
                                      woWObject.SafeName);
            if (Me.IsMoving)
                Navigator.PlayerMover.MoveStop();

            await CommonCoroutines.SleepForLagDuration();

            return ActionResult.Done;
        }

        public static float GetGroundZ(WoWPoint p)
        {
            WoWPoint ground;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + 0.5)), new WoWPoint(p.X, p.Y, (p.Z - 5)),
                TraceLineHitFlags.Collision, out ground);
            return ground != WoWPoint.Empty ? ground.Z : float.MinValue;
        }
    }
}
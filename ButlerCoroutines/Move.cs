﻿#region

using System;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.Pathing;
using Styx.WoWInternals.World;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {
        private static string _oldDestMessage;
        private static MoveResult _lastMoveResult;

// ReSharper disable once CSharpWarnings::CS1998
        public static async Task<Result> MoveTo(WoWPoint destination, string destinationMessage = null)
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
                    return new Result(ActionResult.Failed);

                case MoveResult.ReachedDestination:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination.");
                    return new Result(ActionResult.Done);

                case MoveResult.Moved:
                    return new Result(ActionResult.Running);

                case MoveResult.PathGenerationFailed:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: PathGenerationFailed.");
                    break;

                case MoveResult.PathGenerated:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: PathGenerated");
                    break;

                default:
                    GarrisonButler.Diagnostic("[Navigation] MoveResult: " +
                                              Enum.GetName(typeof (MoveResult), _lastMoveResult));
                    break;
            }
            return new Result(ActionResult.Running);
            //MoveResult.UnstuckAttempt, MoveResult.Moved, MoveResult.PathGenerated
        }

        public static async Task<Result> MoveToInteract(WoWObject woWObject)
        {
            if (!woWObject.WithinInteractRange)
                return await MoveTo(woWObject.Location, "[MoveToInteract] Moving to interact with " + woWObject.SafeName);
            
            if(Me.IsMoving)
            {
                await CommonCoroutines.StopMoving("[MoveToInteract] MoveResult: ReachedDestination to interact with " +
                                      woWObject.SafeName);
                await CommonCoroutines.SleepForLagDuration();
            }

            GarrisonButler.Diagnostic("[MoveToInteract] Reached {0}", woWObject.SafeName);
            return new Result(ActionResult.Done);
        }

        public static float GetGroundZ(WoWPoint p)
        {
            WoWPoint ground;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + 0.5)), new WoWPoint(p.X, p.Y, (p.Z - 5)),
                TraceLineHitFlags.Collision, out ground);
            return ground != WoWPoint.Empty ? ground.Z : Single.MinValue;
        }

        private static async Task<Result> MoveToInteract(WoWPoint cachedToHarvestLocation, float cachedInteractRangeSqr)
        {
            if (Me.Location.Distance(cachedToHarvestLocation) > cachedInteractRangeSqr)
                return
                    await
                        MoveTo(cachedToHarvestLocation,
                            "[Navigation] Moving to interact at cached location " + cachedToHarvestLocation);

            GarrisonButler.Diagnostic("[Navigation] MoveResult: ReachedDestination to interact at cached location " +
                                      cachedToHarvestLocation);
            if (Me.IsMoving)
                Navigator.PlayerMover.MoveStop();

            await CommonCoroutines.SleepForLagDuration();

            return new Result(ActionResult.Done);
        }
    }
}
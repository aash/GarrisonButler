#region

using System;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot.POI;
using Styx.WoWInternals.WoWObjects;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static async Task<bool> HarvestWoWGameOject(WoWGameObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }

            if (node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);

            return true;
        }

        private static WoWPoint CachedToHarvestLocation = WoWPoint.Empty;
        private static async Task<bool> HarvestWoWGameObjectCachedLocation(WoWGameObject toHarvest)
        {
            if (toHarvest == null)
                return false;

            // Original passed in toHarvest object will remain invalid since
            // paramters are not passed by refrence with an async method
            if (toHarvest.IsValid)
            {
                CachedToHarvestLocation = toHarvest.Location;
                return await HarvestWoWGameOject(toHarvest);
            }

            // First moving to cached location
            if (CachedToHarvestLocation != WoWPoint.Empty && (Me.Location.Distance(CachedToHarvestLocation) > 5))
                if (await MoveTo(CachedToHarvestLocation)) // returns false for Failed and ReachedDestination
                    return true;

            CachedToHarvestLocation = WoWPoint.Empty;

            // Get the new game object since the old one will be destroyed
            Tuple<bool, WoWGameObject> mineToGetTuple = CanRunMine();

            toHarvest = mineToGetTuple.Item1 ? mineToGetTuple.Item2 : null;

            // No object found
            if(toHarvest == null)
                return false;

            // Valid object found
            if (toHarvest.IsValid)
            {
                CachedToHarvestLocation = toHarvest.Location;
                return await HarvestWoWGameOject(toHarvest);
            }

            CachedToHarvestLocation = WoWPoint.Empty;

            return false;


            //WoWPoint startLocation = Me.Location;
            //WoWGameObject mineToGet = mineToGetTuple.Item1 ? mineToGetTuple.Item2 : null;
            //System.Diagnostics.Stopwatch stuckWatch = new System.Diagnostics.Stopwatch();
            //stuckWatch.Start();

            //while(true)
            //{
            //    if (mineToGet == null)
            //        return false;

            //    // Handle getting stuck
            //    if(stuckWatch.ElapsedMilliseconds > 10000)
            //    {
            //        if(Me.Location.Distance(startLocation) < 1)
            //        {
            //            GarrisonButler.Diagnostic("[Mine] Appear to be stuck, trying to strafe and move backwards.");
            //            WoWMovement.Move(WoWMovement.MovementDirection.Backwards, TimeSpan.FromSeconds(1));
            //            WoWMovement.Move(WoWMovement.MovementDirection.StrafeLeft, TimeSpan.FromSeconds(1));
            //            WoWMovement.Move(WoWMovement.MovementDirection.Backwards, TimeSpan.FromSeconds(1));
            //            WoWMovement.Move(WoWMovement.MovementDirection.StrafeLeft, TimeSpan.FromSeconds(1));
            //        }

            //        startLocation = Me.Location;
            //        stuckWatch.Reset();
            //    }

            //    // Handle being within 5 yards of mine
            //    if(Me.Location.Distance(mineToGet.Location) < 5)
            //    {
            //        WoWMovement.MoveStop();
            //        await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();
            //        await Buddy.Coroutines.Coroutine.Sleep(200);
            //        break;
            //    }

            //    // Attempt to move to mine
            //    await Coroutine.MoveTo(mineToGet.Location);

            //    // Handle object going invalid during move
            //    if(mineToGet.IsValid == false)
            //    {
            //        mineToGetTuple = CanRunMine();

            //        if (!mineToGetTuple.Item1)
            //            return false;

            //        mineToGet = mineToGetTuple.Item2;
            //    }
            //}

            //return await HarvestWoWGameOject(toHarvest);
        }
    }
}
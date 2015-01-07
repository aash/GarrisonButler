#region

using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static WoWPoint CachedToHarvestLocation = WoWPoint.Empty;

        private static async Task<bool> HarvestWoWGameOject(WoWGameObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }

            if (node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);

            node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
                return false;

            return true;
        }

        private static async Task<bool> HarvestWoWGameObjectCachedLocation(WoWGameObject toHarvest)
        {
            if (toHarvest == null)
                return false;

            // STEP 1 - Attempt to harvest original node
            if (toHarvest.IsValid)
            {
                CachedToHarvestLocation = toHarvest.Location;
                if (await HarvestWoWGameOject(toHarvest))   // returns false if toHarvest becomes null or invalid
                    return true;
            }

            // STEP 2 - Make sure we are within 5 yards of the location of toHarvest
            if (CachedToHarvestLocation != WoWPoint.Empty && (Me.Location.Distance(CachedToHarvestLocation) > 5))
                if (await MoveTo(CachedToHarvestLocation)) // returns false for Failed and ReachedDestination
                    return true;

            // STEP 3 - See if the location of toHarvest still exists in all currently "seen" nodes
            //List<WoWGameObject> mineNodes = GetAllMineNodesIfCanRunMine();

            WoWGameObject foundNodeStillExists =
                GetAllMineNodesIfCanRunMine()
                .GetEmptyIfNull()
                .Where(o => o.Location == CachedToHarvestLocation)
                .FirstOrDefault();

            if (foundNodeStillExists == default(WoWGameObject))
            {
                CachedToHarvestLocation = WoWPoint.Empty;
                return false;
            }

            // STEP 4 - toHarvest still exists, try to harvest
            if (foundNodeStillExists == null)
            {
                CachedToHarvestLocation = WoWPoint.Empty;
                return false;
            }

            if (!foundNodeStillExists.IsValid)
            {
                CachedToHarvestLocation = WoWPoint.Empty;
                return false;
            }

            CachedToHarvestLocation = foundNodeStillExists.Location;
            if (await HarvestWoWGameOject(foundNodeStillExists))
                return true;

            return false;
            // Get the new game object since the old one will be destroyed
            //Tuple<bool, WoWGameObject> mineToGetTuple = CanRunMine();

            //toHarvest = mineToGetTuple.Item1 ? mineToGetTuple.Item2 : null;

            // No object found
            //if (toHarvest == null)
            //    return false;

            // Valid object found
            //if (toHarvest.IsValid)
            //{
            //    CachedToHarvestLocation = toHarvest.Location;
            //    if (await HarvestWoWGameOject(toHarvest))
            //        return true;
            //}

            //CachedToHarvestLocation = WoWPoint.Empty;
            //return false;


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
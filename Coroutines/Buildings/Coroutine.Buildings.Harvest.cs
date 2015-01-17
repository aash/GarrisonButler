#region

using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Coroutines;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static WoWPoint _cachedToHarvestLocation = WoWPoint.Empty;
        private static float _cachedInteractRangeSqr;

        private static async Task<bool> HarvestWoWGameObject(WoWObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }

            if (await MoveToInteract(toHarvest) == ActionResult.Running)
                return true;

            if (node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);

            node = BotPoi.Current.AsObject as WoWGameObject;
            return node != null && node.IsValid;
        }

        private static async Task<ActionResult> HarvestWoWGameObjectCachedLocation(WoWGameObject toHarvest)
        {
            // toHarvest's original value will not be modified inside an async Task
            if (toHarvest == null)
                return ActionResult.Refresh;

            // In case the object has been invalidated too early on the way
            if (!toHarvest.IsValid)
            {
                if (Blacklist.Contains(toHarvest, BlacklistFlags.Node))
                {
                    var harvest = toHarvest;
                    Blacklist.Clear(e =>
                    {
                        if (e.Guid != harvest.Guid) return false;
                        GarrisonButler.Diagnostic("Found a match for blacklist clear.");
                        return true;
                    });
                }
                var toHarvestTemp = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .FirstOrDefault(o => o.Location == _cachedToHarvestLocation);
                if (toHarvestTemp != default(WoWGameObject))
                    toHarvest = toHarvestTemp;
            }

            // STEP 1 - Attempt to harvest original node
            if (toHarvest.IsValid)
            {
                _cachedToHarvestLocation = toHarvest.Location;
                _cachedInteractRangeSqr = toHarvest.InteractRangeSqr;
                if (await HarvestWoWGameObject(toHarvest)) // returns false if toHarvest becomes null or invalid
                {
                    if ((!toHarvest.WithinInteractRange) &&
                        (!(Me.Location.DistanceSqr(_cachedToHarvestLocation) <= _cachedInteractRangeSqr)))
                        return ActionResult.Running;
                    
                    GarrisonButler.Diagnostic("[Harvest] STEP 0: Done with harvesting.");
                    return ActionResult.Running;
                }
            }

            // Check if had been harvested (ie standing within interact range)
            if (Me.Location.DistanceSqr(_cachedToHarvestLocation) <= _cachedInteractRangeSqr)
            {
                GarrisonButler.Diagnostic("[Harvest] STEP 1 - Finished cached harvesting sequence naturally.");
                return ActionResult.Refresh;
            }


            // If we are here, it is either that the bot did something wrong or node too far. 
            // STEP 2 - Make sure we are within interact range of the location of toHarvest
            if (_cachedToHarvestLocation != WoWPoint.Empty &&
                (Me.Location.DistanceSqr(_cachedToHarvestLocation) > _cachedInteractRangeSqr))
                if (await MoveToInteract(_cachedToHarvestLocation, _cachedInteractRangeSqr) == ActionResult.Running)
                    // returns false for Failed and ReachedDestination
                    return ActionResult.Running;


            // If we are here it means we moved to the cache location but at no points
            // on the way the node was discovered again.


            //GarrisonButler.Diagnostic("YYYYYYYYYYYYYY.");

            //if (toHarvest != null)
            //{
            //    toHarvest.Interact();
            //    await CommonCoroutines.SleepForLagDuration();
            //}

            //// STEP 3 - See if the location of toHarvest still exists in all currently "seen" nodes
            ////          Only if Herb or Mine
            //List<WoWGameObject> searchList = null;

            //if (IsWoWObjectMine(toHarvest))
            //    searchList = GetAllMineNodesIfCanRunMine();
            //else if (IsWoWObjectHerbNode(toHarvest))
            //    searchList = GetAllGardenNodesIfCanRunGarden();
            //else if (IsWoWObjectGarrisonCache(toHarvest))
            //    searchList = GetCacheIfCanRunCache();
            //else if (IsWoWObjectFinalizeGarrisonPlot(toHarvest))
            //    searchList = GetAllBuildingsToActivateIfCanActivateAtLeastOneBuilding();
            //else if (IsWoWObjectShipment(toHarvest))
            //    searchList = GetAllShipmentObjectsIfCanRunShipments();
            //else
            //{
            //    // toHarvest.SubType is invalid here since toHarvest.IsValid is false
            //    //switch (toHarvest.SubType)
            //    //{
            //    //    case WoWGameObjectType.GarrisonMonument:
            //    //        break;

            //    //    case WoWGameObjectType.GarrisonMonumentPlaque:
            //    //        break;

            //    //    case WoWGameObjectType.GarrisonShipment:
            //    //        break;
            //    //}
            //}

            //WoWGameObject foundNodeStillExists =
            //    searchList
            //    .GetEmptyIfNull()
            //    .Where(o => o.Location == CachedToHarvestLocation)
            //    .FirstOrDefault();

            //if (foundNodeStillExists == default(WoWGameObject))
            //{
            //    GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node default");
            //    CachedToHarvestLocation = WoWPoint.Empty;
            //    CachedInteractRangeSqr = 0.0f;
            //    return false;
            //}


            //if (foundNodeStillExists == null)
            //{
            //    GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node null");
            //    CachedToHarvestLocation = WoWPoint.Empty;
            //    CachedInteractRangeSqr = 0.0f;
            //    return false;
            //}

            //if (!foundNodeStillExists.IsValid)
            //{
            //    GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node not valid");
            //    CachedToHarvestLocation = WoWPoint.Empty;
            //    CachedInteractRangeSqr = 0.0f;
            //    return false;
            //}

            //// STEP 4 - if toHarvest still exists, try to harvest
            //GarrisonButler.Diagnostic("[Harvest] STEP 4 - Attempt to HarvestWoWGameObject - name="
            //    + foundNodeStillExists.Name
            //    + "; Entry="
            //    + foundNodeStillExists.Entry);
            //CachedToHarvestLocation = foundNodeStillExists.Location;
            //CachedInteractRangeSqr = foundNodeStillExists.InteractRangeSqr;

            //bool harvestReturnValue = await HarvestWoWGameOject(foundNodeStillExists);
            //if (harvestReturnValue)
            //    return true;

            //GarrisonButler.Diagnostic("[Harvest] STEP 4 - HarvestWoWGameObject returned false!");

            //// STEP 0-2 - Check for blacklisted object.
            ////if (Blacklist.Contains(toHarvest, BlacklistFlags.Node))
            ////{
            ////    Blacklist.BlacklistEntry entry = Blacklist.GetEntry(toHarvest);
            ////    GarrisonButler.Diagnostic("[Harvest] Skipping Node {0} at {1} due to blacklist="
            ////        + entry.Flags.ToString(), toHarvest.Name, toHarvest.Location);
            ////    return false;
            ////}

            GarrisonButler.Diagnostic("[Harvest] STEP 5 - Finished");

            return ActionResult.Refresh;
        }
    }
}
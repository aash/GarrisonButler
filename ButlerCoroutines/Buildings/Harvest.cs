#region

using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
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

            if ((await MoveToInteract(toHarvest)).Status == ActionResult.Running)
                return true;

            if (node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);

            node = BotPoi.Current.AsObject as WoWGameObject;
            return node != null && node.IsValid;
        }

        private static async Task<Result> HarvestWoWGameObjectCachedLocation(object obj)
        {
            var toHarvest = obj as WoWGameObject;
            // toHarvest's original value will not be modified inside an async Task
            if (toHarvest == null)
                return new Result(ActionResult.Refresh);

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
                        return new Result(ActionResult.Running);

                    GarrisonButler.Diagnostic("[Harvest] STEP 0: Done with harvesting.");
                    return new Result(ActionResult.Refresh);
                }
            }

            // Check if had been harvested (ie standing within interact range)
            if (Me.Location.DistanceSqr(_cachedToHarvestLocation) <= _cachedInteractRangeSqr)
            {
                GarrisonButler.Diagnostic("[Harvest] STEP 1 - Finished cached harvesting sequence naturally.");
                return new Result(ActionResult.Refresh);
            }


            // If we are here, it is either that the bot did something wrong or node too far. 
            // STEP 2 - Make sure we are within interact range of the location of toHarvest
            if (_cachedToHarvestLocation != WoWPoint.Empty &&
                (Me.Location.DistanceSqr(_cachedToHarvestLocation) > _cachedInteractRangeSqr))
                if ((await MoveToInteract(_cachedToHarvestLocation, _cachedInteractRangeSqr)).Status ==
                    ActionResult.Running)
                    // returns false for Failed and ReachedDestination
                    return new Result(ActionResult.Running);


            // If we are here it means we moved to the cache location but at no points
            // on the way the node was discovered again.

            GarrisonButler.Diagnostic("[Harvest] STEP 5 - Finished");

            return new Result(ActionResult.Refresh);
        }
    }
}
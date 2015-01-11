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
        private static float CachedInteractRangeSqr = 0f;

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
            // toHarvest's original value will not be modified inside an async Task
            if (toHarvest == null)
                return false;
            
            // STEP 1 - Attempt to harvest original node
            if (toHarvest.IsValid)
            {
                CachedToHarvestLocation = toHarvest.Location;
                CachedInteractRangeSqr = toHarvest.InteractRangeSqr;
                if (await HarvestWoWGameOject(toHarvest))   // returns false if toHarvest becomes null or invalid
                    return true;
            }
            // Check if had been harvested (ie standing within interact range)
            if (Me.Location.DistanceSqr(CachedToHarvestLocation) <= CachedInteractRangeSqr)
            {
                GarrisonButler.Diagnostic("[Harvest]Finished cached harvesting sequence naturally.");
                return false;
            }
            // If we are here, it is either that the bot did something wrong or node too far. 
            // STEP 2 - Make sure we are within interact range of the location of toHarvest
            if (CachedToHarvestLocation != WoWPoint.Empty && (Me.Location.DistanceSqr(CachedToHarvestLocation) > CachedInteractRangeSqr))
                if (await MoveTo(CachedToHarvestLocation)) // returns false for Failed and ReachedDestination
                    return true;

            // STEP 3 - See if the location of toHarvest still exists in all currently "seen" nodes
            //          Only if Herb or Mine
            List<WoWGameObject> searchList = null;

            if (IsWoWObjectMine(toHarvest))
                searchList = GetAllMineNodesIfCanRunMine();
            else if (IsWoWObjectHerbNode(toHarvest))
                searchList = GetAllGardenNodesIfCanRunGarden();
            else if (IsWoWObjectGarrisonCache(toHarvest))
                searchList = GetCacheIfCanRunCache();
            else if (IsWoWObjectFinalizeGarrisonPlot(toHarvest))
                searchList = GetAllBuildingsToActivateIfCanActivateAtLeastOneBuilding();
            else if (IsWoWObjectShipment(toHarvest))
                searchList = GetAllShipmentObjectsIfCanRunShipments();
            else
            {
                // toHarvest.SubType is invalid here since toHarvest.IsValid is false
                //switch (toHarvest.SubType)
                //{
                //    case WoWGameObjectType.GarrisonMonument:
                //        break;

                //    case WoWGameObjectType.GarrisonMonumentPlaque:
                //        break;

                //    case WoWGameObjectType.GarrisonShipment:
                //        break;
                //}
            }

            WoWGameObject foundNodeStillExists =
                searchList
                .GetEmptyIfNull()
                .Where(o => o.Location == CachedToHarvestLocation)
                .FirstOrDefault();

            if (foundNodeStillExists == default(WoWGameObject))
            {
                GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node default");
                CachedToHarvestLocation = WoWPoint.Empty;
                CachedInteractRangeSqr = 0.0f;
                return false;
            }

            
            if (foundNodeStillExists == null)
            {
                GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node null");
                CachedToHarvestLocation = WoWPoint.Empty;
                CachedInteractRangeSqr = 0.0f;
                return false;
            }

            if (!foundNodeStillExists.IsValid)
            {
                GarrisonButler.Diagnostic("[Harvest] STEP 3 - Found node not valid");
                CachedToHarvestLocation = WoWPoint.Empty;
                CachedInteractRangeSqr = 0.0f;
                return false;
            }

            // STEP 4 - if toHarvest still exists, try to harvest
            GarrisonButler.Diagnostic("[Harvest] STEP 4 - Attempt to HarvestWoWGameObject - name="
                + foundNodeStillExists.Name
                + "; Entry="
                + foundNodeStillExists.Entry);
            CachedToHarvestLocation = foundNodeStillExists.Location;
            CachedInteractRangeSqr = foundNodeStillExists.InteractRangeSqr;
            if (await HarvestWoWGameOject(foundNodeStillExists))
                return true;

            // STEP 0-2 - Check for blacklisted object.
            if (Blacklist.Contains(toHarvest, BlacklistFlags.Node))
            {
                Blacklist.BlacklistEntry entry = Blacklist.GetEntry(toHarvest);
                GarrisonButler.Diagnostic("[Harvest3] Skipping Node {0} at {1} due to blacklist="
                    + entry.Flags.ToString(), toHarvest.Name, toHarvest.Location);
                return false;
            }

            GarrisonButler.Diagnostic("[Harvest] STEP 5 - Finished");

            return false;
        }
    }
}
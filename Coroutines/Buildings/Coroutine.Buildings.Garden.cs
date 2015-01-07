#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static WoWGameObject CurrentHerbToCollect;

        internal static readonly List<uint> GardenItems = new List<uint>
        {
            235390, // Nagrand Arrowbloom
            235388, // Gorgrond Flytrap
            235376, // Frostweed 
            235389, // Starflower
            235387, // Fireweed
            235391 // Talador Orchid
        };

        private static bool ShouldRunGarden()
        {
            return CanRunGarden().Item1;
        }

        private static Tuple<bool, WoWGameObject> CanRunGarden()
        {
            if (!GaBSettings.Get().HarvestGarden)
            {
                GarrisonButler.Diagnostic("[Garden] Deactivated in user settings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }
            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap.GetEmptyIfNull().Count() < 2
                ? false
                : ShipmentsMap
                    .GetEmptyIfNull()
                    .ElementAt(1) // struct so no need to check for null
                    .buildingIds
                    .GetEmptyIfNull()
                    .Contains(b.id)
                ))
            {
                GarrisonButler.Diagnostic("[Garden] Building not detected in Garrison's Buildings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Is there something to gather? 
            WoWGameObject herbToGather =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => GardenItems.Contains(o.Entry))
                    .OrderBy(o => o.DistanceSqr)
                    .FirstOrDefault();
            if (herbToGather == null)
            {
                GarrisonButler.Diagnostic("[Garden] No herb detected.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonButler.Diagnostic("[Garden] Herb detected at :" + herbToGather.Location);
            return new Tuple<bool, WoWGameObject>(true, herbToGather);
        }

        //public static async Task<bool> CleanGarden()
        //{
        //    WoWGameObject toGather = null;
        //    if (!CanRunGarden(out toGather))
        //        return false;

        //    GarrisonButler.Log("[Garden] Moving to harvest herb at: " + toGather.Location);
        //    return await HarvestWoWGameOject(toGather);
        //}
    }
}
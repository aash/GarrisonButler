using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
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

        private static Tuple<bool,WoWGameObject> CanRunGarden()
        {
            if (!GaBSettings.Get().HarvestGarden)
            {
                GarrisonBuddy.Diagnostic("[Garden] Deactivated in user settings.");
                return new Tuple<bool, WoWGameObject>(false,null);
            }
            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
            {
                GarrisonBuddy.Diagnostic("[Garden] Building not detected in Garrison's Buildings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Is there something to gather? 
            var herbToGather = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().FirstOrDefault(o => GardenItems.Contains(o.Entry));
            if (herbToGather == null)
            {
                GarrisonBuddy.Diagnostic("[Garden] No herb detected.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonBuddy.Diagnostic("[Garden] Herb detected at :" + herbToGather.Location);
            return new Tuple<bool, WoWGameObject>(true,herbToGather);
        }

        //public static async Task<bool> CleanGarden()
        //{
        //    WoWGameObject toGather = null;
        //    if (!CanRunGarden(out toGather))
        //        return false;

        //    GarrisonBuddy.Log("[Garden] Moving to harvest herb at: " + toGather.Location);
        //    return await HarvestWoWGameOject(toGather);
        //}
    }
}
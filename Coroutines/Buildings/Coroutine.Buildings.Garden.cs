using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

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

        private static Tuple<bool,WoWGameObject> CanRunGarden()
        {
            if (!GaBSettings.Get().HarvestGarden)
            {
                GarrisonButler.Diagnostic("[Garden] Deactivated in user settings.");
                return new Tuple<bool, WoWGameObject>(false,null);
            }
            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
            {
                GarrisonButler.Diagnostic("[Garden] Building not detected in Garrison's Buildings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Is there something to gather? 
            var herbToGather = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Where(o => GardenItems.Contains(o.Entry)).OrderBy(o=> o.DistanceSqr).FirstOrDefault();
            if (herbToGather == null)
            {
                GarrisonButler.Diagnostic("[Garden] No herb detected.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonButler.Diagnostic("[Garden] Herb detected at :" + herbToGather.Location);
            return new Tuple<bool, WoWGameObject>(true,herbToGather);
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
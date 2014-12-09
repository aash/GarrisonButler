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
            WoWGameObject herb = null;
            return CanRunGarden(out herb);
        }

        private static bool CanRunGarden(out WoWGameObject herbToGather)
        {
            herbToGather = null;

            if (!GaBSettings.Mono.HarvestGarden)
            {
                GarrisonBuddy.Diagnostic("[Garden] Deactivated in user settings.");
                return false;
            }
            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
            {
                GarrisonBuddy.Diagnostic("[Garden] Building not detected in Garrison's Buildings.");
                return false;
            }

            // Is there something to gather? 
            herbToGather = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GardenItems.Contains(o.Entry));
            if (herbToGather == null)
            {
                GarrisonBuddy.Diagnostic("[Garden] No herb detected.");
                return false;
            }

            GarrisonBuddy.Diagnostic("[Garden] Herb detected at :" + herbToGather.Location);
            return true;
        }

        public static async Task<bool> CleanGarden()
        {
            WoWGameObject toGather = null;
            if (!CanRunGarden(out toGather))
                return false;

            GarrisonBuddy.Log("[Garden] Moving to harvest herb at: " + toGather.Location);
            return await HarvestWoWGameOject(toGather);
        }
    }
}
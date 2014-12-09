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
                return false;

            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
                return false;

            // Is there something to gather? 
            herbToGather = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GardenItems.Contains(o.Entry));
            if (herbToGather == null)
                return false;

            return true;
        }

        public static async Task<bool> CleanGarden()
        {
            WoWGameObject toGather = null;
            if (!CanRunGarden(out toGather))
                return false;

            GarrisonBuddy.Log("Found herb to gather, moving to herb at: " + toGather.Location);

            return await HarvestWoWGameOject(toGather);
        }
    }
}
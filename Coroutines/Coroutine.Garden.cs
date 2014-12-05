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
        internal static readonly List<uint> GardenItems = new List<uint>
        {
            235390, // Nagrand Arrowbloom
            235388, // Gorgrond Flytrap
            235376, // Frostweed 
            235389, // Starflower
            235387, // Fireweed
            235391 // Talador Orchid
        };

        private static WaitTimer _gardenWaitTimer;
        private static bool gardenRunning;

        private static bool IsToDoGarden()
        {
            return _gardenWaitTimer == null || _gardenWaitTimer.IsFinished || gardenRunning;

            if (!GaBSettings.Mono.HarvestGarden)
                return false;

            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
                return false;

            // Is there something to gather? 
            return ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o => GardenItems.Contains(o.Entry));
        }

        public static async Task<bool> CleanGarden()
        {
            if (!GaBSettings.Mono.HarvestGarden ||( _gardenWaitTimer != null && !_gardenWaitTimer.IsFinished && !gardenRunning))
            {
                gardenRunning = false;
                return false;
            } 
            
            if (_gardenWaitTimer == null)
                _gardenWaitTimer = new WaitTimer(TimeSpan.FromMinutes(1));
            _gardenWaitTimer.Reset();

            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap[1].buildingIds.Contains(b.id)))
                return false;
            // Is there something to gather? 
            List<WoWGameObject> herbs =
                ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => GardenItems.Contains(o.Entry)).ToList();
            if (!herbs.Any())
            {
                gardenRunning = false;
                return false;
            }
            gardenRunning = true;
            WoWGameObject itemToCollect = herbs.OrderBy(i => i.Distance).First();

            GarrisonBuddy.Diagnostic("Found herb to gather at: " + itemToCollect.Location);
            if (await MoveTo(itemToCollect.Location))
                return true;

            await Buddy.Coroutines.Coroutine.Sleep(300);
            itemToCollect.Interact();
            //SetLootPoi(itemToCollect);
            await Buddy.Coroutines.Coroutine.Sleep(3500);
            return true;
        }
    }
}
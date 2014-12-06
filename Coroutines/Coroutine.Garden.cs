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

        private static bool CanRunGarden()
        {
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
            if (!CanRunGarden())
                return false;

            List<WoWGameObject> herbs = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => GardenItems.Contains(o.Entry)).ToList();
            WoWGameObject itemToCollect = herbs.OrderBy(i => i.Distance).First();
            GarrisonBuddy.Log("Found herb to gather, moving to herb at: " + itemToCollect.Location);
            if (await MoveTo(itemToCollect.Location))
                return true;

            if (!await Buddy.Coroutines.Coroutine.Wait(500, () => !Me.IsMoving))
            {
                WoWMovement.MoveStop();   
            }

            itemToCollect.Interact();

            await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsCasting); 
            await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();
            await CheckLootFrame();
            return true;
        }
    }
}
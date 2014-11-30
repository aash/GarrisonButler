using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly List<uint> GardenItems = new List<uint>
        {
            235390, // Nagrand Arrowbloom
            235388, // Gorgrond Flytrap
            235376, // Frostweed 
            235389, // Starflower
            235387, // Fireweed
            235391 // Talador Orchid
        };

        public static async Task<bool> CleanGarden()
        {
            if (!GaBSettings.Mono.HarvestGarden)
                return false;

            // Do i have a garden?
            if (!_buildings.Any(b => GardensId.Contains(b.id)))
                return false;
            // Is there something to gather? 
            List<WoWGameObject> herbs =
                ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => GardenItems.Contains(o.Entry)).ToList();
            if (!herbs.Any())
                return false;

            WoWGameObject itemToCollect = herbs.OrderBy(i => i.Distance).First();
            if (await MoveTo(itemToCollect.Location))
                return true;

            itemToCollect.Interact();
            await Buddy.Coroutines.Coroutine.Sleep(3500);
            return true;
        }
    }
}
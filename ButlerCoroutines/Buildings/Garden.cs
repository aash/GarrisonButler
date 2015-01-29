#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
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

        private static async Task<Result> CanRunGarden()
        {
            if (!GaBSettings.Get().HarvestGarden)
            {
                GarrisonButler.Diagnostic("[Garden] Deactivated in user settings.");
                return new Result(ActionResult.Failed);
            }
            // Do i have a garden?
            if (!_buildings.Any(b => ShipmentsMap.GetEmptyIfNull().Count() >= 2 && ShipmentsMap
                .GetEmptyIfNull()
                .ElementAt(1) // struct so no need to check for null
                .BuildingIds
                .GetEmptyIfNull()
                .Contains(b.Id)
                ))
            {
                GarrisonButler.Diagnostic("[Garden] Building not detected in Garrison's Buildings.");
                return new Result(ActionResult.Failed);
            }

            // Is there something to gather? 
            var herbToGather =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => GardenItems.Contains(o.Entry))
                    .OrderBy(o => o.DistanceSqr)
                    .FirstOrDefault();
            if (herbToGather == null)
            {
                GarrisonButler.Diagnostic("[Garden] No herb detected.");
                return new Result(ActionResult.Failed);
            }

            GarrisonButler.Diagnostic("[Garden] Herb detected at :" + herbToGather.Location);
            return new Result(ActionResult.Running, herbToGather);
        }
    }
}
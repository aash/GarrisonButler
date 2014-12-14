using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {

        private static readonly List<int> SalvageCratesIds = new List<int>
        {
            114120, // Big Crate of Salvage lvl 3
            114119, // Crate of Salvage lvl 2
	        114116 // Bag of Salvaged Goods lvl 1
        };

        private static bool ShouldRunSalvage()
        {
            IEnumerable<WoWItem> salvagecrates;
            Building building;
            return CanRunSalvage(out salvagecrates, out building);
        }

        private static bool CanRunSalvage(out IEnumerable<WoWItem> salvageCratesFound, out Building building)
        {
            salvageCratesFound = null;
            building = null;

            if (!GaBSettings.Get().SalvageCrates)
            {
                GarrisonBuddy.Diagnostic("[Salvage] Deactivated in user settings.");
                return false;
            }
            var salvageBuildings = _buildings.Where(b => b.id == 52 || b.id == 140 || b.id == 141);
            if (!salvageBuildings.Any())
            {
                GarrisonBuddy.Diagnostic("[Salvage] No recycle center detected.");
                return false;
            }
            building = salvageBuildings.First();

            salvageCratesFound = Me.BagItems.Where(i => SalvageCratesIds.Contains((int)i.Entry));
            int numSalvageCrates = salvageCratesFound.Count();
            if (numSalvageCrates == 0)
            {
                GarrisonBuddy.Diagnostic("[Salvage] Recycle center detected but no salvage crates detected in bags.");
                return false;
            }

            GarrisonBuddy.Diagnostic("[Salvage] Found Recycle center and salvage crates - #{0}", numSalvageCrates);
            return true;
        }

        private static async Task<bool> DoSalvages()
        {
           IEnumerable<WoWItem> salvageCrates;
            Building building;

            if (!CanRunSalvage(out salvageCrates, out building))
                return false;
            
            WoWUnit unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
            // can't find it? Let's try to get closer to the default location.
            if (unit == null)
            {
                await MoveTo(building.Pnj);
                return true;
            }

            if (await MoveTo(unit.Location))
                return true;

            if (Me.Mounted)
            {
                await CommonCoroutines.Dismount("Salvaging");
                await CommonCoroutines.SleepForLagDuration();
            }
            foreach (WoWItem salvageCrate in salvageCrates)
            {
                salvageCrate.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsCasting);
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return true;
        }
    }
}

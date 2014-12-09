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

        private static bool CanRunSalvage(out IEnumerable<WoWItem> salvageCrates, out Building building)
        {
            salvageCrates = null;
            building = null;

            if (!GaBSettings.Mono.SalvageCrates)
                return false;

            building = _buildings.FirstOrDefault(b => b.id == 52 || b.id == 140 || b.id == 141);
            salvageCrates = Me.BagItems.Where(i => SalvageCratesIds.Contains((int) i.Entry));

            return salvageCrates.Any() && building != null;
        }

        private static async Task<bool> DoSalvages()
        {
           IEnumerable<WoWItem> salvageCrates;
            Building building;

            if (!CanRunSalvage(out salvageCrates, out building))
                return false;
            
            WoWUnit unit = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
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
                await Buddy.Coroutines.Coroutine.Wait(5000, () => Me.IsCasting);
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return true;
        }
    }
}

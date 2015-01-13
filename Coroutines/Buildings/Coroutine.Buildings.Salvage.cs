#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
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
                GarrisonButler.Diagnostic("[Salvage] Deactivated in user settings.");
                return false;
            }
            var salvageBuildings = _buildings.Where(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
            var buildings = salvageBuildings as Building[] ?? salvageBuildings.ToArray();
            if (!buildings.Any())
            {
                GarrisonButler.Diagnostic("[Salvage] No recycle center detected.");
                return false;
            }
            building = buildings.First();

            salvageCratesFound = Me.BagItems.Where(i => SalvageCratesIds.Contains((int) i.Entry));
            var numSalvageCrates = salvageCratesFound.Count();
            if (numSalvageCrates == 0)
            {
                GarrisonButler.Diagnostic("[Salvage] Recycle center detected but no salvage crates detected in bags.");
                return false;
            }

            GarrisonButler.Diagnostic("[Salvage] Found Recycle center and salvage crates - #{0}", numSalvageCrates);
            return true;
        }

        private static async Task<ActionResult> DoSalvages()
        {
            IEnumerable<WoWItem> salvageCrates;
            Building building;

            if (!CanRunSalvage(out salvageCrates, out building))
                return ActionResult.Done;

            var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
            // can't find it? Let's try to get closer to the default location.
            if (unit == null)
            {
                await MoveTo(building.Pnj, "[Salvage] Moving to building at " + building.Pnj);
                return ActionResult.Running;
            }

            // If we don't dismount earlier, the bot will determine that it has reached the
            // unit when it is within 2 yards of the location.  Need to stop and dismount earlier,
            // then call "MoveTo" again after the dismount logic to finish the movement by foot
            if (Me.Location.Distance(unit.Location) > 10)
                if (await MoveTo(unit.Location) == ActionResult.Running)
                    return ActionResult.Running;

            if (Me.Mounted)
            {
                await CommonCoroutines.Dismount("Salvaging");
                await CommonCoroutines.SleepForLagDuration();
            }

            if (await MoveTo(unit.Location) == ActionResult.Running)
                return ActionResult.Running;

            foreach (var salvageCrate in salvageCrates)
            {
                salvageCrate.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsCasting);
                await Buddy.Coroutines.Coroutine.Yield();
            }
            return ActionResult.Running;
        }
    }
}
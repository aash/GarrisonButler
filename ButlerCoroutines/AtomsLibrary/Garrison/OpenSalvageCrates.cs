using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.Config;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    //internal class OpenSalvageCrates : OpenAllContainers
    //{
    //    private static List<uint> _cratesIds = new List<uint>()
    //        {
    //            114120, // Big Crate of Salvage lvl 3
    //            114119, // Crate of Salvage lvl 2
    //            114116 // Bag of Salvaged Goods lvl 1
    //        }; 
    //    public OpenSalvageCrates()
    //        : base(_cratesIds, )
    //    {
    //        // DEPENDENCY
    //        var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
    //        // can't find it? Let's try to get closer to the default location.
    //        if (unit == null)
    //        {
    //            await MoveTo(building.Pnj, "[Salvage] Moving to building at " + building.Pnj);
    //            return;
    //            return new Result(ActionResult.Running);
    //        }

    //        // If we don't dismount earlier, the bot will determine that it has reached the
    //        // unit when it is within 2 yards of the location.  Need to stop and dismount earlier,
    //        // then call "MoveTo" again after the dismount logic to finish the movement by foot
    //        if (Me.Location.Distance(unit.Location) > 10)
    //            if ((await MoveTo(unit.Location)).State == ActionResult.Running)
    //                return new Result(ActionResult.Running);
    //    }


    //    /// <summary>
    //    /// Test if the local player has a salvage yard
    //    /// </summary>
    //    /// <returns></returns>
    //    public override bool RequirementsMet()
    //    {
    //        if (!GaBSettings.Get().SalvageCrates)
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] Deactivated in user settings.");
    //            return false;
    //        }

    //        var salvageBuildings = ButlerCoroutine._buildings.Where(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
    //        var buildings = salvageBuildings as Building[] ?? salvageBuildings.ToArray();
    //        if (!buildings.Any())
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] No recycle center detected.");
    //            return false;
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    /// Test if the player has anything to salvage in bags
    //    /// </summary>
    //    /// <returns></returns>
    //    public override bool IsFulfilled()
    //    {
    //        var salvageCratesFound = GetSalvageCrates();
    //        var numSalvageCrates = salvageCratesFound.Count();
    //        if (numSalvageCrates == 0)
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] No crates to salvage.");
    //            return true;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Salvage crate
    //    /// </summary>
    //    /// <returns></returns>
    //    public override async Task Action()
    //    {

    //        var building = GetSalvageBuilding();
    //        if (building == null)
    //        {
    //            // FAILED
    //            return;
    //        }


    //        if (StyxWoW.Me.Mounted)
    //        {
    //            await CommonCoroutines.Dismount("Salvaging");
    //            await CommonCoroutines.SleepForLagDuration();
    //        }

    //        var salvageCratesFound = GetSalvageCrates();
    //        foreach (var salvageCrate in salvageCratesFound)
    //        {
    //            salvageCrate.UseContainerItem();
    //            await CommonCoroutines.SleepForLagDuration();
    //            await Coroutine.Wait(5000, () => !StyxWoW.Me.IsCasting);
    //            await Coroutine.Yield();
    //        }
    //        return;
    //    }

    //    private static Building GetSalvageBuilding()
    //    {
    //        return ButlerCoroutine._buildings.FirstOrDefault(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
    //    }

    //    private static IEnumerable<WoWItem> GetSalvageCrates()
    //    {
    //        var cratesIds = new List<uint>()
    //        {
    //            114120, // Big Crate of Salvage lvl 3
    //            114119, // Crate of Salvage lvl 2
    //            114116 // Bag of Salvaged Goods lvl 1
    //        };

    //        return HbApi.GetItemsInBags(cratesIds);
    //    }
    //}







    //internal class OpenSalvageCrates : Atom
    //{
    //    public OpenSalvageCrates()
    //    {
    //        // DEPENDENCY
    //        var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
    //        // can't find it? Let's try to get closer to the default location.
    //        if (unit == null)
    //        {
    //            await MoveTo(building.Pnj, "[Salvage] Moving to building at " + building.Pnj);
    //            return;
    //            return new Result(ActionResult.Running);
    //        }

    //        // If we don't dismount earlier, the bot will determine that it has reached the
    //        // unit when it is within 2 yards of the location.  Need to stop and dismount earlier,
    //        // then call "MoveTo" again after the dismount logic to finish the movement by foot
    //        if (Me.Location.Distance(unit.Location) > 10)
    //            if ((await MoveTo(unit.Location)).State == ActionResult.Running)
    //                return new Result(ActionResult.Running);
    //    }


    //    /// <summary>
    //    /// Test if the local player has a salvage yard
    //    /// </summary>
    //    /// <returns></returns>
    //    public override bool RequirementsMet()
    //    {
    //        if (!GaBSettings.Get().SalvageCrates)
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] Deactivated in user settings.");
    //            return false;
    //        }

    //        var salvageBuildings = ButlerCoroutine._buildings.Where(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
    //        var buildings = salvageBuildings as Building[] ?? salvageBuildings.ToArray();
    //        if (!buildings.Any())
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] No recycle center detected.");
    //            return false;
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    /// Test if the player has anything to salvage in bags
    //    /// </summary>
    //    /// <returns></returns>
    //    public override bool IsFulfilled()
    //    {
    //        var salvageCratesFound = GetSalvageCrates();
    //        var numSalvageCrates = salvageCratesFound.Count();
    //        if (numSalvageCrates == 0)
    //        {
    //            GarrisonButler.Diagnostic("[SalvageCrates] No crates to salvage.");
    //            return true;
    //        }
    //        return false;
    //    }

    //    /// <summary>
    //    /// Salvage crate
    //    /// </summary>
    //    /// <returns></returns>
    //    public override async Task Action()
    //    {

    //        var building = GetSalvageBuilding();
    //        if (building == null)
    //        {
    //            // FAILED
    //            return;
    //        }


    //        if (StyxWoW.Me.Mounted)
    //        {
    //            await CommonCoroutines.Dismount("Salvaging");
    //            await CommonCoroutines.SleepForLagDuration();
    //        }

    //        var salvageCratesFound = GetSalvageCrates();
    //        foreach (var salvageCrate in salvageCratesFound)
    //        {
    //            salvageCrate.UseContainerItem();
    //            await CommonCoroutines.SleepForLagDuration();
    //            await Coroutine.Wait(5000, () => !StyxWoW.Me.IsCasting);
    //            await Coroutine.Yield();
    //        }
    //        return;
    //    }

    //    private static Building GetSalvageBuilding()
    //    {
    //        return ButlerCoroutine._buildings.FirstOrDefault(b => b.Id == 52 || b.Id == 140 || b.Id == 141);
    //    }

    //    private static IEnumerable<WoWItem> GetSalvageCrates()
    //    {
    //        var cratesIds = new List<uint>()
    //        {
    //            114120, // Big Crate of Salvage lvl 3
    //            114119, // Crate of Salvage lvl 2
    //            114116 // Bag of Salvaged Goods lvl 1
    //        };

    //        return HbApi.GetItemsInBags(cratesIds);
    //    }
    //}
}

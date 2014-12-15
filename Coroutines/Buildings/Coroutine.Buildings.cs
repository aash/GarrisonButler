using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly WaitTimer RefreshBuildingsTimer = new WaitTimer(TimeSpan.FromMinutes(5));

        internal static readonly List<uint> FinalizeGarrisonPlotIds = new List<uint>
        {
            231217,
            231964,
            233248,
            233249,
            233250,
            233251,
            232651,
            232652,
            236261,
            236262,
            236263,
            236175,
            236176,
            236177,
            236185,
            236186,
            236187,
            236188,
            236190,
            236191,
            236192,
            236193,
        };

        private static List<ActionBasic> BuildingsActions; 
        public static ActionsSequence InitializeBuildingsCoroutines()
        {
            
            // Initializing coroutines
            GarrisonBuddy.Diagnostic("Initialization Buildings coroutines...");
            var buildingsActionsSequence = new ActionsSequence();

            var mine = _buildings.FirstOrDefault(
                 b =>
                     (b.id == (int)buildings.MineLvl1) || (b.id == (int)buildings.MineLvl2) ||
                     (b.id == (int)buildings.MineLvl3));
            var garden = _buildings.FirstOrDefault(
                 b =>
                     (b.id == (int)buildings.GardenLvl1) || (b.id == (int)buildings.GardenLvl2) ||
                     (b.id == (int)buildings.GardenLvl3));
            if (mine != null)
            {
                // Harvest mine
                buildingsActionsSequence.AddAction(
                    new ActionOnTimer<WoWGameObject>(
                        HarvestWoWGameOject,
                        CanRunMine,
                        1000,
                        false,
                        new ActionOnTimer<WoWItem>(
                            UseItemInbags,
                            () =>
                            {
                                var canUse = CanUseItemInBags(MinersCofeeItemId, MinersCofeeAura, 2)();
                                return new Tuple<bool, WoWItem>(canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseCoffee, canUse.Item2);
                            }),
                        new ActionOnTimer<WoWItem>(
                            UseItemInbags,
                            () =>
                            {
                                var canUse = CanUseItemInBags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1)();
                                return new Tuple<bool, WoWItem>(canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseMiningPick, canUse.Item2);
                            })));

                // Take care of mine shipments
                buildingsActionsSequence.AddAction(
                    new ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                        PickUpOrStartAtLeastOneShipment, () => CanPickUpOrStartAtLeastOneShipmentAt(mine)));
            }
            if (garden != null)
            {
                // Harvest garden
                buildingsActionsSequence.AddAction(
                    new ActionOnTimer<WoWGameObject>(HarvestWoWGameOject, CanRunGarden));

                // Take care of garden shipments
                buildingsActionsSequence.AddAction(
                    new ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                        PickUpOrStartAtLeastOneShipment, () => CanPickUpOrStartAtLeastOneShipmentAt(garden)));
            }
            // Take care of all shipments
            buildingsActionsSequence.AddAction(
                new ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                    PickUpOrStartAtLeastOneShipment, CanPickUpOrStartAtLeastOneShipmentFromAll));

                        // Garrison cache
            buildingsActionsSequence.AddAction(
                new ActionOnTimer<WoWGameObject>(PickUpGarrisonCache, CanRunCache));

                        // Buildings activation
            buildingsActionsSequence.AddAction(
                new ActionOnTimer<WoWGameObject>(ActivateFinishedBuildings, CanActivateAtLeastOneBuilding));
        
            GarrisonBuddy.Diagnostic("Initialization Buildings done!");
            return buildingsActionsSequence;
        }

        private static async Task<bool> DoBuildingRelated()
        {
            if (BuildingsActions == null)
            {
                GarrisonBuddy.Warning("[Buildings] Buildings actions not initialized!");
                return false;
            }

            foreach (var action in BuildingsActions)
            {
                if (await action.ExecuteAction())
                    return true;
            }

            return false;
        }

        private static void RefreshBuildings(bool forced = false)
        {
            if (!RefreshBuildingsTimer.IsFinished && _buildings != null && !forced) return;

            GarrisonBuddy.Log("Refreshing Buildings and shipments databases.");
            _buildings = BuildingsLua.GetAllBuildings();
            RefreshBuildingsTimer.Reset();
        }



        internal static Tuple<bool, WoWGameObject> CanActivateAtLeastOneBuilding()
        {
            if (!GaBSettings.Get().ActivateBuildings)
                return new Tuple<bool, WoWGameObject>(false,null);

            IOrderedEnumerable<WoWGameObject> allToActivate =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .ToList()
                    .OrderBy(o => o.Location.X);
            if (!allToActivate.Any())
            {
                return new Tuple<bool, WoWGameObject>(false, null);
            }
            WoWGameObject toActivate = allToActivate.First();
            GarrisonBuddy.Log("Found building to activate(" + toActivate.Name + "), moving to building.");
            GarrisonBuddy.Diagnostic("Building  " + toActivate.SafeName + " - " + toActivate.Entry + " - " +
                                     toActivate.DisplayId + ": " + toActivate.Location);
            return new Tuple<bool, WoWGameObject>(true, toActivate);
        }

        private static async Task<bool> ActivateFinishedBuildings(WoWGameObject toActivate)
        {
            if (await MoveTo(toActivate.Location, "Building activation"))
                return true;

            await Buddy.Coroutines.Coroutine.Sleep(300);
            toActivate.Interact();
            GarrisonBuddy.Log("Activating " + toActivate.SafeName + ", waiting...");
            if (await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                toActivate.Interact();
                return
                    !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .Any(o => FinalizeGarrisonPlotIds.Contains(o.Entry) && o.Guid == toActivate.Guid);
            }))
            {
                GarrisonBuddy.Warning("Failed to activate building: " + toActivate.Name);
            }
            return true;
        }
    }
}
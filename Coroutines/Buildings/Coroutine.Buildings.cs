#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using GarrisonLua;
using Styx;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
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

        private static List<ActionHelpers.ActionBasic> BuildingsActions;

        public static ActionHelpers.ActionsSequence InitializeBuildingsCoroutines()
        {
            // Initializing coroutines
            GarrisonButler.Diagnostic("Initialization Buildings coroutines...");
            var buildingsActionsSequence = new ActionHelpers.ActionsSequence();

            Building mine = _buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.id == (int) buildings.MineLvl1) || (b.id == (int) buildings.MineLvl2) ||
                    (b.id == (int) buildings.MineLvl3));

            Building garden = _buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.id == (int) buildings.GardenLvl1) || (b.id == (int) buildings.GardenLvl2) ||
                    (b.id == (int) buildings.GardenLvl3));

            if (mine != default(Building))
            {
                // Harvest mine
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimer<WoWGameObject>(
                        HarvestWoWGameObjectCachedLocation,
                        CanRunMine,
                        1000,
                        false,
                        true,
                        // Drink coffee
                        new ActionHelpers.ActionOnTimer<WoWItem>(
                            UseItemInbags,
                            () =>
                            {
                                Tuple<bool, WoWItem> canUse = CanUseItemInBags(MinersCofeeItemId, MinersCofeeAura, 1)();
                                return
                                    new Tuple<bool, WoWItem>(
                                        canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseCoffee, canUse.Item2);
                            }),
                        // Use Mining Pick 
                        new ActionHelpers.ActionOnTimer<WoWItem>(
                            UseItemInbags,
                            () =>
                            {
                                Tuple<bool, WoWItem> canUse =
                                    CanUseItemInBags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1)();
                                return
                                    new Tuple<bool, WoWItem>(
                                        canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseMiningPick, canUse.Item2);
                            }),
                        // Delete Coffee 
                        new ActionHelpers.ActionOnTimer<WoWItem>(
                            DeleteItemInbags,
                            () =>
                            {
                                Tuple<bool, WoWItem> tooMany =
                                    TooManyItemInBags(MinersCofeeItemId, 5)();
                                return
                                    new Tuple<bool, WoWItem>(
                                        tooMany.Item1 && GaBSettings.Get().DeleteCoffee, tooMany.Item2);
                            }),
                        // Delete Mining Pick 
                        new ActionHelpers.ActionOnTimer<WoWItem>(
                            DeleteItemInbags,
                            () =>
                            {
                                Tuple<bool, WoWItem> tooMany =
                                    TooManyItemInBags(PreserverdMiningPickItemId, 5)();
                                return
                                    new Tuple<bool, WoWItem>(
                                        tooMany.Item1 && GaBSettings.Get().DeleteMiningPick, tooMany.Item2);
                            })));

                // Take care of mine shipments
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                        PickUpOrStartAtLeastOneShipment, () => CanPickUpOrStartAtLeastOneShipmentAt(mine)));
            }
            if (garden != null)
            {
                // Harvest garden
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimer<WoWGameObject>(HarvestWoWGameOject, CanRunGarden));

                // Take care of garden shipments
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                        PickUpOrStartAtLeastOneShipment, () => CanPickUpOrStartAtLeastOneShipmentAt(garden)));
            }
            // Take care of all shipments
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer<Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                    PickUpOrStartAtLeastOneShipment, CanPickUpOrStartAtLeastOneShipmentFromAll));

            // Garrison cache
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer<WoWGameObject>(PickUpGarrisonCache, CanRunCache));

            // Buildings activation
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer<WoWGameObject>(ActivateFinishedBuildings, CanActivateAtLeastOneBuilding));

            GarrisonButler.Diagnostic("Initialization Buildings done!");
            return buildingsActionsSequence;
        }

        private static async Task<bool> DoBuildingRelated()
        {
            if (BuildingsActions == null)
            {
                GarrisonButler.Warning("[Buildings] Buildings actions not initialized!");
                return false;
            }

            foreach (ActionHelpers.ActionBasic action in BuildingsActions)
            {
                if (await action.ExecuteAction())
                    return true;
            }

            return false;
        }

        private static void RefreshBuildings(bool forced = false)
        {
            if (!RefreshBuildingsTimer.IsFinished && _buildings != null && !forced) return;

            GarrisonButler.Log("Refreshing Buildings database.");

            _buildings = BuildingsLua.GetAllBuildings();
            RefreshBuildingsTimer.Reset();
        }


        internal static Tuple<bool, WoWGameObject> CanActivateAtLeastOneBuilding()
        {
            if (!GaBSettings.Get().ActivateBuildings)
                return new Tuple<bool, WoWGameObject>(false, null);

            IOrderedEnumerable<WoWGameObject> allToActivate =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .ToList()
                    .OrderBy(o => o.Location.X);

            if (!allToActivate.Any())
            {
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            WoWGameObject toActivate = allToActivate.First();

            GarrisonButler.Log("Found building to activate(" + toActivate.Name + "), moving to building.");
            GarrisonButler.Diagnostic("Building  " + toActivate.SafeName + " - " + toActivate.Entry + " - " +
                                      toActivate.DisplayId + ": " + toActivate.Location);

            return new Tuple<bool, WoWGameObject>(true, toActivate);
        }

        private static async Task<bool> ActivateFinishedBuildings(WoWGameObject toActivate)
        {
            if (await MoveToInteract(toActivate, "Building activation"))
                return true;

            await Buddy.Coroutines.Coroutine.Sleep(300);
            toActivate.Interact();

            GarrisonButler.Log("Activating " + toActivate.SafeName + ", waiting...");

            if (await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                toActivate.Interact();
                return
                    !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .GetEmptyIfNull()
                        .Any(o => FinalizeGarrisonPlotIds.Contains(o.Entry) && o.Guid == toActivate.Guid);
            }))
            {
                GarrisonButler.Warning("Failed to activate building: " + toActivate.Name);
            }
            return true;
        }
    }
}
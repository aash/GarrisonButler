#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
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
            236193
        };

        public static ActionHelpers.ActionsSequence InitializeBuildingsCoroutines()
        {
            // Initializing coroutines
            GarrisonButler.Diagnostic("Initialization Buildings coroutines...");
            var buildingsActionsSequence = new ActionHelpers.ActionsSequence();

            var mine = _buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl1) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl2) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl3));

            var garden = _buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.Id == (int) global::GarrisonButler.Buildings.GardenLvl1) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.GardenLvl2) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.GardenLvl3));

            if (mine != default(Building))
            {
                // Harvest mine
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimerCached(
                        HarvestWoWGameObjectCachedLocation,
                        CanRunMine,
                        5000,
                        100,
                        // Drink coffee
                        new ActionHelpers.ActionOnTimer(
                            UseItemInbags,
                            async () =>
                            {
                                var canUse = CanUseItemInBags(MinersCofeeItemId, MinersCofeeAura, 1)();
                                return
                                    new Result(canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseCoffee
                                        ? ActionResult.Running : ActionResult.Failed, canUse.Item2);
                            }, 10000, 3000),
                        // Use Mining Pick 
                        new ActionHelpers.ActionOnTimer(
                            UseItemInbags,
                            async () =>
                            {
                                var canUse =
                                    CanUseItemInBags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1)();
                                return
                                    new Result(
                                        canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseMiningPick
                                        ? ActionResult.Running : ActionResult.Failed, canUse.Item2);
                            }, 10000, 3000),
                        // Delete Coffee 
                        new ActionHelpers.ActionOnTimer(
                            DeleteItemInbags,
                            async () =>
                            {
                                var tooMany =
                                    TooManyItemInBags(MinersCofeeItemId, 5)();
                                return
                                    new Result(
                                        tooMany.Item1 && GaBSettings.Get().DeleteCoffee
                                        ? ActionResult.Running : ActionResult.Failed, tooMany.Item2);
                            }, 10000, 3000),
                        // Delete Mining Pick 
                        new ActionHelpers.ActionOnTimer(
                            DeleteItemInbags,
                            async () =>
                            {
                                var tooMany =
                                    TooManyItemInBags(PreserverdMiningPickItemId, 5)();
                                return
                                    new Result(
                                        tooMany.Item1 && GaBSettings.Get().DeleteMiningPick
                                        ? ActionResult.Running : ActionResult.Failed, tooMany.Item2);
                            }, 10000, 30000)));

                // Take care of mine shipments
                buildingsActionsSequence.AddAction(PickUpOrStartSequence(mine));
            }
            if (garden != default(Building))
            {
                // Harvest garden
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation,
                        CanRunGarden, 5000));

                // Take care of garden shipments
                buildingsActionsSequence.AddAction(PickUpOrStartSequence(garden));
            }
            // Take care of all shipments
            buildingsActionsSequence.AddAction(PickUpOrStartSequenceAll());

            // Garrison cache
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation, CanRunCache,
                    10000));

            // Buildings activation
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation,
                    CanActivateAtLeastOneBuilding, 10000));

            GarrisonButler.Diagnostic("Initialization Buildings done!");
            return buildingsActionsSequence;
        }

        private static void RefreshBuildings(bool forced = false)
        {
            if (!RefreshBuildingsTimer.IsFinished && !_buildings.IsNullOrEmpty() && !forced) return;

            GarrisonButler.Log("Refreshing Buildings database.");

            _buildings = BuildingsLua.GetAllBuildings();
            RefreshBuildingsTimer.Reset();
        }


        internal static async Task<Result> CanActivateAtLeastOneBuilding()
        {
            if (!GaBSettings.Get().ActivateBuildings)
                return new Result(ActionResult.Failed);

            var allToActivate =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .OrderBy(o => o.Location.X);

            if (!allToActivate.Any())
                return new Result(ActionResult.Failed);


            var toActivate = allToActivate.First();

            GarrisonButler.Log("Found building to activate(" + toActivate.Name + "), moving to building.");
            GarrisonButler.Diagnostic("Building  " + toActivate.SafeName + " - " + toActivate.Entry + " - " +
                                      toActivate.DisplayId + ": " + toActivate.Location);

            return new Result(ActionResult.Running, toActivate);
        }
    }
}
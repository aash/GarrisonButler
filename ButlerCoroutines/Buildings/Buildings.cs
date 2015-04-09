#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
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

        public static ActionHelpers.ActionsSequence InitializeMineAndGarden()
        {
            GarrisonButler.Diagnostic("Initialization Mine and Garden coroutines...");
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
                        1000,
                        15000,
                        // Drink coffee
                        new ActionHelpers.ActionOnTimer(
                            UseItemInbags,
                            async () =>
                            {
                                var canUse = CanUseItemInBags(MinersCofeeItemId, MinersCofeeAura, 5)();
                                return
                                    new Result(canUse.Item1 && MeIsInMine() && GaBSettings.Get().UseCoffee
                                        ? ActionResult.Running
                                        : ActionResult.Failed, canUse.Item2);
                            }, 5000, 3000),
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
                                            ? ActionResult.Running
                                            : ActionResult.Failed, canUse.Item2);
                            }, 5000, 3000)));

                // Take care of mine shipments
                buildingsActionsSequence.AddAction(PickUpOrStartSequence(mine));
            }
            if (garden != default(Building))
            {
                // Harvest garden
                buildingsActionsSequence.AddAction(
                    new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation,
                        CanRunGarden, 1000, 10000));

                // Take care of garden shipments
                buildingsActionsSequence.AddAction(PickUpOrStartSequence(garden));
            }
            return buildingsActionsSequence;
        }

        public static ActionHelpers.ActionsSequence InitializeBuildingsCoroutines()
        {
            // Initializing coroutines
            GarrisonButler.Diagnostic("Initialization Buildings coroutines...");
            var buildingsActionsSequence = new ActionHelpers.ActionsSequence();

            // Buildings activation
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation,
                    CanActivateAtLeastOneBuilding, 10000));

            // Take care of all shipments
            buildingsActionsSequence.AddAction(PickUpOrStartSequenceAll());

            // Garrison cache
            buildingsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimerCached(HarvestWoWGameObjectCachedLocation, CanRunCache,
                    10000));

            GarrisonButler.Diagnostic("Initialization Buildings done!");
            return buildingsActionsSequence;
        }

        public static void RefreshBuildings(bool forced = false)
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
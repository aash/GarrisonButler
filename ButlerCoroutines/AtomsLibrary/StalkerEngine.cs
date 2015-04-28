#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.ButlerCoroutines.AtomsLibrary;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

//namespace GarrisonButler.ButlerCoroutines
//{
//     <summary>
//     The stalker is checking what is needed to be done and add it with the correct priority to the sequencer
//     </summary>
//    internal class StalkerEngine
//    {
//        private static StalkerEngine _instance;
//        private readonly WaitTimer _timer;

//        public static StalkerEngine Instance
//        {
//            get
//            {
//                if (_instance != null)
//                    return _instance;
//                _instance = new StalkerEngine();
//                return _instance;
//            }
//        }

//        private StalkerEngine()
//        {
//            _timer = new WaitTimer(TimeSpan.FromMilliseconds(1000));
//            _timerMine = new WaitTimer(TimeSpan.FromSeconds(10));
//        }

//        public void Stalk()
//        {
//            if (!_timer.IsFinished)
//                return;

//            StalkMineItems(-100);
//            StalkOrdersMinePickUp(-99);
//            StalkOrdersMineStart(-98);
//            StalkGardenItems(-97);
//            StalkOrdersGardenPickUp(-96);
//            StalkOrdersGardenStart(-95);
//            _timer.Reset();
//        }

//        private void StalkOrdersGardenPickUp(int priority)
//        {
//            var garden = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
//                b =>
//                    (b.Id == (int)Buildings.GardenLvl1) ||
//                    (b.Id == (int)Buildings.GardenLvl2) ||
//                    (b.Id == (int)Buildings.GardenLvl3));
//            if (garden != default(Building))
//            {
//                StalkOrdersPickUp(garden, priority);
//            }
//        }
//        private void StalkOrdersGardenStart(int priority)
//        {
//            var garden = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
//                b =>
//                    (b.Id == (int)Buildings.GardenLvl1) ||
//                    (b.Id == (int)Buildings.GardenLvl2) ||
//                    (b.Id == (int)Buildings.GardenLvl3));
//            if (garden != default(Building))
//            {
//                StalkOrdersStart(garden, priority);
//            }
//        }
//        private void StalkOrdersMinePickUp(int priority)
//        {
//            var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
//                b =>
//                    (b.Id == (int)Buildings.MineLvl1) ||
//                    (b.Id == (int)Buildings.MineLvl2) ||
//                    (b.Id == (int)Buildings.MineLvl3));
//            if (mine != default(Building))
//            {
//                StalkOrdersPickUp(mine, priority);
//            }
//        }
//        private void StalkOrdersMineStart(int priority)
//        {
//            var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
//                b =>
//                    (b.Id == (int)Buildings.MineLvl1) ||
//                    (b.Id == (int)Buildings.MineLvl2) ||
//                    (b.Id == (int)Buildings.MineLvl3));
//            if (mine != default(Building))
//            {
//                StalkOrdersStart(mine, priority);
//            }
//        }
//        private void StalkOrdersStart(Building building, int priority)
//        {
//            if (building == null)
//            {
//                GarrisonButler.Diagnostic(
//                    "[ShipmentStart] Building is null, either not built or not properly scanned.");
//                return;
//            }

//            building.Refresh();
//            var buildingsettings = GaBSettings.Get().GetBuildingSettings(building.Id);
//            if (buildingsettings == null)
//                return;

//            // Activated by user ?
//            if (!buildingsettings.CanStartOrder)
//            {
//                GarrisonButler.Diagnostic("[ShipmentStart,{0}] Deactivated in user settings: {1}", building.Id,
//                    building.Name);
//                return;
//            }

//            if (building.WorkFrameWorkAroundTries >= Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
//            {
//                GarrisonButler.Warning(
//                    "[ShipmentStart,{0}] Building has been blacklisted due to reaching maximum Blizzard Workframe Bug workaround tries ({1})",
//                    building.Id, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
//                return;
//            }

//            // No Shipment left to start
//            if (building.NumberShipmentLeftToStart <= 0)
//            {
//                GarrisonButler.Diagnostic("[ShipmentStart,{0}] No shipment left to start: {1}", building.Id,
//                    building.Name);
//                return;
//            }

//            // Under construction
//            if (building.IsBuilding || building.CanActivate)
//            {
//                GarrisonButler.Diagnostic(
//                    "[ShipmentStart,{0}] Building under construction, can't start work order: {1}",
//                    building.Id, building.Name);
//                return;
//            }

//            // Structs cannot be null
//            var shipmentObjectFound = ButlerCoroutine.ShipmentsMap.FirstOrDefault(s => s.BuildingIds.Contains(building.Id));

//            if (!shipmentObjectFound.CompletedPreQuest)
//            {
//                GarrisonButler.Warning("[ShipmentStart,{0}] Cannot collect shipments until pre-quest is done: {1}",
//                    building.Id, building.Name);
//                GarrisonButler.Diagnostic("[ShipmentStart,{0}] preQuest not completed A={2} H={3}: {1}",
//                    building.Id, building.Name, shipmentObjectFound.ShipmentPreQuestIdAlliance,
//                    shipmentObjectFound.ShipmentPreQuestIdHorde);
//                return;
//            }

//            // Reached limit of tries?
//            if (building.StartWorkOrderTries >= Building.StartWorkOrderMaxTries)
//            {
//                GarrisonButler.Warning("[ShipmentStart,{0}] Cannot start shipments due to reaching max tries ({2}): {1}",
//                    building.Id, building.Name, Building.StartWorkOrderMaxTries);
//                return;
//            }

//            // If need to do an action to know maxShipment to start
//            if (building.IsActionForRefreshNeeded())
//            {
//                // Add refresh action
//                Sequencer.Instance.AddAction(new InteractWithOrderNpc(building), priority);
//                return;
//            }

//            // max start by user ?
//            var maxToStart = building.maxCanComplete();

//            if (maxToStart <= 0)
//            {
//                GarrisonButler.Diagnostic(
//                    "[ShipmentStart,{0}] Can't start more work orders. {1} - ShipmentsTotal={2}, MaxCanStartOrder={3}",
//                    building.Id,
//                    building.Name,
//                    building.ShipmentsTotal,
//                    buildingsettings.MaxCanStartOrder);
//                return;
//            }

//            GarrisonButler.Diagnostic("[ShipmentStart,{0}] Found {1} new work orders to start: {2}",
//                building.Id, maxToStart, building.Name);

//            // Add start action
//            Sequencer.Instance.AddAction(new StartShipment(building), priority);
//            return;
//        }
        

//    }
//}
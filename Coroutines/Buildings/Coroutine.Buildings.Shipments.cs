﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private const int BiggerIsBetterAlliance = 36592;
        private const int BiggerIsBetterHorde = 36567;

        private static readonly List<Shipment> ShipmentsMap = new List<Shipment>
        {
            // Mine
            new Shipment(235886, new List<int>
            {
                61, // lvl 1
                62, // lvl 2
                63, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5474.07, 4451.756, 144.5106),
            35154, 34192),

            // Garden
            new Shipment(235885, new List<int>
            {
                29, // lvl 1
                136, // lvl 2
                137, // lvl 3
            }, new WoWPoint(1862, 139, 78), new WoWPoint(5414.973, 4574.003, 137.4256),
            34193, 36404),

            #region large
            // Barracks
            new Shipment(235885, new List<int>
            {
                26, // lvl 1
                27, // lvl 2
                28, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Dwarven Bunker
            new Shipment(235885, new List<int>
            {
                8, // lvl 1
                9, // lvl 2
                10, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Gnomish Gearworks
            new Shipment(235885, new List<int>
            {
                162, // lvl 1
                163, // lvl 2
                164, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Mage Tower
            new Shipment(235885, new List<int>
            {
                37, // lvl 1
                38, // lvl 2
                39, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Stables
            new Shipment(235885, new List<int>
            {
                65, // lvl 1
                66, // lvl 2
                67, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            #endregion
            #region medium
            // Barn
            new Shipment(235885, new List<int>
            {
                24, // lvl 1
                25, // lvl 2
                133, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            36345, 36271),  // Breaking into the Trap Game

            // Gladiator's Sanctum
            new Shipment(235885, new List<int>
            {
                159, // lvl 1
                160, // lvl 2
                161, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Lumber Mill
            new Shipment(235885, new List<int>
            {
                40, // lvl 1
                41, // lvl 2
                138, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            36138, 36192),

            // Trading Post
            new Shipment(235885, new List<int>
            {
                111, // lvl 1
                144, // lvl 2
                145, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Inn / Tavern
            new Shipment(235885, new List<int>
            {
                34, // lvl 1
                35, // lvl 2
                36, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            #endregion
            #region small
            // Alchemy Lab
            new Shipment(235885, new List<int>
            {
                76, // lvl 1
                119, // lvl 2
                120, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37568, 36641),

            // Enchanter's Study
            new Shipment(235885, new List<int>
            {
                93, // lvl 1
                125, // lvl 2
                126, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37570, 36645),

            // Gem Boutique
            new Shipment(235885, new List<int>
            {
                96, // lvl 1
                131, // lvl 2
                132, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37573, 36644),

            // Salvage Yard
            new Shipment(235885, new List<int>
            {
                52, // lvl 1
                140, // lvl 2
                141, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Scribe's Quarters
            new Shipment(235885, new List<int>
            {
                95, // lvl 1
                129, // lvl 2
                130, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37572, 36647),

            // Storehouse
            new Shipment(235885, new List<int>
            {
                51, // lvl 1
                142, // lvl 2
                143, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            BiggerIsBetterHorde, BiggerIsBetterAlliance),   // 37060 - Lost in Transition - One time quest (extra??)

            // Tailoring Emporium
            new Shipment(235885, new List<int>
            {
                94, // lvl 1
                127, // lvl 2
                128, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37575, 36643),

            // The Forge
            new Shipment(235885, new List<int>
            {
                60, // lvl 1
                117, // lvl 2
                118, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37569, 35168),

            // The Tannery
            new Shipment(235885, new List<int>
            {
                90, // lvl 1
                121, // lvl 2
                122, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37574, 36642),

            // Engineering Works
            new Shipment(235885, new List<int>
            {
                91, // lvl 1
                123, // lvl 2
                124, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256),
            37571, 36646),

            // Others? 
        };

        private static WoWGuid lastSeen = WoWGuid.Empty;

        //private static bool CanRunPickUpOrder(ref WoWGameObject buildingAsObject)
        //{
        //    buildingAsObject = null;

        //    if (!GaBSettings.Mono.CollectingShipments)
        //    {
        //        GarrisonButler.Diagnostic("[ShipmentCollect] Deactivated in user settings.");
        //        return false;
        //    }

        //    RefreshBuildings();

        //    // Check if a building has shipment to pickup
        //    var BuildingToCollects = _buildings.Where(b => b.shipmentsReady > 0);
        //    if (!BuildingToCollects.Any())
        //    {
        //        GarrisonButler.Diagnostic("[ShipmentCollect] Deactivated in user settings.");
        //        return false;
        //    }

        //    // Get the list of the building objects with pickup
        //    var BuildingsIds = BuildingToCollects.Select(b => b.Displayids).SelectMany(id => id);
        //    var buildingsAsObjects =
        //        ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Where(o => BuildingsIds.Contains(o.DisplayId)).OrderBy(o=>o.DistanceSqr);
        //    if (!buildingsAsObjects.Any())
        //    {
        //        GarrisonButler.Diagnostic("[ShipmentCollect] Found pickup but couldn't find buildings.");
        //        foreach (var toCollect in BuildingToCollects)
        //        {
        //            GarrisonButler.Diagnostic("[ShipmentCollect]     Building {0}", toCollect.name);
        //            foreach (var id in toCollect.Displayids)
        //            {
        //                GarrisonButler.Diagnostic("[ShipmentCollect]         ID {0}", id);                        
        //            }
        //        }
        //        return false;
        //    }

        //    buildingAsObject = buildingsAsObjects.FirstOrDefault();
        //    GarrisonButler.Diagnostic("[ShipmentCollect] Found shipment to collect for building {0} - {1}", buildingAsObject.SafeName, buildingAsObject.Location);
        //    return true;
        //}

        private static List<uint> DisplayIdToPickUp = new List<uint>
        {
            17819, // ???? skull?
            13845, // barn 1/1 full or not?
            16091, // Not Full ally
            16092, // Full ally
            19959, // horde not full?
            // NEVER PICKUP : 15585 Ally empty
        };

        #endregion

        private static void InitializeShipments()
        {
            RefreshBuildings();
        }

        private static bool IsWoWObjectShipment(WoWObject toCheck)
        {
            return ShipmentsMap
                .GetEmptyIfNull()
                .Any(s => s.buildingIds.Contains((int)toCheck.Entry));
        }

        private static Shipment GetShipmentFromWoWObject(WoWObject toGet)
        {
            return ShipmentsMap
                    .GetEmptyIfNull()
                    .Where(s => s.buildingIds.Contains((int)toGet.Entry))
                    .FirstOrDefault();
        }

        private static List<WoWGameObject> GetAllShipmentObjectsIfCanRunShipments()
        {
            List<WoWGameObject> returnList =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => ShipmentsMap.Any(j => j.shipmentId == o.Entry))
                    .OrderBy(o => o.DistanceSqr)
                    .ToList();

            return returnList;
        }

        private static bool ShouldRunPickUpOrStartShipment()
        {
            return CanPickUpOrStartAtLeastOneShipmentFromAll().Item1;
        }

        internal static Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>
            CanPickUpOrStartAtLeastOneShipmentFromAll()
        {
            foreach (Building building in _buildings)
            {
                Tuple<bool, Building> canStart = CanStartShipment(building);
                Tuple<bool, WoWGameObject> canPickUp = CanPickUpShipment(building);

                if (canPickUp.Item1 || canStart.Item1)
                    return new Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                        true,
                        new Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>(canStart, canPickUp));
            }
            return new Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(false, null);
        }

        internal static Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>
            CanPickUpOrStartAtLeastOneShipmentAt(Building building)
        {
            Tuple<bool, Building> canStart = CanStartShipment(building);
            Tuple<bool, WoWGameObject> canPickUp = CanPickUpShipment(building);

            if (canPickUp.Item1 || canStart.Item1)
                return new Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(
                    true,
                    new Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>(canStart, canPickUp));
            return new Tuple<bool, Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>>>(false, null);
        }

        internal static async Task<ActionResult> PickUpOrStartAtLeastOneShipment(
            Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>> input)
        {
            Tuple<bool, WoWGameObject> canPickUp = input.Item2;
            Tuple<bool, Building> canStart = input.Item1;

            if (canPickUp.Item1)
                if (await PickUpShipment(canPickUp.Item2))
                    return ActionResult.Running;

            if (canStart.Item1)
                if (await StartShipment(canStart.Item2))
                    return ActionResult.Running;

            return ActionResult.Done; // Done
        }

        internal static Tuple<bool, Building> CanStartShipment(Building building)
        {
            if (building == null)
            {
                GarrisonButler.Diagnostic("[ShipmentStart] Building is null, either not built or not properly scanned.");
                return new Tuple<bool, Building>(false, null);
            }

            building.Refresh();

            // Activated by user ?
            if (!GaBSettings.Get().GetBuildingSettings(building.id).CanStartOrder)
            {
                GarrisonButler.Diagnostic("[ShipmentStart] Deactivated in user settings: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            // No Shipment left to start
            if (building.NumberShipmentLeftToStart() <= 0)
            {
                GarrisonButler.Diagnostic("[ShipmentStart] No shipment left to start: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            // Structs cannot be null
            Shipment shipmentObjectFound = ShipmentsMap.FirstOrDefault(s => s.buildingIds.Contains(building.id));

            if(!shipmentObjectFound.completedPreQuest)
            {
                GarrisonButler.Warning("[ShipmentStart] Cannot collect shipments until pre-quest is done: {0}", building.name);
                GarrisonButler.Diagnostic("[ShipmentStart] preQuest not completed A={1} H={2}: {0}",
                    building.name, shipmentObjectFound.shipmentPreQuestIdAlliance, shipmentObjectFound.shipmentPreQuestIdHorde);
                return new Tuple<bool, Building>(false, null);
            }


            // Under construction
            if (building.isBuilding || building.canActivate)
            {
                GarrisonButler.Diagnostic("[ShipmentStart] Building under construction, can't start work order: {0}",
                    building.name);
                return new Tuple<bool, Building>(false, null);
            }
            int MaxToStart = GetMaxShipmentToStart(building);

            // max start by user ?
            if (MaxToStart <= 0)
            {
                GarrisonButler.Diagnostic(
                    "[ShipmentStart] Reached limit of work orders started: {0} - current# {1} max {2} ",
                    building.name,
                    building.shipmentsTotal,
                    GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder);
                return new Tuple<bool, Building>(false, null);
            }

            // Do I fulfill the conditions to complete the order? 
            if (!building.canCompleteOrder())
            {
                GarrisonButler.Diagnostic(
                    "[ShipmentStart] Do not fulfill the requirements to start a new work order: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            GarrisonButler.Diagnostic("[ShipmentStart] Found {0} new work orders to start: {1}",
                MaxToStart, building.name);
            return new Tuple<bool, Building>(true, building);
        }


        internal static Tuple<bool, WoWGameObject> CanPickUpShipment(Building building)
        {
            if (building == null)
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] Building is null, either not built or not properly scanned.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            building.Refresh();

            // No Shipment ready
            if (building.shipmentsReady <= 0)
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] No shipment left to pickup: {0}", building.name);
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Activated by user ?
            if (!GaBSettings.Get().GetBuildingSettings(building.id).CanCollectOrder)
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] Deactivated in user settings: {0}", building.name);
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Get the list of the building objects
            WoWGameObject buildingAsObject =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => building.Displayids.Contains(o.DisplayId))
                    .OrderBy(o => o.DistanceSqr)
                    .FirstOrDefault();
            if (buildingAsObject == default(WoWGameObject))
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] Building could not be found in the area: {0}", building.name);
                foreach (uint id in building.Displayids)
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp]     ID {0}", id);
                }
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonButler.Diagnostic("[ShipmentPickUp] Found {0} shipments to collect: {1}", building.shipmentsReady,
                building.name);
            return new Tuple<bool, WoWGameObject>(true, buildingAsObject);
        }

        private static async Task<bool> StartShipment(Building building)
        {
            WoWUnit unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
            if (unit == null)
            {
                await
                    MoveTo(building.Pnj,
                        "[ShipmentStart] Could not find unit (" + building.PnjId + "), moving to default location.");
                return true;
            }

            if (await MoveToInteract(unit) == ActionResult.Running)
                return true;

            unit.Interact();

            await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                var res = InterfaceLua.IsGarrisonCapacitiveDisplayFrame();
                if (!res)
                {
                    Navigator.PlayerMover.MoveTowards(unit.Location); 
                }
                return res;
            });

            await CommonCoroutines.SleepForRandomUiInteractionTime();
            if (!await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                var res = InterfaceLua.IsGarrisonCapacitiveDisplayFrame();
                if (!res)
                {
                    unit.Interact(); 
                    IfGossip(unit);
                }
                return res;
            }))
            {
                GarrisonButler.Warning(
                    "[ShipmentStart] Failed to open Work order frame. Maybe Blizzard bug, trying to move away.");
                await WorkAroundBugFrame();
                return true;
            }
            GarrisonButler.Log("[ShipmentStart] Work order frame opened.");

            // Interesting events to check out : Shipment crafter opened/closed, shipment crafter info, gossip show, gossip closed, 
            // bag update delayed is the last fired event when adding a work order.  

            int MaxToStart = GetMaxShipmentToStart(building);

            for (int i = 0; i < MaxToStart; i++)
            {
                InterfaceLua.ClickStartOrderButton();
                building.Refresh();
                await CommonCoroutines.SleepForRandomUiInteractionTime();
                await Buddy.Coroutines.Coroutine.Yield();
            }
            if (!await Buddy.Coroutines.Coroutine.Wait(10000, () =>
            {
                //This diag message was causing a lot of spam
                //GarrisonButler.Diagnostic("[ShipmentStart] Waiting for WoW shipment data to refresh newly started.");
                var buildingShipment = _buildings.FirstOrDefault(b => b.id == building.id);
                if (buildingShipment != null)
                {
                    buildingShipment.Refresh();
                    var max = GetMaxShipmentToStart(buildingShipment);
                    return max == 0;
                }
                GarrisonButler.Diagnostic("[ShipmentStart] buildingShipment null.");
                return false;
            }))
            {
                GarrisonButler.Diagnostic("[ShipmentStart] Mismatch starting work orders.");
            }
            building.Refresh();
            _startOrderTriggered = false;
            return false; // done here
        }

        private static int GetMaxShipmentToStart(Building building)
        {
            int MaxInProgress;
            int maxPossible = building.shipmentCapacity - building.shipmentsTotal;
            int MaxSettings = GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder;
            if (MaxSettings == 0)
            {
                MaxInProgress = building.shipmentCapacity;
            }
            else
            {
                MaxInProgress = Math.Min(building.shipmentCapacity, MaxSettings);
            }
            return MaxInProgress - building.shipmentsTotal;
        }
        private static async Task WorkAroundBugFrame()
        {
            Buddy.Coroutines.Coroutine.Wait(6000, () =>
            {
                MoveToTable();
                return false;
            });
        }

        private static void IfGossip(WoWUnit pnj)
        {
            if (GossipFrame.Instance != null)
            {
                GossipFrame frame = GossipFrame.Instance;
                if (frame.GossipOptionEntries.GetEmptyIfNull().Any())
                {
                    foreach (GossipEntry gossipOptionEntry in frame.GossipOptionEntries)
                    {
                        Logging.WriteDiagnostic("Gossip: " + gossipOptionEntry.Type);
                    }
                    frame.SelectGossipOption(frame.GossipOptionEntries.Count - 1);
                }
            }
        }


        private static async Task<bool> PickUpShipment(WoWGameObject building)
        {
            WoWPoint locationToLookAt = WoWPoint.Empty;

            // Fix for the mine position
            IEnumerable<uint> minesIds = _buildings.Where(
                b =>
                    (b.id == (int) buildings.MineLvl1) || (b.id == (int) buildings.MineLvl2) ||
                    (b.id == (int) buildings.MineLvl3)).SelectMany(b => b.Displayids);
            if (minesIds.Contains(building.DisplayId))
                locationToLookAt = Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
            else
                locationToLookAt = building.Location;

            // Search for shipment next to building
            WoWGameObject shipmentToCollect =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(
                        o =>
                            o.SubType == WoWGameObjectType.GarrisonShipment &&
                            o.Location.DistanceSqr(locationToLookAt) < 2500f)
                    .OrderBy(o => o.Location.DistanceSqr(locationToLookAt))
                    .FirstOrDefault();

            if (shipmentToCollect == null)
            {
                if (
                    await
                        MoveTo(building.Location,
                            "[ShipmentCollect] Moving to Building to search for shipment to pick up.") == ActionResult.Running)
                    return true;
            }
            else
            {
                if (await HarvestWoWGameObjectCachedLocation(shipmentToCollect) == ActionResult.Running)
                    return true;

                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(10000, () =>
                {
                    //This diag was causing a lot of spam
                    //GarrisonButler.Diagnostic("[ShipmentCollect] Waiting for WoW shipment data to refresh newly collected.");
                    var buildingShipment = _buildings.FirstOrDefault(b => b.Displayids.Contains(building.DisplayId));
                    if (buildingShipment != null)
                    {
                        buildingShipment.Refresh();
                        return buildingShipment.shipmentsReady > 0;
                    }
                    GarrisonButler.Diagnostic("[ShipmentCollect] buildingShipment null.");
                    return false;
                });
                return false; // Done here
            }
            return false; // should never reach that point!
        }

        private struct Shipment
        {
            public readonly List<int> buildingIds;
            public readonly WoWPoint defaultAllyLocation;
            public readonly WoWPoint defaultHordeLocation;
            public readonly int shipmentId;
            public readonly int shipmentPreQuestIdHorde;
            public readonly int shipmentPreQuestIdAlliance;
            public bool completedPreQuest
            {
                get
                {
                    if(Me == null)
                    {
                        GarrisonButler.Diagnostic("Error in class Shipment getting completedPreQuest - Me == null");
                        return false;
                    }

                    if(!Me.IsValid)
                    {
                        GarrisonButler.Diagnostic("Error in class Shipment getting completedPreQuest - Me.IsValid = false");
                        return false;
                    }

                    if (shipmentPreQuestIdHorde == 0 || shipmentPreQuestIdAlliance == 0)
                        return false;

                    Styx.CommonBot.Profiles.Quest.Order.ProfileHelperFunctionsBase helper = new Styx.CommonBot.Profiles.Quest.Order.ProfileHelperFunctionsBase();

                    uint questToUse = Me.IsAlliance ? (uint)shipmentPreQuestIdAlliance : (uint)shipmentPreQuestIdHorde;

                    bool returnValue = helper.IsQuestCompleted(questToUse);

                    return returnValue;
                }
            }

            public Shipment(int shipmentId, List<int> buildingIds, WoWPoint defaultAllyLocation,
                WoWPoint defaultHordeLocation, int shipmentPreQuestIdHorde, int shipmentPreQuestIdAlliance)
                : this()
            {
                this.shipmentId = shipmentId;
                this.buildingIds = buildingIds;
                this.defaultAllyLocation = defaultAllyLocation;
                this.defaultHordeLocation = defaultHordeLocation;
                this.shipmentPreQuestIdHorde = shipmentPreQuestIdHorde;
                this.shipmentPreQuestIdAlliance = shipmentPreQuestIdAlliance;
            }
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly List<Shipment> ShipmentsMap = new List<Shipment>
        {
            // Mine
            new Shipment(235886, new List<int>
            {
                61, // lvl 1
                62, // lvl 2
                63, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5474.07, 4451.756, 144.5106)),

            // Garden
            new Shipment(235885, new List<int>
            {
                29, // lvl 1
                136, // lvl 2
                137, // lvl 3
            }, new WoWPoint(1862, 139, 78), new WoWPoint(5414.973, 4574.003, 137.4256)),

            #region large
            // Barracks
            new Shipment(235885, new List<int>
            {
                26, // lvl 1
                27, // lvl 2
                28, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Dwarven Bunker
            new Shipment(235885, new List<int>
            {
                8, // lvl 1
                9, // lvl 2
                10, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Gnomish Gearworks
            new Shipment(235885, new List<int>
            {
                162, // lvl 1
                163, // lvl 2
                164, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Mage Tower
            new Shipment(235885, new List<int>
            {
                37, // lvl 1
                38, // lvl 2
                39, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Stables
            new Shipment(235885, new List<int>
            {
                65, // lvl 1
                66, // lvl 2
                67, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            #endregion
            #region medium
            // Barn
            new Shipment(235885, new List<int>
            {
                24, // lvl 1
                25, // lvl 2
                133, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Gladiator's Sanctum
            new Shipment(235885, new List<int>
            {
                159, // lvl 1
                160, // lvl 2
                161, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Lumber Mill
            new Shipment(235885, new List<int>
            {
                40, // lvl 1
                41, // lvl 2
                138, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Trading Post
            new Shipment(235885, new List<int>
            {
                111, // lvl 1
                144, // lvl 2
                145, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            #endregion
            #region small
            // Alchemy Lab
            new Shipment(235885, new List<int>
            {
                76, // lvl 1
                119, // lvl 2
                120, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Enchanter's Study
            new Shipment(235885, new List<int>
            {
                93, // lvl 1
                125, // lvl 2
                126, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Gem Boutique
            new Shipment(235885, new List<int>
            {
                96, // lvl 1
                131, // lvl 2
                132, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Salvage Yard
            new Shipment(235885, new List<int>
            {
                52, // lvl 1
                140, // lvl 2
                141, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Scribe's Quarters
            new Shipment(235885, new List<int>
            {
                95, // lvl 1
                129, // lvl 2
                130, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Storehouse
            new Shipment(235885, new List<int>
            {
                51, // lvl 1
                142, // lvl 2
                143, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Tailoring Emporium
            new Shipment(235885, new List<int>
            {
                94, // lvl 1
                127, // lvl 2
                128, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // The Forge
            new Shipment(235885, new List<int>
            {
                60, // lvl 1
                117, // lvl 2
                118, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // The Tannery
            new Shipment(235885, new List<int>
            {
                90, // lvl 1
                121, // lvl 2
                122, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Engineering Works
            new Shipment(235885, new List<int>
            {
                91, // lvl 1
                123, // lvl 2
                124, // lvl 3
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

            // Others? 
        };

        private static WoWGuid lastSeen = WoWGuid.Empty;

        //private static bool CanRunPickUpOrder(ref WoWGameObject buildingAsObject)
        //{
        //    buildingAsObject = null;

        //    if (!GaBSettings.Mono.CollectingShipments)
        //    {
        //        GarrisonBuddy.Diagnostic("[ShipmentCollect] Deactivated in user settings.");
        //        return false;
        //    }

        //    RefreshBuildings();

        //    // Check if a building has shipment to pickup
        //    var BuildingToCollects = _buildings.Where(b => b.shipmentsReady > 0);
        //    if (!BuildingToCollects.Any())
        //    {
        //        GarrisonBuddy.Diagnostic("[ShipmentCollect] Deactivated in user settings.");
        //        return false;
        //    }

        //    // Get the list of the building objects with pickup
        //    var BuildingsIds = BuildingToCollects.Select(b => b.Displayids).SelectMany(id => id);
        //    var buildingsAsObjects =
        //        ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Where(o => BuildingsIds.Contains(o.DisplayId)).OrderBy(o=>o.DistanceSqr);
        //    if (!buildingsAsObjects.Any())
        //    {
        //        GarrisonBuddy.Diagnostic("[ShipmentCollect] Found pickup but couldn't find buildings.");
        //        foreach (var toCollect in BuildingToCollects)
        //        {
        //            GarrisonBuddy.Diagnostic("[ShipmentCollect]     Building {0}", toCollect.name);
        //            foreach (var id in toCollect.Displayids)
        //            {
        //                GarrisonBuddy.Diagnostic("[ShipmentCollect]         ID {0}", id);                        
        //            }
        //        }
        //        return false;
        //    }

        //    buildingAsObject = buildingsAsObjects.FirstOrDefault();
        //    GarrisonBuddy.Diagnostic("[ShipmentCollect] Found shipment to collect for building {0} - {1}", buildingAsObject.SafeName, buildingAsObject.Location);
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

        internal static async Task<bool> PickUpOrStartAtLeastOneShipment(
            Tuple<Tuple<bool, Building>, Tuple<bool, WoWGameObject>> input)
        {
            Tuple<bool, Building> canStart = input.Item1;
            Tuple<bool, WoWGameObject> canPickUp = input.Item2;
            if (canStart.Item1)
                if (await StartShipment(canStart.Item2))
                    return true;

            if (canPickUp.Item1)
                if (await PickUpShipment(canPickUp.Item2))
                    return true;

            return true; // Done
        }

        internal static Tuple<bool, Building> CanStartShipment(Building building)
        {
            if (building == null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Building is null, either not built or not properly scanned.");
            }

            building.Refresh();

            // No Shipment left to start
            if (building.NumberShipmentLeftToStart() <= 0)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] No shipment left to start: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            // Activated by user ?
            if (!GaBSettings.Get().GetBuildingSettings(building.id).CanStartOrder)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Deactivated in user settings: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            // Under construction
            if (building.isBuilding || building.canActivate)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Building under construction, can't start work order: {0}",
                    building.name);
                return new Tuple<bool, Building>(false, null);
            }

            // max start by user ?
            if (GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder != 0
                && GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder <= building.shipmentsTotal)
            {
                GarrisonBuddy.Diagnostic(
                    "[ShipmentStart] Reached limit of work orders started: {0} - current# {1} max {2} ",
                    building.name,
                    building.shipmentsTotal - building.shipmentCapacity - building.shipmentsReady,
                    GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder);
                return new Tuple<bool, Building>(false, null);
            }

            // Do I fulfill the conditions to complete the order? 
            if (!building.canCompleteOrder())
            {
                GarrisonBuddy.Diagnostic(
                    "[ShipmentStart] Do not fulfill the requirements to start a new work order: {0}", building.name);
                return new Tuple<bool, Building>(false, null);
            }

            GarrisonBuddy.Diagnostic("[ShipmentStart] Found {0} new work orders to start: {1}",
                building.shipmentCapacity, building.name);
            return new Tuple<bool, Building>(true, building);
        }


        internal static Tuple<bool, WoWGameObject> CanPickUpShipment(Building building)
        {
            if (building == null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentPickUp] Building is null, either not built or not properly scanned.");
            }

            building.Refresh();
            
            // No Shipment ready
            if (building.shipmentsReady <= 0)
            {
                GarrisonBuddy.Diagnostic("[ShipmentPickUp] No shipment left to pickup: {0}", building.name);
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Activated by user ?
            if (!GaBSettings.Get().GetBuildingSettings(building.id).CanCollectOrder)
            {
                GarrisonBuddy.Diagnostic("[ShipmentPickUp] Deactivated in user settings: {0}", building.name);
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
                GarrisonBuddy.Diagnostic("[ShipmentPickUp] Building could not be found in the area: {0}", building.name);
                foreach (uint id in building.Displayids)
                {
                    GarrisonBuddy.Diagnostic("[ShipmentPickUp]     ID {0}", id);
                }
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonBuddy.Diagnostic("[ShipmentPickUp] Found {0} shipments to collect: {1}", building.shipmentsReady,
                building.name);
            return new Tuple<bool, WoWGameObject>(true, buildingAsObject);
        }

        private static async Task<bool> StartShipment(Building building)
        {
            GarrisonBuddy.Log("[ShipmentStart] Moving to start work order:" + building.name);

            WoWUnit unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
            if (unit == null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Could not find unit (" + building.PnjId +
                                         "), moving to default location.\n" +
                                         "If this message is spammed, please post the ID of the PNJ for your work orders on the forum post of Garrison Buddy!");

                ObjectManager.Update();
                await MoveTo(building.Pnj);
                return true;
            }

            if (await MoveTo(unit.Location))
                return true;

            GarrisonBuddy.Diagnostic("[ShipmentStart] Arrived at location.");
            unit.Interact();
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            if (!await Buddy.Coroutines.Coroutine.Wait(500, () =>
            {
                if (!InterfaceLua.IsGarrisonCapacitiveDisplayFrame())
                {
                    //    unit.Interact(); 
                    IfGossip(unit);
                }
                return InterfaceLua.IsGarrisonCapacitiveDisplayFrame();
            }))
            {
                GarrisonBuddy.Warning("[ShipmentStart] Failed to open Work order frame. Maybe Blizzard bug, trying to move away.");
                WorkAroundBugFrame();
                return true;
            }
            GarrisonBuddy.Log("[ShipmentStart] Work order frame opened.");

            // Interesting events to check out : Shipment crafter opened/closed, shipment crafter info, gossip show, gossip closed, 
            // bag update delayed is the last fired event when adding a work order.  

            int NumberToStart = 0;
            int maxPossible = building.shipmentCapacity - building.shipmentsTotal;
            int MaxSettings = GaBSettings.Get().GetBuildingSettings(building.id).MaxCanStartOrder;
            if (MaxSettings == 0)
            {
                NumberToStart = maxPossible;
            }
            else
            {
                NumberToStart = Math.Min(maxPossible, MaxSettings - building.shipmentsTotal);
            }

            for (int i = NumberToStart; i > 0; i--)
            {
                InterfaceLua.ClickStartOrderButton();
                await CommonCoroutines.SleepForRandomUiInteractionTime();
            }

            if (await Buddy.Coroutines.Coroutine.Wait(5000, () => BuildingsLua.GetShipmentTotal(building.id) == building.shipmentsTotal + NumberToStart))
            {
                GarrisonBuddy.Log("Successfully started {0} work orders.", NumberToStart);
            }
            else
            {
                GarrisonBuddy.Diagnostic("Mismatch between number to start and actually started.");
            }

            StartOrderTriggered = false;
            return false; // done here
        }

        private static void WorkAroundBugFrame()
        {
            WoWMovement.Move(WoWMovement.MovementDirection.ForwardBackMovement,TimeSpan.FromSeconds(3));
        }
        private static async Task<bool> IfGossip(WoWUnit pnj)
        {
            if (GossipFrame.Instance != null)
            {
                GossipFrame frame = GossipFrame.Instance;
                foreach (GossipEntry gossipOptionEntry in frame.GossipOptionEntries)
                {
                    Logging.WriteDiagnostic("Gossip: " + gossipOptionEntry.Type);
                }
                frame.SelectGossipOption(1);
                frame.SelectGossipOption(0);
            }
            return true;
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
                //// Fix for the mine position
                ////list of all the mines IDs
                //var minesIds = _buildings.Where(
                //    b =>
                //        (b.id == (int)buildings.MineLvl1) || (b.id == (int)buildings.MineLvl2) ||
                //        (b.id == (int)buildings.MineLvl3)).SelectMany(b => b.Displayids);

                //if (minesIds.Contains(buildingAsObject.DisplayId))
                //{

                //} 
                GarrisonBuddy.Diagnostic(
                    "[ShipmentCollect] Could not Find shipment location in world for building {0} - {1}",
                    building.SafeName, building.Location);
                GarrisonBuddy.Log("[ShipmentCollect] Moving to Building to search for shipment to pick up.");
                if (await MoveTo(building.Location))
                    return true;
            }
            else
            {
                GarrisonBuddy.Diagnostic("[ShipmentCollect] Moving to harvest shipment {0} - {1}",
                    shipmentToCollect.Name, shipmentToCollect.Location);
                await HarvestWoWGameOject(shipmentToCollect);
                return false; // Done here
            }
            return false; // should never reach that point!
        }


        //private static async Task<bool> PickUpAllWorkOrder()
        //{
        //    WoWGameObject buildingAsObject = null;

        //    if (!CanRunPickUpOrder(ref buildingAsObject))
        //        return false;

        //    WoWPoint locationToLookAt = WoWPoint.Empty;
        //    // Fix for the mine position
        //    //list of all the mines IDs
        //    var minesIds = _buildings.Where(
        //        b =>
        //            (b.id == (int)buildings.MineLvl1) || (b.id == (int)buildings.MineLvl2) ||
        //            (b.id == (int)buildings.MineLvl3)).SelectMany(b => b.Displayids);

        //    if (minesIds.Contains(buildingAsObject.DisplayId))
        //        locationToLookAt = Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
        //    else
        //        locationToLookAt = buildingAsObject.Location;


        //    // Search for shipment next to building
        //    var shipmentToCollect =
        //        ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
        //            .Where(
        //                o =>
        //                    o.SubType == WoWGameObjectType.GarrisonShipment &&
        //                    o.Location.DistanceSqr(locationToLookAt) < 400f)
        //            .OrderBy(o => o.Location.DistanceSqr(locationToLookAt))
        //            .FirstOrDefault();

        //    if (shipmentToCollect == null)
        //    {

        //        //// Fix for the mine position
        //        ////list of all the mines IDs
        //        //var minesIds = _buildings.Where(
        //        //    b =>
        //        //        (b.id == (int)buildings.MineLvl1) || (b.id == (int)buildings.MineLvl2) ||
        //        //        (b.id == (int)buildings.MineLvl3)).SelectMany(b => b.Displayids);

        //        //if (minesIds.Contains(buildingAsObject.DisplayId))
        //        //{

        //        //} 
        //        GarrisonBuddy.Diagnostic("[ShipmentCollect] Could not Find shipment location in world for building {0} - {1}", buildingAsObject.SafeName, buildingAsObject.Location);
        //        GarrisonBuddy.Log("[ShipmentCollect] Moving to Building to search for shipment to pick up.");
        //        if (await MoveTo(buildingAsObject.Location))
        //            return true;
        //    }
        //    else
        //    {
        //        GarrisonBuddy.Diagnostic("[ShipmentCollect] Moving to harvest shipment {0} - {1}", shipmentToCollect.Name, shipmentToCollect.Location);
        //        await HarvestWoWGameOject(shipmentToCollect);
        //        RefreshBuildings(true);
        //        return true;
        //    }
        //    return false; // should never reach that point!
        //}


        private struct Shipment
        {
            public readonly List<int> buildingIds;
            public readonly WoWPoint defaultAllyLocation;
            public readonly WoWPoint defaultHordeLocation;
            public readonly int shipmentId;

            public Shipment(int shipmentId, List<int> buildingIds, WoWPoint defaultAllyLocation,
                WoWPoint defaultHordeLocation)
                : this()
            {
                this.shipmentId = shipmentId;
                this.buildingIds = buildingIds;
                this.defaultAllyLocation = defaultAllyLocation;
                this.defaultHordeLocation = defaultHordeLocation;
            }
        }
    }
}
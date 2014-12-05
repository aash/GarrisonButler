using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

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
            }, new WoWPoint(1901.799, 103.2309, 83.52671), new WoWPoint(5414.973, 4574.003, 137.4256)),

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

        private static bool PickupRunning;
        private static WaitTimer _PickUpWaitTimer;
        private static WaitTimer _StartOrderWaitTimer;
        private static bool StartOrderRunning;
        #endregion

        private static void InitializeShipments()
        {
            RefreshBuildings();
        }

        private static bool IsToDoShipment()
        {
            return _StartOrderWaitTimer.IsFinished || StartOrderRunning;

            // Work orders to start 
            IOrderedEnumerable<Building> toStarts =
                _buildings.Where(b => b.canCompleteOrder() && BuildingsLua.GetNumberShipmentLeftToStart(b.id) > 0)
                    .OrderBy(b => b.id);
            ;
            if (toStarts.Any())
                return true;

            // Work orders to pick up
            WoWGameObject shipmentToCollect =
                ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SubType == WoWGameObjectType.GarrisonShipment && o.DisplayId == 16091)
                    .OrderBy(o => o.Location.X)
                    .FirstOrDefault();
            return shipmentToCollect != null;
        }

        private static async Task<bool> startWorkOrder()
        {
            if (_StartOrderWaitTimer != null && !_StartOrderWaitTimer.IsFinished && !StartOrderRunning)
                return false;
            if (_StartOrderWaitTimer == null)
                _StartOrderWaitTimer = new WaitTimer(TimeSpan.FromMinutes(1));
            _StartOrderWaitTimer.Reset();

            // check if needed
            IOrderedEnumerable<Building> toStarts =
                _buildings.Where(b => b.canCompleteOrder() && b.shipmentCapacity - b.shipmentsTotal > 0)
                    .OrderBy(b => b.id);
            ;
            if (!toStarts.Any())
            {
                StartOrderRunning = false;
                return false;
            }
            StartOrderRunning = true;
            Building toStart = toStarts.First();
            // First go to the PNJ.
            WoWUnit unit = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == toStart.PnjId);
            // can't find it? Let's try to get closer to the default location.
            if (unit == null)
            {
                await MoveTo(toStart.Pnj);
                return true;
            }

            if (await MoveTo(unit.Location))
                return true;

            await Buddy.Coroutines.Coroutine.Wait(500, () =>
            {
                if (!InterfaceLua.IsGarrisonCapacitiveDisplayFrame())
                {
                    unit.Interact();
                    IfGossip(unit);
                }
          
                return InterfaceLua.IsGarrisonCapacitiveDisplayFrame();
            });
            await Buddy.Coroutines.Coroutine.Wait(3000, () =>
            {
                if (toStart.canCompleteOrder() && BuildingsLua.GetNumberShipmentLeftToStart(toStart.id) > 0)
                {
                    InterfaceLua.ClickStartOrderButton();
                    GarrisonBuddy.Log("Starting work order at " + toStart.name);
                    return false;
                }
                return true;
            });
            RefreshBuildings(true); 
            return true;
        }


        private static async Task<bool> IfGossip(WoWUnit pnj)
        {
            if (GossipFrame.Instance != null)
            {
                var frame = GossipFrame.Instance;
                foreach (var gossipOptionEntry in frame.GossipOptionEntries)
                {
                    Logging.WriteDiagnostic("Gossip: " + gossipOptionEntry.Type);
                }
                frame.SelectGossipOption(0);
                frame.SelectGossipOption(1);
            }
            return true;
        }
        //itemToCollect = ores.OrderBy(i => i.Distance).First();
        //        // Do I have a mining pick to use
        //        WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
        //        if (miningPick != null && miningPick.Usable
        //            && !Me.HasAura(PreserverdMiningPickAura)
        //            && miningPick.CooldownTimeLeft.TotalSeconds == 0)
        //        {
        //            GarrisonBuddy.Diagnostic("Using Mining pick");
        //            miningPick.Use();
        //        }
        //        // Do I have a cofee to use
        //        WoWItem coffee = Me.BagItems.Where(o => o.Entry == MinersCofeeItemId).ToList().FirstOrDefault();
        //        if (coffee != null && coffee.Usable &&
        //            (!Me.HasAura(MinersCofeeAura) ||
        //             Me.Auras.FirstOrDefault(a => a.Value.SpellId == MinersCofeeAura).Value.StackCount < 2)
        //            && coffee.CooldownTimeLeft.TotalSeconds == 0)
        //        {
        //            GarrisonBuddy.Diagnostic("Using coffee");
        //            coffee.Use();
        //        }

        //        if (await MoveTo(itemToCollect.Location))
        //            return true;

        //        itemToCollect.Interact();
        //        SetLootPoi(itemToCollect);
        //        await Buddy.Coroutines.Coroutine.Sleep(3500);
        //        return true;

        private static async Task<bool> PickUpWorkOrder(Building building)
        {
            //Building building = _buildings.FirstOrDefault(b => shipment.buildingIds.Contains(b.id));
            if (building == null)
                return false;

            int numShipments = building.shipmentsReady;
            if (numShipments < 1)
                return false;

            GarrisonBuddy.Log("Detected " + numShipments + " shipments to collect from " + building.name + ".");
            if (!ShipmentsMap.Any(s => s.buildingIds.Contains(building.id)))
                return false;

            Shipment shipment = ShipmentsMap.First(s => s.buildingIds.Contains(building.id));

            WoWGameObject toCollect =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == shipment.shipmentId);
            if (toCollect == null)
            {
                GarrisonBuddy.Log("Can't find PNJ for " + building.name + " shipments, moving closer.");
                return
                    await
                        MoveTo(Me.IsAlliance ? shipment.defaultAllyLocation : shipment.defaultHordeLocation,
                            "Default location for " + building.name + " shipments");
            }
            GarrisonBuddy.Log("Moving to PNJ for " + building.name + " shipments.");
            if (await MoveTo(toCollect.Location, "Collecting " + building.name + " shipments"))
                return true;


            GarrisonBuddy.Log("Collecting " + building.name + " shipments.");
            await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                toCollect.Interact();
                ObjectManager.Update();
                return
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Any(o => o.Guid == toCollect.Guid && o.DisplayId == 16091);
            });

            GarrisonBuddy.Log("Collecting Garrison cache.");
            toCollect.Interact();
            RefreshBuildings(true); 
            return true;
        }

        private static Styx.WoWInternals.WoWGuid lastSeen = new WoWGuid();

        private static async Task<bool> PickUpAllWorkOrder()
        {

            if (_PickUpWaitTimer != null && !_PickUpWaitTimer.IsFinished && !PickupRunning)
                return false;
            if (_PickUpWaitTimer == null)
                _PickUpWaitTimer = new WaitTimer(TimeSpan.FromMinutes(1));
            _PickUpWaitTimer.Reset();

            WoWGameObject ShipmentToCollect = Me.ToGameObject();
            if (lastSeen != new WoWGuid())
            {
               ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Guid == lastSeen && o.DisplayId == 169091);
            }
            if (ShipmentToCollect == null)
            {
                ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SubType == WoWGameObjectType.GarrisonShipment && o.DisplayId == 16091)
                    .OrderBy(o => o.Location.X)
                    .FirstOrDefault();
            }
              
            if (ShipmentToCollect == null)
            {
                PickupRunning = false;
                return false;
            }
            lastSeen = ShipmentToCollect.Guid;
            PickupRunning = true;
            GarrisonBuddy.Log("Found shipment to collect(" + ShipmentToCollect.Name + "), moving to shipment.");
            GarrisonBuddy.Diagnostic("Shipment " + ShipmentToCollect.SafeName + " - " + ShipmentToCollect.Entry + " - " +
                                     ShipmentToCollect.DisplayId + ": " + ShipmentToCollect.Location);
            if (await MoveTo(Dijkstra.ClosestToNodes(ShipmentToCollect.Location)))
                return true;

            GarrisonBuddy.Log("Collecting shipment(" + ShipmentToCollect.Name + ").");
            if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                ShipmentToCollect.Interact();
                ObjectManager.Update();
                return
                    !ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Any(o => o.Guid == ShipmentToCollect.Guid && o.DisplayId == 16091);
            }))
            {
                GarrisonBuddy.Warning("Failed to collect shipment(" + ShipmentToCollect.Name + ").");
            }
            RefreshBuildings(true);
            return true;
        }

        //private static async Task<bool> PickUpMineWorkOrders()
        //{
        //    if (!GaBSettings.Mono.ShipmentsMine)
        //        return false;

        //    Building mine = _buildings.FirstOrDefault(b => MinesId.Contains(b.id));
        //    if (mine == null)
        //        return false;

        //    int numShipments = mine.shipmentsReady;
        //    if (numShipments < 1)
        //        return false;

        //    GarrisonBuddy.Log("Buildings: Detected " + numShipments + " shipments to collect from mine.");

        //    WoWGameObject mineShipment =
        //        ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235886);
        //    if (mineShipment == null)
        //    {
        //        return
        //            await
        //                MoveTo(Me.IsAlliance ? MineShipmentAlly : MineShipmentHorde,
        //                    "Default location for mine shipments");
        //    }

        //    if (await MoveTo(mineShipment.Location, "Collecting mine shipments"))
        //        return true;

        //    mineShipment.Interact();
        //    RefreshBuildings();
        //    return true;
        //}


        //private static async Task<bool> PickUpGardenWorkOrders()
        //{
        //    if (!GaBSettings.Mono.ShipmentsGarden)
        //        return false;

        //    Building garden = _buildings.FirstOrDefault(b => GardensId.Contains(b.id));
        //    if (garden == null)
        //        return false;


        //    if ( garden.shipmentsReady == 0)
        //        return false;

        //    GarrisonBuddy.Log("Buildings: Detected " + garden.shipmentsReady + " shipments to collect from garden.");

        //    WoWGameObject gardenShipment =
        //        ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235885);
        //    if (gardenShipment == null)
        //    {
        //        return
        //            await
        //                MoveTo(Me.IsAlliance ? GardenShipmentAlly : GardenShipmentHorde,
        //                    "Default location for garden shipments");
        //    }

        //    if (await MoveTo(gardenShipment.Location, "Collecting garden shipments"))
        //        return true;

        //    gardenShipment.Interact();
        //    RefreshBuildings();
        //    return true;
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


        // DOESNT WORK SINCE SOME WORK ORDER ARE ALWAYS THERE! 
        //public static async Task<bool> PickUpAllWorkOrders()
        //{
        //    var shipments = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.SubType == WoWGameObjectType.GarrisonShipment).OrderBy(o=>o.Entry);

        //    foreach (var shipment in shipments)
        //    {
        //        if (await MoveTo(shipment.Location))
        //            return true;
        //        shipment.Interact();

        //    }
        //    return true;
        //}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Bots.Quest.QuestOrder;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
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

        #endregion

        private static void InitializeShipments()
        {
            RefreshBuildings();
        }


        private static bool ShouldRunStartOrder()
        {
            Building d;
            return CanRunStartOrder(out d);
        }

        private static bool CanRunStartOrder(out Building buildingWithShipmentsToStart)
        {
            buildingWithShipmentsToStart = null;

            if (!GaBSettings.Mono.StartOrder)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Deactivated in user settings.");
                return false;
            }

            var buildingsShipmentLeft =
                _buildings.Where(b => BuildingsLua.GetNumberShipmentLeftToStart(b.id) > 0);
            if (!buildingsShipmentLeft.Any())
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] No buildings with shipment left to start.");
                return false;
            }
            var buildingsShipmentActivated = buildingsShipmentLeft.Where(b => b.CollectShipment);
            if (!buildingsShipmentActivated.Any())
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Buildings with shipments but none activated.");
                return false;
            }

            var buildingsToStart = buildingsShipmentActivated.Where(b => b.canCompleteOrder()).OrderBy(b => b.id);
            if (!buildingsToStart.Any())
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Can't complete work orders, missing reagents.");
                return false;
            }

            GarrisonBuddy.Diagnostic("[ShipmentStart] #buildings {0} - first {1} - #Max {2}", buildingsToStart.Count(), buildingsToStart.First().name, buildingsToStart.First().shipmentCapacity);
            buildingWithShipmentsToStart = buildingsToStart.First();
            return true;
        }

        private static async Task<bool> StartOrder()
        {
            Building buildingWithShipmentsToStart;

            if (!CanRunStartOrder(out buildingWithShipmentsToStart))
                return false;

            GarrisonBuddy.Log("[ShipmentStart] Moving to start work order:" + buildingWithShipmentsToStart.name);

            WoWUnit unit = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == buildingWithShipmentsToStart.PnjId);
            if (unit == null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] Could not find unit (" + buildingWithShipmentsToStart.PnjId + "), moving to default location.\n" +
                                         "If this message is spammed, please post the ID of the PNJ for your work orders on the forum post of Garrison Buddy!");

                ObjectManager.Update();
                await MoveTo(buildingWithShipmentsToStart.Pnj);
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
                GarrisonBuddy.Warning("[ShipmentStart] Failed to open Work order frame.");
                return true;
            }
            else
            {
                GarrisonBuddy.Log("[ShipmentStart] Work order frame opened.");
            }

            // Interesting events to check out : Shipment crafter opened/closed, shipment crafter info, gossip show, gossip closed, 
            // bag update delayed is the last fired event when adding a work order.  

            int NumberToStart = BuildingsLua.GetCapacitiveFrameMaxShipments();

            for (int i = NumberToStart; i > 0; i--)
            {
                InterfaceLua.ClickStartOrderButton();
                await Buddy.Coroutines.Coroutine.Yield();
            }
            StartOrderTriggered = false;
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
                frame.SelectGossipOption(1);
                frame.SelectGossipOption(0);
            }
            return true;
        }


        private static WoWGuid lastSeen = WoWGuid.Empty;

        private static bool CanRunPickUpOrder()
        {
            if (!GaBSettings.Mono.CollectingShipments)
                return false;

            WoWGameObject ShipmentToCollect;
            if (lastSeen != WoWGuid.Empty)
            {
                ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Guid == lastSeen && (o.DisplayId == 16091 || o.DisplayId == 19959));
                if (ShipmentToCollect == null)
                {
                    ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.SubType == WoWGameObjectType.GarrisonShipment && (o.DisplayId == 16091 || o.DisplayId == 19959));
                }
            }
            else
            {
                ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.SubType == WoWGameObjectType.GarrisonShipment && (o.DisplayId == 16091 || o.DisplayId == 19959));
            }

            if (ShipmentToCollect == null)
            {
                return false;
            }
            lastSeen = ShipmentToCollect.Guid;
            return true;
        }
        private static async Task<bool> PickUpAllWorkOrder()
        {
            if (!CanRunPickUpOrder())
                return false;

            WoWGameObject ShipmentToCollect = Me.ToGameObject();
            if (lastSeen != WoWGuid.Empty)
            {
                ShipmentToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Guid == lastSeen && (o.DisplayId == 169091 || o.DisplayId == 19959));
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
                return false;
            }
            lastSeen = ShipmentToCollect.Guid;
            GarrisonBuddy.Log("Found shipment to collect(" + ShipmentToCollect.Name + "), moving to shipment.");
            GarrisonBuddy.Diagnostic("Shipment " + ShipmentToCollect.SafeName + " - " + ShipmentToCollect.Entry + " - " +
                                     ShipmentToCollect.DisplayId + ": " + ShipmentToCollect.Location);
            if(await HarvestWoWGameOject(ShipmentToCollect))
                return true;
            lastSeen = WoWGuid.Empty;
            RefreshBuildings(true);
            return true;
        }



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



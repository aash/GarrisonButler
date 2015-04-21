#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.LuaObjects;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {
        private const int BiggerIsBetterAlliance = 36592;
        private const int BiggerIsBetterHorde = 36567;

        internal static readonly List<Shipment> ShipmentsMap = new List<Shipment>
        {
            // Mine
            new Shipment(new List<int>
            {
                61, // lvl 1
                62, // lvl 2
                63 // lvl 3
            },
                35154, 34192),

            // Garden
            new Shipment(new List<int>
            {
                29, // lvl 1
                136, // lvl 2
                137 // lvl 3
            },
                34193, 36404),

            #region large

            // Barracks
            new Shipment(new List<int>
            {
                26, // lvl 1
                27, // lvl 2
                28 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Dwarven Bunker
            new Shipment(new List<int>
            {
                8, // lvl 1
                9, // lvl 2
                10 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Gnomish Gearworks
            new Shipment(new List<int>
            {
                162, // lvl 1
                163, // lvl 2
                164 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Mage Tower
            new Shipment(new List<int>
            {
                37, // lvl 1
                38, // lvl 2
                39 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Stables
            new Shipment(new List<int>
            {
                65, // lvl 1
                66, // lvl 2
                67 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            #endregion
            #region medium

            // Barn
            new Shipment(new List<int>
            {
                24, // lvl 1
                25, // lvl 2
                133 // lvl 3
            },
                36345, 36271), // Breaking into the Trap Game

            // Gladiator's Sanctum
            new Shipment(new List<int>
            {
                159, // lvl 1
                160, // lvl 2
                161 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Lumber Mill
            new Shipment(new List<int>
            {
                40, // lvl 1
                41, // lvl 2
                138 // lvl 3
            },
                36138, 36192),

            // Trading Post
            new Shipment(new List<int>
            {
                111, // lvl 1
                144, // lvl 2
                145 // lvl 3
            },
                37062, 37088),

            // Inn / Tavern
            new Shipment(new List<int>
            {
                34, // lvl 1
                35, // lvl 2
                36 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            #endregion
            #region small

            // Alchemy Lab
            new Shipment(new List<int>
            {
                76, // lvl 1
                119, // lvl 2
                120 // lvl 3
            },
                37568, 36641),

            // Enchanter's Study
            new Shipment(new List<int>
            {
                93, // lvl 1
                125, // lvl 2
                126 // lvl 3
            },
                37570, 36645),

            // Gem Boutique
            new Shipment(new List<int>
            {
                96, // lvl 1
                131, // lvl 2
                132 // lvl 3
            },
                37573, 36644),

            // Salvage Yard
            new Shipment(new List<int>
            {
                52, // lvl 1
                140, // lvl 2
                141 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance),

            // Scribe's Quarters
            new Shipment(new List<int>
            {
                95, // lvl 1
                129, // lvl 2
                130 // lvl 3
            },
                37572, 36647),

            // Storehouse
            new Shipment(new List<int>
            {
                51, // lvl 1
                142, // lvl 2
                143 // lvl 3
            },
                BiggerIsBetterHorde, BiggerIsBetterAlliance), // 37060 - Lost in Transition - One time quest (extra??)

            // Tailoring Emporium
            new Shipment(new List<int>
            {
                94, // lvl 1
                127, // lvl 2
                128 // lvl 3
            },
                37575, 36643),

            // The Forge
            new Shipment(new List<int>
            {
                60, // lvl 1
                117, // lvl 2
                118 // lvl 3
            },
                37569, 35168),

            // The Tannery
            new Shipment(new List<int>
            {
                90, // lvl 1
                121, // lvl 2
                122 // lvl 3
            },
                37574, 36642),

            // Engineering Works
            new Shipment(new List<int>
            {
                91, // lvl 1
                123, // lvl 2
                124 // lvl 3
            },
                37571, 36646)

            // Others? 
        };

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

        #endregion

        public static void InitializeShipments()
        {
            RefreshBuildings(true);
        }


        internal struct Shipment
        {
            public readonly List<int> BuildingIds;
            public readonly int ShipmentPreQuestIdHorde;
            public readonly int ShipmentPreQuestIdAlliance;

            public bool CompletedPreQuest
            {
                get
                {
                    if (Me == null)
                    {
                        GarrisonButler.Diagnostic("Error in class Shipment getting completedPreQuest - Me == null");
                        return false;
                    }

                    if (!Me.IsValid)
                    {
                        GarrisonButler.Diagnostic(
                            "Error in class Shipment getting completedPreQuest - Me.IsValid = false");
                        return false;
                    }

                    if (ShipmentPreQuestIdHorde == 0 || ShipmentPreQuestIdAlliance == 0)
                        return false;

                    var helper = new ProfileHelperFunctionsBase();

                    var questToUse = Me.IsAlliance ? (uint) ShipmentPreQuestIdAlliance : (uint) ShipmentPreQuestIdHorde;

                    var returnValue = helper.IsQuestCompleted(questToUse);

                    return returnValue;
                }
            }

            public Shipment(List<int> buildingIds, int shipmentPreQuestIdHorde, int shipmentPreQuestIdAlliance)
                : this()
            {
                BuildingIds = buildingIds;
                ShipmentPreQuestIdHorde = shipmentPreQuestIdHorde;
                ShipmentPreQuestIdAlliance = shipmentPreQuestIdAlliance;
            }
        }
    }
}
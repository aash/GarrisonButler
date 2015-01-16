#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.Profiles.Quest.Order;
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
            BiggerIsBetterHorde, BiggerIsBetterAlliance),

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

        private static void InitializeShipments()
        {
            RefreshBuildings();
        }

        private static bool ShouldRunPickUpOrStartShipment()
        {
            return
                _buildings.Any(
                    building =>
                        CanPickUpShipmentGeneration(building)().Item1 || CanStartShipmentGeneration(building)().Item1);
            }
        
        internal static ActionHelpers.ActionsSequence PickUpOrStartSequenceAll()
        {
            var sequence = new ActionHelpers.ActionsSequence();
            foreach (var building in _buildings)
            {
                sequence.AddAction(new ActionHelpers.ActionOnTimerCached<WoWGameObject>(PickUpShipment,
                    CanPickUpShipmentGeneration(building)));
                sequence.AddAction(new ActionHelpers.ActionOnTimerCached<Building>(StartShipment,
                    CanStartShipmentGeneration(building)));
            }
            return sequence;
        }

        internal static ActionHelpers.ActionsSequence PickUpOrStartSequence(Building building)
        {
            var sequence = new ActionHelpers.ActionsSequence();

            sequence.AddAction(new ActionHelpers.ActionOnTimerCached<WoWGameObject>(PickUpShipment,
                CanPickUpShipmentGeneration(building)));
            sequence.AddAction(new ActionHelpers.ActionOnTimerCached<Building>(StartShipment,
                CanStartShipmentGeneration(building)));

            return sequence;
        }
        
        [SuppressMessage("ReSharper", "InvertIf")]
        internal static Func<Tuple<bool, Building>> CanStartShipmentGeneration(Building building)
        {
            return () =>
            {
                if (building == null)
                {
                    GarrisonButler.Diagnostic(
                        "[ShipmentStart] Building is null, either not built or not properly scanned.");
                    return new Tuple<bool, Building>(false, null);
                }

                building.Refresh();
                var buildingsettings = GaBSettings.Get().GetBuildingSettings(building.Id);
                if (buildingsettings == null)
                    return new Tuple<bool, Building>(false, null);

                // Activated by user ?
                if (!buildingsettings.CanStartOrder)
                {
                    GarrisonButler.Diagnostic("[ShipmentStart,{0}] Deactivated in user settings: {1}", building.Id,
                        building.Name);
                    return new Tuple<bool, Building>(false, null);
                }

                if (building.WorkFrameWorkAroundTries >= Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
                {
                    GarrisonButler.Warning(
                        "[ShipmentStart,{0}] Building has been blacklisted due to reaching maximum Blizzard Workframe Bug workaround tries ({1})",
                        building.Id, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                    return new Tuple<bool, Building>(false, null);
                }

                // No Shipment left to start
                if (building.NumberShipmentLeftToStart() <= 0)
                {
                    GarrisonButler.Diagnostic("[ShipmentStart,{0}] No shipment left to start: {1}", building.Id,
                        building.Name);
                    return new Tuple<bool, Building>(false, null);
                }

                // Structs cannot be null
                var shipmentObjectFound = ShipmentsMap.FirstOrDefault(s => s.BuildingIds.Contains(building.Id));

                if (!shipmentObjectFound.CompletedPreQuest)
                {
                    GarrisonButler.Warning("[ShipmentStart,{0}] Cannot collect shipments until pre-quest is done: {1}",
                        building.Id, building.Name);
                    GarrisonButler.Diagnostic("[ShipmentStart,{0}] preQuest not completed A={2} H={3}: {1}",
                        building.Id, building.Name, shipmentObjectFound.ShipmentPreQuestIdAlliance,
                        shipmentObjectFound.ShipmentPreQuestIdHorde);
                    return new Tuple<bool, Building>(false, null);
                }


                // Under construction
                if (building.IsBuilding || building.CanActivate)
                {
                    GarrisonButler.Diagnostic(
                        "[ShipmentStart,{0}] Building under construction, can't start work order: {1}",
                        building.Id, building.Name);
                    return new Tuple<bool, Building>(false, null);
                }
                var maxToStart = GetMaxShipmentToStart(building);

                // max start by user ?
                if (maxToStart <= 0)
                {
                    GarrisonButler.Diagnostic("[ShipmentStart,{0}] Can't start more work orders. {1} - ShipmentsTotal={2}, MaxCanStartOrder={3}",
                        building.Id,
                        building.Name,
                        building.ShipmentsTotal,
                        buildingsettings.MaxCanStartOrder);
                    return new Tuple<bool, Building>(false, null);
                }
                //MaxToStart = Math.Min(building.canCompleteOrder(), MaxToStart);
                //// Do I fulfill the conditions to complete the order? 
                //if (MaxToStart <= 0)
                //{
                //    GarrisonButler.Diagnostic(
                //        "[ShipmentStart] Do not fulfill the requirements to start a new work order: {0}", building.name);
                //    return new Tuple<bool, Building>(false, null);
                //}

                GarrisonButler.Diagnostic("[ShipmentStart,{0}] Found {1} new work orders to start: {2}",
                    building.Id, maxToStart, building.Name);
                return new Tuple<bool, Building>(true, building);
            };
        }
        
        [SuppressMessage("ReSharper", "InvertIf")]
        internal static Func<Tuple<bool, WoWGameObject>> CanPickUpShipmentGeneration(Building building)
        {
            return () =>
            {
                if (building == null)
                {
                    GarrisonButler.Diagnostic(
                        "[ShipmentPickUp] Building is null, either not built or not properly scanned.");
                    return new Tuple<bool, WoWGameObject>(false, null);
                }

                building.Refresh();

                // No Shipment ready
                if (building.ShipmentsReady <= 0)
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp] No shipment left to pickup: {0}", building.Name);
                    return new Tuple<bool, WoWGameObject>(false, null);
                }
                var buildingsettings = GaBSettings.Get().GetBuildingSettings(building.Id);
                if (buildingsettings == null)
                    return new Tuple<bool, WoWGameObject>(false, null);

                // Activated by user ?
                if (!buildingsettings.CanCollectOrder)
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp] Deactivated in user settings: {0}", building.Name);
                    return new Tuple<bool, WoWGameObject>(false, null);
                }

                // Get the list of the building objects
                var buildingAsObject =
                    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .Where(o => building.Displayids.Contains(o.DisplayId))
                        .OrderBy(o => o.DistanceSqr)
                        .FirstOrDefault();
                if (buildingAsObject == default(WoWGameObject))
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp] Building could not be found in the area: {0}",
                        building.Name);
                    foreach (var id in building.Displayids)
                    {
                        GarrisonButler.Diagnostic("[ShipmentPickUp]     ID {0}", id);
                    }
                    return new Tuple<bool, WoWGameObject>(false, null);
                }

                GarrisonButler.Diagnostic("[ShipmentPickUp] Found {0} shipments to collect: {1}",
                    building.ShipmentsReady,
                    building.Name);
                return new Tuple<bool, WoWGameObject>(true, buildingAsObject);
            };
        }

        private static async Task<ActionResult> StartShipment(Building building)
        {
            if (building == null)
            {
                GarrisonButler.Diagnostic("[StartShipment] ERROR - Building passed in to StartShipment() was null");
                return ActionResult.Done;
            }

            var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
            if (unit == null)
            {
                await
                    MoveTo(building.Pnj,
                        String.Format("[ShipmentStart,{0}] Could not find unit ({1}), moving to default location.",
                            building.Id, building.PnjId));
                return ActionResult.Running;
            }

            if (await MoveToInteract(unit) == ActionResult.Running)
                return ActionResult.Running;

            unit.Interact();

            await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                var res = InterfaceLua.IsGarrisonCapacitiveDisplayFrame();
                if (!res)
                {
                    Navigator.PlayerMover.MoveTowards(unit.Location); 
                }
                else
                {
                    GarrisonButler.Diagnostic("[ShipmentStart,{0}] Found GarrisonCapacitiveDisplayFrame, no need to do workaround bug.",
                        building.Id);
                }
                return res;
            });

            await CommonCoroutines.SleepForRandomUiInteractionTime();

            if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                var gossipFrame = GossipFrame.Instance;
                // Will try workaround if GossipFrame isn't valid/visible & GarrisonFrame isn't valid
                var shouldTryWorkAround = !InterfaceLua.IsGarrisonCapacitiveDisplayFrame()
                    && (gossipFrame == null ? true : !gossipFrame.IsVisible);
                if (shouldTryWorkAround)
                {
                    unit.Interact();
                }
                return shouldTryWorkAround;
            }))
            {
                if (building.WorkFrameWorkAroundTries < Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
                    building.WorkFrameWorkAroundTries++;
                else
                {
                    GarrisonButler.Warning(
                        "[ShipmentStart,{0}] ERROR - NOW BLACKLISTING BUILDING {1} REACHED MAX TRIES FOR WORKFRAME/GOSSIP WORKAROUND ({2})",
                        building.Id, building.Name, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                    await ButlerLua.CloseLandingPage();
                    return ActionResult.Done;
                }
                GarrisonButler.Warning(
                    "[ShipmentStart,{0}] Failed to open Work order or Gossip frame. Maybe Blizzard bug, trying to move away.  Try #{1} out of {2} max.",
                    building.Id, building.WorkFrameWorkAroundTries, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                await WorkAroundBugFrame();
                return ActionResult.Running;
            }
            building.WorkFrameWorkAroundTries = 0;


            // Only returns ActionResult.Done or ActionResult.Failed
            // Returning ActionResult.Done means it is the GarrisonCapacitiveFrame
            if (await IfGossip(unit) == ActionResult.Failed)
            {
                return ActionResult.Running;
            }

            // One more check to make sure this is the right frame!!!
            if (!InterfaceLua.IsGarrisonCapacitiveDisplayFrame())
            {
                if (building.workFrameWorkAroundTries < Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
                    building.workFrameWorkAroundTries++;
                else
                {
                    GarrisonButler.Warning("[ShipmentStart,{0}] ERROR - NOW BLACKLISTING BUILDING {1} REACHED MAX TRIES FOR WORKFRAME WORKAROUND ({2})",
                        building.Id, building.Name, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                    await ButlerLua.CloseLandingPage();
                    return ActionResult.Done;
                }
                GarrisonButler.Warning(
                    "[ShipmentStart,{0}] Failed to open Work order frame. Maybe Blizzard bug, trying to move away.  Try #{1} out of {2} max.",
                    building.Id, building.workFrameWorkAroundTries, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                await WorkAroundBugFrame();
                return ActionResult.Running;
            }
            building.workFrameWorkAroundTries = 0;

            GarrisonButler.Log("[ShipmentStart] Work order frame opened.");

            // Interesting events to check out : Shipment crafter opened/closed, shipment crafter info, gossip show, gossip closed, 
            // bag update delayed is the last fired event when adding a work order.  

            var maxToStart = GetMaxShipmentToStart(building);

            for (var i = 0; i < maxToStart; i++)
            {
                InterfaceLua.ClickStartOrderButton();
                building.Refresh();
                await CommonCoroutines.SleepForRandomUiInteractionTime();
                await Buddy.Coroutines.Coroutine.Yield();
            }

            for (var i = 0; i < 10; i++)
            {
                var buildingShipment = _buildings.FirstOrDefault(b => b.Id == building.Id);
                if (buildingShipment != null)
                {
                    await ButlerLua.OpenLandingPage();
                    buildingShipment.Refresh();
                    var max = GetMaxShipmentToStart(buildingShipment);
                    if (max == 0)
                    {
                        GarrisonButler.Log("[ShipmentStart] Finished starting work orders at {0}.",
                            buildingShipment.Name);
                        await ButlerLua.CloseLandingPage();
                        return ActionResult.Done;
                    }
                        GarrisonButler.Diagnostic("[ShipmentStart] Waiting for shipment to update.");
                    }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            await ButlerLua.CloseLandingPage();
            return ActionResult.Refresh;
        }

        private static int GetMaxShipmentToStart(Building building)
        {
            var maxSettings = GaBSettings.Get().GetBuildingSettings(building.Id).MaxCanStartOrder; 
            var maxInProgress = maxSettings == 0
                ? building.ShipmentCapacity
                : Math.Min(building.ShipmentCapacity, maxSettings);
            var maxToStart = maxInProgress - building.ShipmentsTotal;
            maxToStart = Math.Min(building.CanCompleteOrder(), maxToStart);
            GarrisonButler.Diagnostic("GetMaxShipmentToStart: maxSettings={0} maxInProgress={1} ShipmentCapacity={2} CanCompleteOrder={3} maxToStart={4}",
                maxSettings, maxInProgress, building.ShipmentCapacity, building.CanCompleteOrder(), maxToStart);
            return maxToStart;
        }

        private static async Task WorkAroundBugFrame()
        {
            var keepGoing = true;
            var workaroundTimer = new Stopwatch();
            workaroundTimer.Start();
            
            // Total time to try workaround is 5s
            // Need to do it this way because MoveToTable is a Task which returns true
            // when it needs to do more work (such as between MoveTo pulses)
            while (keepGoing && (workaroundTimer.ElapsedMilliseconds < 6000))
            {
                //var task = MoveToTable();
                var result = await Buddy.Coroutines.Coroutine.ExternalTask(Task.Run(new System.Func<Task<bool>>(MoveToTable)), 6000);
                keepGoing = result.Completed && result.Result;
                await Buddy.Coroutines.Coroutine.Yield();
            }
        }


        /// <summary>
        /// Will return immediately if the frame detected is GarrisonCapacitiveDisplayFrame.
        /// </summary>
        /// <param name="pnj"></param>
        /// <returns>Only returns ActionResult.Done or ActionResult.Failed.  ActionResult.Done is returned when GarrisonFrame found.</returns>
        private async static Task<ActionResult> IfGossip(WoWUnit pnj)
        {
            // STEP 0 - Return if GarrisonFrame detected
            if (InterfaceLua.IsGarrisonCapacitiveDisplayFrame())
            {
                GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Done due to IsGarrisonCapacitiveDisplayFrame()");
                return ActionResult.Done;
            }

            // STEP 1 - Return if unit isn't valid or null
            if (pnj == null)
            {
                GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Failed due to pnj==null");
                return ActionResult.Failed;
            }

            if (pnj.IsValid == false)
            {
                GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Failed due to pnj.IsValid==false");
                return ActionResult.Failed;
            }

            GossipFrame frame = GossipFrame.Instance;

            // STEP 2 - Return if gossip frame not valid / null
            if (frame == null)
            {
                GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Failed due to gossip frame null");
                return ActionResult.Failed;
            }

            if(frame.IsVisible == false)
            {
                GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Failed due to gossip frame not visible");
                return ActionResult.Failed;
            }

            // STEP 3 - Enumerate the possible entries to a cached data structure
            var cachedEntryIndexes = new int[frame.GossipOptionEntries.GetEmptyIfNull().Count()];
            for (int i = 0; i < cachedEntryIndexes.Length; i++)
            {
                cachedEntryIndexes[i] = frame.GossipOptionEntries[i].Index;
            }
            GarrisonButler.Diagnostic("[Gossip,{0}] Found {1} possible options.", pnj.Entry, cachedEntryIndexes.Length);
            
            // STEP 4 - Go through all of the CACHED gossip entries and find the right one.
            //          Each entry has a 10s timeout to complete a loop in the foreach
            foreach (var cachedIndex in cachedEntryIndexes)
            {
                var timeoutTimer = new WaitTimer(TimeSpan.FromSeconds(10));
                var atLeastOne = true;
                frame = GossipFrame.Instance;
                GarrisonButler.Diagnostic("[Gossip,{0}] Trying option: {1}", pnj.Entry, cachedIndex);

                // STEP 4a - Attempt to open the frame if it is not open
                //           a) Tries to move to the unit
                //           b) Tries to interact with unit
                timeoutTimer.Reset();
                while (((frame.GossipOptionEntries == null ||
                    frame.GossipOptionEntries.Count <= 0) 
                    && !timeoutTimer.IsFinished) || atLeastOne)
                {
                    if (await MoveToInteract(pnj) == ActionResult.Running)
                    {
                        await Buddy.Coroutines.Coroutine.Yield(); // return ActionResult.Running;
                        //ActionResult.Runing can happen in these cases:
                        // MoveResult.Moved
                        // MoveResult.PathGenerated
                        // MoveResult.PathGenerationFailed
                        // MoveResult.UnstuckAttempt
                        continue;
                    }

                    pnj.Interact();
                    await CommonCoroutines.SleepForLagDuration();
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                    frame = GossipFrame.Instance;
                    await Buddy.Coroutines.Coroutine.Yield();
                    atLeastOne = false;
                }

                // STEP 4b - Check that this index is still valid
                if(frame == null || frame.GossipOptionEntries.GetEmptyIfNull().All(o => o.Index != cachedIndex))
                    continue;

                // STEP 4c - Attempt to select the gossip option
                frame.SelectGossipOption(cachedIndex);
                await CommonCoroutines.SleepForLagDuration();
                await CommonCoroutines.SleepForRandomUiInteractionTime();

                // STEP 4d - Return if the GarrisonCapacitiveDisplayFrame was found
                if (InterfaceLua.IsGarrisonCapacitiveDisplayFrame())
                    return ActionResult.Done;

                // STEP 4e - Close this gossip frame because it didn't end up being the correct gossip chosen
                await Buddy.Coroutines.Coroutine.Yield();
                var newFrame = GossipFrame.Instance;
                if (newFrame != null)
                {
                    await Buddy.Coroutines.Coroutine.Wait(5000, () =>
                    {
                        newFrame.Close();
                        newFrame = GossipFrame.Instance;
                        return (newFrame.GossipOptionEntries == null ||
                    newFrame.GossipOptionEntries.Count <= 0);
                    });
                    await CommonCoroutines.SleepForLagDuration();
                    await CommonCoroutines.SleepForRandomUiInteractionTime();
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }

            GarrisonButler.Diagnostic("[Gossip] Returning ActionResult.Failed at end of function");
            return ActionResult.Failed;
        }


        private static async Task<ActionResult> PickUpShipment(WoWGameObject building)
        {
            WoWPoint locationToLookAt;

            // Fix for the mine position
            var minesIds = _buildings.Where(
                b =>
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl1) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl2) ||
                    (b.Id == (int) global::GarrisonButler.Buildings.MineLvl3)).SelectMany(b => b.Displayids);
            if (minesIds.Contains(building.DisplayId))
                locationToLookAt = Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
            else
                locationToLookAt = building.Location;

            // Search for shipment next to building
            var shipmentToCollect =
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
                            "[ShipmentCollect] Moving to Building to search for shipment to pick up.") ==
                    ActionResult.Running)
                    return ActionResult.Running;
            }
            else
            {
                var actionResult = await HarvestWoWGameObjectCachedLocation(shipmentToCollect);
                if (actionResult == ActionResult.Running)
                    return ActionResult.Running;

                await ButlerLua.OpenLandingPage();

                if (actionResult == ActionResult.Refresh)
                {
                    await ButlerLua.CloseLandingPage(); 
                    return ActionResult.Refresh;
                }


                for (var i = 0; i < 10; i++)
                {
                    var buildingShipment = _buildings.FirstOrDefault(b => b.Displayids.Contains(building.DisplayId));
                    if (buildingShipment != null)
                    {
                        await ButlerLua.OpenLandingPage();
                        buildingShipment.Refresh();
                        if (buildingShipment.ShipmentsReady == 0)
                        {
                            GarrisonButler.Log("[ShipmentCollect] Finished collecting.");
                            await ButlerLua.CloseLandingPage();
                            return ActionResult.Done;
                        }
                        GarrisonButler.Diagnostic("[ShipmentCollect] Waiting for shipment to update.");
                    }
                    await Buddy.Coroutines.Coroutine.Yield();
                }
                await ButlerLua.CloseLandingPage();
                return ActionResult.Refresh;
            }
            await ButlerLua.CloseLandingPage();
            return ActionResult.Done; // should never reach that point!
        }

        private struct Shipment
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
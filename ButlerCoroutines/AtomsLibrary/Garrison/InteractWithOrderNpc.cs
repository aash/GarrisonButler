using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Libraries;
using GarrisonButler.LuaObjects;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    class InteractWithOrderNpc : Atom
    {
        private WoWUnit _unit = null; 
        public override string Name()
        {
            return "[InteractWithOrderNpc|"+_building.Name+"]";
        }
        private Building _building;
        public InteractWithOrderNpc(Building building)
        {
            _building = building;

            if (_building.PnjIds != null && _building.PnjIds.Count != 0)
                Dependencies.Add(new MoveToObject(_building.PnjIds, WoWObjectTypeFlag.Unit, _building.Pnj, 3));
            else
                Dependencies.Add(new MoveToObject((uint)_building.PnjId, WoWObjectTypeFlag.Unit, _building.Pnj, 3));
        }
        /// <summary>
        /// Must have the building?
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return true;
        }

        /// <summary>
        /// Fulfilled when capacitive display frame opened
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            return CapacitiveDisplayFrame.Instance != null 
                && Dependencies.All(d => d.IsFulfilled() 
                    && _unit != null);
        }
        /// <summary>
        /// The dependency is to be next to the PNJ, so we just have to open the frame
        /// </summary>
        /// <returns></returns>
        public async override Task Action()
        {
            if (_unit == null)
            {
                // Search PNJ
                _unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().GetEmptyIfNull()
                    .FirstOrDefault(
                        u => _building.PnjIds != null ? _building.PnjIds.Contains(u.Entry) : u.Entry == _building.PnjId);
            }

            if (_unit == null)
            {
                return;
            }

            // Interact with PNJ
            GarrisonButler.Diagnostic("[ShipmentStart,{0}] Interacting with ({1}).",
                            _building.Id, _unit.Entry);


            _unit.Interact();
            await CommonCoroutines.SleepForLagDuration();
            await CommonCoroutines.SleepForRandomUiInteractionTime();


            if (!await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                var gossipFrame = GossipFrame.Instance;
                // Will try workaround if GossipFrame isn't valid/visible & GarrisonFrame isn't valid
                var shouldTryWorkAround = CapacitiveDisplayFrame.Instance == null
                                          && (gossipFrame == null || !gossipFrame.IsVisible);
                GarrisonButler.Diagnostic("test test test test test test test ");
                if (shouldTryWorkAround)
                {
                    _unit.Interact();
                }
                return !shouldTryWorkAround;
            }))
            {
                if (_building.WorkFrameWorkAroundTries < Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
                {
                    GarrisonButler.Diagnostic("test2222222222 test test test test test test ");
                    _building.WorkFrameWorkAroundTries++;
                }
                else
                {
                    GarrisonButler.Warning(
                        "[ShipmentStart,{0}] ERROR - NOW BLACKLISTING BUILDING {1} REACHED MAX TRIES FOR WORKFRAME/GOSSIP WORKAROUND ({2})",
                        _building.Id, _building.Name, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                    //await ButlerLua.CloseLandingPage();
                    return;
                }
                GarrisonButler.Warning(
                    "[ShipmentStart,{0}] Failed to open Work order or Gossip frame. Maybe Blizzard bug, trying to move away.  Try #{1} out of {2} max.",
                    _building.Id, _building.WorkFrameWorkAroundTries, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                await WorkAroundBugFrame();
                Status = new Result(ActionResult.Running);
                return;
            }
            _building.WorkFrameWorkAroundTries = 0;

            GarrisonButler.Diagnostic("test3333333 test test test test test test ");

            // Only returns ActionResult.Done or ActionResult.Failed
            // Returning ActionResult.Done means it is the GarrisonCapacitiveFrame
            if (await IfGossip(_unit) == ActionResult.Failed)
            {
                Status = new Result(ActionResult.Running);
                return;
            }
            GarrisonButler.Diagnostic("test444444 test test test test test test ");

            // One more check to make sure this is the right frame!!!
            if (CapacitiveDisplayFrame.Instance == null)
            {
                if (_building.workFrameWorkAroundTries < Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
                    _building.workFrameWorkAroundTries++;
                else
                {
                    GarrisonButler.Warning(
                        "[ShipmentStart,{0}] ERROR - NOW BLACKLISTING BUILDING {1} REACHED MAX TRIES FOR WORKFRAME WORKAROUND ({2})",
                        _building.Id, _building.Name, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                    //await ButlerLua.CloseLandingPage();

                    Status = new Result(ActionResult.Done);
                    return;
                }
                GarrisonButler.Warning(
                    "[ShipmentStart,{0}] Failed to open Work order frame. Maybe Blizzard bug, trying to move away.  Try #{1} out of {2} max.",
                    _building.Id, _building.workFrameWorkAroundTries, Building.WorkFrameWorkAroundMaxTriesUntilBlacklist);
                await WorkAroundBugFrame();

                Status = new Result(ActionResult.Running);
                return;
            }
            _building.workFrameWorkAroundTries = 0;

            GarrisonButler.Log("[ShipmentStart] Work order frame opened.");

            Status = new Result(ActionResult.Done);
            return;
        }

        private static async Task<ActionResult> IfGossip(WoWUnit pnj)
        {
            // STEP 0 - Return if GarrisonFrame detected
            if (CapacitiveDisplayFrame.Instance != null)
            {
                GarrisonButler.Diagnostic(
                    "[Gossip] Returning ActionResult.Done due to IsGarrisonCapacitiveDisplayFrame()");
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

            if (frame.IsVisible == false)
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
                    if (pnj.Location.Distance(StyxWoW.Me.Location) > pnj.InteractRange)
                    {
                        Navigator.MoveTo(pnj.Location);
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
                if (frame == null || frame.GossipOptionEntries.GetEmptyIfNull().All(o => o.Index != cachedIndex))
                    continue;

                // STEP 4c - Attempt to select the gossip option
                frame.SelectGossipOption(cachedIndex);
                await CommonCoroutines.SleepForLagDuration();
                await CommonCoroutines.SleepForRandomUiInteractionTime();

                // STEP 4d - Return if the GarrisonCapacitiveDisplayFrame was found
                if (CapacitiveDisplayFrame.Instance != null)
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
                var result =
                    await Buddy.Coroutines.Coroutine.ExternalTask(Task.Run(new Func<Task<bool>>(ButlerCoroutine.MoveToTable)), 6000);
                keepGoing = result.Completed && result.Result;
                await Buddy.Coroutines.Coroutine.Yield();
            }
        }
    }
}

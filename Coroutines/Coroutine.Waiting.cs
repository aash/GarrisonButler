using GarrisonButler.API;
using GarrisonButler.Coroutines;

#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonButler.Config;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;

#endregion

// ReSharper disable once CheckNamespace

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static bool _hbRelogSkipped;
        private static int _hbRelogSkippedCounter;

        private static async Task<ActionResult> Waiting()
        {
            int townHallLevel = BuildingsLua.GetTownHallLevel();
            if (townHallLevel < 1)
                return ActionResult.Failed;

            List<WoWPoint> myFactionWaitingPoints;
            if (Me.IsAlliance)
                myFactionWaitingPoints = AllyWaitingPoints;
            else
                myFactionWaitingPoints = HordeWaitingPoints;

            if (myFactionWaitingPoints[townHallLevel - 1] == new WoWPoint())
            {
                throw new NotImplementedException(
                    "This level of garrison is not supported! Please upgrade at least to level 2 the main building.");
            }
            GarrisonButler.Log("You Garrison has been taken care of! Waiting for orders...");
            return ActionResult.Done;
        }

        private async static Task<bool> JobDoneSwitch()
        {
            var hbRelogApi = new HBRelogApi();

            if (hbRelogApi.IsConnected && GaBSettings.Get().HBRelogMode)
            {
                if (_hbRelogSkipped == false)
                {
                    GarrisonButler.Log("[HBRelogMode] Skipping current task.");
                    hbRelogApi.SkipCurrentTask(hbRelogApi.CurrentProfileName);
                    _hbRelogSkipped = true;
                }
                else if (_hbRelogSkippedCounter > 10)
                {
                    GarrisonButler.Diagnostic(
                        "[HBRelogMode] Still not closed by HBRelog after 10 ticks, shutting down honorbuddy.");
                    TreeRoot.Shutdown();
                    _hbRelogSkippedCounter = 0;
                }
                else
                {
                    _hbRelogSkippedCounter++;
                    GarrisonButler.Diagnostic("[HBRelogMode] Task skipped, waiting...");
                }
            }
            else if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx)BotManager.Current;
                if (botBase.PrimaryBot.Name.ToLower().Contains("angler"))
                {
                    WoWPoint fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                    GarrisonButler.Log(
                        "You Garrison has been taken care of, bot safe. AutoAngler with Mixed Mode has been detected, moving to fishing area. Happy catch! :)");
                    if (Me.Location.Distance(fishingSpot) > 2)
                    {
                        if (await MoveTo(fishingSpot, "[Waiting] Moving to fishing spot.") == ActionResult.Running)
                            return true;
                    }
                }
            }

            return true;
        }
        private static bool AnythingLeftToDoBeforeEnd()
        {
            if (ReadyToSwitch)
                return false;
            return true;
        }

        public static bool AnythingTodo()
        {
            RefreshBuildings();
            // dailies cd
            if (helperTriggerWithTimer(ShouldRunDailies, ref _dailiesWaitTimer, ref _dailiesTriggered,
                DailiesWaitTimerValue))
                return true;
            // Cache
            if (helperTriggerWithTimer(ShouldRunCache, ref _cacheWaitTimer, ref _cacheTriggered, CacheWaitTimerValue))
                return true;

            // Mine
            if (helperTriggerWithTimer(ShouldRunMine, ref _mineWaitTimer, ref _mineTriggered, MineWaitTimerValue))
                return true;

            // gardenla
            if (helperTriggerWithTimer(ShouldRunGarden, ref _gardenWaitTimer, ref _gardenTriggered, GardenWaitTimerValue))
                return true;

            // Start or pickup work orders
            if (helperTriggerWithTimer(ShouldRunPickUpOrStartShipment, ref _startOrderWaitTimer, ref _startOrderTriggered,
                StartOrderWaitTimerValue))
                return true;

            // Missions
            if (helperTriggerWithTimer(ShouldRunTurnInMissions, ref _turnInMissionWaitTimer, ref _turnInMissionsTriggered,
                TurnInMissionWaitTimerValue))
                return true;

            // Missions completed 
            if (helperTriggerWithTimer(ShouldRunStartMission, ref _startMissionWaitTimer, ref _startMissionTriggered,
                StartMissionWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(ShouldRunSalvage, ref _salvageWaitTimer, ref _salvageTriggered,
                SalvageWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(CanRunLastRound, ref _lastRoundWaitTimer, ref _lastRoundTriggered,
                LastRoundWaitTimerValue))
                return true;

            return AnythingLeftToDoBeforeEnd();
        }


        // The trigger must be set off by someone else to avoid pauses in the behavior! 
        private static bool helperTriggerWithTimer(Func<bool> condition, ref WaitTimer timer, ref bool toModify,
            int timerValueInSeconds)
        {
            if (timer != null && !timer.IsFinished)
                return toModify;

            if (timer == null)
                timer = new WaitTimer(TimeSpan.FromSeconds(timerValueInSeconds));
            timer.Reset();

            if (condition())
                toModify = true;
            else toModify = false;

            return toModify;
        }

        // The trigger must be set off by someone else to avoid pauses in the behavior! 
    }
}
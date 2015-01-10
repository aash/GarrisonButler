using GarrisonButler.API;

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

        private static async Task<bool> Waiting()
        {
            int townHallLevel = BuildingsLua.GetTownHallLevel();
            if (townHallLevel < 1)
                return false;

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
            return false;
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
                        if (await MoveTo(fishingSpot, "[Waiting] Moving to fishing spot."))
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
            if (helperTriggerWithTimer(ShouldRunDailies, ref DailiesWaitTimer, ref DailiesTriggered,
                DailiesWaitTimerValue))
                return true;
            // Cache
            if (helperTriggerWithTimer(ShouldRunCache, ref CacheWaitTimer, ref CacheTriggered, CacheWaitTimerValue))
                return true;

            // Mine
            if (helperTriggerWithTimer(ShouldRunMine, ref MineWaitTimer, ref MineTriggered, MineWaitTimerValue))
                return true;

            // gardenla
            if (helperTriggerWithTimer(ShouldRunGarden, ref GardenWaitTimer, ref GardenTriggered, GardenWaitTimerValue))
                return true;

            // Start or pickup work orders
            if (helperTriggerWithTimer(ShouldRunPickUpOrStartShipment, ref StartOrderWaitTimer, ref StartOrderTriggered,
                StartOrderWaitTimerValue))
                return true;

            // Missions
            if (helperTriggerWithTimer(ShouldRunTurnInMissions, ref TurnInMissionWaitTimer, ref TurnInMissionsTriggered,
                TurnInMissionWaitTimerValue))
                return true;

            // Missions completed 
            if (helperTriggerWithTimer(ShouldRunStartMission, ref StartMissionWaitTimer, ref StartMissionTriggered,
                StartMissionWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(ShouldRunSalvage, ref SalvageWaitTimer, ref SalvageTriggered,
                SalvageWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(CanRunLastRound, ref LastRoundWaitTimer, ref LastRoundTriggered,
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
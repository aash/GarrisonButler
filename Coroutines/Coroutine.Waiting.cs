using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GarrisonLua;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
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
                throw new NotImplementedException();
            }


            if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (botBase.PrimaryBot.Name.ToLower().Contains("angler"))
                {
                    WoWPoint fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                    GarrisonBuddy.Log(
                        "You Garrison has been taken care of, bot safe. AutoAngler with Mixed Mode has been detected, moving to fishing area. Happy catch! :)");
                    if (Me.Location.Distance(fishingSpot) > 2)
                    {
                        if (await MoveTo(fishingSpot))
                            return true;
                    }
                }
            }
            else
            {
                GarrisonBuddy.Log("You Garrison has been taken care of! Waiting for orders...");

                if (await MoveTo(myFactionWaitingPoints[townHallLevel - 1]))
                    return true;
            }
            return false;
        }

        private static bool AnythingLeftToDoBeforeEnd()
        {
            if (ReadyToSwitch)
                // && Location.Distance(Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde) > 10 || Me.IsMoving)
                return false;
            return true;
        }

        public static bool AnythingTodo()
        {
            RefreshBuildings();
            // dailies cd
            if (helperTriggerWithTimer(ShouldRunDailies, ref DailiesWaitTimer, ref DailiesTriggered, DailiesWaitTimerValue))
                return true;
            // Cache
            if (helperTriggerWithTimer(ShouldRunCache, ref CacheWaitTimer, ref CacheTriggered, CacheWaitTimerValue))
                return true;

            // Start work orders
            if (helperTriggerWithTimer(ShouldRunStartOrder, ref StartOrderWaitTimer, ref StartOrderTriggered,
                StartOrderWaitTimerValue))
                return true;

            // Pick Up work orders
            if (helperTriggerWithTimer(CanRunPickUpOrder, ref PickUpOrderWaitTimer, ref PickUpOrderTriggered,
                PickUpOrderWaitTimerValue))
                return true;

            // Mine
            if (helperTriggerWithTimer(ShouldRunMine, ref MineWaitTimer, ref MineTriggered, MineWaitTimerValue))
                return true;

            // gardenla
            if (helperTriggerWithTimer(ShouldRunGarden, ref GardenWaitTimer, ref GardenTriggered, GardenWaitTimerValue))
                return true;

            // Missions
            if (helperTriggerWithTimer(CanRunTurnInMissions, ref TurnInMissionWaitTimer, ref TurnInMissionsTriggered,
                TurnInMissionWaitTimerValue))
                return true;

            // Missions completed 
            if (helperTriggerWithTimer(CanRunStartMission, ref StartMissionWaitTimer, ref StartMissionTriggered,
                StartMissionWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(ShouldRunSalvage, ref SalvageWaitTimer, ref SalvageTriggered, SalvageWaitTimerValue))
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
    }
}
﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        public static bool Check = true;

        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        private static readonly WaitTimer RefreshMissionsTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static readonly WaitTimer RefreshFollowerTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static WoWPoint _tablePosition = WoWPoint.Empty;

        private static void InitializeMissions()
        {
            RefreshMissions();
            RefreshFollowers();
        }

        private static void RefreshMissions(bool forced = false)
        {
            if (!RefreshMissionsTimer.IsFinished && _missions != null && !forced) return;
            GarrisonButler.Log("Refreshing Missions database.");
            _missions = MissionLua.GetAllAvailableMissions();
            RefreshMissionsTimer.Reset();
        }

        private static void RefreshFollowers(bool forced = false)
        {
            if (!RefreshFollowerTimer.IsFinished && _followers != null && !forced) return;
            GarrisonButler.Log("Refreshing Followers database.");
            _followers = FollowersLua.GetAllFollowers();
            RefreshFollowerTimer.Reset();
        }

        private static bool ShouldRunStartMission()
        {
            return CanRunStartMission().Item1;
        }

        private static Tuple<bool, Tuple<Mission, Follower[]>> CanRunStartMission()
        {
            if (!GaBSettings.Get().StartMissions)
            {
                GarrisonButler.Diagnostic("[Missions] Deactivated in user settings.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            var numberMissionAvailable = MissionLua.GetNumberAvailableMissions();

            // Is there mission available to start
            if (numberMissionAvailable == 0)
            {
                GarrisonButler.Diagnostic("[Missions] No missions available to start.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            RefreshMissions();
            RefreshFollowers();


            var garrisonRessources = BuildingsLua.GetGarrisonRessources();
            IEnumerable<Mission> missions = _missions.Where(m => m.Cost < garrisonRessources).ToList();
            if (!missions.Any())
            {
                GarrisonButler.Diagnostic("[Missions] Not enough ressources to start a mission.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            var toStart = new List<Tuple<Mission, Follower[]>>();
            var followersTemp = _followers.ToList();
            foreach (var mission in missions)
            {
                var match =
                    mission.FindMatch(followersTemp.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match == null)
                    continue;
                toStart.Add(new Tuple<Mission, Follower[]>(mission, match));
                followersTemp.RemoveAll(match.Contains);
            }

            var mess = "Found " + numberMissionAvailable + " available missions to complete. " +
                       "Can successfully complete: " + toStart.Count + " missions.";
            if (toStart.Count > 0)
                GarrisonButler.Log(mess);
            else
            {
                GarrisonButler.Diagnostic(mess);
            }
            return !toStart.Any()
                ? new Tuple<bool, Tuple<Mission, Follower[]>>(false, null)
                : new Tuple<bool, Tuple<Mission, Follower[]>>(true, toStart.First());
        }

        public static async Task<ActionResult> StartMission(Tuple<Mission, Follower[]> missionToStart)
        {
            if (await MoveToTable())
                return ActionResult.Running;

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
                    return ActionResult.Running;
                }
            }
            if (!InterfaceLua.IsGarrisonMissionVisible())
            {
                GarrisonButler.Diagnostic("Mission not visible, opening mission: " + missionToStart.Item1.MissionId +
                                          " - " +
                                          missionToStart.Item1.Name);
                InterfaceLua.OpenMission(missionToStart.Item1);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionFrame.");
                    return ActionResult.Running;
                }
            }
            else if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(missionToStart.Item1.MissionId))
            {
                GarrisonButler.Diagnostic("Mission not visible or not valid, close and then opening mission: " +
                                          missionToStart.Item1.MissionId + " - " + missionToStart.Item1.Name);
                InterfaceLua.ClickCloseMission();
                InterfaceLua.OpenMission(missionToStart.Item1);
                if (
                    !await
                        Buddy.Coroutines.Coroutine.Wait(2000,
                            () => InterfaceLua.IsGarrisonMissionVisibleAndValid(missionToStart.Item1.MissionId)))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionFrame or wrong mission opened.");
                    return ActionResult.Running;
                }
            }
            await missionToStart.Item1.AddFollowersToMission(missionToStart.Item2.ToList());
            InterfaceLua.StartMission(missionToStart.Item1);
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            InterfaceLua.ClickCloseMission();
            RefreshFollowers(true);
            RefreshMissions(true);
            return ActionResult.Refresh;
        }


        public static async Task<bool> MoveToTable()
        {
            if (_tablePosition == WoWPoint.Empty)
            {
                var tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != default(WoWGameObject))
                {
                    GarrisonButler.Diagnostic("Found Command table location, not using default anymore.");
                    _tablePosition = tableForLoc.Location;
                }
            }

            if (_tablePosition != WoWPoint.Empty)
            {
                if (InterfaceLua.IsGarrisonMissionFrameOpen())
                    return false;

                var tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != default(WoWGameObject))
                {
                    if (await MoveToInteract(tableForLoc) == ActionResult.Running)
                        return true;
                    if (tableForLoc.WithinInteractRange)
                    {
                        tableForLoc.Interact();
                        return true;
                    }
                }
                else
                {
                    if (await MoveTo(_tablePosition, "[Missions] Moving to command table") == ActionResult.Running)
                        return true;
                }
            }
            else
            {
                if (await MoveTo(Me.IsAlliance ? TableAlliance : TableHorde, "[Missions] Moving to command table") ==
                    ActionResult.Running)
                    return true;
            }

            if (InterfaceLua.IsGarrisonMissionFrameOpen())
                return false;

            var table = MissionLua.GetCommandTableOrDefault();
            if (table == default(WoWObject))
            {
                GarrisonButler.Diagnostic("[Missions] Trouble getting command table from LUA.");
                return false;
            }

            try
            {
                table.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }
            catch (Exception e)
            {
                GarrisonButler.Warning(e.ToString());
            }
            return true;
        }

        private static Tuple<bool, int> CanRunTurnInMissions()
        {
            return
                new Tuple<bool, int>(
                    GaBSettings.Get().CompletedMissions && MissionLua.GetNumberCompletedMissions() != 0, 0);
        }

        private static bool ShouldRunTurnInMissions()
        {
            return CanRunTurnInMissions().Item1;
        }

        public static async Task<ActionResult> DoTurnInCompletedMissions(int osef)
        {
            GarrisonButler.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");
            // are we at the action table?
            if (await MoveToTable())
                return ActionResult.Running;

            await CommonCoroutines.SleepForRandomUiInteractionTime();
            MissionLua.TurnInAllCompletedMissions();
            //RestoreCompletedMission = true;
            // Restore UI
            Lua.DoString("GarrisonMissionFrame.MissionTab.MissionList.CompleteDialog:Hide();" +
                         "GarrisonMissionFrame.MissionComplete:Hide();" +
                         "GarrisonMissionFrame.MissionCompleteBackground:Hide();" +
                         "GarrisonMissionFrame.MissionComplete.currentIndex = nil;" +
                         "GarrisonMissionFrame.MissionTab:Show();" +
                         "GarrisonMissionList_UpdateMissions();");

            await CommonCoroutines.SleepForRandomUiInteractionTime();
            _turnInMissionsTriggered = false;
            RefreshFollowers(true);
            RefreshMissions(true);
            return ActionResult.Refresh;
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
            //GarrisonButler.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED - Removing from ToStart mission " + missionId);
            //ToStart.RemoveAll(m => m.Key.MissionId == missionId);
        }

        public static ActionHelpers.ActionsSequence InitializeMissionsCoroutines()
        {
            // Initializing coroutines
            GarrisonButler.Diagnostic("Initialization Missions coroutines...");
            var missionsActionsSequence = new ActionHelpers.ActionsSequence();

            // DoTurnInCompletedMissions
            missionsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer<int>(DoTurnInCompletedMissions, CanRunTurnInMissions));

            //// StartMissions
            missionsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer<Tuple<Mission, Follower[]>>(StartMission, CanRunStartMission));

            GarrisonButler.Diagnostic("Initialization Missions coroutines done!");
            return missionsActionsSequence;
        }
    }
}
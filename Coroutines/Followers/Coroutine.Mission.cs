#region

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
        private static bool CheckedMissions;
        private static readonly WaitTimer RefreshMissionsTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static readonly WaitTimer RefreshFollowerTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static WoWPoint TablePosition = WoWPoint.Empty;

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

            int numberMissionAvailable = MissionLua.GetNumberAvailableMissions();

            // Is there mission available to start
            if (numberMissionAvailable == 0)
            {
                GarrisonButler.Diagnostic("[Missions] No missions available to start.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            RefreshMissions();
            RefreshFollowers();


            int garrisonRessources = BuildingsLua.GetGarrisonRessources();
            IEnumerable<Mission> Missions = _missions.Where(m => m.Cost < garrisonRessources);
            if (!Missions.Any())
            {
                GarrisonButler.Diagnostic("[Missions] Not enough ressources to start a mission.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            var ToStart = new List<Tuple<Mission, Follower[]>>();
            List<Follower> followersTemp = _followers.ToList();
            foreach (Mission mission in Missions)
            {
                Follower[] match =
                    mission.FindMatch(followersTemp.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match == null)
                    continue;
                ToStart.Add(new Tuple<Mission, Follower[]>(mission, match));
                followersTemp.RemoveAll(match.Contains);
            }

            string mess = "Found " + numberMissionAvailable + " available missions to complete. " +
                          "Can successfully complete: " + ToStart.Count + " missions.";
            if (ToStart.Count > 0)
                GarrisonButler.Log(mess);
            else
            {
                GarrisonButler.Diagnostic(mess);
            }
            if (!ToStart.Any())
            {
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            return new Tuple<bool, Tuple<Mission, Follower[]>>(true, ToStart.First());
        }

        public static async Task<bool> StartMission(Tuple<Mission, Follower[]> missionToStart)
        {
            if (await MoveToTable())
                return true;

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
                    return true;
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
                    return true;
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
                    return true;
                }
            }
            await missionToStart.Item1.AddFollowersToMission(missionToStart.Item2.ToList());
            InterfaceLua.StartMission(missionToStart.Item1);
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            InterfaceLua.ClickCloseMission();
            RefreshFollowers(true);
            RefreshMissions(true);
            return false;
        }


        public static async Task<bool> MoveToTable()
        {
            if (TablePosition == WoWPoint.Empty)
            {
                WoWObject tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != null)
                {
                    GarrisonButler.Diagnostic("Found Command table location, not using default anymore.");
                    TablePosition = tableForLoc.Location;
                }
            }

            if (TablePosition != WoWPoint.Empty)
            {
                WoWObject tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != null)
                {
                    if (await MoveToInteract(tableForLoc))
                        return true;
                }
                else
                {
                    if (await MoveTo(TablePosition, "[Missions] Moving to command table"))
                        return true;
                }
            }
            else
            {
                if (await MoveTo(Me.IsAlliance ? TableAlliance : TableHorde, "[Missions] Moving to command table"))
                    return true;
            }

            if (InterfaceLua.IsGarrisonMissionFrameOpen())
                return false;

            WoWObject table = MissionLua.GetCommandTableOrDefault();
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

        public static async Task<bool> DoTurnInCompletedMissions(int osef)
        {
            GarrisonButler.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");
            // are we at the action table?
            if (await MoveToTable())
                return true;

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
            TurnInMissionsTriggered = false;
            RefreshFollowers(true);
            RefreshMissions(true);
            return false;
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
            string missionId = args.Args[0].ToString();
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
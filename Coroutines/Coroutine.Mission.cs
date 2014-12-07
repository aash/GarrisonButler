using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using MainDev.RemoteASM.Handlers;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        public static bool Check = true;

        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1933, 346, 91);
        private static bool CheckedMissions;
        private static readonly WaitTimer refreshMissionsTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static readonly WaitTimer refreshFollowerTimer = new WaitTimer(TimeSpan.FromMinutes(5));

        private static void InitializeMissions()
        {
            RefreshMissions();
        }

        private static void RefreshMissions(bool forced = false)
        {
            if (!refreshMissionsTimer.IsFinished && _followers != null && !forced) return;
            GarrisonBuddy.Log("Refreshing Missions database.");
            _missions = MissionLua.GetAllAvailableMissions();
            refreshMissionsTimer.Reset();
        }

        private static void RefreshFollowers(bool forced = false)
        {
            if (!refreshFollowerTimer.IsFinished && _followers != null && !forced) return;
            GarrisonBuddy.Log("Refreshing Followers database.");
            _followers = FollowersLua.GetAllFollowers();
            refreshFollowerTimer.Reset();
        }

        private static bool CanRunStartMission()
        {
            return GaBSettings.Mono.StartMissions && DoCheckAvailableMissions() && ToStart.Count > 0;
        }

        public static async Task<bool> StartMissions()
        {
            if (!CanRunStartMission())
                return false;

            KeyValuePair<Mission, Follower[]> match = ToStart.First();

            if (await MoveToTable())
                return true;

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonBuddy.Diagnostic("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonBuddy.Warning("Couldn't display GarrisonMissionTab.");
                    return true;
                }
            }
            if (!InterfaceLua.IsGarrisonMissionVisible())
            {
                GarrisonBuddy.Diagnostic("Mission not visible, opening mission: " + match.Key.MissionId + " - " +
                                         match.Key.Name);
                InterfaceLua.OpenMission(match.Key);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionVisible))
                {
                    GarrisonBuddy.Warning("Couldn't display GarrisonMissionFrame.");
                    return true;
                }
            }
            else if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(match.Key.MissionId))
            {
                GarrisonBuddy.Diagnostic("Mission not visible or not valid, close and then opening mission: " +
                                         match.Key.MissionId + " - " + match.Key.Name);
                InterfaceLua.ClickCloseMission();
                InterfaceLua.OpenMission(match.Key);
                if (
                    !await
                        Buddy.Coroutines.Coroutine.Wait(2000,
                            () => InterfaceLua.IsGarrisonMissionVisibleAndValid(match.Key.MissionId)))
                {
                    GarrisonBuddy.Warning("Couldn't display GarrisonMissionFrame or wrong mission opened.");
                    return true;
                }
            }
            match.Key.AddFollowersToMission(match.Value.ToList());
            InterfaceLua.StartMission(match.Key);
            InterfaceLua.ClickCloseMission();
            return true;
        }

        private static WaitTimer CheckAvailableMissionTimer;
        private static bool TurnInCompletedRunning;
        private static bool StartMissionRunning;
        private static WaitTimer _StartMissionsWaitTimer;

        private static bool NeedToRun = false;
        public static bool DoCheckAvailableMissions()
        {
            int numberMissionAvailable = MissionLua.GetNumberAvailableMissions();
            // Is there mission available to start
            if (numberMissionAvailable == 0)
                return false;

            bool forced = _missions != null && numberMissionAvailable != _missions.Count;
            if (CheckedMissions && !forced)
                return false;
               
            RefreshMissions(forced);
            RefreshFollowers(forced);
            
            var temp = new List<KeyValuePair<Mission, Follower[]>>();
            foreach (Mission mission in _missions)
            {
                Follower[] match =
                    mission.FindMatch(_followers.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match != null)
                {
                    temp.Add(new KeyValuePair<Mission, Follower[]>(mission, match));
                    _followers.RemoveAll(match.Contains);
                }
            }
            ToStart.AddRange(temp.Where(x => ToStart.All(y => y.Key.MissionId != x.Key.MissionId)));
            var mess = "Found " + numberMissionAvailable + " available missions to complete. " +
                       "Can succesfully complete: " + ToStart.Count + " missions.";
            if (ToStart.Any())
              GarrisonBuddy.Log(mess);
            
        else
                GarrisonBuddy.Diagnostic(mess);
            

            CheckedMissions = true;
            return ToStart.Any();
        }

        private static WoWPoint TablePosition = WoWPoint.Empty;

        public static async Task<bool> MoveToTable()
        {
            if (TablePosition == WoWPoint.Empty)
            {
                WoWObject tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != null)
                {
                    GarrisonBuddy.Diagnostic("Found Command table location, not using default anymore.");
                    TablePosition = tableForLoc.Location;
                }
            }
            if (TablePosition != WoWPoint.Empty)
            {
                if (await MoveTo(TablePosition, "Command table"))
                    return true;
            }
            else
            {
                if (await MoveTo(Me.IsAlliance ? TableAlliance : TableHorde, "Command table"))
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
                GarrisonBuddy.Warning(e.ToString());
            }
            return true;
        }

        private static bool CanRunTurnInMissions()
        {
            return GaBSettings.Mono.CompletedMissions && MissionLua.GetNumberCompletedMissions() != 0;
        }

        public static async Task<bool> DoTurnInCompletedMissions()
        {
            if (!CanRunTurnInMissions())
                return false;

            GarrisonBuddy.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");

            // are we at the action table?
            if (await MoveToTable())
                return true;

            MissionLua.TurnInAllCompletedMissions();
            RestoreCompletedMission = true;
            await CommonCoroutines.SleepForLagDuration();
            TurnInMissionsTriggered = false;
            return true;
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
            string missionId = args.Args[0].ToString();
            GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED - Removing from ToStart mission " + missionId);
            ToStart.RemoveAll(m => m.Key.MissionId == missionId);
        }

        private async static Task<bool> DoMissions()
        {
            // Missions
            if (await DoTurnInCompletedMissions())
                return true;

            if (await StartMissions())
                return true;

            return false;
        }
    }
}
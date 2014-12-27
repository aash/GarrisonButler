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
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
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

        private static bool ShouldRunStartMission()
        {
            return CanRunStartMission().Item1;
        }

        private static Tuple<bool, Tuple<Mission, Follower[]>> CanRunStartMission()
        {
            if (!GaBSettings.Get().StartMissions)
            {
                GarrisonBuddy.Diagnostic("[Missions] Deactivated in user settings.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            int numberMissionAvailable = MissionLua.GetNumberAvailableMissions();

            // Is there mission available to start
            if (numberMissionAvailable == 0)
            {
                GarrisonBuddy.Diagnostic("[Missions] No missions available to start.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            RefreshMissions(true);
            RefreshFollowers(true);


            int garrisonRessources = BuildingsLua.GetGarrisonRessources();
            var Missions = _missions.Where(m => m.Cost < garrisonRessources);
            if (!Missions.Any())
            {
                GarrisonBuddy.Diagnostic("[Missions] Not enough ressources to start a mission.");
                return new Tuple<bool, Tuple<Mission, Follower[]>>(false, null);
            }

            var ToStart = new List<Tuple<Mission, Follower[]>>();
            foreach (Mission mission in Missions)
            {
                Follower[] match =
                    mission.FindMatch(_followers.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match == null)
                    continue;
                else
                {
                    ToStart.Add(new Tuple<Mission, Follower[]>(mission, match));
                    _followers.RemoveAll(match.Contains);
                }                
            }

            var mess = "Found " + numberMissionAvailable + " available missions to complete. " +
                       "Can succesfully complete: " + ToStart.Count + " missions.";
            GarrisonBuddy.Log(mess);

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
                GarrisonBuddy.Diagnostic("Mission not visible, opening mission: " + missionToStart.Item1.MissionId + " - " +
                                         missionToStart.Item1.Name);
                InterfaceLua.OpenMission(missionToStart.Item1);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionVisible))
                {
                    GarrisonBuddy.Warning("Couldn't display GarrisonMissionFrame.");
                    return true;
                }
            }
            else if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(missionToStart.Item1.MissionId))
            {
                GarrisonBuddy.Diagnostic("Mission not visible or not valid, close and then opening mission: " +
                                         missionToStart.Item1.MissionId + " - " + missionToStart.Item1.Name);
                InterfaceLua.ClickCloseMission();
                InterfaceLua.OpenMission(missionToStart.Item1);
                if (
                    !await
                        Buddy.Coroutines.Coroutine.Wait(2000,
                            () => InterfaceLua.IsGarrisonMissionVisibleAndValid(missionToStart.Item1.MissionId)))
                {
                    GarrisonBuddy.Warning("Couldn't display GarrisonMissionFrame or wrong mission opened.");
                    return true;
                }
            }
            await missionToStart.Item1.AddFollowersToMission(missionToStart.Item2.ToList());
            InterfaceLua.StartMission(missionToStart.Item1);
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            InterfaceLua.ClickCloseMission();
            return true;
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

        private static Tuple<bool, int> CanRunTurnInMissions()
        {
            return new Tuple<bool, int>(GaBSettings.Get().CompletedMissions && MissionLua.GetNumberCompletedMissions() != 0, 0);
        }

        private static bool ShouldRunTurnInMissions()
        {
            return CanRunTurnInMissions().Item1;
        }

        public static async Task<bool> DoTurnInCompletedMissions(int osef)
        {
            GarrisonBuddy.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");
            // are we at the action table?
            if (await MoveToTable())
                return true;

            await CommonCoroutines.SleepForRandomUiInteractionTime();
            MissionLua.TurnInAllCompletedMissions();
            RestoreCompletedMission = true;
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            TurnInMissionsTriggered = false;
            return true;
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
            string missionId = args.Args[0].ToString();
            //GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED - Removing from ToStart mission " + missionId);
            //ToStart.RemoveAll(m => m.Key.MissionId == missionId);
        }

        public static ActionsSequence InitializeMissionsCoroutines()
        {
            // Initializing coroutines
            GarrisonBuddy.Diagnostic("Initialization Missions coroutines...");
            var missionsActionsSequence = new ActionsSequence();

            // DoTurnInCompletedMissions
            missionsActionsSequence.AddAction(
                new ActionOnTimer<int>(DoTurnInCompletedMissions, CanRunTurnInMissions));

            //// StartMissions
            missionsActionsSequence.AddAction(
                new ActionOnTimer<Tuple<Mission, Follower[]>>(StartMission, CanRunStartMission));

            GarrisonBuddy.Diagnostic("Initialization Missions coroutines done!");
            return missionsActionsSequence;
        }
    }
}
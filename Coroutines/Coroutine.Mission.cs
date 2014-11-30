using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        public static bool Check = true;

        public static async Task<bool> DoCheckAvailableMissions()
        {
            if (!GaBSettings.Mono.DoMissions)
                return false;

            if (!Check)
                return false;

            // Is there mission to turn in?
            if (MissionLua.GetNumberAvailableMissions() == 0)
                return false;

            GarrisonBuddy.Log("Found " + MissionLua.GetNumberAvailableMissions() + " available missions to complete.");
            List<Follower> tempFollowers = FollowersLua.GetAllFollowers().Select(x => x).ToList();
            var temp = new List<KeyValuePair<Mission, Follower[]>>();
            foreach (Mission mission in MissionLua.GetAllAvailableMissionsReport())
            {
                Follower[] match =
                    mission.FindMatch(tempFollowers.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match != null)
                {
                    temp.Add(new KeyValuePair<Mission, Follower[]>(mission, match));
                    tempFollowers.RemoveAll(match.Contains);
                }
            }
            ToStart.AddRange(temp.Where(x => ToStart.All(y => y.Key.MissionId != x.Key.MissionId)));
            GarrisonBuddy.Log("Can succesfully complete: " + ToStart.Count + " missions.");
            Check = false;
            return true;
        }

        public static async Task<bool> DoStartMissions()
        {
            if (!GaBSettings.Mono.DoMissions)
                return false;

            if (await DoCheckAvailableMissions())
                return true;

            if (ToStart.Count <= 0)
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
                    return false;
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
                    return false;
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
                    return false;
                }
            }
            match.Key.AddFollowersToMission(match.Value.ToList());
            InterfaceLua.StartMission(match.Key.MissionId);
            InterfaceLua.ClickCloseMission();
            return true;
        }

        public static async Task<bool> MoveToTable()
        {
            //move to table
            if (await MoveTo(new WoWPoint(1933, 346, 91), "Command table"))
                return true;
            // TO DO

            //
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
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (!Check)
                return false;

            // Is there mission to turn in?
            if (MissionLua.GetNumberAvailableMissions() == 0)
                return false;

            GarrisonButler.Log("Found " + MissionLua.GetNumberAvailableMissions() + " available missions to complete.");
            var tempFollowers = FollowersLua.GetAllFollowers().Select(x => x).ToList();
            var temp = new List<KeyValuePair<Mission, Follower[]>>();
            foreach (Mission mission in MissionLua.GetAllAvailableMissions())
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
            GarrisonButler.Log("Can succesfully complete: " + ToStart.Count + " missions.");
            Check = false;
            return true;
        }
        public static async Task<bool> DoStartMissions()
        {
            if (ToStart.Count <= 0)
                return false;
            var match = ToStart.First();

            if (await MoveToTable())
                return true;

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Debug("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionTab.");
                    return false;
                }
            }
            if (!InterfaceLua.IsGarrisonMissionVisible())
            {
                GarrisonButler.Debug("Mission not visible, opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                InterfaceLua.OpenMission(match.Key);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionVisible))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionFrame.");
                    return false;
                }
            }
            else if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(match.Key.MissionId))
            {
                GarrisonButler.Debug("Mission not visible or not valid, close and then opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                InterfaceLua.ClickCloseMission();
                InterfaceLua.OpenMission(match.Key);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, () => InterfaceLua.IsGarrisonMissionVisibleAndValid(match.Key.MissionId)))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionFrame or wrong mission opened.");
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
                GarrisonButler.Err(e.ToString());
            }
            return true;
        }
    }
}

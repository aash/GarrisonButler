using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler
{
    partial class Coroutine
    {
        public static bool Check = true;

        public static async Task<bool> DoCheckAvailableMissions()
        {
            if (!Check)
                return false;

            // Is there mission to turn in?
            if (GarrisonApi.GetNumberAvailableMissions() == 0)
                return false;

            GarrisonButler.Log("Found " + GarrisonApi.GetNumberAvailableMissions() + " available missions to complete.");
            var tempFollowers = GarrisonApi.GetAllFollowers().Select(x => x).ToList();
            var temp = new List<KeyValuePair<Mission, Follower[]>>();
            foreach (Mission mission in GarrisonApi.GetAllAvailableMissions())
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

            if (!GarrisonApi.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Debug("Mission tab not visible, clicking.");
                GarrisonApi.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, GarrisonApi.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionTab.");
                    return false;
                }
            }
            if (!GarrisonApi.IsGarrisonMissionVisible())
            {
                GarrisonButler.Debug("Mission not visible, opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                GarrisonApi.OpenMission(match.Key);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, GarrisonApi.IsGarrisonMissionVisible))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionFrame.");
                    return false;
                }
            }
            else if (!GarrisonApi.IsGarrisonMissionVisibleAndValid(match.Key.MissionId))
            {
                GarrisonButler.Debug("Mission not visible or not valid, close and then opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                GarrisonApi.ClickCloseMission();
                GarrisonApi.OpenMission(match.Key);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, () => GarrisonApi.IsGarrisonMissionVisibleAndValid(match.Key.MissionId)))
                {
                    GarrisonButler.Err("Couldn't display GarrisonMissionFrame or wrong mission opened.");
                    return false;
                }
            }
            match.Key.AddFollowersToMission(match.Value.ToList());
            GarrisonApi.StartMission(match.Key.MissionId);
            GarrisonApi.ClickCloseMission();
            return true;
        }
        public static async Task<bool> MoveToTable()
        {
            //move to table

            // TO DO

            //
            if (GarrisonApi.IsGarrisonMissionFrameOpen())
                return false;

            WoWObject table = GarrisonApi.GetCommandTableOrDefault();
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

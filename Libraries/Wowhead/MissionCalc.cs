using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using GarrisonButler.Libraries;
using GarrisonButler.Libraries.JSON;
using Styx.Helpers;

namespace GarrisonButler.Libraries.Wowhead
{
    public class MissionCalc
    {
        //public static string jsonString = System.IO.File.ReadAllText(ResourceWebUI.follower_data);
        public static Hashtable g_garrison_followers { get; set; }
        public static Hashtable g_garrison_abilities { get; set; }
        public static Hashtable g_garrison_mechanics { get; set; }
        public static Hashtable g_garrison_missions { get; set; }
        public static Hashtable wowheadMissionObject { get; set; }
        private static Mission _mission;

        public static Mission mission
        {
            get
            {
                return _mission;
            }
            set
            {
                _mission = value;
                string missionCalcPage = "";
                using (var client = new WebClient())
                {
                    missionCalcPage = client.DownloadString("http://www.wowhead.com/mission=" + value.MissionId);
                }
                missionCalcPage = missionCalcPage.Replace("\r\n", "\n");
                var startString = "new MissionCalc(";
                var startAdjust = startString.Length;
                var startingIndex = missionCalcPage.IndexOf(startString);
                var endString = "} });\n</script>";
                var endAdjust = ("} }").Length;
                var endingIndex = missionCalcPage.IndexOf(endString);
                var missionCalcJSON = missionCalcPage.Substring(startingIndex + startAdjust,
                    (endingIndex + endAdjust) - (startingIndex + startAdjust));
                var missionJSON = missionCalcJSON.Substring(missionCalcJSON.IndexOf("mission: ") + 9, missionCalcJSON.LastIndexOf("}") - (missionCalcJSON.IndexOf("mission: ") + 8));
                var decoded = JSON.JSON.JsonDecode(missionJSON);
                wowheadMissionObject = (Hashtable) decoded;
                //foreach (Hashtable currentMission in g_garrison_missions)
                //{
                //    if (currentMission["id"].ToString().ToInt32() == value.MissionId.ToInt32())
                //        wowheadMissionObject = currentMission;
                //}
            }
        }

        private static List<Follower> _followers; 
        public static List<Follower> followers
        {
            get { return _followers; }
            set { _followers = value;
                followerInfo = value.Select(f => new FollowerInfo(f)).ToList();
            }
        }

        public static List<FollowerInfo> followerInfo { get; set; }

        public static List<object> mechanicInfo { get; set; }

        public static void LoadJSONData()
        {
            try
            {
                //string jsonString = "";
                //using (var client = new WebClient())
                //{
                //    jsonString = client.DownloadString("http://www.wowhead.com/data=followers&locale=0&7w19342");
                //}
                var jsonString = ResourceWebUI.follower_data;
                jsonString = jsonString.Replace("\r\n", "\n");
                var line1 = jsonString.Substring(0, jsonString.IndexOf(";\n"));
                line1 = line1.Replace("var g_garrison_followers = ", "");
                var jsonStringWithoutLine1 = jsonString.Substring(jsonString.IndexOf(";\n") + 1);
                var line2 = jsonStringWithoutLine1.Substring(0, jsonStringWithoutLine1.IndexOf(";\n"));
                line2 = line2.Replace("var g_garrison_abilities = ", "");
                var jsonStringWithoutLine1Line2 = jsonStringWithoutLine1.Substring(jsonStringWithoutLine1.IndexOf(";\n") + 1);
                var line3 = jsonStringWithoutLine1Line2;
                line3 = line3.Replace("var g_garrison_mechanics = ", "");
                g_garrison_followers = (Hashtable)JSON.JSON.JsonDecode(line1);
                g_garrison_abilities = (Hashtable)JSON.JSON.JsonDecode(line2);
                g_garrison_mechanics = (Hashtable)JSON.JSON.JsonDecode(line3);

                //var jsonString2 = ResourceWebUI.mission_data;   //https://www.wowhead.com/data=missions&locale=0&7w19342
                //jsonString2 = jsonString2.Replace("\r\n", "\n");
                //g_garrison_missions = (ArrayList)JSON.JSON.JsonDecode(jsonString2);
                //foreach (var a in (ArrayList)g_garrison_missions)
                //{
                //    foreach(DictionaryEntry entry in (Hashtable)a)
                //        GarrisonButler.Diagnostic("key={0}, value={1}", entry.Key, entry.Value);
                //}
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic("Could not LoadJSONData(): " + e.GetType());
            }
        }

        public static Tuple<double, double> CalculateSuccessChance()
        {
            var successChance = 0.0f;
            var chanceOver = 0.0f;
            var returnValue = new Tuple<double, double>(0, 0);

            var mentorinfo = GetMentorInfo();

            //how do I find encounter data?

            return returnValue;
        }

        public static Tuple<int, int> GetMentorInfo()
        {
            var level = 0;
            var itemlevel = 0;

            if (mission.NumFollowers <= 0)
                return new Tuple<int, int>(0, 0);

            for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
            {
                var currentFollower = followerInfo[followerIndex];

                for (var abilityIndex = 0; abilityIndex < currentFollower.abilities.Count; abilityIndex++)
                {
                    var currentAbility = (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                    var type = (ArrayList) currentAbility["type"];
                    var numTypes = type.Count;

                    for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                    {
                        if (type[typeIndex].ToString().ToInt32() == 18)
                        {
                            if (currentFollower.level > level)
                                level = currentFollower.level;
                            if (currentFollower.avgilvl > itemlevel)
                                itemlevel = currentFollower.avgilvl;
                        }
                    }
                }
            }

            return new Tuple<int, int>(level, itemlevel);
        }

        //public static double fround(float b)
        //{
        //    if(Math.Round())
        //}
    }
}
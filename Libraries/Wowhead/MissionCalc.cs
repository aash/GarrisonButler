using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines;
using GarrisonButler.Libraries;
using GarrisonButler.Libraries.JSON;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
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
        public static ArrayList mechanicInfo { get; set; }
        public static bool[, ,] registeredThreatCounters { get; set; }
        private static Mission _mission;
        public static bool enableDebugPrint = false;
        public static bool valid = true;    // Used for when wowhead pages don't contain a "new MissionCalc" variable
                                            // such as the selfie missions and any Logistic missions

        public static Mission mission
        {
            get
            {
                return _mission;
            }
            set
            {
                _mission = value;
                try
                {
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
                    //mechanicInfo = (ArrayList)wowheadMissionObject["mechanics"];
                    var encounters = (Hashtable) wowheadMissionObject["encounters"];
                    if (encounters == null)
                        return;
                    mechanicInfo = new ArrayList();

                    foreach (DictionaryEntry encounter in encounters)
                    {
                        Hashtable curEncounter = (Hashtable)encounter.Value;
                        // Skip if mechanics are empty (will be ArrayList if empty)
                        if (curEncounter["mechanics"].GetType() == typeof (ArrayList))
                        {
                            continue;
                        }
                        Hashtable mechanics = (Hashtable)curEncounter["mechanics"];
                        if (mechanics == null)
                            continue;
                        foreach (DictionaryEntry mechanic in mechanics)
                        {
                            mechanicInfo.Add((Hashtable)mechanic.Value);
                        }
                    }
                }
                catch (Exception e)
                {
                    GarrisonButler.Warning("EXCEPTION IN MISSION: " + e.GetType() + " - " + e.StackTrace);
                    valid = false;
                }
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
                valid = false;
            }
        }

        public static void NonValidMissionCalc()
        {
            //using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            //{
            //    Stopwatch timer = new Stopwatch();
            //    timer.Start();
            //    while (!InterfaceLua.IsGarrisonMissionTabVisible()
            //           && timer.ElapsedMilliseconds < 2000)
            //    {
            //        GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
            //        InterfaceLua.ClickTabMission();
            //    }

            //    if (!InterfaceLua.IsGarrisonMissionTabVisible())
            //    {
            //        GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
            //        return;
            //    }

            //    timer.Reset();
            //    timer.Start();
            //    while (!InterfaceLua.IsGarrisonMissionVisible()
            //           && timer.ElapsedMilliseconds < 2000)
            //    {
            //        GarrisonButler.Diagnostic("Mission not visible, opening mission: " + mission.MissionId +
            //                                  " - " +
            //                                  mission.Name);
            //        InterfaceLua.OpenMission(mission);
            //    }

            //    if (!InterfaceLua.IsGarrisonMissionVisible())
            //    {
            //        GarrisonButler.Warning("Couldn't display GarrisonMissionFrame.");
            //        return;
            //    }

            //    timer.Reset();
            //    timer.Start();
            //    while (!InterfaceLua.IsGarrisonMissionVisibleAndValid(mission.MissionId)
            //           && timer.ElapsedMilliseconds < 2000)
            //    {
            //        GarrisonButler.Diagnostic(
            //            "Mission not visible or not valid, close and then opening mission: " +
            //            mission.MissionId + " - " + mission.Name);
            //        InterfaceLua.ClickCloseMission();
            //        InterfaceLua.OpenMission(mission);
            //    }

            //    if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(mission.MissionId))
            //    {
            //        GarrisonButler.Warning("Couldn't display GarrisonMissionFrame or wrong mission opened.");
            //        return;
            //    }

            //    InterfaceLua.AddFollowersToMissionNonTask(mission.MissionId, followerIds);
            //    API.MissionLua.GetPartyMissionInfo(mission);
            //    combo.ForEach(f => RemoveFollowerFromMission(mission.MissionId.ToInt32(), f.FollowerId.ToInt32()));
            //    InterfaceLua.ClickCloseMission();
            //}
        }

        public static Tuple<double, double> CalculateSuccessChance()
        {
            var successChance = 0.0d;
            var chanceOver = 0.0d;
            var returnValue = new Tuple<double, double>(-1.0, 0);

            if(enableDebugPrint) GarrisonButler.Diagnostic("----- Start CalculateSuccessChance -----");

            // Probably because this mission has a base success chance of 100% and wowhead has no data on it
            if (!valid)
            {
                try
                {
                    GarrisonButler.Diagnostic("Mission not valid, manually determine success chance");
                    //var followersToAssign = followers.Take(mission.NumFollowers);
                    //if (!followersToAssign.Any())
                    //    return returnValue;
                    //followersToAssign.ForEach(f => API.FollowersLua.AddFollowerToMission(mission.MissionId.ToInt32(), f.UniqueId.ToInt32()));
                    //var missionInfo = API.MissionLua.GetPartyMissionInfo(mission);
                    //returnValue = new Tuple<double, double>(missionInfo.SuccessChance.ToFloat(), 0.0d);
                    //followersToAssign.ForEach(f => API.FollowersLua.RemoveFollowerFromMission(mission.MissionId.ToInt32(), f.UniqueId.ToInt32()));
                }
                catch (Exception e)
                {
                    GarrisonButler.Diagnostic("ERROR in !valid workaround during CalculateSuccessChance - " + e.GetType() + " - " + e.StackTrace);
                }
                
                return returnValue;
            }

            var mentorinfo = GetMentorInfo();

            foreach (var currentFollower in followerInfo)
            {
                if (mentorinfo.Item1 > currentFollower.level)
                {
                    if (enableDebugPrint) GarrisonButler.Diagnostic("Mentored follower {0} from level {1} to level {2}", 
                        currentFollower.follower, currentFollower.level, mentorinfo.Item1);

                    currentFollower.level = mentorinfo.Item1;
                }

                if (mentorinfo.Item2 > currentFollower.avgilvl)
                {
                    if (enableDebugPrint) GarrisonButler.Diagnostic("Mentored follower {0} from item level {1} to item level {2}",
                        currentFollower.follower, currentFollower.avgilvl, mentorinfo.Item2);

                    currentFollower.avgilvl = mentorinfo.Item2;
                }

                currentFollower.bias = GetFollowerBias(currentFollower.level, currentFollower.avgilvl);
                if (enableDebugPrint) GarrisonButler.Diagnostic("Follower {0} bias: {1:0.00}", currentFollower.follower, currentFollower.bias);
            }

            var D = mission.NumFollowers*100;
            var B = D;

            if (mechanicInfo.Count > 0)
            {
                foreach (Hashtable currentMechanic in mechanicInfo)
                {
                    //Integers = setkey, id, amount, type, category
                    //Strings = name, description, icon
                    var category = currentMechanic["category"];
                    if (category == null)
                        continue;
                    var categoryInt = category.ToString().ToInt32();
                    if (categoryInt != 2)
                    {
                        D = B;
                    }
                    else
                    {
                        var amount = currentMechanic["amount"];
                        if (amount == null)
                            continue;
                        var amountInt = amount.ToString().ToInt32();
                        D = B + amountInt;
                        B += amountInt;
                    }
                }
            }

            if (D <= 0)
            {
                return new Tuple<double, double>(100, 0);
            }

            double coeff = 100.0d/(double)D;
            if (enableDebugPrint) GarrisonButler.Diagnostic("coeff: {0}", coeff);

            for (int followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
            {
                var currentFollower = followerInfo[followerIndex];
                var calcChance = CalcChance(100, 150, currentFollower.bias);
                var z = calcChance * coeff;
                successChance += z;
                if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to follower {1} bias - CalcChance returned {2}", z, currentFollower.follower, calcChance);
            }

            var mechanicIndex = 0;
            registeredThreatCounters = new bool[100, 100, 100];
            var typeAmountHashtable = new Hashtable();
            if (mechanicInfo.Count > 0)
            {
                do
                {
                    Hashtable currentMechanic = (Hashtable)mechanicInfo[mechanicIndex];
                    // Category 2 = Abilities
                    if (!currentMechanic.ContainsKey("category") || currentMechanic["category"].ToString().ToInt32() == 2)
                    {
                        var amt = currentMechanic["amount"].ToString().ToInt32();
                        //Hashtable currentIndex = o.ContainsKey(currentMechanic["type"]) ? (Hashtable)o[currentMechanic["type"]] : o.Add(currentMechanic["type"].ToString(), );
                        if (typeAmountHashtable.ContainsKey(currentMechanic["type"]))
                        {
                            Hashtable currentIndex = (Hashtable)typeAmountHashtable[currentMechanic["type"]];
                            currentIndex["amount1"] = currentIndex["amount1"].ToString().ToInt32() + amt;
                            currentIndex["amount2"] = currentIndex["amount2"].ToString().ToInt32() + amt;
                        }
                        else
                        {
                            Hashtable currentIndex = new Hashtable();
                            currentIndex.Add("amount1", amt);
                            currentIndex.Add("amount2", amt);
                            currentIndex.Add("id", currentMechanic["id"].ToString().ToInt32());
                            typeAmountHashtable.Add(currentMechanic["type"], currentIndex);
                        }
                        //var mechanicAmount = currentMechanic["amount"].ToString().ToInt32();

                        //if (mission.NumFollowers > 0)
                        //{
                        //    for (int followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                        //    {
                        //        var currentFollower = followerInfo[followerIndex];
                        //        for (int abilityIndex = 0;
                        //            abilityIndex < currentFollower.abilities.Count;
                        //            abilityIndex++)
                        //        {
                        //            var currentAbility = (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                        //            var currentAbilityType = (ArrayList)currentAbility["type"];
                        //            var numTypes = currentAbilityType.Count;

                        //            for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                        //            {
                        //                var counters = (ArrayList) currentAbility["counters"];
                        //                var amount1 = (ArrayList) currentAbility["amount1"];
                        //                var amount2 = (ArrayList) currentAbility["amount2"];
                        //                var amount3 = (ArrayList) currentAbility["amount3"];
                        //                var currentMechanicType = currentMechanic["type"];
                        //                var currentMechanicTypeInt = currentMechanicType.ToString().ToInt32();
                        //                var counterInt = counters[typeIndex].ToString().ToInt32();
                        //                var amount1Int = amount1[typeIndex].ToString().ToInt32();
                        //                var amount2Int = amount2[typeIndex].ToString().ToInt32();
                        //                var amount3Int = amount3[typeIndex].ToString().ToInt32();
                        //                if ((currentMechanicTypeInt == counterInt)
                        //                    && ((amount1Int & 1) != 1)
                        //                    && (mechanicAmount > 0)
                        //                    && !ThreatCounterIsAlreadyRegistered(followerIndex, abilityIndex, typeIndex))
                        //                {
                        //                    var q = CalcChance(amount2Int, amount3Int, currentFollower.bias);
                        //                    var a = currentMechanic["amount"].ToString().ToInt32();
                        //                    if (q <= a)
                        //                        a = Convert.ToInt32(q);
                        //                    RegisterThreatCounter(followerIndex, abilityIndex, typeIndex);
                        //                    // Reduce mechanic amount by amount countered
                        //                    mechanicAmount -= a;
                        //                }
                        //            }
                        //        }
                        //    }
                        //} // if (mission.NumFollowers > 0)

                        //if (mechanicAmount < 0)
                        //    mechanicAmount = 0;

                        //// Calculate success based on how much of the mechanic was countered
                        //var f = ((double)(currentMechanic["amount"].ToString().ToInt32() - mechanicAmount))*coeff;
                        //successChance += f;
                        //if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to followers countering boss mechanics {1}.", f, currentMechanic["id"]);
                    }
                    mechanicIndex++;
                } while (mechanicIndex < mechanicInfo.Count);
            } //if (mechanicInfo.Count > 0)

            //for(var currentTypeAmountIndex = 0; currentTypeAmountIndex < typeAmountHashtable.Count; currentTypeAmountIndex++)
            foreach (DictionaryEntry currentTypeAmountDictionaryEntry in typeAmountHashtable)
            {
                Hashtable currentTypeAmount = (Hashtable)currentTypeAmountDictionaryEntry.Value;
                // currentTypeAmount produces Hashtable with the following structure:
                // amount1: <int>
                // amount2: <int>
                // type: <int>
                var currentTypeAmount_Amount2 = currentTypeAmount["amount2"].ToString().ToInt32();
                if (mission.NumFollowers > 0)
                {
                    for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                    {
                        var currentFollower = followerInfo[followerIndex];
                        for (int abilityIndex = 0;
                            abilityIndex < currentFollower.abilities.Count;
                            abilityIndex++)
                        {
                            var currentAbility =
                                (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                            var type = (ArrayList)currentAbility["type"];
                            var numTypes = type.Count;

                            for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                            {
                                var counters = (ArrayList)currentAbility["counters"];
                                var amount1 = (ArrayList)currentAbility["amount1"];
                                var amount2 = (ArrayList)currentAbility["amount2"];
                                var amount3 = (ArrayList)currentAbility["amount3"];
                                if (typeIndex >= counters.Count
                                    || typeIndex >= amount1.Count
                                    || typeIndex >= amount2.Count
                                    || typeIndex >= amount3.Count)
                                    continue;
                                var typeInt = type[typeIndex].ToString().ToInt32();
                                var counterInt = counters[typeIndex].ToString().ToInt32();
                                var amount1Int = amount1[typeIndex].ToString().ToInt32();
                                var amount2Int = amount2[typeIndex].ToString().ToInt32();
                                var amount3Int = amount3[typeIndex].ToString().ToInt32();
                                if (currentTypeAmountDictionaryEntry.Key.ToString().ToInt32() == counterInt
                                    && ((amount1Int & 1) != 1)
                                    && currentTypeAmount_Amount2 > 0)
                                {
                                    var q = CalcChance(amount2Int, amount3Int, currentFollower.bias);
                                    var s = currentTypeAmount_Amount2 - q;
                                    if (s < 0)
                                        s = 0;

                                    currentTypeAmount_Amount2 = Convert.ToInt32(s);
                                    //if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to follower {1} enemy race ability {2}", q, currentFollower.follower, currentMechanic["id"]);
                                }
                            }
                        }
                    } // for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                } // if (mission.NumFollowers > 0)

                var v28 = (currentTypeAmount["amount1"].ToString().ToInt32() - currentTypeAmount_Amount2)*coeff;
                successChance += v28;
                if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to followers countering boss mechanic type {1}.", v28, currentTypeAmount["id"]);
            }

            for (mechanicIndex = 0; mechanicIndex < mechanicInfo.Count; mechanicIndex++)
            {
                Hashtable currentMechanic = (Hashtable)mechanicInfo[mechanicIndex];
                // Category 1 = Races
                if (currentMechanic["category"].ToString().ToInt32() == 1)
                {
                    if (mission.NumFollowers > 0)
                    {
                        for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                        {
                            var currentFollower = followerInfo[followerIndex];
                            for (int abilityIndex = 0;
                                abilityIndex < currentFollower.abilities.Count;
                                abilityIndex++)
                            {
                                var currentAbility =
                                    (Hashtable) g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                                var type = (ArrayList) currentAbility["type"];
                                var numTypes = type.Count;

                                for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                                {
                                    var counters = (ArrayList) currentAbility["counters"];
                                    var amount1 = (ArrayList) currentAbility["amount1"];
                                    var amount2 = (ArrayList) currentAbility["amount2"];
                                    var amount3 = (ArrayList) currentAbility["amount3"];
                                    if (typeIndex >= counters.Count
                                        || typeIndex >= amount1.Count
                                        || typeIndex >= amount2.Count
                                        || typeIndex >= amount3.Count)
                                        continue;
                                    var typeInt = type[typeIndex].ToString().ToInt32();
                                    var counterInt = counters[typeIndex].ToString().ToInt32();
                                    var amount1Int = amount1[typeIndex].ToString().ToInt32();
                                    var amount2Int = amount2[typeIndex].ToString().ToInt32();
                                    var amount3Int = amount3[typeIndex].ToString().ToInt32();
                                    if (currentMechanic["type"].ToString().ToInt32() == counterInt)
                                    {
                                        var q = CalcChance(amount2Int, amount3Int, currentFollower.bias);
                                        q *= coeff;
                                        successChance += q;
                                        if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to follower {1} enemy race ability {2}", q, currentFollower.follower, currentMechanic["id"]);
                                    }
                                }
                            }
                        } // for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                    } // if (mission.NumFollowers > 0)
                } // if ((double) currentMechanic["category"] == 1.0d)
            } // for (mechanicIndex = 0; mechanicIndex < mechanicInfo.Count; mechanicIndex++)

            if (mission.NumFollowers > 0)
            {
                for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                {
                    var currentFollower = followerInfo[followerIndex];
                    for (int abilityIndex = 0;
                        abilityIndex < currentFollower.abilities.Count;
                        abilityIndex++)
                    {
                        var currentAbility =
                            (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                        var type = (ArrayList)currentAbility["type"];
                        var numTypes = type.Count;

                        for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                        {
                            var counters = (ArrayList)currentAbility["counters"];
                            var amount1 = (ArrayList)currentAbility["amount1"];
                            var amount2 = (ArrayList)currentAbility["amount2"];
                            var amount3 = (ArrayList)currentAbility["amount3"];
                            if (typeIndex >= counters.Count
                                || typeIndex >= amount1.Count
                                || typeIndex >= amount2.Count
                                || typeIndex >= amount3.Count)
                                continue;
                            var typeInt = type[typeIndex].ToString().ToInt32();
                            var counterInt = counters[typeIndex].ToString().ToInt32();
                            var amount1Int = amount1[typeIndex].ToString().ToInt32();
                            var amount2Int = amount2[typeIndex].ToString().ToInt32();
                            var amount3Int = amount3[typeIndex].ToString().ToInt32();
                            if (wowheadMissionObject["mechanictype"].ToString().ToInt32() == counterInt)
                            {
                                var q = CalcChance(amount2Int, amount3Int, currentFollower.bias);
                                q *= coeff;
                                successChance += q;
                                if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to follower {1} environment ability {2}", q, currentFollower.follower, currentAbility["id"]);
                            }
                        }
                    }
                } // for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
            } // if (mission.NumFollowers > 0)

            var y = GetMissionTimes();

            if (mission.NumFollowers > 0)
            {
                for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                {
                    var currentFollower = followerInfo[followerIndex];
                    for (int abilityIndex = 0;
                        abilityIndex < currentFollower.abilities.Count;
                        abilityIndex++)
                    {
                        var currentAbility =
                            (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                        var type = (ArrayList)currentAbility["type"];
                        var numTypes = type.Count;

                        for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                        {
                            var u = false;
                            var counters = (ArrayList)currentAbility["counters"];
                            var amount1 = (ArrayList)currentAbility["amount1"];
                            var amount2 = (ArrayList)currentAbility["amount2"];
                            var amount3 = (ArrayList)currentAbility["amount3"];
                            var race = (ArrayList) currentAbility["race"];
                            var hours = (ArrayList) currentAbility["hours"];
                            if (typeIndex >= counters.Count
                                || typeIndex >= amount1.Count
                                || typeIndex >= amount2.Count
                                || typeIndex >= amount3.Count
                                || typeIndex >= race.Count
                                || typeIndex >= hours.Count)
                                continue;
                            var typeInt = type[typeIndex].ToString().ToInt32();
                            var counterInt = counters[typeIndex].ToString().ToInt32();
                            var amount1Int = amount1[typeIndex].ToString().ToInt32();
                            var amount2Int = amount2[typeIndex].ToString().ToInt32();
                            var amount3Int = amount3[typeIndex].ToString().ToInt32();
                            var raceInt = race[typeIndex].ToString().ToInt32();
                            var hoursInt = hours[typeIndex].ToString().ToInt32();

                            switch (typeInt)
                            {
                                // Lone Wolf - Increases success chance when on a mission alone
                                case 1:
                                    if (followerInfo.Count == 1)
                                        u = true;
                                    break;

                                // Combat Experience - Grants a bonus to mission success chance
                                case 2:
                                    u = true;
                                    break;

                                // Race - Gnome-Lover / Humanist / Dwarvenborn / etc...
                                // Increases success chance when on a mission with a <race>
                                case 5:
                                    if (CheckEffectRace(raceInt, followerIndex))
                                        u = true;
                                    break;

                                // High Stamina - Increases success chance on missions with duration longer than 7 hours
                                case 6:
                                    if (y.Item1 > 3600*hoursInt)
                                        u = true;
                                    break;

                                // Burst of Power - Increases success chance on missions with duration shorter than 7 hours
                                case 7:
                                    if (y.Item1 < 3600*hoursInt)
                                        u = true;
                                    break;

                                // Doesn't appear to matter anymore??  travel time??
                                case 9:
                                    if (y.Item2 > 3600*hoursInt)
                                        u = true;
                                    break;

                                // doesn't appear to matter anymore??  travel time??
                                case 10:
                                    if (y.Item2 < 3600*hoursInt)
                                        u = true;
                                    break;

                                default:
                                    break;
                            }

                            if (u)
                            {
                                var q = CalcChance(amount2Int, amount3Int, currentFollower.bias);
                                q *= coeff;
                                successChance += q;
                                if (enableDebugPrint) GarrisonButler.Diagnostic("Added {0} to success due to follower {1} trait {2}.", q,
                                    currentFollower.follower, typeInt);
                            }
                        }
                    }
                } // for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
            } // if (mission.NumFollowers > 0)

            if (enableDebugPrint) GarrisonButler.Diagnostic("Total before adding base chance: {0}", successChance);

            var t = true;
            var k = 100;
            var h = 0d;
            var p = (((100 - wowheadMissionObject["basebonuschance"].ToString().ToFloat()) * successChance) * 0.01d) +
                    wowheadMissionObject["basebonuschance"].ToString().ToFloat();
            if (enableDebugPrint) GarrisonButler.Diagnostic("Total after base chance: {0}", p);
            h = p;
            var g = h;
            if (t && k <= p)
            {
                h = k;
            }

            if (t && g > 100)
                if (enableDebugPrint) GarrisonButler.Diagnostic("Total success chance: {0}, ({1} before clamping)", h, g);
            else
                    if (enableDebugPrint) GarrisonButler.Diagnostic("Total success chance: {0}", h);

            if (enableDebugPrint) GarrisonButler.Diagnostic("----- End CalculateSuccessChance -----");

            returnValue = new Tuple<double, double>(Math.Floor(h), g - h);

            //how do I find encounter data?

            return returnValue;
        }

        public static bool CheckEffectRace(int race, int index)
        {
            if (mission.NumFollowers > 0)
            {
                for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                {
                    if (followerIndex == index)
                        continue;
                    var currentFollower = followerInfo[followerIndex];
                    if (g_garrison_followers == null)
                        continue;
                    Hashtable gfollowerobject = (Hashtable) g_garrison_followers[currentFollower.follower.ToString()];
                    if (gfollowerobject == null)
                        continue;
                    Hashtable sideInfo = (Hashtable) gfollowerobject[StyxWoW.Me.IsAlliance ? "alliance" : "horde"];
                    if (sideInfo == null)
                        continue;
                    if (sideInfo["race"] == null)
                        continue;
                    var raceInt = sideInfo["race"].ToString().ToInt32();
                    if (raceInt == race)
                        return true;
                }
            }

            return false;
        }

        public static Tuple<int, int> GetMissionTimes()
        {
            var missiontime = wowheadMissionObject["missiontime"].ToString().ToInt32();
            var traveltime = wowheadMissionObject["traveltime"].ToString().ToInt32();

            if (mission.NumFollowers > 0)
            {
                for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
                {
                    var currentFollower = followerInfo[followerIndex];
                    for (int abilityIndex = 0;
                        abilityIndex < currentFollower.abilities.Count;
                        abilityIndex++)
                    {
                        var currentAbility =
                            (Hashtable)g_garrison_abilities[currentFollower.abilities[abilityIndex].ToString()];
                        var type = (ArrayList)currentAbility["type"];
                        var numTypes = type.Count;

                        for (var typeIndex = 0; typeIndex < numTypes; typeIndex++)
                        {
                            var counters = (ArrayList)currentAbility["counters"];
                            var amount1 = (ArrayList)currentAbility["amount1"];
                            var amount2 = (ArrayList)currentAbility["amount2"];
                            var amount3 = (ArrayList)currentAbility["amount3"];
                            var amount4 = (ArrayList) currentAbility["amount4"];
                            if (typeIndex >= counters.Count
                                || typeIndex >= amount1.Count
                                || typeIndex >= amount2.Count
                                || typeIndex >= amount3.Count
                                || typeIndex >= amount4.Count)
                                continue;
                            var typeInt = type[typeIndex].ToString().ToInt32();
                            var counterInt = counters[typeIndex].ToString().ToInt32();
                            var amount1Int = amount1[typeIndex].ToString().ToInt32();
                            var amount2Int = amount2[typeIndex].ToString().ToInt32();
                            var amount3Int = amount3[typeIndex].ToString().ToInt32();
                            var amount4Int = amount4[typeIndex].ToString().ToInt32();
                            if (typeInt == 3)
                            {
                                traveltime *= amount4Int;
                            }

                            if (typeInt == 17)
                            {
                                missiontime *= amount4Int;
                            }
                        }
                    }
                } // for (var followerIndex = 0; followerIndex < followerInfo.Count; followerIndex++)
            } // if (mission.NumFollowers > 0)

            return new Tuple<int, int>(missiontime, traveltime);
        }

        public static bool ThreatCounterIsAlreadyRegistered(int a, int b, int c)
        {
            return registeredThreatCounters[a, b, c];
        }

        public static void RegisterThreatCounter(int a, int b, int c)
        {
            registeredThreatCounters[a, b, c] = true;
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

        public static double GetFollowerBias(int level, int ilvl)
        {
            const int defaultMinItemLevel = 600;
            double returnVal = (level - mission.Level) * (1.0d/3.0d);

            if (mission.Level == 100)
            {
                var d = mission.ItemLevel;

                if (d <= 0)
                    d = defaultMinItemLevel;

                if (d > 0)
                    returnVal += (ilvl - d)*(1.0d/15.0d);
            }

            if (returnVal < -1)
            {
                returnVal = -1;
            }

            if (returnVal > 1)
            {
                returnVal = 1;
            }

            return returnVal;
        }

        public static double CalcChance(int amount2, int amount3, double bias)
        {
            double b = Convert.ToDouble(amount2);
            double a = Convert.ToDouble(amount3);
            double c = bias;
            var d = 0.0d;

            //if (bias >= 0)
            //{
            //    d = (amount3 - amount2)*bias + amount2;
            //}
            //else
            //{
            //    d = (bias + 1)*amount2;
            //}

            if (c >= 0)
            {
                d = (a - b)*c + b;
            }
            else
            {
                d = (c + 1)*b;
            }

            return d;
        }

        //public static double fround(float b)
        //{
        //    if(Math.Round())
        //}
    }
}
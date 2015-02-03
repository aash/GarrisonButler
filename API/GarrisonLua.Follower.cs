#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;
using Facet.Combinatorics;
using Styx.Common;

#endregion

namespace GarrisonButler.API
{
    public static class FollowersLua
    {
        // Return list of all available followers

        public static List<Follower> GetAllFollowers()
        {
            var followers = API.ButlerLua.GetAllFromLua<Follower>(GetListFollowersId, GetFollowerById);
            //var newMissionStubCodeResult = NewMissionStubCode(followers);
            return followers;
        }

        public static List<Follower> NewMissionStubCode(List<Follower> followers )
        {
            var missions = MissionLua.GetAllAvailableMissions();
            var slots = missions.Sum(m => m.NumFollowers);
            var numFollowers = followers.Count;
            var numMissions = missions.Count;

            missions.ForEach(f =>
            {
                GarrisonButler.Diagnostic(">> Mission: " + f.Name);
                f.Rewards.ForEach(r => GarrisonButler.Diagnostic("  >> Reward: " + r.Name));
            });

            if (slots == 0)
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # slots = 0");
                return new List<Follower>();
            }

            if (numFollowers == 0)
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # followers = 0");
                return new List<Follower>();
            }

            if (numMissions == 0)
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # missions = 0");
                return new List<Follower>();
            }

            //IEnumerable<> missions
            //    .OrderBy(m => m.Rewards[0].Id)
            //    .ThenBy(m => m.Rewards[1].Id)
            //    .ThenBy(m => m.Rewards[2])


            //CASE 1 - slots < # followers
            //if (slots < numFollowers)
            if(false)
            {
                missions = missions.OrderBy(m => m.NumFollowers).ToList();
                //STEP 1 - Sort by "reward type"
                GarrisonButler.Diagnostic("followers count=" + numFollowers);
                GarrisonButler.Diagnostic("slots count=" + slots);
                GarrisonButler.Diagnostic("missions count=" + missions.Count);
                var followerCombinations = new Combinations<Follower>(followers, slots, GenerateOption.WithoutRepetition);
                GarrisonButler.Diagnostic("Combinations Count=" + followerCombinations.Count);
                int myCount = 0;
                int iter = 0;
                //Enumerable
                DateTime startedAt = DateTime.Now;
                var missionFollowerCombos = followerCombinations
                    .Select(
                        fc =>
                        {
                            // Loop all missions and all followers
                            // to fill up missions slots with followers
                            //var mfComboList = new List<MissionFollowersCombo>();
                            var mfComboList = new Dictionary<Mission, List<Follower>>();
                            var slotsRemaining = slots;
                            List<Follower> followerCombo = fc.ToList();
                            foreach (Mission m in missions)
                            {
                                var startIndex = slots - slotsRemaining;
                                var slotsToFill = m.NumFollowers;
                                var followersToAdd = followerCombo.GetRange(startIndex, slotsToFill);
                                //mfComboList.Add(new MissionFollowersCombo(m, followersToAdd));
                                mfComboList.Add(m, followersToAdd);
                                slotsRemaining -= slotsToFill;
                            }
                            return mfComboList;
                        });
                using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
                {
                    missionFollowerCombos.ForEach(mfc =>
                    {
                        myCount += mfc.Count;
                        iter++;
                        mfc.ForEach(
                            kv =>
                                InterfaceLua.AddFollowersToMission(kv.Key.MissionId,
                                    kv.Value.Select(f => f.FollowerId).ToList()));
                        //InterfaceLua.AddFollowersToMission(mfc.Keys)
                        //GarrisonButler.Diagnostic("Configuration - count=" + mfc.Count);
                        //foreach (MissionFollowersCombo combo in mfc)
                        //{
                        //    GarrisonButler.Diagnostic("  mission: " + combo._mission.Name);
                        //    GarrisonButler.Diagnostic("  followers:");
                        //    int count = 0;
                        //    foreach (Follower f in combo._followers)
                        //    {
                        //        count++;
                        //        GarrisonButler.Diagnostic("    " + count + ")" + f.Name);
                        //    }
                        //}
                    });
                }

                GarrisonButler.Diagnostic("======");
                GarrisonButler.Diagnostic("myCount=" + myCount);
                GarrisonButler.Diagnostic("iter=" + iter);
                GarrisonButler.DiagnosticLogTimeTaken("Follower LUA loop", startedAt);
                GarrisonButler.Diagnostic("======");
            }
            //CASE 2 - slots = # followers
            else if (slots == numFollowers)
            {

            }
            //CASE 3 - slots > # followers
            else
            {

            }
            //List<MissionFollowersCombo> sortedByExperience = missionList
            //    .SelectMany<Mission, MissionFollowersCombo, MissionFollowersCombo>(
            //        (mission, index) =>
            //        {
            //            int remainingFollowers = followerList.Count;
            //            List<MissionFollowersCombo> mfComboList = new List<MissionFollowersCombo>();

            //            while (remainingFollowers > mission.NumFollowers)
            //            {
            //                List<Follower> followers = followerList.GetRange(followerList.Count - remainingFollowers, mission.NumFollowers);
            //                remainingFollowers -= mission.NumFollowers;
            //                mfComboList.Add(new MissionFollowersCombo(mission, followers));
            //            }
            //            return mfComboList;
            //        },
            //        (origMission, combo) =>
            //        {
            //            return .GetExperience();
            //        });

            return followers;
        }

        public static string GetFollowerName(string follower)
        {
            var lua = String.Format("return C_Garrison.GetFollowerName(\"{0}\");", follower);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerNameById(string followerId)
        {
            var lua = String.Format("return C_Garrison.GetFollowerNameByID(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerClassSpecName(string follower)
        {
            var lua = String.Format("return C_Garrison.GetFollowerClassSpecName(\"{0}\");", follower);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }


        public static string GetFollowerDisplayIdbyId(string followerId)
        {
            var lua = String.Format("return C_Garrison.GetFollowerDisplayIDByID(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerStatus(string followerId)
        {
            var lua =
                "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format("for i,v in ipairs(followers) do " +
                              "local followerID = (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID;" +
                              "if (followerID == {0}) then table.insert(RetInfo,tostring(v.status)); end;" +
                              "end;" +
                              "return unpack(RetInfo)", followerId);

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault() == "nil"
                ? "None"
                : (Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault() == default(string)
                    ? "None"
                    : Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault());
        }

        public static string GetFollowerInfo(string followerId)
        {
            var lua = String.Format("return {0} and C_Garrison.GetFollowerInfo(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        // Return the follower corresponding to the id

        public static Follower GetFollowerById(String followerId)
        {
            var lua =
                "local RetInfo = {}; local Temp = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format(
                    "for i,f in ipairs(followers) do " +
                    "local followerID = (f.garrFollowerID) and tonumber(f.garrFollowerID) or f.followerID;" +
                    "if (followerID == {0}) then " +
                    "Temp[0] = followerID;" +
                    "Temp[1] = f.name;" +
                    "Temp[2] = f.status;" +
                    "Temp[3] = f.ClassSpecName;" +
                    "Temp[4] = f.quality;" +
                    "Temp[5] = f.level;" +
                    "Temp[6] = f.isCollected ;" +
                    "Temp[7] = f.iLevel;" +
                    "Temp[8] = f.levelXP;" +
                    "Temp[9] = f.xp;" +
                    "end;" +
                    "end;" +
                    "for j_=0,9 do table.insert(RetInfo,tostring(Temp[j_]));end; " +
                    "return unpack(RetInfo)", followerId);
            var follower = Lua.GetReturnValues(lua);

            if (follower.IsNullOrEmpty())
                return null;

            var name = follower[1];
            var status = follower[2];
            var classSpecName = follower[3];
            var quality = follower[4];
            var level = follower[5].ToInt32();
            var isCollected = follower[6].ToBoolean();
            var iLevel = follower[7].ToInt32();
            var xp = follower[8].ToInt32();
            var levelXp = follower[9].ToInt32();

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  "for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(counterID));" +
                  "end;" +
                  "end;" +
                  "return unpack(RetInfo)";

            var abilities = Lua.GetReturnValues(lua);

            return new Follower(followerId, name, status, classSpecName, quality, level, isCollected, iLevel, xp,
                levelXp, abilities);
        }

        // Return list of all available missions

        public static List<string> GetListFollowersId()
        {
            const string lua = "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                               "for i,v in ipairs(followers) do " +
                               "table.insert(RetInfo,tostring( (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID));" +
                               "end;" +
                               "return unpack(RetInfo)";

            var followerId = Lua.GetReturnValues(lua);
            return followerId;
        }

        public static int GetNumFollowersOnMission(int missionId)
        {
            var lua = String.Format("return C_Garrison.GetNumFollowersOnMission(\"{0}\");", missionId);
            var luaRet = Lua.GetReturnValues(lua);
            return Convert.ToInt32(luaRet.GetEmptyIfNull().FirstOrDefault());
        }

        public static void AddFollowerToMission(int missionId, int followerId)
        {
            var lua = String.Format("C_Garrison.AddFollowerToMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }

        public static void RemoveFollowerFromMission(int missionId, int followerId)
        {
            var lua = String.Format("C_Garrison.RemoveFollowerFromMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }
    }
}
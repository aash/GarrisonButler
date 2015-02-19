#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Documents;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;
using Facet.Combinatorics;
using GarrisonButler.Config;
using GarrisonButler.Libraries.JSON;
using GarrisonButler.Libraries.Wowhead;
using GarrisonButler.Objects;
using Styx;
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
            var newMissionStubCodeResult = NewMissionStubCode(followers);
            return followers;
        }

        public static List<Follower> NewMissionStubCode(List<Follower> followers )
        {
            var missions = MissionLua.GetAllAvailableMissions();
            var slots = missions.Sum(m => m.NumFollowers);
            var rewards = GaBSettings.Get().MissionRewardSettings;
            var numMissions = missions.Count;
            var numFollowers = followers.Count;
            var numRewards = rewards.Count;
            DateTime missionCodeStartedAt = DateTime.Now;
            long combosTried = 0;
            double totalSuccessChance = 0;

            //TEST JSON
            MissionCalc.LoadJSONData();
            int count = 0;



            //var toAdd = new List<Follower>();
            //while (count < 3)
            //{
            //    toAdd.Add(followers[count]);
            //    count++;
            //}
            //MissionCalc.followers = toAdd;


            //foreach (Mission m in missions)
            //{
            //    MissionCalc.mission = m;
            //    GarrisonButler.Diagnostic("** Mission = {0} **", m.Name);
            //    for (int offset = 0; offset < numFollowers; offset += m.NumFollowers)
            //    {
            //        MissionCalc.followers = followers.GetRange(offset, m.NumFollowers);
            //        count = 0;
            //        while (count < MissionCalc.followers.Count)
            //        {
            //            GarrisonButler.Diagnostic("  Follower {0}: {1}", (count + 1).ToString(),
            //                MissionCalc.followers[count]);
            //            count++;
            //        }
            //        var result = MissionCalc.CalculateSuccessChance();
            //        GarrisonButler.Diagnostic("  Success: " + result.Item1);
            //        GarrisonButler.Diagnostic("  ChanceOver: " + result.Item2);
            //    }
            //}
            //TEST JSON

            missions.ForEach(f =>
            {
                GarrisonButler.Diagnostic(">> Mission: " + f.Name);
                f.Rewards.ForEach(r => GarrisonButler.Diagnostic("  >> Reward: " + r.Name));
            });

            followers.ForEach(f =>
            {
                GarrisonButler.Diagnostic(">> Follower: " + f.Name);
                GarrisonButler.Diagnostic("  Quality: " + f.Quality);
                GarrisonButler.Diagnostic("  Level: " + f.Level);
                GarrisonButler.Diagnostic("  Status: " + f.Status);
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

            // Only consider followers that are collected
            followers.RemoveAll(f => !f.IsCollected);

            //local statusPriority = {
            //    [GARRISON_FOLLOWER_IN_PARTY] = 1,
            //    [GARRISON_FOLLOWER_WORKING] = 2,
            //    [GARRISON_FOLLOWER_ON_MISSION] = 3,
            //    [GARRISON_FOLLOWER_EXHAUSTED] = 4,
            //    [GARRISON_FOLLOWER_INACTIVE] = 5,
            //}
            // Only consider followers that are available (0) or in party (1)
            followers.RemoveAll(f => f.Status.ToInt32() > 1);

            // Get the current top of the list reward
            foreach (var reward in rewards)
            {
                if (followers.Count <= 0)
                {
                    GarrisonButler.Diagnostic("Breaking reward loop due to followers.Count = 0");
                    break;
                }

                GarrisonButler.Diagnostic("-- Reward: {0} --", reward.Name);

                // Discard any missions where the reward is set to "disallowed"
                if (reward.DisallowMissionsWithThisReward)
                {
                    missions.RemoveAll(m => m.Rewards.Any(mr => mr.Id == reward.Id));
                    continue;
                }
                
                var missionsWithCurrentReward = missions
                    // Pick the missions off the list matching the current reward
                    .Where(m => m.Rewards.Any(mr => mr.Id == reward.Id))
                    // Sort missions by highest quantity of reward
                    .OrderByDescending(m =>
                    {
                        var found = m.Rewards.FirstOrDefault(mr => mr.Id == reward.Id);
                        return found == null ? 0 : found.Quantity;
                    })
                    .ToList();

                if (!missionsWithCurrentReward.Any())
                {
                    GarrisonButler.Diagnostic("  !! No Missions with this reward !!");
                    continue;
                }

                missionsWithCurrentReward.ForEach(m =>
                {
                    GarrisonButler.Diagnostic("  >> Mission: {0} <<", m.Name);
                    m.Rewards.ForEach(r => GarrisonButler.Diagnostic("    ** Reward ({0}): {1} **", r.Quantity, r.Name));
                });

                // Skip any missions that don't meet user requirements (quantity > X for example)
                var missionsThatMeetRequirement = missionsWithCurrentReward
                    .Where(m => m.Rewards
                        .Where(mr => mr.Id == reward.Id)
                        // Check both Mission amount conditions & Player amount conditions
                        .Any(mr => 
                            (reward.ConditionForMission == null || reward.ConditionForMission.GetCondition(mr))
                            && (reward.ConditionForPlayer == null || reward.ConditionForPlayer.GetCondition(mr))
                        )
                    )
                    .ToList();

                if (!missionsThatMeetRequirement.Any())
                {
                    GarrisonButler.Diagnostic("  !! No Missions meet user requirements !!");
                    continue;
                }

                //Indicates the quality (or rarity) of an item. Possible values and examples:
                //0. Poor (gray): Broken I.W.I.N. Button
                //1. Common (white): Archmage Vargoth's Staff
                //2. Uncommon (green): X-52 Rocket Helmet
                //3. Rare / Superior (blue): Onyxia Scale Cloak
                //4. Epic (purple): Talisman of Ephemeral Power
                //5. Legendary (orange): Fragment of Val'anyr
                //6. Artifact (golden yellow): The Twin Blades of Azzinoth
                //7. Heirloom (light yellow): Bloodied Arcanite Reaper

                var followersToConsider =
                    // If reward type is FollowerXP, discard all level 100 epic followers
                    (
                    reward.Category == MissionReward.MissionRewardCategory.FollowerExperience
                        ? followers.SkipWhile(f => f.Quality.ToInt32() > 3 && f.Level >= 100)
                        : followers
                    )
                    .ToList();

                GarrisonButler.Diagnostic("Only considering {0} of {1} followers", followersToConsider.Count, followers.Count);
                followersToConsider.ForEach(f => GarrisonButler.Diagnostic(">> FollowerToConsider: " + f.Name));

                DateTime startedAt = DateTime.Now;
                foreach (var mission in missionsThatMeetRequirement)
                {
                    Combinations<Follower> followerCombinations = new Combinations<Follower>(followersToConsider, mission.NumFollowers);
                    //GarrisonButler.Diagnostic("**Combination**");
                    //GarrisonButler.Diagnostic("Mission: " + mission.Name);
                    //GarrisonButler.Diagnostic("Combinations: " + followerCombinations.Count);

                    MissionCalc.mission = mission;
                    //for (int offset = 0; offset < numFollowers; offset += mission.NumFollowers)
                    //{
                    var bestCombo = followerCombinations.FirstOrDefault();
                    var bestSuccess = 0.0d;
                    List<Tuple<IList<Follower>, double>> successChances = new List<Tuple<IList<Follower>, double>>();
                    foreach (var combo in followerCombinations)
                    {
                        //MissionCalc.followers = followers.GetRange(offset, mission.NumFollowers);
                        MissionCalc.followers = combo.ToList();
                        //MissionCalc.followers.ForEach(f => GarrisonButler.Diagnostic("  Follower: " + f));
                        //count = 0;
                        //while (count < MissionCalc.followers.Count)
                        //{
                        //    GarrisonButler.Diagnostic("  Follower {0}: {1}", (count + 1).ToString(),
                        //        MissionCalc.followers[count]);
                        //    count++;
                        //}
                        var result = MissionCalc.CalculateSuccessChance();
                        //GarrisonButler.Diagnostic("  Success: " + result.Item1);
                        //GarrisonButler.Diagnostic("  ChanceOver: " + result.Item2);
                        successChances.Add(new Tuple<IList<Follower>, double>(combo, result.Item1));
                    }

                    if (bestCombo != null)
                    {
                        GarrisonButler.Diagnostic("************* BEGIN Mission=" + mission.Name + "**************");
                        GarrisonButler.Diagnostic("Total combinations tried = " + followerCombinations.Count);
                        GarrisonButler.DiagnosticLogTimeTaken("Trying all " + followerCombinations.Count + " combinations", startedAt);
                        var first = successChances.OrderByDescending(sc => sc.Item2).FirstOrDefault();
                        if (first != null)
                        {
                            bestCombo = first.Item1;
                            bestSuccess = first.Item2;

                            var sucChanceToCompareAgainst = reward.IndividualSuccessChanceEnabled
                                ? reward.RequiredSuccessChance
                                : GaBSettings.Get().DefaultMissionSuccessChance;

                            if (Convert.ToInt32(bestSuccess) < sucChanceToCompareAgainst)
                            {
                                GarrisonButler.Diagnostic("Best combo doesn't meet minimum success chance requirements!");
                                continue;
                            }
                        }
                        else
                        {
                            GarrisonButler.Diagnostic("Error retrieving success chance for best combo: bestCombo.Count={0}, successChances.Count={1}", bestCombo.GetEmptyIfNull().Count(), successChances.GetEmptyIfNull().Count());
                            continue;
                        }
                        combosTried += followerCombinations.Count;
                        GarrisonButler.Diagnostic("Best Combination with success=" + bestSuccess  + "% for Mission=" + mission.Name + " is ");
                        bestCombo.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
                        successChances.ForEach(c =>
                        {
                            //GarrisonButler.Diagnostic(" >> Combo << ");
                            //c.Item1.ForEach(i => GarrisonButler.Diagnostic(" -> Follower: " + i.Name));
                            //GarrisonButler.Diagnostic(" = " + c.Item2 + "% success");
                            totalSuccessChance += c.Item2;
                        });
                        GarrisonButler.Diagnostic("Followers before removal: " + followersToConsider.Count);
                        bestCombo.ForEach(c =>
                        {
                            followersToConsider.RemoveAll(f => f.FollowerId == c.FollowerId);
                            followers.RemoveAll(f => f.FollowerId == c.FollowerId);
                        });
                        GarrisonButler.Diagnostic("Followers after removal: " + followersToConsider.Count);
                        GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
                        if (followersToConsider.Count <= 0)
                        {
                            GarrisonButler.Diagnostic("Breaking mission loop due to followersToConsider = 0");
                            break;
                        }
                    }
                    //}

                    //using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
                    //{
                    //    foreach (var combo in followerCombinations)
                    //    {
                    //        var followerIds = combo.Select(f => f.FollowerId).ToList();
                    //        Stopwatch timer = new Stopwatch();
                    //        timer.Start();
                    //        while (!InterfaceLua.IsGarrisonMissionTabVisible()
                    //               && timer.ElapsedMilliseconds < 2000)
                    //        {
                    //            GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
                    //            InterfaceLua.ClickTabMission();
                    //        }

                    //        if (!InterfaceLua.IsGarrisonMissionTabVisible())
                    //        {
                    //            GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
                    //            return new List<Follower>();
                    //        }

                    //        timer.Reset();
                    //        timer.Start();
                    //        while (!InterfaceLua.IsGarrisonMissionVisible()
                    //               && timer.ElapsedMilliseconds < 2000)
                    //        {
                    //            GarrisonButler.Diagnostic("Mission not visible, opening mission: " + mission.MissionId +
                    //                                      " - " +
                    //                                      mission.Name);
                    //            InterfaceLua.OpenMission(mission);
                    //        }

                    //        if (!InterfaceLua.IsGarrisonMissionVisible())
                    //        {
                    //            GarrisonButler.Warning("Couldn't display GarrisonMissionFrame.");
                    //            return new List<Follower>();
                    //        }

                    //        timer.Reset();
                    //        timer.Start();
                    //        while (!InterfaceLua.IsGarrisonMissionVisibleAndValid(mission.MissionId)
                    //               && timer.ElapsedMilliseconds < 2000)
                    //        {
                    //            GarrisonButler.Diagnostic(
                    //                "Mission not visible or not valid, close and then opening mission: " +
                    //                mission.MissionId + " - " + mission.Name);
                    //            InterfaceLua.ClickCloseMission();
                    //            InterfaceLua.OpenMission(mission);
                    //        }

                    //        if (!InterfaceLua.IsGarrisonMissionVisibleAndValid(mission.MissionId))
                    //        {
                    //            GarrisonButler.Warning("Couldn't display GarrisonMissionFrame or wrong mission opened.");
                    //            return new List<Follower>();
                    //        }


                    //        InterfaceLua.AddFollowersToMissionNonTask(mission.MissionId, followerIds);
                    //        API.MissionLua.GetPartyMissionInfo(mission);
                    //        combo.ForEach(f => RemoveFollowerFromMission(mission.MissionId.ToInt32(), f.FollowerId.ToInt32()));
                    //        //RemoveFollowerFromMission(mission.MissionId, );
                    //        GarrisonButler.Diagnostic("** Current Combo: ");
                    //        combo.ForEach(f => GarrisonButler.Diagnostic(" - " + f.Name));
                    //        GarrisonButler.Diagnostic("--> Success Chance: " + mission.SuccessChance);
                    //        GarrisonButler.Diagnostic("--> Material Multiplier: " + mission.MaterialMultiplier);
                    //        GarrisonButler.Diagnostic("--> XP Bonus: " + mission.XpBonus);
                    //    }
                    //}

                }

                //Combinations<Follower> followerCombinations = new Combinations<Follower>(followersToConsider.ToList(), );
            }

            GarrisonButler.Diagnostic("Done with Mission Calculations");
            GarrisonButler.Diagnostic("Total combinations = " + combosTried);
            GarrisonButler.Diagnostic("Total missions = " + missions.Count);
            GarrisonButler.Diagnostic("Total followers = " + followers.Count);
            GarrisonButler.Diagnostic("Average success chance = " + totalSuccessChance / (double)combosTried + "%");
            GarrisonButler.DiagnosticLogTimeTaken("Mission Stub Code", missionCodeStartedAt);
            // Sort missions by highest quantity
            // Pick the missions off the list matching the current reward
            // Discard any missions where the reward is set to "disallowed"
            // Skip any missions that don't meet user requirements (quantity > X for example)
            // Calculate # of slots for the remaining misssions in this batch

            // If reward type is FollowerXP, discard all level 100 epic followers
            // Find all combinations of X followers to Y mission slots for this batch
            // Calculate success chance (via LUA call) for each follower combination

            // STEP 1 - Take missions off the list where they match the highest
            //          priority in the user settings

            // STEP 2 - Sort missions by highest quantity

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
                    //"Temp[2] = f.status;" +
                    "if f.status == GARRISON_FOLLOWER_IN_PARTY then Temp[2] = 1;" +
                    "elseif f.status == GARRISON_FOLLOWER_WORKING then Temp[2] = 2;" +
                    "elseif f.status == GARRISON_FOLLOWER_ON_MISSION then Temp[2] = 3;" +
                    "elseif f.status == GARRISON_FOLLOWER_EXHAUSTED then Temp[2] = 4;" +
                    "elseif f.status == GARRISON_FOLLOWER_INACTIVE then Temp[2] = 5;" +
                    "else Temp[2] = 0;" +
                    "end;" + 
                    "Temp[3] = f.ClassSpecName;" +
                    "Temp[4] = f.quality;" +
                    "Temp[5] = f.level;" +
                    "Temp[6] = f.isCollected ;" +
                    "Temp[7] = f.iLevel;" +
                    "Temp[8] = f.levelXP;" +
                    "Temp[9] = f.xp;" +
                    "Temp[10] = f.followerID;" +
                    "end;" +
                    "end;" +
                    "for j_=0,10 do table.insert(RetInfo,tostring(Temp[j_]));end; " +
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
            var uniqueId = follower[10];

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", uniqueId != "nil" ? uniqueId : followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  "for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(counterID));" +
                  "end;" +
                  "end;" +
                  "return unpack(RetInfo)";

            var counters = Lua.GetReturnValues(lua);

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", uniqueId != "nil" ? uniqueId : followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  //"for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(ability.id));" +
                  //"end;" +
                  "end;" +
                  "return unpack(RetInfo)";

            var abil_string = Lua.GetReturnValues(lua);
            var abilities = abil_string.Select(a => a.ToInt32()).ToList();

            return new Follower(followerId, uniqueId, name, status, classSpecName, quality, level, isCollected, iLevel, xp,
                levelXp, counters, abilities);
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
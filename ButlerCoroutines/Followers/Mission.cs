#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Facet.Combinatorics;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.Libraries.Wowhead;
using GarrisonButler.Objects;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {
        public static bool Check = true;

        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        private static readonly WaitTimer RefreshMissionsTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static readonly WaitTimer RefreshFollowerTimer = new WaitTimer(TimeSpan.FromMinutes(5));
        private static WoWPoint _tablePosition = WoWPoint.Empty;

        private static void InitializeMissions()
        {
            RefreshMissions(true);
            RefreshFollowers(true);
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

        public static List<Tuple<Mission, Follower[]>> NewMissionStubCode(List<Follower> followers)
        {
            var missions = MissionLua.GetAllAvailableMissions();
            var slots = missions.Sum(m => m.NumFollowers);
            var rewards = GaBSettings.Get().MissionRewardSettings;
            var numMissions = missions.Count;
            var numFollowers = followers.Count;
            var numRewards = rewards.Count;
            DateTime missionCodeStartedAt = DateTime.Now;
            long combosTried = 0;
            var acceptedCombos = 0;
            double totalSuccessChance = 0;
            var toStart = new List<Tuple<Mission, Follower[]>>();

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
                return toStart;
            }

            if (numFollowers == 0)
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # followers = 0");
                return toStart;
            }

            if (numMissions == 0)
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # missions = 0");
                return toStart;
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
                        .Where(mr => mr.Id == reward.Id
                            // Handle the case where we are looking at a category of rewards instead of individual rewards
                            || (mr.Id != reward.Id && mr.Category == reward.Category && mr.Id == (int)mr.Category))
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

                List<Follower> followersToConsider;

                if (reward.Category == MissionReward.MissionRewardCategory.FollowerExperience
                    && !GaBSettings.Get().IncludeEpicMaxLevelFollowersForExperience)
                {
                    GarrisonButler.Diagnostic("Going to use reduced follower list");
                    followersToConsider = new List<Follower>();
                    for (var i = 0; i < followers.Count; i++)
                    {
                        if (followers[i].Quality.ToInt32() > 3
                            && followers[i].Level >= 100)
                        {
                            GarrisonButler.Diagnostic("Skipping max level epic 100 follower: " + followers[i].Name);
                        }
                        else
                        {
                            GarrisonButler.Diagnostic("Using follower: " + followers[i].Name);
                            followersToConsider.Add(followers[i]);
                        }
                    }
                }
                else
                {
                    GarrisonButler.Diagnostic("Using full followers list");
                    followersToConsider = followers;
                }

                // For some reason this code isn't working properly
                //var followersToConsider =
                //    // If reward type is FollowerXP, discard all level 100 epic followers
                //    (
                //    (reward.Category == MissionReward.MissionRewardCategory.FollowerExperience) && !GaBSettings.Get().IncludeEpicMaxLevelFollowersForExperience
                //        ? followers.SkipWhile(f => (f.Quality.ToInt32() > 3) && (f.Level >= 100))
                //        : followers
                //    )
                //    .ToList();

                GarrisonButler.Diagnostic("Only considering {0} of {1} followers", followersToConsider.Count, followers.Count);
                followersToConsider.ForEach(f => GarrisonButler.Diagnostic(">> FollowerToConsider: " + f.Name));

                var followersToRemoveIfFailure = new List<Follower>();

                foreach (var mission in missionsThatMeetRequirement)
                {
                    if (followersToConsider.Count < mission.NumFollowers)
                    {
                        var shouldBreak = true;

                        if (!GaBSettings.Get().IncludeEpicMaxLevelFollowersForExperience
                            && reward.Category == MissionReward.MissionRewardCategory.FollowerExperience)
                        {
                            // Followers were excluded (followers.Count > followersToConsider.Count)
                            // but still a follower remaining in the queue that needs experience (followersToConsider.Count > 0)
                            if (followersToConsider.Count > 0
                                && followers.Count > followersToConsider.Count)
                            {
                                var reducedFollowerSet = followers.Except(followersToConsider).ToList();
                                if (!reducedFollowerSet.Any())
                                    break;
                                // Get only the followers that were excluded
                                //var reducedFollowerSet =
                                //    followers
                                //    .SkipWhile((f, i) => f.FollowerId == followersToConsider[i].FollowerId)
                                //    .ToList();
                                // Adding some followers back in would allow us to complete the mission
                                if ((followersToConsider.Count + reducedFollowerSet.Count) >= mission.NumFollowers)
                                {
                                    var amount = mission.NumFollowers - followersToConsider.Count;
                                    followersToRemoveIfFailure = reducedFollowerSet.Take(amount).ToList();

                                    if (followersToRemoveIfFailure.Count > 0)
                                    {
                                        GarrisonButler.Diagnostic("Using epic level 100 followers to help fill up mission slots:");
                                        followersToRemoveIfFailure.ForEach(f => GarrisonButler.Diagnostic(" -> " + f.Name));
                                        followersToConsider.AddRange(followersToRemoveIfFailure);
                                        shouldBreak = false;
                                    }
                                }
                            }
                        }

                        if (shouldBreak)
                        {
                            GarrisonButler.Diagnostic(
                                "Breaking mission loop due to followersToConsider < mission.NumFollowers");
                            break;
                        }
                    }
                    DateTime startedAt = DateTime.Now;
                    Combinations<Follower> followerCombinations = new Combinations<Follower>(followersToConsider, mission.NumFollowers);
                    MissionCalc.mission = mission;
                    var bestCombo = followerCombinations.FirstOrDefault();
                    var bestSuccess = 0.0d;
                    List<Tuple<IList<Follower>, double>> successChances = new List<Tuple<IList<Follower>, double>>();
                    foreach (var combo in followerCombinations)
                    {
                        MissionCalc.followers = combo.ToList();
                        var result = MissionCalc.CalculateSuccessChance();
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
                                GarrisonButler.Diagnostic("Best combo doesn't meet minimum success chance requirements!  Need {0}% and only have {1}%", sucChanceToCompareAgainst, Convert.ToInt32(bestSuccess));
                                bestCombo.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
                                followersToConsider.RemoveAll(f => followersToRemoveIfFailure.Any(fr => fr.FollowerId == f.FollowerId));
                                continue;
                            }
                        }
                        else
                        {
                            GarrisonButler.Diagnostic("Error retrieving success chance for best combo: bestCombo.Count={0}, successChances.Count={1}", bestCombo.GetEmptyIfNull().Count(), successChances.GetEmptyIfNull().Count());
                            followersToConsider.RemoveAll(f => followersToRemoveIfFailure.Any(fr => fr.FollowerId == f.FollowerId));
                            continue;
                        }
                        combosTried += followerCombinations.Count;
                        acceptedCombos++;
                        GarrisonButler.Diagnostic("Best Combination with success=" + bestSuccess + "% for Mission=" + mission.Name + " is ");
                        bestCombo.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
                        //successChances.ForEach(c =>
                        //{
                        //    totalSuccessChance += c.Item2;
                        //});
                        totalSuccessChance += bestSuccess;
                        toStart.Add(new Tuple<Mission, Follower[]>(mission, bestCombo.ToArray()));
                        GarrisonButler.Diagnostic("Followers before removal: " + followersToConsider.Count);
                        bestCombo.ForEach(c =>
                        {
                            followersToConsider.RemoveAll(f => f.FollowerId == c.FollowerId);
                            followers.RemoveAll(f => f.FollowerId == c.FollowerId);
                        });
                        GarrisonButler.Diagnostic("Followers after removal: " + followersToConsider.Count);
                        GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
                    } // if (bestCombo != null)
                } // foreach (var mission in missionsThatMeetRequirement)
            } // foreach (var reward in rewards)

            GarrisonButler.Diagnostic("Done with Mission Calculations");
            GarrisonButler.Diagnostic("Total combinations tried = " + combosTried);
            GarrisonButler.Diagnostic("Total combinations accepted = " + acceptedCombos);
            GarrisonButler.Diagnostic("Total missions = " + missions.Count);
            GarrisonButler.Diagnostic("Followers not assigned = " + followers.Count);
            GarrisonButler.Diagnostic("Average success chance = " + totalSuccessChance / (double)acceptedCombos + "%");
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
            if (false)
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

            return toStart;
        }


        private static async Task<Result> CanRunStartMission()
        {
            if (!GaBSettings.Get().StartMissions)
            {
                GarrisonButler.Diagnostic("[Missions] Deactivated in user settings.");
                return new Result(ActionResult.Failed);
            }

            var numberMissionAvailable = MissionLua.GetNumberAvailableMissions();

            // Is there mission available to start
            if (numberMissionAvailable == 0)
            {
                GarrisonButler.Diagnostic("[Missions] No missions available to start.");
                return new Result(ActionResult.Failed);
            }

            RefreshMissions();
            RefreshFollowers();


            var garrisonRessources = BuildingsLua.GetGarrisonRessources();
            IEnumerable<Mission> missions = _missions.Where(m => m.Cost < garrisonRessources).ToList();
            if (!missions.Any())
            {
                GarrisonButler.Diagnostic("[Missions] Not enough ressources to start a mission.");
                return new Result(ActionResult.Failed);
            }

            //var toStart = new List<Tuple<Mission, Follower[]>>();
            //var followersTemp = _followers.ToList();
            //foreach (var mission in missions)
            //{
            //    var match =
            //        mission.FindMatch(followersTemp.Where(f => f.IsCollected && f.Status == "nil").ToList());
            //    if (match == null)
            //        continue;
            //    toStart.Add(new Tuple<Mission, Follower[]>(mission, match));
            //    followersTemp.RemoveAll(match.Contains);
            //}

            var toStart = NewMissionStubCode(_followers.ToList());

            var mess = "Found " + numberMissionAvailable + " available missions to complete. " +
                       "Can successfully complete: " + toStart.Count + " missions.";
            if (toStart.Count > 0)
                GarrisonButler.Log(mess);
            else
            {
                GarrisonButler.Diagnostic(mess);
            }
            return !toStart.Any()
                ? new Result(ActionResult.Failed)
                : new Result(ActionResult.Running, toStart.First());
        }

        //public static async Task<Result> HandleTableAndMissionInterface()
        //{
            
        //}

        public static async Task<Result> StartMission(object obj)
        {
            var missionToStart = obj as Tuple<Mission, Follower[]>;
            if (missionToStart == null)
                return new Result(ActionResult.Failed);

            if (await MoveToTable())
                return new Result(ActionResult.Running);

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
                    return new Result(ActionResult.Running);
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
                    return new Result(ActionResult.Running);
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
                    return new Result(ActionResult.Running);
                }
            }
            GarrisonButler.Diagnostic("Adding followers to mission: " + missionToStart.Item1.Name);
            missionToStart.Item2.ForEach(f => GarrisonButler.Diagnostic(" -> " + f.Name));
            await missionToStart.Item1.AddFollowersToMission(missionToStart.Item2.ToList());
            InterfaceLua.StartMission(missionToStart.Item1);
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            InterfaceLua.ClickCloseMission();
            RefreshFollowers(true);
            RefreshMissions(true);
            return new Result(ActionResult.Refresh);
        }


        public static async Task<bool> MoveToTable()
        {
            var tableForLoc = default(WoWObject);
            if (_tablePosition == WoWPoint.Empty)
            {
                tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != default(WoWGameObject))
                {
                    GarrisonButler.Diagnostic("Found Command table location, not using default anymore.");
                    _tablePosition = tableForLoc.Location;
                }
            }

            if (_tablePosition != WoWPoint.Empty)
            {
                if (InterfaceLua.IsGarrisonMissionFrameOpen())
                    return false;

                tableForLoc = MissionLua.GetCommandTableOrDefault();
                if (tableForLoc != default(WoWGameObject))
                {
                    if ((await MoveToInteract(tableForLoc)).Status == ActionResult.Running)
                        return true;
                    if (tableForLoc.WithinInteractRange)
                    {
                        WoWMovement.MoveStop();
                        tableForLoc.Interact();
                        GarrisonButler.Diagnostic("[Missions] Interacting with mission table.");
                        return true;
                    }
                    GarrisonButler.Diagnostic("[Missions] Can't interaction with mission table, not in range!");
                    GarrisonButler.Diagnostic("[Missions] Table at: {0}", tableForLoc.Location);
                    GarrisonButler.Diagnostic("[Missions] Me at: {0}", Me.Location);
                }
                else
                {
                    if ((await MoveTo(_tablePosition, "[Missions] Moving to command table")).Status ==
                        ActionResult.Running)
                        return true;
                }
            }
            else
            {
                if (
                    (await MoveTo(Me.IsAlliance ? TableAlliance : TableHorde, "[Missions] Moving to command table"))
                        .Status ==
                    ActionResult.Running)
                    return true;
            }

            if (InterfaceLua.IsGarrisonMissionFrameOpen())
                return false;

            var table = MissionLua.GetCommandTableOrDefault();
            if (table == default(WoWObject))
            {
                GarrisonButler.Diagnostic("[Missions] Trouble getting command table from LUA.");
                return false;
            }

            try
            {
                table.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }
            catch (Exception e)
            {
                if (e is CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning(e.ToString());
            }
            return true;
        }

        private static async Task<Result> CanRunTurnInMissions()
        {
            var canRun = GaBSettings.Get().CompletedMissions && MissionLua.GetNumberCompletedMissions() != 0;
            return canRun
                ? new Result(ActionResult.Running)
                : new Result(ActionResult.Failed);
        }


        public static async Task<Result> DoTurnInCompletedMissions(object o)
        {
            GarrisonButler.Log("Found " + MissionLua.GetNumberCompletedMissions() + " completed missions to turn in.");
            // are we at the action table?
            if (await MoveToTable())
                return new Result(ActionResult.Running);

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
            _turnInMissionsTriggered = false;
            RefreshFollowers(true);
            RefreshMissions(true);
            return new Result(ActionResult.Refresh);
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
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
                new ActionHelpers.ActionOnTimer(DoTurnInCompletedMissions, CanRunTurnInMissions));

            //// StartMissions
            missionsActionsSequence.AddAction(
                new ActionHelpers.ActionOnTimer(StartMission, CanRunStartMission));

            GarrisonButler.Diagnostic("Initialization Missions coroutines done!");
            return missionsActionsSequence;
        }
    }
}
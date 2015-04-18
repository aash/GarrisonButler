#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Facet.Combinatorics;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.Libraries.Wowhead;
using GarrisonButler.Objects;
using Styx;
using Styx.Common;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    internal class StartMissions : Atom
    {
        private static List<Mission> _missions;
        private static List<Follower> _followers;

        private List<Tuple<Mission, Follower[]>> toStart;
        private static readonly WoWPoint TableHorde = new WoWPoint(5559, 4599, 140);
        private static readonly WoWPoint TableAlliance = new WoWPoint(1943, 330, 91);
        private static readonly List<uint> CommandTables = new List<uint>
        {
            81661,
            82507,
            82601,
            81546,
            80432,
            84224,
            86062,
            86031,
            84698,
            82600,
            81649,
            85805
        };


        public StartMissions()
        {
            Dependencies.Add(new MoveToObject(CommandTables, WoWObjectTypeFlag.Unit, StyxWoW.Me.IsAlliance ? TableAlliance : TableHorde));
            
            _followers = FollowersLua.GetAllFollowers();
            _missions = MissionLua.GetAllAvailableMissions();
            ShouldRepeat = true;
        }

        public override bool RequirementsMet()
        {
            if (!GaBSettings.Get().StartMissions)
            {
                GarrisonButler.Diagnostic("[Missions] Deactivated in user settings.");
                return false;
            }
            
            //RefreshMissions();
            //RefreshFollowers();

            _followers = FollowersLua.GetAllFollowers();
            _missions = MissionLua.GetAllAvailableMissions();

            var garrisonRessources = BuildingsLua.GetGarrisonRessources();
            IEnumerable<Mission> missions = _missions.Where(m => m.Cost < garrisonRessources).ToList();
            if (!missions.Any())
            {
                GarrisonButler.Diagnostic("[Missions] Not enough ressources to start a mission.");
                return false;
            }


            // Enhanced Mission Logic
            if (GarrisonButler.IsIceVersion())
            {
                toStart = NewMissionStubCode(_followers.ToList());
            }
            else
            {
                toStart = new List<Tuple<Mission, Follower[]>>();
                var followersTemp = _followers.ToList();
                foreach (var mission in missions)
                {
                    // Make sure that status is Idle or In Party
                    var match =
                        mission.FindMatch(followersTemp.Where(f => f.IsCollected && f.Status.ToInt32() <= 1).ToList());
                    if (match == null)
                        continue;
                    toStart.Add(new Tuple<Mission, Follower[]>(mission, match));
                    break; // let's not calculate all of them right now. 
                    followersTemp.RemoveAll(match.Contains);
                }
            }

            var mess = "Found available missions to complete. " +
                       "Can successfully complete: " + toStart.Count + " missions.";
            if (toStart.Count > 0)
                GarrisonButler.Log(mess);
            else
            {
                GarrisonButler.Diagnostic(mess);
            }
            return toStart.Any();
        }

        public override bool IsFulfilled()
        {

            var numberMissionAvailable = MissionLua.GetNumberAvailableMissions();

            // Is there mission available to start
            if (numberMissionAvailable == 0)
            {
                GarrisonButler.Diagnostic("[Missions] No missions available to start.");
                return true;
            }
            return false;
        }

        public override async Task Action()
        {
            if (toStart == null)
                return; 

            if (!InterfaceLua.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Diagnostic("Mission tab not visible, clicking.");
                InterfaceLua.ClickTabMission();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionTabVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionTab.");
                    return;
                }
            }

            var missionToStart = toStart.First();

            if (!InterfaceLua.IsGarrisonMissionVisible())
            {
                GarrisonButler.Diagnostic("Mission not visible, opening mission: " + missionToStart.Item1.MissionId +
                                          " - " +
                                          missionToStart.Item1.Name);
                InterfaceLua.OpenMission(missionToStart.Item1);
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonMissionVisible))
                {
                    GarrisonButler.Warning("Couldn't display GarrisonMissionFrame.");
                    return;
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
                    return;
                }
            }
            GarrisonButler.Diagnostic("Adding followers to mission: " + missionToStart.Item1.Name);
            missionToStart.Item2.ForEach(f => GarrisonButler.Diagnostic(" -> " + f.Name));
            await missionToStart.Item1.AddFollowersToMission(missionToStart.Item2.ToList());
            InterfaceLua.StartMission(missionToStart.Item1);
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            InterfaceLua.ClickCloseMission();

            _followers = FollowersLua.GetAllFollowers();
            _missions = MissionLua.GetAllAvailableMissions();

        }

        public override string Name()
        {
            return "[StartMissions]";
        }

        public static bool IsMissionDisallowed(Mission mission)
        {
            var disallowedRewardSettings =
                GaBSettings.Get().MissionRewardSettings.Where(mrs => mrs.DisallowMissionsWithThisReward);
            var disallowed = disallowedRewardSettings.Any(drs =>
                drs.IsCategoryReward
                    ? mission.Rewards.Any(r => r.Category == drs.Category)
                    : mission.Rewards.Any(r => r.Id == drs.Id));

            if (!disallowed)
            {
                var missionRewardWithGarrisonResources
                    = mission.Rewards.FirstOrDefault(r => r.IsGarrisonResources);

                if (missionRewardWithGarrisonResources != null)
                {
                    var garrsionResourcesCurrency = WoWCurrency.GetCurrencyById(824);

                    if (garrsionResourcesCurrency != null)
                    {
                        var cap = garrsionResourcesCurrency.TotalMax;
                        var missionAmt = missionRewardWithGarrisonResources.Quantity;
                        var playerAmt = garrsionResourcesCurrency.Amount;

                        disallowed = (playerAmt + missionAmt) > cap;

                        GarrisonButler.Diagnostic(
                            "[Missions] Garrison Resources = {0} for mission ({1}) {2}.  Player has {3} Garrison Resources.  Total max is {4}.  {5} + {6} > {7}?  {8}",
                            missionAmt, mission.MissionId, mission.Name, playerAmt, cap, missionAmt, playerAmt, cap,
                            disallowed);
                    }
                }
            }

            GarrisonButler.Diagnostic(
                disallowed
                    ? "[Missions] Mission is DISALLOWED id: {0} name: {1}"
                    : "[Missions] Mission is enabled id: {0} name: {1}", mission.MissionId, mission.Name);

            return disallowed;
        }

        public static List<Tuple<Mission, Follower[]>> NewMissionStubCode(List<Follower> followers)
        {
            var allAvailableMissions = MissionLua.GetAllAvailableMissions();
            allAvailableMissions.ForEach(m => IsMissionDisallowed(m));
            GarrisonButler.Diagnostic("[Missions] ------------");
            var missions = allAvailableMissions
                .GetEmptyIfNull()
                .SkipWhile(IsMissionDisallowed)
                .ToList();
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

            // Status 5 is INACTIVE
            // Make sure there's at least 1 follower
            // and make sure the number of active followers is less than 20
            if (numFollowers == 0 ||
                followers.Count(f => f.Status.ToInt32() != 5) <=
                (ButlerCoroutine._buildings.Any(b => b.Id == (int) Buildings.BarracksLvl3) ? 25 : 20))
            {
                GarrisonButler.Diagnostic("returning from GetAllFollowers(): # followers = {0}", followers.Count);
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

            // Filter rewards based on user settings
            if (GaBSettings.Get().DisallowRushOrderRewardIfBuildingDoesntExist)
            {
                rewards.RemoveAll(r => r.IsRushOrder && !r.RushOrderBuildingExists);
            }

            // Make sure player meets minimum item level requirements
            foreach (var r in rewards)
            {
                if (StyxWoW.Me.AverageItemLevelEquipped < r.MinimumCharacterItemLevel)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Removing reward ({0}) {1} due to character item level {2} not meeting minimum {3} requirement for this reward.",
                        r.Id, r.Name, StyxWoW.Me.AverageItemLevelEquipped, r.MinimumCharacterItemLevel);
                    rewards.Remove(r);
                    continue;
                }

                var category = rewards.FirstOrDefault(s => s.IsCategoryReward && s.Category == r.Category);

                if (category == null)
                    continue;

                if (StyxWoW.Me.AverageItemLevelEquipped < category.MinimumCharacterItemLevel)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Removing reward ({0}) {1} due to character item level {2} not meeting minimum {3} requirement for this reward's category ({4}).",
                        r.Id, r.Name, StyxWoW.Me.AverageItemLevelEquipped, r.MinimumCharacterItemLevel, r.Category);
                    rewards.Remove(r);
                }
            }

            // Get the current top of the list reward
            foreach (var reward in rewards)
            {
                if (StyxWoW.Me.Level < reward.RequiredPlayerLevel)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Player level {0} does not meet minimum {1} for this reward: ({2}) {3}",
                        StyxWoW.Me.Level, reward.RequiredPlayerLevel, reward.Id, reward.Name);
                }

                if (followers.Count <= 0)
                {
                    GarrisonButler.Diagnostic("[Missions] Breaking reward loop due to followers.Count = 0");
                    break;
                }

                GarrisonButler.Diagnostic("-- Reward: {0} --", reward.Name);

                // Discard any missions where the reward is set to "disallowed"
                if (reward.DisallowMissionsWithThisReward)
                {
                    missions.RemoveAll(m => m.Rewards.Any(mr => mr.Id == reward.Id));
                    continue;
                }

                // Remove all missions that are disallowed (perhaps by Garrison Resource limit)
                missions.RemoveAll(IsMissionDisallowed);

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
                    GarrisonButler.Diagnostic("  Slots: {0}", m.NumFollowers);
                    m.Rewards.ForEach(r => GarrisonButler.Diagnostic("    ** Reward ({0}): {1} **", r.Quantity, r.Name));
                });

                // Skip any missions that don't meet user requirements (quantity > X for example)
                var missionsThatMeetRequirement = missionsWithCurrentReward
                    // Make sure the mission level meets minimum required by global settings OR reward settings
                    .Where(
                        m =>
                            reward.IndividualMissionLevelEnabled
                                ? m.Level >= reward.RequiredMissionLevel
                                : m.Level >= GaBSettings.Get().MinimumMissionLevel)
                    .Where(m => m.Rewards
                        .Where(mr => mr.Id == reward.Id
                            // Handle the case where we are looking at a category of rewards instead of individual rewards
                                     ||
                                     (mr.Id != reward.Id && mr.Category == reward.Category && mr.Id == (int) mr.Category))
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
                    && !GaBSettings.Get().AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers)
                {
                    GarrisonButler.Diagnostic("Going to use reduced follower list");
                    followersToConsider = new List<Follower>();
                    for (var i = 0; i < followers.Count; i++)
                    {
                        if (followers[i].IsMaxLevelEpic)
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

                followersToConsider.RemoveAll(f => f.ItemLevel < reward.MinimumFollowerItemLevel);

                // For some reason this code isn't working properly
                //var followersToConsider =
                //    // If reward type is FollowerXP, discard all level 100 epic followers
                //    (
                //    (reward.Category == MissionReward.MissionRewardCategory.FollowerExperience) && !GaBSettings.Get().IncludeEpicMaxLevelFollowersForExperience
                //        ? followers.SkipWhile(f => (f.Quality.ToInt32() > 3) && (f.Level >= 100))
                //        : followers
                //    )
                //    .ToList();

                GarrisonButler.Diagnostic("Only considering {0} of {1} followers", followersToConsider.Count,
                    followers.Count);
                followersToConsider.ForEach(f => GarrisonButler.Diagnostic(">> FollowerToConsider: " + f.Name));

                var result = DoMissionCalc(followersToConsider, missionsThatMeetRequirement, reward, followers);
                var returnedToStart = result.Item1;
                var returnedCombosTried = result.Item2;
                var returnedAcceptedCombos = result.Item3;
                var returnedTotalSuccessChance = result.Item4;

                // No combos were found
                // Attempt boosting (re-run with all followers considered)
                if (returnedToStart != null && returnedToStart.Count <= 0)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] >>> No combos found normally, attempting with Epic Max Level Boost <<<");
                    result = DoMissionCalc(followers, missionsThatMeetRequirement, reward, followers);
                    returnedToStart = result.Item1;
                    returnedCombosTried = result.Item2;
                    returnedAcceptedCombos = result.Item3;
                    returnedTotalSuccessChance = result.Item4;
                }

                if (returnedToStart != null && returnedToStart.Count > 0)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Able to start {0} missions. (unassigned followers = {1}, tried {2} combos, accepted {3} combos, avg success = {4}%",
                        returnedToStart.Count, followersToConsider.Count, returnedCombosTried, returnedAcceptedCombos,
                        returnedAcceptedCombos > 0
                            ? (returnedAcceptedCombos/(double) returnedAcceptedCombos).ToString()
                            : "0");
                    toStart.AddRange(returnedToStart);
                    combosTried += returnedCombosTried;
                    acceptedCombos += returnedAcceptedCombos;
                    totalSuccessChance += returnedTotalSuccessChance;
                }
            } // foreach (var reward in rewards)

            GarrisonButler.Diagnostic("Done with Mission Calculations");
            GarrisonButler.Diagnostic("Total combinations tried = " + combosTried);
            GarrisonButler.Diagnostic("Total combinations accepted = " + acceptedCombos);
            GarrisonButler.Diagnostic("Total missions = " + missions.Count);
            GarrisonButler.Diagnostic("Followers not assigned = " + followers.Count);
            GarrisonButler.Diagnostic("Average success chance = " +
                                      (acceptedCombos > 0 ? (totalSuccessChance/acceptedCombos) : 0d) + "%");
            GarrisonButler.DiagnosticLogTimeTaken("Mission Stub Code", missionCodeStartedAt);

            return toStart;
        }

        public static List<Follower> FillFollowersWithEpicLevel100(List<Follower> followersToConsider,
            List<Follower> followers, Mission mission)
        {
            var followersToRemoveIfFailure = new List<Follower>();
            // Followers were excluded (followers.Count > followersToConsider.Count)
            // but still a follower remaining in the queue that needs experience (followersToConsider.Count > 0)
            if (followersToConsider.Count > 0
                && followers.Count > 0
                && followers.Count > followersToConsider.Count
                && GaBSettings.Get().UseEpicMaxLevelFollowersToBoostLowerFollowers
                && GaBSettings.Get().MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting > 0)
            {
                var reducedFollowerSet = followers
                    // Gets only the followers that were originally excluded
                    .Except(followersToConsider)
                    // Reduces the number of epic followers by user settings
                    .Take(GaBSettings.Get().MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting)
                    .ToList();
                if (!reducedFollowerSet.Any())
                    return followersToRemoveIfFailure;
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
                    }
                }
            }

            return followersToRemoveIfFailure;
        }

        public static void RemoveAddedMaxLevelFollowers(List<Follower> followersToConsider,
            List<Follower> followersToRemoveIfFailure)
        {
            if (followersToRemoveIfFailure.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Missions] Can't RemoveAddedMaxLevelFollowers: there's none to remove");
                return;
            }
            GarrisonButler.Diagnostic("[Missions] Removing added followers: ");
            followersToRemoveIfFailure.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
            followersToConsider.RemoveAll(f => followersToRemoveIfFailure.Any(fr => fr.FollowerId == f.FollowerId));
            GarrisonButler.Diagnostic("[Missions] Followers after removing: {0}", followersToConsider.Count);
        }

        public static void AddExcludedFollowers(List<Follower> followersToConsider,
            List<Follower> excludedFollowers)
        {
            if (excludedFollowers.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Missions] Can't AddExcludedFollowers: there's none to add");
                return;
            }
            GarrisonButler.Diagnostic("[Missions] Re-adding {0} excluded followers: ", excludedFollowers.Count);
            excludedFollowers.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
            followersToConsider.AddRange(excludedFollowers);
            GarrisonButler.Diagnostic("[Missions] Followers after re-adding: {0}", followersToConsider.Count);
        }

        // Returns list mission/followers combos with combosTried, acceptedCombos, totalSuccessChance
        public static Tuple<List<Tuple<Mission, Follower[]>>, long, int, double> DoMissionCalc(
            List<Follower> followersToConsider, List<Mission> missionsThatMeetRequirement, MissionReward reward,
            List<Follower> followers)
        {
            long combosTried = 0;
            var acceptedCombos = 0;
            double totalSuccessChance = 0;
            var toStart = new List<Tuple<Mission, Follower[]>>();
            var followersToRemoveIfFailure = new List<Follower>();
            var excludedFollowers = new List<Follower>();

            foreach (var mission in missionsThatMeetRequirement)
            {
                GarrisonButler.Diagnostic("************* BEGIN Mission=" + mission.Name + "**************");
                if (followersToConsider.Count < mission.NumFollowers)
                {
                    var shouldBreak = true;

                    // Attempt to "fill up" slots if they didn't enable AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers
                    if (!GaBSettings.Get().AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers
                        && reward.Category == MissionReward.MissionRewardCategory.FollowerExperience)
                    {
                        followersToRemoveIfFailure = FillFollowersWithEpicLevel100(followersToConsider, followers,
                            mission);
                        if (followersToRemoveIfFailure.Count > 0)
                        {
                            shouldBreak = false;
                        }
                        // Followers were excluded (followers.Count > followersToConsider.Count)
                        // but still a follower remaining in the queue that needs experience (followersToConsider.Count > 0)
                        //if (followersToConsider.Count > 0
                        //    && followers.Count > followersToConsider.Count)
                        //{
                        //    var reducedFollowerSet = followers.Except(followersToConsider).ToList();
                        //    if (!reducedFollowerSet.Any())
                        //        continue;
                        //    // Get only the followers that were excluded
                        //    //var reducedFollowerSet =
                        //    //    followers
                        //    //    .SkipWhile((f, i) => f.FollowerId == followersToConsider[i].FollowerId)
                        //    //    .ToList();
                        //    // Adding some followers back in would allow us to complete the mission
                        //    if ((followersToConsider.Count + reducedFollowerSet.Count) >= mission.NumFollowers)
                        //    {
                        //        var amount = mission.NumFollowers - followersToConsider.Count;
                        //        followersToRemoveIfFailure = reducedFollowerSet.Take(amount).ToList();

                        //        if (followersToRemoveIfFailure.Count > 0)
                        //        {
                        //            GarrisonButler.Diagnostic("Using epic level 100 followers to help fill up mission slots:");
                        //            followersToRemoveIfFailure.ForEach(f => GarrisonButler.Diagnostic(" -> " + f.Name));
                        //            followersToConsider.AddRange(followersToRemoveIfFailure);
                        //            shouldBreak = false;
                        //        }
                        //    }
                        //}
                    }

                    if (shouldBreak)
                    {
                        GarrisonButler.Diagnostic(
                            "Breaking mission loop due to followersToConsider < mission.NumFollowers");
                        continue;
                    }
                }

                // Abort if we don't have enough Garrison Resources
                var gr = WoWCurrency.GetCurrencyById(824).Amount;
                var mingr = GaBSettings.Get().MinimumGarrisonResourcesToStartMissions;
                if (gr < mingr)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Breaking MissionCalc due to Minimum Required Garrison Resources.  Have {0} and need {1}.",
                        gr, mingr);
                }

                if (mission.Cost > gr)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Breaking MissionCalc due to insufficient Garrison Resources to start mission ({0}) {1}.  Have {2} and need {3}.",
                        mission.MissionId, mission.Name, gr, mission.Cost);
                    break;
                }

                // Garrison Resources
                if (mission.Rewards.Any(r => r.IsGarrisonResources))
                {
                    if (GaBSettings.Get().PreferFollowersWithScavengerForGarrisonResourcesReward)
                    {
                        GarrisonButler.Diagnostic(
                            "[Missions] Mission reward is GARRISON RESOURCES and user settings indicate to prefer followers with Scavenger trait.  {0} followers have Scavenger",
                            followersToConsider.Count(f => f.HasScavenger));
                        followersToConsider = followersToConsider.OrderByDescending(f => f.HasScavenger).ToList();
                    }
                }
                // NOT Garrison Resources & user wants to disallow followers with Scavenger on these missions
                else if (GaBSettings.Get().DisallowScavengerOnNonGarrisonResourcesMissions)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Mission reward is ***NOT*** GARRISON RESOURCES and user settings indicate to NOT allow followers with Scavenger.  {0} followers have Scavenger",
                        followersToConsider.Count(f => f.HasScavenger));
                    excludedFollowers = followersToConsider.Where(f => f.HasScavenger).ToList();
                    followersToConsider.RemoveAll(f => excludedFollowers.Any(e => e.FollowerId == f.FollowerId));
                }

                // Gold
                if (mission.Rewards.Any(r => r.IsGold))
                {
                    if (GaBSettings.Get().PreferFollowersWithTreasureHunterForGoldReward)
                    {
                        GarrisonButler.Diagnostic(
                            "[Missions] Mission reward is GOLD and user settings indicate to prefer followers with Treasure Hunter trait.  {0} followers have Treasure Hunter",
                            followersToConsider.Count(f => f.HasTreasureHunter));
                        followersToConsider = followersToConsider.OrderByDescending(f => f.HasTreasureHunter).ToList();
                    }
                }
                // NOTGold & user wants to disallow followers with Treasure Hunter on these missions
                else if (GaBSettings.Get().DisallowTreasureHunterOnNonGoldMissions)
                {
                    GarrisonButler.Diagnostic(
                        "[Missions] Mission reward is ***NOT*** GOLD and user settings indicate to NOT allow followers with Treasure Hunter.  {0} followers have Treasure Hunter",
                        followersToConsider.Count(f => f.HasTreasureHunter));
                    excludedFollowers = followersToConsider.Where(f => f.HasTreasureHunter).ToList();
                    followersToConsider.RemoveAll(f => excludedFollowers.Any(e => e.FollowerId == f.FollowerId));
                }

                DateTime startedAt = DateTime.Now;
                Combinations<Follower> followerCombinations = new Combinations<Follower>(followersToConsider,
                    mission.NumFollowers);
                MissionCalc.mission = mission;
                var bestCombo = Enumerable.Empty<Follower>();
                var bestSuccess = 0.0d;
                List<Tuple<IList<Follower>, double>> successChances = new List<Tuple<IList<Follower>, double>>();
                foreach (var combo in followerCombinations)
                {
                    var allMaxLevelEpic = combo.All(c => c.IsMaxLevelEpic);
                    var isFollowerExperienceCategory = reward.Category ==
                                                       MissionReward.MissionRewardCategory.FollowerExperience;
                    if (!GaBSettings.Get().AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers
                        && isFollowerExperienceCategory
                        && allMaxLevelEpic)
                    {
                        // Don't fill all slots with epic max level followers
                        continue;
                    }
                    // Restrict combinations based on user settings
                    // Only for FollowerExperience
                    if (GaBSettings.Get().UseEpicMaxLevelFollowersToBoostLowerFollowers
                        && reward.Category == MissionReward.MissionRewardCategory.FollowerExperience)
                    {
                        // Skip any combinations where # of epic followers is greater than allowed
                        var maxEpicFollowers = GaBSettings.Get().MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting;
                        maxEpicFollowers = maxEpicFollowers < 0 ? 0 : maxEpicFollowers;
                        if (combo.Count(c => c.IsMaxLevelEpic) > maxEpicFollowers)
                            continue;
                    }
                    MissionCalc.followers = combo.ToList();
                    var result = MissionCalc.CalculateSuccessChance();
                    successChances.Add(new Tuple<IList<Follower>, double>(combo, result.Item1));
                }

                if (successChances != null && successChances.Count > 0)
                {
                    GarrisonButler.Diagnostic("Total combinations tried = " + followerCombinations.Count);
                    GarrisonButler.DiagnosticLogTimeTaken("Trying all " + followerCombinations.Count + " combinations",
                        startedAt);
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
                            GarrisonButler.Diagnostic(
                                "Best combo doesn't meet minimum success chance requirements!  Need {0}% and only have {1}%",
                                sucChanceToCompareAgainst, Convert.ToInt32(bestSuccess));
                            bestCombo.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
                            RemoveAddedMaxLevelFollowers(followersToConsider, followersToRemoveIfFailure);
                            AddExcludedFollowers(followersToConsider, excludedFollowers);
                            GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
                            continue;
                        }
                    }
                    else
                    {
                        GarrisonButler.Diagnostic(
                            "Error retrieving success chance for best combo: bestCombo.Count={0}, successChances.Count={1}",
                            bestCombo.GetEmptyIfNull().Count(), successChances.GetEmptyIfNull().Count());
                        RemoveAddedMaxLevelFollowers(followersToConsider, followersToRemoveIfFailure);
                        AddExcludedFollowers(followersToConsider, excludedFollowers);
                        GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
                        continue;
                    }

                    combosTried += (int) followerCombinations.Count;

                    if (bestCombo.IsNullOrEmpty())
                    {
                        GarrisonButler.Diagnostic("[Missions] No best combo found for mission {0}", mission.Name);
                        RemoveAddedMaxLevelFollowers(followersToConsider, followersToRemoveIfFailure);
                        AddExcludedFollowers(followersToConsider, excludedFollowers);
                        GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
                        continue;
                    }
                    acceptedCombos++;
                    GarrisonButler.Diagnostic("[Missions] Best Combination with success=" + bestSuccess +
                                              "% for Mission=" + mission.Name + " is ");
                    bestCombo.ForEach(c => GarrisonButler.Diagnostic(" -> Follower: " + c.Name));
                    //successChances.ForEach(c =>
                    //{
                    //    totalSuccessChance += c.Item2;
                    //});
                    totalSuccessChance += bestSuccess;
                    toStart.Add(new Tuple<Mission, Follower[]>(mission, bestCombo.ToArray()));
                    GarrisonButler.Diagnostic("[Missions] Followers before removal: " + followersToConsider.Count);
                    bestCombo.ForEach(c =>
                    {
                        followersToConsider.RemoveAll(f => f.FollowerId == c.FollowerId);
                        followers.RemoveAll(f => f.FollowerId == c.FollowerId);
                    });
                    GarrisonButler.Diagnostic("Followers after removal: " + followersToConsider.Count);
                    AddExcludedFollowers(followersToConsider, excludedFollowers);
                } // if (bestCombo != null)

                GarrisonButler.Diagnostic("************* END Mission=" + mission.Name + "**************");
            } // if (successChances != null && successChances.Any())

            return new Tuple<List<Tuple<Mission, Follower[]>>, long, int, double>(toStart, combosTried, acceptedCombos,
                totalSuccessChance);
        }
    }
}
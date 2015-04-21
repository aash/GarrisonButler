#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.DB;
using Styx.WoWInternals.Garrison;
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

        public static void InitializeMissions()
        {
            RefreshMissions(true);
            RefreshFollowers(true);
        }

        public static void RefreshMissions(bool forced = false)
        {
            if (!RefreshMissionsTimer.IsFinished && _missions_old != null && !forced) return;
            GarrisonButler.Log("Refreshing Missions database.");
            _missions_old = MissionLua.GetAllAvailableMissions();
            RefreshMissionsTimer.Reset();
        }

        public static void RefreshFollowers(bool forced = false)
        {
            if (!RefreshFollowerTimer.IsFinished && _followers_old != null && !forced) return;
            GarrisonButler.Log("Refreshing Followers database.");
            _followers_old = FollowersLua.GetAllFollowers();
            RefreshFollowerTimer.Reset();
        }

        private static async Task<Result> CanRunPutGearOnFollower()
        {
            // Check addon FollowerGearOptimizer
            const int maxItemLevel = 675;
            const int minItemLevel = 600;
            RefreshMissions();
            RefreshFollowers();

            var rewardSettings = GaBSettings.Get().MissionRewardSettings;

            var allRewardSettingsWithCategory = rewardSettings
                .Where(f => f.Category == MissionReward.MissionRewardCategory.FollowerGear
                || f.Category == MissionReward.MissionRewardCategory.FollowerItem)
                .ToList();

            if (allRewardSettingsWithCategory.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Followers] No follower items in user settings.");
                return new Result(ActionResult.Failed);
            }

            var categorySettings = allRewardSettingsWithCategory.SkipWhile(f => !f.IsCategoryReward).ToList();

            var followerRewardSettings = allRewardSettingsWithCategory
                .SkipWhile(
                    f =>
                        // Make sure this item isn't a category setting
                        !f.IsCategoryReward &&
                        // Make sure category settings exist for this item
                        categorySettings.Any(c => c.Category == f.Category)
                        // Check category setting is NOT UseOnFollowers
                        ? !categorySettings.Any(
                            r =>
                                r.IsCategoryReward && r.Category == f.Category &&
                                r.Action != MissionReward.MissionRewardAction.UseOnFollowers)
                        // Check individual setting is NOT UseOnFollowers
                        : f.Action != MissionReward.MissionRewardAction.UseOnFollowers)
                .SkipWhile(f => f.IsCategoryReward)
                .ToList();

            if (followerRewardSettings.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Followers] No follower tokens in user settings.");
                return new Result(ActionResult.Failed);
            }

            // Any items in bags?
            var tokensAvailable = followerRewardSettings
                // Transform to array of WoWItem objects in bag
                // Only keeps first entry found in bags for each item
                .Select(f => HbApi.GetItemInBags((uint) f.Id).FirstOrDefault())
                // Start with highest level items (to assign to lowest quality followers)
                .OrderByDescending(f => f.ItemInfo.Level)
                .ToList();

            if (tokensAvailable.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Followers] No tokens available in user inventory.");
                return new Result(ActionResult.Failed);
            }

            // Any actual followers available?
            var numberFollowersAvailable = _followers_old
                // Only followers In Party (1) or Doing Nothing (nil = 0)
                // Only Max Level Epic followers
                // Only Followers NOT at max item level
                .Where(f => f.ItemLevel < maxItemLevel || !f.IsMaxLevelEpic || f.Status.ToInt32() < 2)
                // Prioritize lower ilvl followers
                .OrderBy(f => f.ItemLevel)
                .ToList();

            if (numberFollowersAvailable.Count <= 0)
            {
                GarrisonButler.Diagnostic("[Followers] No followers eligible to use tokens.");
                return new Result(ActionResult.Failed);
            }

            var armorSetsInBags = tokensAvailable
                .Where(f => Enum.IsDefined(typeof (ArmorSetItemIds), f.Entry))
                .ToList();

            var armorUpgradesInBags = tokensAvailable
                .Where(f => Enum.IsDefined(typeof(ArmorUpgradeItemIds), f.Entry))
                .ToList();

            var weaponSetsInBags = tokensAvailable
                .Where(f => Enum.IsDefined(typeof(WeaponSetItemIds), f.Entry))
                .ToList();

            var weaponUpgradesInBags = tokensAvailable
                .Where(f => Enum.IsDefined(typeof(WeaponUpgradeItemIds), f.Entry))
                .ToList();

            return new Result(ActionResult.Running,
                new Tuple<WoWItem, Follower>(tokensAvailable.FirstOrDefault(), numberFollowersAvailable.FirstOrDefault()));
        }

        public enum ArmorSetItemIds
        {
            WarRavagedArmorSet = 114807,
            BlackRockArmorSet = 114806,
            GoredrenchedArmorSet = 114746
        }

        public enum ArmorUpgradeItemIds
        {
            BracedArmorEnhancement = 114745,
            FortifiedArmorEnhancement = 114808,
            HeavilyReinforcedArmorEnhancement = 114822
        }

        public enum WeaponSetItemIds
        {
            WarRavagedWeaponry = 114616,
            BlackrockWeaponry = 114081,
            GoredrenchedWeaponry = 114622
        }

        public enum WeaponUpgradeItemIds
        {
            BalancedWeaponEnhancement = 114128,
            StrikingWeaponEnhancement = 114129,
            PowerOverrunWeaponEnhancement = 114131
        }

        public static async Task<Result> PutGearOnFollower(object obj)
        {
            var inventoryAndFollower = obj as Tuple<WoWItem, Follower>;

            if(inventoryAndFollower == null)
            {
                GarrisonButler.Diagnostic("[Followers] Passed in obj is null.");
                return new Result(ActionResult.Failed);
            }

            var token = inventoryAndFollower.Item1;
            var follower = inventoryAndFollower.Item2;

            if (token == null)
            {
                GarrisonButler.Diagnostic("[Followers] Token is null.");
                return new Result(ActionResult.Failed);
            }

            if (follower == null)
            {
                GarrisonButler.Diagnostic("[Followers] Follower is null.");
                return new Result(ActionResult.Failed);
            }

            if (await MoveToTable())
                return new Result(ActionResult.Running);

            if (!InterfaceLua.IsGarrisonFollowersTabVisible())
            {
                GarrisonButler.Diagnostic("[Followers] Followers tab not visible, clicking.");
                InterfaceLua.ClickTabFollowers();
                if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonFollowersTabVisible))
                {
                    GarrisonButler.Warning("[Followers] Couldn't display GarrisonFollowerTab.");
                    return new Result(ActionResult.Running);
                }
            }

            //if (!InterfaceLua.IsGarrisonFollowerVisible())
            //{
            //    GarrisonButler.Diagnostic("Follower not visible, opening follower: "
            //                              + follower.FollowerId + " (" + follower.Name + ")");
            //    InterfaceLua.OpenFollower(follower);
            //    if (!await Buddy.Coroutines.Coroutine.Wait(2000, InterfaceLua.IsGarrisonFollowerVisible))
            //    {
            //        GarrisonButler.Warning("Couldn't display GarrisonFollowerFrame.");
            //        return new Result(ActionResult.Running);
            //    }
            //}
            //else if (!InterfaceLua.IsGarrisonFollowerVisibleAndValid(follower.FollowerId))
            //{
            //    GarrisonButler.Diagnostic("Follower not visible or not valid, close and then opening follower: " +
            //                              follower.FollowerId + " - " + follower.Name);
            //    //InterfaceLua.ClickCloseFollower();
            //    InterfaceLua.OpenFollower(follower);
            //    if (
            //        !await
            //            Buddy.Coroutines.Coroutine.Wait(2000,
            //                () => InterfaceLua.IsGarrisonFollowerVisibleAndValid(follower.FollowerId)))
            //    {
            //        GarrisonButler.Warning("Couldn't display GarrisonFollowerFrame or wrong follower opened.");
            //        return new Result(ActionResult.Running);
            //    }
            //}

            GarrisonButler.Diagnostic("Adding item {0} ({1}) to follower {2} ({3}) with item level {4}", token.Entry, token.Name, follower.FollowerId, follower.Name, follower.ItemLevel);
            await InterfaceLua.UseItemOnFollower(token, follower.UniqueId);
            await CommonCoroutines.SleepForRandomUiInteractionTime();

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

                    if (!tableForLoc.WithinInteractRange)
                    {
                        Navigator.MoveTo(tableForLoc.Location);
                        return true; 
                    }

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
                    Navigator.MoveTo(_tablePosition);
                    return true;
                }
            }
            else
            {
                Navigator.MoveTo(Me.IsAlliance ? TableAlliance : TableHorde);
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


        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
        }
    }
}
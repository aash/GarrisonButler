using Bots.DungeonBuddy.Helpers;
using Bots.Professionbuddy.Dynamic;
using GarrisonButler.API;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals.DB;

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using GarrisonButler.Config;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

// ReSharper disable once CheckNamespace

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static Composite _deathBehavior;
        private static Composite _lootBehavior;
        private static Composite _combatBehavior;
        private static Composite _vendorBehavior;
        private static readonly WaitTimer ResetAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
        private static List<Building> _buildings;
        private static List<Mission> _missions;
        private static List<Follower> _followers;

        private static readonly List<WoWPoint> AllyWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(1866.069, 230.9416, 76.63979), //level 2
            new WoWPoint(1866.069, 230.9416, 76.63979) //level 3
        };

        private static readonly List<WoWPoint> HordeWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(5590.288, 4568.919, 136.1698), //level 2
            new WoWPoint(5585.125, 4565.036, 135.9761), //level 3
        };

        private static readonly WoWPoint AllyMailbox = new WoWPoint(1927.694, 294.151, 88.96585);
        private static readonly WoWPoint HordeMailbox = new WoWPoint(5580.682, 4570.392, 136.558);

        public static DateTime NextCheck = DateTime.Now;

        internal static readonly uint GarrisonHearthstone = 110560;
        private static readonly WoWPoint FishingSpotAlly = new WoWPoint(2021.108, 187.5952, 84.55713);
        private static readonly WoWPoint FishingSpotHorde = new WoWPoint(5482.597, 4637.465, 136.1296);

        public static bool ReadyToSwitch = false;

        private static bool _restoreCompletedMission;
        private static ActionHelpers.ActionsSequence _mainSequence;
        private static Stopwatch _testStopwatch = new Stopwatch();

        #region MixedModeTimers

        private static bool _dailiesTriggered;
        private static WaitTimer _dailiesWaitTimer;
        private const int DailiesWaitTimerValue = 30;

        private static WaitTimer _cacheWaitTimer;
        private static bool _cacheTriggered;
        private const int CacheWaitTimerValue = 30;


        private static WaitTimer _startOrderWaitTimer;
        private static bool _startOrderTriggered;
        private const int StartOrderWaitTimerValue = 30;


        private static WaitTimer _gardenWaitTimer;
        private const int GardenWaitTimerValue = 30;
        private static bool _gardenTriggered;


        private static WaitTimer _mineWaitTimer;
        private const int MineWaitTimerValue = 30;
        private static bool _mineTriggered;


        private static WaitTimer _turnInMissionWaitTimer;
        private static bool _turnInMissionsTriggered;
        private const int TurnInMissionWaitTimerValue = 30;

        private static WaitTimer _startMissionWaitTimer;
        private static bool _startMissionTriggered;
        private const int StartMissionWaitTimerValue = 30;

        private static WaitTimer _salvageWaitTimer;
        private static bool _salvageTriggered;
        private const int SalvageWaitTimerValue = 30;

        private const int LastRoundWaitTimerValue = 30;
        private static bool _lastRoundTriggered;
        private static WaitTimer _lastRoundWaitTimer;

        #endregion

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        private static Composite LootBehavior
        {
            get { return _lootBehavior ?? (_lootBehavior = LevelBot.CreateLootBehavior()); }
        }

        private static Composite DeathBehavior
        {
            get { return _deathBehavior ?? (_deathBehavior = LevelBot.CreateDeathBehavior()); }
        }

        private static Composite CombatBehavior
        {
            get { return _combatBehavior ?? (_combatBehavior = LevelBot.CreateCombatBehavior()); }
        }

        private static Composite VendorBehavior
        {
            get { return _vendorBehavior ?? (_vendorBehavior = LevelBot.CreateVendorBehavior()); }
        }

        internal static void OnStart()
        {
            try
            {
                InitializeShipments();
                GarrisonButler.Diagnostic("InitializeShipments");
                InitializeMissions();
                GarrisonButler.Diagnostic("InitializeMissions");
                InitializationMove();
                GarrisonButler.Diagnostic("InitializationMove");

                _mainSequence = new ActionHelpers.ActionsSequence();
                if(GarrisonButler.NameStatic.ToLower().Contains("ice")) 
                    _mainSequence.AddAction(new ActionHelpers.ActionOnTimer<int>(GetMails, HasMails));
                _mainSequence.AddAction(new ActionHelpers.ActionOnTimer<WoWItem>(UseItemInbags, ShouldTPToGarrison, 10000, 1000));
                _mainSequence.AddAction(new ActionHelpers.ActionOnTimer<DailyProfession>(DoDailyCd, CanRunDailies, 15000, 1000));
                _mainSequence.AddAction(InitializeBuildingsCoroutines());
                _mainSequence.AddAction(InitializeMissionsCoroutines());
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(DoSalvages));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(SellJunk));
                if (GarrisonButler.NameStatic.ToLower().Contains("ice")) 
                    _mainSequence.AddAction(new ActionHelpers.ActionOnTimer<List<MailItem>>(MailItem, CanMailItem, 15000, 1000));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(LastRound));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(Waiting));

                LootTargeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
            }
            catch (Exception e)
            {
                GarrisonButler.Warning(e.ToString());
            }
        }
        private static readonly WaitTimer VendorCheckTimer = new WaitTimer(TimeSpan.FromSeconds(30));
        private static Profile _currentProfile;
        private static readonly WaitTimer CheckBagsFullTimer = new WaitTimer(TimeSpan.FromSeconds(15));
        private static readonly Stopwatch BagsFullAlertTimer = new Stopwatch();
        private static async Task<bool> CheckBagsFull()
        {
            if (!CheckBagsFullTimer.IsFinished)
                return false;

            if (BagsFullAlertTimer.IsRunning)
            {
                if (BagsFullAlertTimer.Elapsed > TimeSpan.FromSeconds(35))
                    BagsFullAlertTimer.Stop();
                else
                    return true;
            }
            if (Me.FreeBagSlots < 3)
            {
                if (await HbApi.StackAllItemsIfPossible())
                    return true;

                if (await SellJunkCoroutine() == ActionResult.Running)
                    return true;

                // Without a timer it will spam Alert messages over and over
                // Should probably have a way to hook in to the "ok" button
                // but for now we will just wait 30s
                BagsFullAlertTimer.Reset();
                BagsFullAlertTimer.Start();
                Alert.Show("Bags space is low", "Bag space is low on this toon, please make room before GarrisonButler can continue. Min:3 Advised:10", 30, true, false);
                GarrisonButler.Log("!!! Bag space is low, please make room before GarrisonButler can continue. Min:3 Advised:10 - Waiting 35s.");
                
                return true;
            }
            CheckBagsFullTimer.Reset();
            return false;
        }
        private static void CheckForVendors()
        {
            if (VendorCheckTimer.IsFinished)
            {
                GarrisonButler.Diagnostic("Checking for vendors...");
                // load empty profile if none loaded
                if (_currentProfile == null)
                {
                    ProfileManager.LoadEmpty();
                    _currentProfile = ProfileManager.CurrentProfile;
                }

                var profile = ProfileManager.CurrentProfile;

                // get all visible vendors
                var vendors = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().Where(u => u.IsVendor && Dijkstra.ClosestToNodes(u.Location).Distance(u.Location) < 5).GetEmptyIfNull();
                //foreach (var vendor in profile.VendorManager.AllVendors)
                //{
                //    if (vendor.Location == null ||
                //        Dijkstra.ClosestToNodes(vendor.Location).Distance(vendor.Location) > 5)
                //    {
                //        profile.VendorManager.AllVendors.Remove(vendor);
                //    }
                //}
                // add the new ones to the list of vendors as unknown, it should check itself.
                foreach (var woWUnit in vendors.Where(woWUnit => profile.VendorManager.ForcedVendors.All(v => v.Entry != woWUnit.Entry)))
                {
                    if (woWUnit.IsRepairMerchant)
                        profile.VendorManager.ForcedVendors.Add(new Vendor(woWUnit, Vendor.VendorType.Repair));
                    else
                        profile.VendorManager.ForcedVendors.Add(new Vendor(woWUnit, Vendor.VendorType.Unknown));
                    GarrisonButler.Diagnostic("Adding vendor: " + woWUnit);
                }
                if (BotPoi.Current != null)
                {
                    var currentpoi = BotPoi.Current.AsVendor;
                    if (currentpoi != null)
                    {
                        if (!vendors.Any(i => i.Entry == currentpoi.Entry))
                        {
                            profile.VendorManager.AllVendors.Remove(currentpoi);
                            profile.VendorManager.ForcedVendors.Remove(currentpoi);
                            GarrisonButler.Diagnostic(currentpoi.ToString());
                        }
                    }
                }
                GarrisonButler.Diagnostic("Checking for vendors... Done.");
                VendorCheckTimer.Reset();
            }
        }


        internal static void OnStop()
        {
            LootTargeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
            _mainSequence = null;
            Navigator.NavigationProvider = nativeNavigation;
            customNavigation = null;
        }

        internal static void IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            if (StyxWoW.Me.Combat)
                return;

            double lootRadiusSqr = LootTargeting.LootRadius*LootTargeting.LootRadius;

            WoWPoint myLoc = StyxWoW.Me.Location;

            foreach (WoWObject obj in incomingUnits)
            {
                var gObj = obj as WoWGameObject;
                if (gObj != null)
                {
                    if (gObj.SubType != WoWGameObjectType.FishingHole)
                        continue;

                    outgoingUnits.Add(obj);
                    continue;
                }

                if (!LootTargeting.LootMobs)
                    continue;

                var unit = obj as WoWUnit;
                if (unit == null)
                    continue;

                if (!unit.IsDead)
                    continue;

                if (Blacklist.Contains(unit, BlacklistFlags.Loot | BlacklistFlags.Node))
                    continue;

                if (myLoc.DistanceSqr(unit.Location) > lootRadiusSqr)
                    continue;

                outgoingUnits.Add(unit);
            }
        }

        public static async Task<bool> RootLogic()
        {
           //ModuleVersion configVersion = GaBSettings.Get().ConfigVersion;
            /*if (configVersion.Build != GarrisonButler.Version.Build ||
                configVersion.Major != GarrisonButler.Version.Major ||
                configVersion.Minor != GarrisonButler.Version.Minor ||
                configVersion.Revision != GarrisonButler.Version.Revision)
            {
                // Popup to explain this is a beta and they need to reconfigure their configs.
                Bots.DungeonBuddy.Helpers.Alert.Show("GarrisonButler Lite Edition",
                    "Hey!\n" +
                    "Since GarrisonButler is still on heavy development you are required to verify your settings for each new build you install.\n" +
                    "If you have any issues, please post a full log on the GarrisonButler Forum page.\n" +
                    "Bot safe,\n" +
                    "Deams\n",
                    60, true, false, new System.Action(() => { new ConfigForm(); }));
                TreeRoot.Stop("Configuration of GarrisonButler outdated or inexistant.");
                return true;
            }
            */
            // Fast checks
            CheckResetAfk();

            CheckNavigationSystem();

            CheckForVendors();

            if (await RestoreUiIfNeeded())
                return true;

            if (await DeathBehavior.ExecuteCoroutine())
                return true;

            if (StyxWoW.Me.Combat && await CombatBehavior.ExecuteCoroutine())
                return true;

            if (await VendorBehavior.ExecuteCoroutine())
                return true;

            if (await CheckLootFrame())
                return true;

            if (await LootBehavior.ExecuteCoroutine())
                return true;

            if (!StyxWoW.Me.IsAlive || StyxWoW.Me.Combat || RoutineManager.Current.NeedRest)
                return false;

            if (BotPoi.Current.Type == PoiType.None && LootTargeting.Instance.FirstObject != null)
                SetLootPoi(LootTargeting.Instance.FirstObject);

            if (await CheckBagsFull())
                return true;
            
            if (_mainSequence == null)
            {
                GarrisonButler.Warning("ERROR: mainSequence NULL");
                return false;
            }

            // Bot will sleep after one full run waiting for new things to do
            // similar to behavior in MixedMode
            if (ReadyToSwitch)
                if (!GarrisonButler.Instance.RequirementsMet)
                {
                    await JobDoneSwitch();
                    GarrisonButler.Log("Taking a break for 60s");
                    return false;
                }

            // Heavier coroutines on timer
            //GarrisonButler.Diagnostic("Calling await mainSequence.ExecuteAction()");
            var resultActions = await _mainSequence.ExecuteAction();
            if (resultActions == ActionResult.Running || resultActions == ActionResult.Refresh)
            {
                //GarrisonButler.Diagnostic("Returning true from mainSequence.ExecuteAction()");
                return true;
            }

            ReadyToSwitch = true;
            return false;
        }


        private static void CheckNavigationSystem()
        {
            if (customNavigation == null)
            {
                nativeNavigation = Navigator.NavigationProvider;
                customNavigation = new NavigationGaB();
            }
            if (HbApi.IsInGarrison() && !CustomNavigationLoaded)
            {
                Navigator.NavigationProvider = customNavigation;
            }
            else if (CustomNavigationLoaded)
            {
                Navigator.NavigationProvider = nativeNavigation;
            }
        }

        private async static Task<ActionResult> SellJunk()
        {
            if (!GaBSettings.Get().ForceJunkSell)
            {
                GarrisonButler.Diagnostic("[Vendor] Force junk behavior deactivated in User settings.");
                return ActionResult.Done;
            }
            return await SellJunkCoroutine();
        }
        private async static Task<ActionResult> SellJunkCoroutine()
        {
            if (Me.BagItems.Any(i =>
            {
                var res = false;
                try
                {
                    res = i.Quality == WoWItemQuality.Poor;
                }
                catch (Exception)
                {}
                return res;
            }))
            {
                GarrisonButler.Log("[Vendor] Selling Junk.");
                Vendors.ForceSell = true;
                return await _vendorBehavior.ExecuteCoroutine() ? ActionResult.Running : ActionResult.Done;
            }
            Vendors.ForceSell = false;
            GarrisonButler.Diagnostic("[Vendor] No Junk detected.");
            return ActionResult.Done;
        }
        internal static Tuple<bool, WoWItem> ShouldTPToGarrison()
        {
            if (!HbApi.IsInGarrison())
            {
                if (!GaBSettings.Get().UseGarrisonHearthstone)
                {
                    TreeRoot.Stop(
                        "Not in garrison and Hearthstone not activated. Please move the toon to the garrison or modify the settings.");
                    return new Tuple<bool, WoWItem>(true, null);
                }
                WoWItem stone = Me.BagItems.FirstOrDefault(i => i.Entry == GarrisonHearthstone);
                if (stone == null)
                    return new Tuple<bool, WoWItem>(false, null);

                if (stone.CooldownTimeLeft.TotalSeconds > 0)
                    return new Tuple<bool, WoWItem>(true, stone);

                return new Tuple<bool, WoWItem>(true, stone);
            }
            return new Tuple<bool, WoWItem>(false, null);
        }

        private static async Task<bool> TransportToGarrison()
        {
            if (GaBSettings.Get().UseGarrisonHearthstone)
            {
                WoWItem stone = Me.BagItems.FirstOrDefault(i => i.Entry == GarrisonHearthstone);
                if (stone != null)
                {
                    if (stone.CooldownTimeLeft.TotalSeconds > 0)
                    {
                        GarrisonButler.Warning("UseGarrisonHearthstone: On cooldown, " +
                                               stone.CooldownTimeLeft.TotalSeconds + " secs left.");
                        return true;
                    }
                    GarrisonButler.Log("Using garrison hearthstone.");
                    if (Me.IsMoving)
                        WoWMovement.MoveStop();
                    await Buddy.Coroutines.Coroutine.Wait(1000, () => !Me.IsMoving);
                    stone.Use();

                    if (!await Buddy.Coroutines.Coroutine.Wait(15000, () => !Me.IsCasting))
                    {
                        GarrisonButler.Log("Casting Garrison Hearthstone.");

                        GarrisonButler.Log("Waiting after casting Garrison Hearthstone...");
                        if (!await Buddy.Coroutines.Coroutine.Wait(60000, HbApi.IsInGarrison))
                        {
                            return false;
                        }
                    }
                }
                else GarrisonButler.Warning("UseGarrisonHearthstone set to true but can't find it in bags.");
            }
            else
            {
                GarrisonButler.Warning(
                    "Character not in garrison and UseGarrisonHearthstone set to false, doing nothing.");
                return true;
            }
            return false;
        }

        private static bool IsAutoAngler()
        {
            if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (botBase.PrimaryBot.Name.ToLower().Contains("angler"))
                {
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> RestoreUiIfNeeded()
        {
            if (_restoreCompletedMission && MissionLua.GetNumberCompletedMissions() == 0)
            {
                GarrisonButler.Diagnostic("RestoreUiIfNeeded RestoreCompletedMission called");
                // Restore UI
                Lua.DoString("GarrisonMissionFrame.MissionTab.MissionList.CompleteDialog:Hide();" +
                             "GarrisonMissionFrame.MissionComplete:Hide();" +
                             "GarrisonMissionFrame.MissionCompleteBackground:Hide();" +
                             "GarrisonMissionFrame.MissionComplete.currentIndex = nil;" +
                             "GarrisonMissionFrame.MissionTab:Show();" +
                             "GarrisonMissionList_UpdateMissions();");
                _restoreCompletedMission = false;
                return true;
            }
            await CommonCoroutines.SleepForLagDuration();
            return false;
        }

        private static async Task<bool> CheckLootFrame()
        {
            // loot everything.
            if (!GarrisonButler.LootIsOpen) return false;

            var lootSlotInfo = new List<LootSlotInfo>();
            for (int i = 0; i < LootFrame.Instance.LootItems; i++)
            {
                lootSlotInfo.Add(LootFrame.Instance.LootInfo(i));
            }

            if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                LootFrame.Instance.LootAll();
                return !GarrisonButler.LootIsOpen;
            }))
            {
                GarrisonButler.Log("Successfully looted: ");
                foreach (LootSlotInfo lootinfo in lootSlotInfo)
                {
                    GarrisonButler.Log(lootinfo.LootQuantity + "x " + lootinfo.LootName);
                }
            }
            else
            {
                GarrisonButler.Warning("Failed to loot from Frame.");
            }
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        private static void CheckResetAfk()
        {
            if (!ResetAfkTimer.IsFinished) return;
            StyxWoW.ResetAfk();
            ResetAfkTimer.Reset();
        }

        private static void SetLootPoi(WoWObject lootObj)
        {
            if (BotPoi.Current.Type != PoiType.None || lootObj == null || !lootObj.IsValid)
                return;

            if (lootObj is WoWGameObject)
            {
                var obj = lootObj as WoWGameObject;
                if (obj.SubType == WoWGameObjectType.FishingHole)
                    return;
                BotPoi.Current = new BotPoi(lootObj, PoiType.Harvest);
            }
            else
            {
                var unit = lootObj as WoWUnit;
                if (unit != null)
                {
                    if (unit.CanLoot)
                        BotPoi.Current = new BotPoi(lootObj, PoiType.Loot);
                    else if (unit.CanSkin)
                        BotPoi.Current = new BotPoi(lootObj, PoiType.Skin);
                }
            }
        }
    }
}
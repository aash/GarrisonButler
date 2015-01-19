using Styx.CommonBot.Profiles.Quest.Order;
using Bots.DungeonBuddy.Helpers;
using GarrisonButler.API;
using Styx.CommonBot.Profiles;

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
            new WoWPoint(5585.125, 4565.036, 135.9761) //level 3
        };

        private static readonly WoWPoint AllyMailbox = new WoWPoint(1927.694, 294.151, 88.96585);
        private static readonly WoWPoint HordeMailbox = new WoWPoint(5580.682, 4570.392, 136.558);

        public static DateTime NextCheck = DateTime.Now;

        internal static readonly uint GarrisonHearthstone = 110560;
        private static readonly WoWPoint FishingSpotAlly = new WoWPoint(2021.108, 187.5952, 84.55713);
        private static readonly WoWPoint FishingSpotHorde = new WoWPoint(5482.597, 4637.465, 136.1296);

        public static bool ReadyToSwitch;

        private static bool _restoreCompletedMission;
        private static ActionHelpers.ActionsSequence _mainSequence;

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
                _mainSequence.AddAction(new ActionHelpers.ActionOnTimer(UseItemInbagsWithTimer(60000, () => Me.IsInGarrison()), ShouldTpToGarrison,
                    10000, 1000));
                if (GarrisonButler.NameStatic.ToLower().Contains("ice"))
                    _mainSequence.AddAction(new ActionHelpers.ActionOnTimer(GetMails, HasMails));
                _mainSequence.AddAction(InitializeMineAndGarden());
                _mainSequence.AddAction(new ActionHelpers.ActionOnTimer(DoDailyCd, CanRunDailies, 15000,
                    1000));
                _mainSequence.AddAction(InitializeBuildingsCoroutines());
                _mainSequence.AddAction(InitializeMissionsCoroutines());

                _mainSequence.AddAction(new ActionHelpers.ActionBasic(DoSalvages));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(SellJunk));
                if (GarrisonButler.NameStatic.ToLower().Contains("ice"))
                    _mainSequence.AddAction(new ActionHelpers.ActionOnTimer(MailItem, CanMailItem, 15000,
                        1000));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(LastRound));
                _mainSequence.AddAction(new ActionHelpers.ActionBasic(Waiting));

                LootTargeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;

                if (InterfaceLua.IsSplashFrame())
                    GarrisonButler.Log("OnStart(): Splash Frame was open, closing now");

                InterfaceLua.CloseSplashFrame();
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

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

                if ((await SellJunkCoroutine()).Status == ActionResult.Running)
                    return true;

                // Without a timer it will spam Alert messages over and over
                // Should probably have a way to hook in to the "ok" button
                // but for now we will just wait 30s
                BagsFullAlertTimer.Reset();
                BagsFullAlertTimer.Start();
                Alert.Show("Bags space is low",
                    "Bag space is low on this toon, please make room before GarrisonButler can continue. Min:3 Advised:10",
                    30, true, false);
                GarrisonButler.Log(
                    "!!! Bag space is low, please make room before GarrisonButler can continue. Min:3 Advised:10 - Waiting 35s.");

                return true;
            }
            CheckBagsFullTimer.Reset();
            return false;
        }

        private static void CheckForVendors()
        {
            if (!VendorCheckTimer.IsFinished) return;
            GarrisonButler.Diagnostic("Checking for vendors...");
            // load empty profile if none loaded
            if (_currentProfile == null)
            {
                ProfileManager.LoadEmpty();
                _currentProfile = ProfileManager.CurrentProfile;
            }
            var profile = ProfileManager.CurrentProfile;


            //// Deleting vendor mounts to avoid infinite Mount/unmount issues inside small buildings.
            //const int GrandExpeditionYak = 122708;
            //const int TravelersTundraMammothAlliance = 61425;
            //const int TravelersTundraMammothHorde = 61447;
            //Mount.GroundMounts.RemoveAll(m => m.CreatureSpellId == GrandExpeditionYak
            //    || m.CreatureSpellId == TravelersTundraMammothAlliance
            //    || m.CreatureSpellId == TravelersTundraMammothHorde);


            // get all visible vendors
            var vendors =
                ObjectManager.GetObjectsOfTypeFast<WoWUnit>()
                    .Where(u => u.IsVendor && Dijkstra.ClosestToNodes(u.Location).Distance(u.Location) < 5)
                    .GetEmptyIfNull();
            var units = vendors as WoWUnit[] ?? vendors.ToArray();
            foreach (
                var woWUnit in
                    units.Where(woWUnit => profile.VendorManager.ForcedVendors.All(v => v.Entry != woWUnit.Entry)))
            {
                profile.VendorManager.ForcedVendors.Add(woWUnit.IsRepairMerchant
                    ? new Vendor(woWUnit, Vendor.VendorType.Repair)
                    : new Vendor(woWUnit, Vendor.VendorType.Unknown));
                GarrisonButler.Diagnostic("Adding vendor: " + woWUnit);
            }
            if (BotPoi.Current != null)
            {
                var currentpoi = BotPoi.Current.AsVendor;
                if (currentpoi != null)
                {
                    if (units.All(i => i.Entry != currentpoi.Entry))
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


        internal static void OnStop()
        {
            LootTargeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
            _mainSequence = null;
            Navigator.NavigationProvider = NativeNavigation;
            _customNavigation = null;
        }

        internal static void IncludeTargetsFilter(List<WoWObject> incomingUnits, HashSet<WoWObject> outgoingUnits)
        {
            if (StyxWoW.Me.Combat)
                return;

            var lootRadiusSqr = LootTargeting.LootRadius*LootTargeting.LootRadius;

            var myLoc = StyxWoW.Me.Location;

            foreach (var obj in incomingUnits)
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
            

            if ((await VendorCoroutineWorkaround()).Status == ActionResult.Running)
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
                    return false;
                }

            // Reset of mount vendor workaround
            _mountVendorTimeStart = default(DateTime);

            // Heavier coroutines on timer
            //GarrisonButler.Diagnostic("Calling await mainSequence.ExecuteAction()");
            var resultActions = await _mainSequence.ExecuteAction();
            if (resultActions.Status == ActionResult.Running || resultActions.Status == ActionResult.Refresh)
            {
                //GarrisonButler.Diagnostic("Returning true from mainSequence.ExecuteAction()");
                return true;
            }

            ReadyToSwitch = true;
            return false;
        }


        private static void CheckNavigationSystem()
        {
            if (_customNavigation == null)
            {
                NativeNavigation = Navigator.NavigationProvider;
                _customNavigation = new NavigationGaB();
            }
            if (Me.IsInGarrison() && !CustomNavigationLoaded)
            {
                Navigator.NavigationProvider = _customNavigation;
            }
            else if (!Me.IsInGarrison() && CustomNavigationLoaded)
            {
                Navigator.NavigationProvider = NativeNavigation;
            }
        }

        private static async Task<Result> SellJunk()
        {
            if (GaBSettings.Get().ForceJunkSell) return await SellJunkCoroutine();
            GarrisonButler.Diagnostic("[Vendor] Force junk behavior deactivated in User settings.");
            return new Result(ActionResult.Done);
        }

        private static async Task<Result> SellJunkCoroutine()
        {
            if (Me.BagItems.Any(i =>
            {
                var res = false;
                try
                {
                    res = i.Quality == WoWItemQuality.Poor;
                }
                    // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception e)
                {
                    if (e is Buddy.Coroutines.CoroutineStoppedException)
                        throw;
                }
                return res;
            }))
            {
                GarrisonButler.Log("[Vendor] Selling Junk.");
                Vendors.ForceSell = true;
                return await VendorCoroutineWorkaround();
            }
            Vendors.ForceSell = false;
            GarrisonButler.Diagnostic("[Vendor] No Junk detected.");
            return new Result(ActionResult.Done);
        }

        private static DateTime _mountVendorTimeStart = default(DateTime);

        private static async Task<Result> VendorCoroutineWorkaround()
        {
            if (!Vendors.ForceSell &&
                (ProfileManager.CurrentProfile.MinFreeBagSlots <= 0 ||
                 StyxWoW.Me.FreeNormalBagSlots > ProfileManager.CurrentProfile.MinFreeBagSlots))
                return new Result(ActionResult.Done);

            if (_mountVendorTimeStart == default(DateTime))
                _mountVendorTimeStart = DateTime.Now;

            while ((DateTime.Now - _mountVendorTimeStart).TotalSeconds < 5)
            {
                if (!await MoveToTable())
                    break;
            }

            _mountVendorTimeStart = DateTime.Now - TimeSpan.FromMinutes(1);

            var resultCoroutine = await VendorBehavior.ExecuteCoroutine() ? new Result(ActionResult.Running) : new Result(ActionResult.Done);
            return resultCoroutine;
        }

        internal static async Task<Result> ShouldTpToGarrison()
        {
            if (Me.IsInGarrison())
                return new Result(ActionResult.Failed);

            if (!GaBSettings.Get().UseGarrisonHearthstone)
            {
                //TreeRoot.Stop(
                //    "Not in garrison and Hearthstone not activated. Please move the toon to the garrison or modify the settings.");
                return new Result(ActionResult.Failed);
            }
            var stone = Me.BagItems.FirstOrDefault(i => i.Entry == GarrisonHearthstone);
            return (stone == null || stone.CooldownTimeLeft.TotalSeconds > 1)
                ? new Result(ActionResult.Failed)
                : new Result(ActionResult.Running, stone);
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

            var gameObject = lootObj as WoWGameObject;
            if (gameObject != null)
            {
                var obj = gameObject;
                if (obj.SubType == WoWGameObjectType.FishingHole)
                    return;
                BotPoi.Current = new BotPoi(lootObj, PoiType.Harvest);
            }
            else
            {
                var unit = lootObj as WoWUnit;
                if (unit == null) return;
                if (unit.CanLoot)
                    BotPoi.Current = new BotPoi(lootObj, PoiType.Loot);
                else if (unit.CanSkin)
                    BotPoi.Current = new BotPoi(lootObj, PoiType.Skin);
            }
        }
    }
}
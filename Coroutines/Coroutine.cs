using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using GarrisonBuddy.Config;
using GarrisonBuddy.Objects;
using GarrisonLua;
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

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static Composite _deathBehavior;
        private static Composite _lootBehavior;
        private static Composite _combatBehavior;
        private static Composite _vendorBehavior;
        private static readonly WaitTimer ResetAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
        private static WoWPoint _lastMoveTo;
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

        private static readonly WoWPoint allyMailbox = new WoWPoint(1927.694, 294.151, 88.96585);
        private static readonly WoWPoint hordeMailbox = new WoWPoint(5580.682, 4570.392, 136.558);


        private static bool test = true;
        private static long cpt;

        public static DateTime NextCheck = DateTime.Now;
        public static List<KeyValuePair<Mission, Follower[]>> ToStart = new List<KeyValuePair<Mission, Follower[]>>();

        internal static readonly List<uint> GarrisonsZonesId = new List<uint>
        {
            7078, // Lunarfall - Ally
            7004, // Frostwall - Horde
        };

        internal static readonly uint GarrisonHearthstone = 110560;
        private static readonly WoWPoint FishingSpotAlly = new WoWPoint(2021.108, 187.5952, 84.55713);
        private static readonly WoWPoint FishingSpotHorde = new WoWPoint(5482.597, 4637.465, 136.1296);

        private static bool init;

        public static bool ReadyToSwitch = false;

        #region MixedModeTimers

        private static bool DailiesTriggered;
        private static WaitTimer DailiesWaitTimer;
        private static int DailiesWaitTimerValue = 30;

        private static WaitTimer CacheWaitTimer;
        private static bool CacheTriggered;
        private static int CacheWaitTimerValue = 30;


        private static WaitTimer StartOrderWaitTimer;
        private static bool StartOrderTriggered;
        private static int StartOrderWaitTimerValue = 30;

        private static WaitTimer PickUpOrderWaitTimer;
        private static bool PickUpOrderTriggered;
        private static int PickUpOrderWaitTimerValue = 30;


        private static WaitTimer GardenWaitTimer;
        private static int GardenWaitTimerValue = 30;
        private static bool GardenTriggered;


        private static WaitTimer MineWaitTimer;
        private static int MineWaitTimerValue = 30;
        private static bool MineTriggered;

        private static WaitTimer MissionWaitTimer;
        private static bool MissionTriggered;
        private static int MissionWaitTimerValue = 30;

        private static WaitTimer TurnInMissionWaitTimer;
        private static bool TurnInMissionsTriggered;
        private static int TurnInMissionWaitTimerValue = 30;

        private static WaitTimer StartMissionWaitTimer;
        private static bool StartMissionTriggered;
        private static int StartMissionWaitTimerValue = 30;

        private static WaitTimer SalvageWaitTimer;
        private static bool SalvageTriggered;
        private static int SalvageWaitTimerValue = 30;

        private static int LastRoundWaitTimerValue = 30;
        private static bool LastRoundTriggered;
        private static WaitTimer LastRoundWaitTimer;

        #endregion

        private static bool RestoreCompletedMission = false;
        
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

        private static ActionsSequence mainSequence;
        internal static void OnStart()
        {
            try
            {
                InitializeShipments();
                GarrisonBuddy.Warning("InitializeShipments");
                InitializeMissions();
                GarrisonBuddy.Warning("InitializeMissions");
                InitializationMove();
                GarrisonBuddy.Warning("InitializationMove");
                InitializeDailies();
                GarrisonBuddy.Warning("InitializeDailies");

                mainSequence = new ActionsSequence();
                mainSequence.AddAction(new ActionOnTimer<WoWItem>(UseItemInbags, CanTPToGarrison));
                mainSequence.AddAction(InitializeBuildingsCoroutines());
                mainSequence.AddAction(new ActionBasic(DoMissions));
                mainSequence.AddAction(new ActionOnTimer<DailyProfession>(DoDailyCd, CanRunDailies));
                mainSequence.AddAction(new ActionBasic(DoSalvages));
                mainSequence.AddAction(new ActionBasic(LastRound));
                mainSequence.AddAction(new ActionBasic(Waiting));

                InitializeDailies();
                GarrisonBuddy.Warning("mainSequence");

                LootTargeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
                InitializeDailies();
                GarrisonBuddy.Warning("LootTargeting");
            }
            catch (Exception e)
            {
                GarrisonBuddy.Warning(e.ToString());
            }
        }


        internal static void OnStop()
        {
            LootTargeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
            mainSequence = null;
            Navigator.NavigationProvider = oldNavigation;
            navigation = null;
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
        
        private static Stopwatch testStopwatch = new Stopwatch();
        private static bool LogTime = true;
        public static async Task<bool> RootLogic()
        {
            var configVersion = GaBSettings.Get().ConfigVersion;
            if (configVersion.Build != GarrisonBuddy.Version.Build ||
                configVersion.Major != GarrisonBuddy.Version.Major ||
                configVersion.Minor != GarrisonBuddy.Version.Minor ||
                configVersion.Revision != GarrisonBuddy.Version.Revision)
            {
                // Popup to explain this is a beta and they need to reconfigure their configs.
                Bots.DungeonBuddy.Helpers.Alert.Show("GarrisonBuddy Public Beta",
                    "Hey!\n" +
                    "Thanks for your support and your help testing out this new botBase.\n" +
                    "Since GarrisonBuddy is still on heavy development you are required to verify your settings foe each new build you install.\n" +
                    "Be sure to restart the bot after doing so!" +
                    "If you have any issues, please post a full log on the GarrisonBuddy Forum page.\n" +
                    "Bot safe,\n" +
                    "Deams\n",
                    60, true, false);
                TreeRoot.Stop();
                return true;
            }
            // Fast checks
            CheckResetAfk();

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

            if(LogTime)
                GarrisonBuddy.Diagnostic("[Time] TICK: " + testStopwatch.Elapsed);

            // Heavier coroutines on timer
            if (await mainSequence.ExecuteAction())
                return true;

            ReadyToSwitch = true;
            return false;
        }

        internal static Tuple<bool, WoWItem> CanTPToGarrison()
        {
            if (!GaBSettings.Get().UseGarrisonHearthstone)
            {
                return new Tuple<bool, WoWItem>(false, null);
            }

            if (GarrisonsZonesId.Contains(Me.ZoneId))
            {
                return new Tuple<bool, WoWItem>(false, null);
            }

            WoWItem stone = Me.BagItems.FirstOrDefault(i => i.Entry == GarrisonHearthstone);
            if(stone == null)
                return new Tuple<bool, WoWItem>(false, null);

             if (stone.CooldownTimeLeft.TotalSeconds > 0)
                return new Tuple<bool, WoWItem>(false, null);

            return new Tuple<bool, WoWItem>(true,stone);
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
                        GarrisonBuddy.Warning("UseGarrisonHearthstone: On cooldown, " +
                                              stone.CooldownTimeLeft.TotalSeconds + " secs left.");
                        return true;
                    }
                    GarrisonBuddy.Log("Using garrison hearthstone.");
                    if (Me.IsMoving)
                        WoWMovement.MoveStop();
                    await Buddy.Coroutines.Coroutine.Wait(1000, () => !Me.IsMoving);
                    stone.Use();
                    if (!await Buddy.Coroutines.Coroutine.Wait(60000, () => GarrisonsZonesId.Contains(Me.ZoneId)))
                    {
                        GarrisonBuddy.Warning("Used garrison hearthstone but not in garrison yet.");
                        return false;
                    }
                }
                else GarrisonBuddy.Warning("UseGarrisonHearthstone set to true but can't find it in bags.");
            }
            else
            {
                GarrisonBuddy.Warning(
                    "Character not in garrison and UseGarrisonHearthstone set to false, doing nothing.");
                return false;
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

        public static async Task<bool> RestoreUiIfNeeded()
        {
            if (RestoreCompletedMission && MissionLua.GetNumberCompletedMissions() == 0)
            {
                GarrisonBuddy.Diagnostic("RestoreUiIfNeeded RestoreCompletedMission called");
                // Restore UI
                Lua.DoString("GarrisonMissionFrame.MissionTab.MissionList.CompleteDialog:Hide();" +
                             "GarrisonMissionFrame.MissionComplete:Hide();" +
                             "GarrisonMissionFrame.MissionCompleteBackground:Hide();" +
                             "GarrisonMissionFrame.MissionComplete.currentIndex = nil;" +
                             "GarrisonMissionFrame.MissionTab:Show();" +
                             "GarrisonMissionList_UpdateMissions();");
                RestoreCompletedMission = false;
                return true;
            }
            return false;
        }

        private static async Task<bool> CheckLootFrame()
        {
            // loot everything.
            if (!GarrisonBuddy.LootIsOpen) return false;

            var lootSlotInfos = new List<LootSlotInfo>();
            for (int i = 0; i < LootFrame.Instance.LootItems; i++)
            {
                lootSlotInfos.Add(LootFrame.Instance.LootInfo(i));
            }

            if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
            {
                LootFrame.Instance.LootAll();
                return !GarrisonBuddy.LootIsOpen;
            }))
            {
                GarrisonBuddy.Log("Succesfully looted: ");
                foreach (LootSlotInfo lootinfo in lootSlotInfos)
                {
                    GarrisonBuddy.Log(lootinfo.LootQuantity + "x " + lootinfo.LootName);
                }
            }
            else
            {
                GarrisonBuddy.Warning("Failed to loot from Frame.");
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

//var hasItemTomail = Styx.CommonBot.Inventory.InventoryManager.HaveItemsToMail;
//if (hasItemTomail && Styx.Helpers.CharacterSettings.Instance.MailRecipient.Any())
//{
//    var mailBox =
//        ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
//            .Where(o => o.SubType == WoWGameObjectType.Mailbox)
//            .FirstOrDefault();
//    if(mailBox == null)
//        if (await MoveTo(Me.IsAlliance ? allyMailbox : hordeMailbox))
//            return true;
//    if (await MoveTo(mailBox.Location))
//        return true;
//    await Buddy.Coroutines.Coroutine.Wait(1000, () => !Me.IsMoving);
//    mailBox.Interact();
//    await CommonCoroutines.SleepForLagDuration();
//    var items = Styx.CommonBot.Inventory.InventoryManager.GetItemsToMail();
//    var mailFrame = MailFrame.Instance;
//    await mailFrame.SendMailWithManyAttachmentsCoroutine(
//        Styx.Helpers.CharacterSettings.Instance.MailRecipient,
//        items);
//}
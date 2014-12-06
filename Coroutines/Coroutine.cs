using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using GarrisonBuddy.Config;
using GarrisonLua;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
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
        
        private static readonly List<int> SalvageCratesIds = new List<int>
        {
            114120,
            114119
        };

        private static readonly List<WoWPoint> LastRoundWaypointsHorde = new List<WoWPoint>
        {
            new WoWPoint(5595.488, 4530.896, 126.0771),
            new WoWPoint(5502.666, 4475.98, 138.9149),
            new WoWPoint(5440.396, 4572.317, 135.7494),
        };

        private static readonly List<WoWPoint> LastRoundWaypointsAlly = new List<WoWPoint>
        {
            new WoWPoint(1917.989, 127.5877, 83.37553),
            new WoWPoint(1866.669, 226.6118, 76.641),
            new WoWPoint(1819.171, 212.0933, 71.44927),
        };

        private static int lastRoundTemp;

        public static bool ReadyToSwitch = false;

        private static bool DailiesTriggered;
        private static WaitTimer DailiesWaitTimer;
        private static int DailiesWaitTimerValue = 1;

        private static WaitTimer CacheWaitTimer;
        private static bool CacheTriggered;
        private static int CacheWaitTimerValue = 1;


        private static WaitTimer StartOrderWaitTimer;
        private static bool StartOrderTriggered;
        private static int StartOrderWaitTimerValue;

        private static WaitTimer PickUpOrderWaitTimer;
        private static bool PickUpOrderTriggered;
        private static int PickUpOrderWaitTimerValue;


        private static WaitTimer GardenWaitTimer;
        private static int GardenWaitTimerValue;
        private static bool GardenTriggered;


        private static WaitTimer MineWaitTimer;
        private static int MineWaitTimerValue = 1;
        private static bool MineTriggered;

        private static WaitTimer MissionWaitTimer;
        private static bool MissionTriggered;
        private static int MissionWaitTimerValue = 1;

        private static WaitTimer TurnInMissionWaitTimer;
        private static bool TurnInMissionsTriggered;
        private static int TurnInMissionWaitTimerValue = 1;

        private static WaitTimer StartMissionWaitTimer;
        private static bool StartMissionTriggered;
        private static int StartMissionWaitTimerValue = 1;

        private static WaitTimer SalvageWaitTimer;
        private static bool SalvageTriggered;
        private static int SalvageWaitTimerValue = 1;
        private static int LastRoundWaitTimerValue = 1;
        private static bool LastRoundTriggered;
        private static WaitTimer LastRoundWaitTimer;

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

        public static bool RestoreCompletedMission { get; set; }

        private static void RefreshCollections()
        {
            RefreshBuildings();
            RefreshMissions();
            RefreshFollowers();
        }

        internal static void OnStart()
        {
            try
            {
                LootTargeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;
            }
            catch (Exception e)
            {
                GarrisonBuddy.Warning(e.ToString());
            }
        }

        internal static void InitializeCoroutines()
        {
            if (init) return;
            InitializeShipments();
            InitializeMissions();
            InitializationMove();
            InitializeDailies();
            init = true;
        }

        internal static void OnStop()
        {
            LootTargeting.Instance.IncludeTargetsFilter -= IncludeTargetsFilter;
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


            //GarrisonBuddy.Diagnostic("Time before start gar stuff: " + testStopwatch.Elapsed);
            if (await TransportToGarrison())
                return true;
            //GarrisonBuddy.Diagnostic("Time after TransportToGarrison: " + testStopwatch.Elapsed);

            if (await DoBuildingRelated())
                return true;
            //GarrisonBuddy.Diagnostic("Time after DoBuildingRelated: " + testStopwatch.Elapsed);

            if (await DoMissions())
                return true;
            //GarrisonBuddy.Diagnostic("Time after DoMissions: " + testStopwatch.Elapsed);

            if (await DoDailyCd())
                return true;
            //GarrisonBuddy.Diagnostic("Time after DoDailyCd: " + testStopwatch.Elapsed);

            if (await DoSalvages())
                return true;

            //var hasItemTomail = Styx.CommonBot.Inventory.InventoryManager.HaveItemsToMail;
            //if (hasItemTomail && Styx.Helpers.CharacterSettings.Instance.MailRecipient.Any())
            //{
            //    var mailBox =
            //        ObjectManager.GetObjectsOfType<WoWGameObject>()
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

            if (await LastRound())
                return true;

            if (await Waiting())
                return true;

            ReadyToSwitch = true;
            return false;
        }

        private static bool CanRunLastRound()
        {
            TimeSpan elapsedTime = DateTime.Now - lastRoundCheckTime;
            if (elapsedTime.TotalMinutes > 30)
                return true;
            return false;
        }


        private static DateTime lastRoundCheckTime = DateTime.MinValue;
        private static async Task<bool> LastRound()
        {
            if (!CanRunLastRound())
                return false;
            
            GarrisonBuddy.Log("Doing a last round to check if something was not too far to see before.");
            List<WoWPoint> myLastRoundPoints = Me.IsAlliance ? LastRoundWaypointsAlly : LastRoundWaypointsHorde;
            if (lastRoundTemp > myLastRoundPoints.Count - 1)
            {
                lastRoundTemp = 0;
                lastRoundCheckTime = DateTime.Now;
                return false;
            }
            if (await MoveTo(myLastRoundPoints[lastRoundTemp]))
                return true;
            lastRoundTemp++;
            return true;
        }

        private static bool CanRunSalvage()
        {
            if (!GaBSettings.Mono.SalvageCrates)
                return false;
            IEnumerable<WoWItem> crates = Me.BagItems.Where(i => SalvageCratesIds.Contains((int) i.Entry));
            Building building = _buildings.FirstOrDefault(b => b.id == 52 || b.id == 140 || b.id == 141);
            return crates.Any() && building != null;
        }

        private static async Task<bool> DoSalvages()
        {
            if (!CanRunSalvage())
                return false;
            IEnumerable<WoWItem> salvageCrates = Me.BagItems.Where(i => SalvageCratesIds.Contains((int) i.Entry));
            if (salvageCrates.Any())
            {
                Building building = _buildings.FirstOrDefault(b => b.id == 52 || b.id == 140 || b.id == 141);
                if (building != null)
                {
                    ObjectManager.Update();
                    ;
                    WoWUnit unit =
                        ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == building.PnjId);
                    // can't find it? Let's try to get closer to the default location.
                    if (unit == null)
                    {
                        await MoveTo(building.Pnj);
                        return true;
                    }

                    if (await MoveTo(unit.Location))
                        return true;
                    if (Me.Mounted)
                        await CommonCoroutines.Dismount("To salvage");
                    foreach (WoWItem salvageCrate in salvageCrates)
                    {
                        await CommonCoroutines.SleepForLagDuration();
                        salvageCrate.UseContainerItem();
                        await Buddy.Coroutines.Coroutine.Wait(5000, () => Me.IsCasting);
                        await Buddy.Coroutines.Coroutine.Yield();
                    }
                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> TransportToGarrison()
        {
            if (GarrisonsZonesId.Contains(Me.ZoneId)) return false;

            if (GaBSettings.Mono.UseGarrisonHearthstone)
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
                    stone.Use();
                    if (!await Buddy.Coroutines.Coroutine.Wait(60000, () => GarrisonsZonesId.Contains(Me.ZoneId)))
                    {
                        GarrisonBuddy.Warning("UseGarrisonHearthstone set to true but can't find it in bags.");
                        return false;
                    }
                }
                else GarrisonBuddy.Warning("UseGarrisonHearthstone set to true but can't find it in bags.");
            }
            else
            {
                GarrisonBuddy.Log(
                    "Character not in garrison and UseGarrisonHearthstone set to false, doing nothing.");
                return false;
            }
            return true;
        }

        private static async Task<bool> Waiting()
        {
            int townHallLevel = BuildingsLua.GetTownHallLevel();
            if (townHallLevel < 1)
                return false;

            List<WoWPoint> myFactionWaitingPoints;
            if (Me.IsAlliance)
                myFactionWaitingPoints = AllyWaitingPoints;
            else
                myFactionWaitingPoints = HordeWaitingPoints;

            if (myFactionWaitingPoints[townHallLevel - 1] == new WoWPoint())
            {
                throw new NotImplementedException();
            }


            if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (botBase.PrimaryBot.Name.ToLower().Contains("angler"))
                {
                    WoWPoint fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                    GarrisonBuddy.Log(
                        "You Garrison has been taken care of, bot safe. AutoAngler with Mixed Mode has been detected, moving to fishing area. Happy catch! :)");
                    if (Me.Location.Distance(fishingSpot) > 2)
                    {
                        if (await MoveTo(fishingSpot))
                            return true;
                    }
                }
            }
            else
            {
                GarrisonBuddy.Log("You Garrison has been taken care of! Waiting for orders...");

                if (await MoveTo(myFactionWaitingPoints[townHallLevel - 1]))
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

        private static bool AnythingLeftToDoBeforeEnd()
        {
            if (ReadyToSwitch)
                // && Location.Distance(Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde) > 10 || Me.IsMoving)
                return false;
            return true;
        }

        public static bool AnythingTodo()
        {
            RefreshBuildings();
            // dailies cd
            if (helperTriggerWithTimer(CanRunDailies, ref DailiesWaitTimer, ref DailiesTriggered, DailiesWaitTimerValue))
                return true;
            // Cache
            if (helperTriggerWithTimer(CanRunCache, ref CacheWaitTimer, ref CacheTriggered, CacheWaitTimerValue))
                return true;

            // Start work orders
            if (helperTriggerWithTimer(CanRunStartOrder, ref StartOrderWaitTimer, ref StartOrderTriggered,
                StartOrderWaitTimerValue))
                return true;

            // Pick Up work orders
            if (helperTriggerWithTimer(CanRunPickUpOrder, ref PickUpOrderWaitTimer, ref PickUpOrderTriggered,
                PickUpOrderWaitTimerValue))
                return true;

            // Mine
            if (helperTriggerWithTimer(CanRunMine, ref MineWaitTimer, ref MineTriggered, MineWaitTimerValue))
                return true;

            // gardenla
            if (helperTriggerWithTimer(CanRunGarden, ref GardenWaitTimer, ref GardenTriggered, GardenWaitTimerValue))
                return true;

            // Missions
            if (helperTriggerWithTimer(CanRunTurnInMissions, ref TurnInMissionWaitTimer, ref TurnInMissionsTriggered,
                TurnInMissionWaitTimerValue))
                return true;

            // Missions completed 
            if (helperTriggerWithTimer(CanRunStartMission, ref StartMissionWaitTimer, ref StartMissionTriggered,
                StartMissionWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(CanRunSalvage, ref SalvageWaitTimer, ref SalvageTriggered, SalvageWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(CanRunLastRound, ref LastRoundWaitTimer, ref LastRoundTriggered,
                LastRoundWaitTimerValue))
                return true;

            return AnythingLeftToDoBeforeEnd();
        }


        // The trigger must be set off by someone else to avoid pauses in the behavior! 
        private static bool helperTriggerWithTimer(Func<bool> condition, ref WaitTimer timer, ref bool toModify,
            int timerValue)
        {
            if (timer != null && !timer.IsFinished)
                return toModify;

            if (timer == null)
                timer = new WaitTimer(TimeSpan.FromMinutes(timerValue));
            timer.Reset();

            if (condition())
                toModify = true;
            else toModify = false;

            return toModify;
        }
    }
}
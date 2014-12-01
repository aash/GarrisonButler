using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
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

        private static readonly List<WoWPoint> AllyWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(1866.069, 230.9416, 76.63979), //level 2
            new WoWPoint(1866.069, 230.9416, 76.63979) //level 3
        };

        private static readonly List<WoWPoint> HordeWaitingPoints = new List<WoWPoint>
        {
            new WoWPoint(), //level 1
            new WoWPoint(), //level 2
            new WoWPoint() //level 3
        };

        private static bool test = true;

        public static DateTime NextCheck = DateTime.Now;
        public static List<KeyValuePair<Mission, Follower[]>> ToStart = new List<KeyValuePair<Mission, Follower[]>>();

        internal static readonly List<uint> GarrisonsZonesId = new List<uint>
        {
            7078, // Lunarfall - Ally
            7004, // Frostwall - Horde
        };

        internal static readonly uint GarrisonHearthstone = 110560;

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


        internal static void OnStart()
        {
            try
            {
                _buildings = BuildingsLua.GetAllBuildings();
                Check = true;
                LootTargeting.Instance.IncludeTargetsFilter += IncludeTargetsFilter;

                InitializationMove();
            }
            catch (Exception e)
            {
                GarrisonBuddy.Warning(e.ToString());
            }
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
            {
                return true;
            }
            if (await LootBehavior.ExecuteCoroutine())
                return true;

            if (await VendorBehavior.ExecuteCoroutine())
                return true;

            if (!StyxWoW.Me.IsAlive || StyxWoW.Me.Combat || RoutineManager.Current.NeedRest)
                return false;

            if (BotPoi.Current.Type == PoiType.None && LootTargeting.Instance.FirstObject != null)
                SetLootPoi(LootTargeting.Instance.FirstObject);

            if (!GarrisonsZonesId.Contains(Me.ZoneId))
            {
                if (GaBSettings.Mono.UseGarrisonHearthstone)
                {
                    WoWItem stone = Me.BagItems.FirstOrDefault(i => i.Entry == GarrisonHearthstone);
                    if (stone != null)
                    {
                        stone.Use();
                        await Buddy.Coroutines.Coroutine.Wait(60000, () => GarrisonsZonesId.Contains(Me.ZoneId));
                    }
                    else GarrisonBuddy.Warning("UseGarrisonHearthstone set to true but can't find it in bags.");
                }
                else
                {
                    GarrisonBuddy.Log("Character not in garrison and UseGarrisonHearthstone set to false, doing nothing.");
                    return false;
                }
                return true;
            }

            // Check mission every minute
            if ((DateTime.Now - NextCheck).TotalHours > 0)
            {
                NextCheck = DateTime.Now.AddMinutes(1);
                Check = true;
            }

            // Garrison Cache
            if (await PickUpGarrisonCache())
                return true;

            // Mine
            if (await CleanMine())
                return true;
            if (await PickUpMineWorkOrders())
                return true;

            // Garden 
            if (await CleanGarden())
                return true;

            if (await PickUpGardenWorkOrders())
                return true;

            // All other work orders => DOESNT WORK
            //if (await PickUpAllWorkOrders())
            //    return true;

            if (await ActivateFinishedBuildings())
                return true;

            // Missions
            if (await DoTurnInCompletedMissions())
                return true;

            if (await DoStartMissions())
                return true;

            //if(test)
            //if (await MoveTo(new WoWPoint(1948.035, 284.513, 88.96583))) // testing purpose
            //    return true; // testing purpose
            //test = false;
            //if (await MoveToTable()) // testing purpose
            //    return true; // testing purpose

            if (await Waiting())
                return true;

            return false;
        }

        private static async Task<bool> Waiting()
        {
            int TownHallLevel = BuildingsLua.GetTownHallLevel();
            if (TownHallLevel < 1)
                return false;

            List<WoWPoint> myFactionWaitingPoints;
            if (Me.IsAlliance)
                myFactionWaitingPoints = AllyWaitingPoints;
            else
                myFactionWaitingPoints = HordeWaitingPoints;

            if (myFactionWaitingPoints[TownHallLevel - 1] == new WoWPoint())
            {
                throw new NotImplementedException();
            }
            ;

            return await MoveTo(myFactionWaitingPoints[TownHallLevel - 1]);
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

        public static async Task<bool> DoTurnInCompletedMissions()
        {
            if (!GaBSettings.Mono.CompletedMissions)
                return false;

            // Is there mission to turn in?
            if (MissionLua.GetNumberCompletedMissions() == 0)
                return false;
            GarrisonBuddy.Log("Found " + MissionLua.GetNumberCompletedMissions() + "completed missions to turn in.");

            // are we at the action table?
            if (await MoveToTable())
                return true;

            MissionLua.TurnInAllCompletedMissions();
            RestoreCompletedMission = true;
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED");
            string missionId = args.Args[0].ToString();
            GarrisonBuddy.Diagnostic("LuaEvent: GARRISON_MISSION_STARTED - Removing from ToStart mission " + missionId);
            ToStart.RemoveAll(m => m.Key.MissionId == missionId);
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
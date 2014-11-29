using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using CommonBehaviors.Actions;
using GarrisonLua;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
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
        private static DateTime _pulseTimestamp;
        private static readonly WaitTimer AntiAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
        private static readonly WaitTimer HarvestingTimer = WaitTimer.OneSecond;
        private static WoWPoint _lastMoveTo;
        private static readonly WaitTimer MoveToLogTimer = WaitTimer.OneSecond;
        public static List<Building> Buildings;

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
                Buildings = BuildingsLua.GetAllBuildings();
                Check = true;

                InitializationMove();
            }
            catch (Exception e)
            {
                GarrisonButler.Err(e.ToString());
            }
        }

        internal static void OnStop()
        {
        }

        public static async Task<bool> RootLogic()
        {
            CheckPulseTime();
            AnitAfk();
            if (await RestoreUiIfNeeded())
                return true;

            if (await DeathBehavior.ExecuteCoroutine())
                return true;

            if (StyxWoW.Me.Combat && await CombatBehavior.ExecuteCoroutine())
            {
                return true;
            }

            if (await VendorBehavior.ExecuteCoroutine())
                return true;

            if (!StyxWoW.Me.IsAlive || StyxWoW.Me.Combat || RoutineManager.Current.NeedRest)
                return false;

            if (BotPoi.Current.Type == PoiType.None && LootTargeting.Instance.FirstObject != null)
                SetLootPoi(LootTargeting.Instance.FirstObject);

            if (await DoTurnInCompletedMissions())
                return true;

            if ((DateTime.Now - nextCheck).TotalHours > 0)
            {
                nextCheck = DateTime.Now.AddMinutes(1);
                Check = true;
            }

            if (await DoCheckAvailableMissions())
                return true;

            if(await DoStartMissions())
                return true;

            if(await PickUpGarrisonCache())
                return true;

            if (await CleanMine())
                return true;
            //if(test)
            //if (await MoveTo(new WoWPoint(1948.035, 284.513, 88.96583))) // testing purpose
            //    return true; // testing purpose
            //test = false;
            //if (await MoveToTable()) // testing purpose
            //    return true; // testing purpose

            return false;
        }

       private static bool test = true;

        public static DateTime nextCheck = DateTime.Now;
        public static async Task<bool> RestoreUiIfNeeded()
        {
            if (RestoreCompletedMission && MissionLua.GetNumberCompletedMissions() == 0)
            {
                GarrisonButler.Debug("RestoreUiIfNeeded RestoreCompletedMission called");
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
            // Is there mission to turn in?
            if (MissionLua.GetNumberCompletedMissions() == 0)
                return false;
            GarrisonButler.Log("Found " + MissionLua.GetNumberCompletedMissions() + "completed missions to turn in.");

            // are we at the action table?
            if (await MoveToTable())
                return true;

            MissionLua.TurnInAllCompletedMissions();
            RestoreCompletedMission = true;
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static List<KeyValuePair<Mission, Follower[]>> ToStart = new List<KeyValuePair<Mission, Follower[]>>();

        public static void GARRISON_MISSION_STARTED(object sender, LuaEventArgs args)
        {
            GarrisonButler.Debug("LuaEvent: GARRISON_MISSION_STARTED");
            string missionId = args.Args[0].ToString();
            GarrisonButler.Debug("LuaEvent: GARRISON_MISSION_STARTED - Removing from ToStart mission " + missionId);
            ToStart.RemoveAll(m => m.Key.MissionId == missionId);
        }

        public static async Task<bool> FlyTo(WoWPoint destination, string destinationName = null)
        {
            if (destination.DistanceSqr(_lastMoveTo) > 5*5)
            {
                if (MoveToLogTimer.IsFinished)
                {
                    if (string.IsNullOrEmpty(destinationName))
                        destinationName = destination.ToString();
                    //AutoAnglerBot.Log("Flying to {0}", destinationName);
                    MoveToLogTimer.Reset();
                }
                _lastMoveTo = destination;
            }
            Flightor.MoveTo(destination);
            return true;
        }

        public static async Task<bool> Logout()
        {
            WoWUnit activeMover = WoWMovement.ActiveMover;
            if (activeMover == null)
                return false;

            WoWItem hearthStone =
                Me.BagItems.FirstOrDefault(
                    h => h != null && h.IsValid && h.Entry == 6948
                         && h.CooldownTimeLeft == TimeSpan.FromMilliseconds(0));
            if (hearthStone == null)
            {
                //utoAnglerBot.Log("Unable to find a hearthstone");
                return false;
            }

            if (activeMover.IsMoving)
            {
                WoWMovement.MoveStop();
                //if (!await Coroutine.Wait(4000, () => !activeMover.IsMoving))
                return false;
            }

            hearthStone.UseContainerItem();
            //if (await Coroutine.Wait(15000, () => Me.Combat))
            //    return false;

            //AutoAnglerBot.Log("Logging out");
            Lua.DoString("Logout()");
            TreeRoot.Stop();
            return true;
        }

        private static void AnitAfk()
        {
            // keep the bot from going afk.
            if (AntiAfkTimer.IsFinished)
            {
                StyxWoW.ResetAfk();
                AntiAfkTimer.Reset();
            }
        }

        private static void CheckPulseTime()
        {
            if (_pulseTimestamp == DateTime.MinValue)
            {
                _pulseTimestamp = DateTime.Now;
                return;
            }

            TimeSpan pulseTime = DateTime.Now - _pulseTimestamp;
            if (pulseTime >= TimeSpan.FromSeconds(3))
            {
                //AutoAnglerBot.Err("Warning: It took {0} seconds to pulse.\nThis can cause missed bites. To fix try disabling all plugins",pulseTime.TotalSeconds);
            }
            _pulseTimestamp = DateTime.Now;
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

        //private static async Task<bool> CheckLootFrame()
        //{
        //    if (!LootTimer.IsFinished)
        //    {
        //        // loot everything.
        //        if (AutoAnglerBot.Instance.LootFrameIsOpen)
        //        {
        //            for (int i = 0; i < LootFrame.Instance.LootItems; i++)
        //            {
        //                LootSlotInfo lootInfo = LootFrame.Instance.LootInfo(i);
        //                if (AutoAnglerBot.Instance.FishCaught.ContainsKey(lootInfo.LootName))
        //                    AutoAnglerBot.Instance.FishCaught[lootInfo.LootName] += (uint)lootInfo.LootQuantity;
        //                else
        //                    AutoAnglerBot.Instance.FishCaught.Add(lootInfo.LootName, (uint)lootInfo.LootQuantity);
        //            }
        //            LootFrame.Instance.LootAll();
        //            LootTimer.Stop();
        //            await CommonCoroutines.SleepForLagDuration();
        //        }
        //        return true;
        //    }
        //    return false;
        //}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Grind;
using CommonBehaviors.Actions;
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

namespace GarrisonButler
{
    internal class Coroutine
    {
        private static Composite _deathBehavior;
        private static Composite _lootBehavior;
        private static Composite _combatBehavior;
        private static Composite _vendorBehavior;
        private static DateTime _pulseTimestamp;
        private static readonly WaitTimer AntiAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
        private static readonly WaitTimer LootTimer = WaitTimer.FiveSeconds;
        private static WoWPoint _lastMoveTo;
        private static readonly WaitTimer MoveToLogTimer = WaitTimer.OneSecond;
        public static List<Follower> Followers;
        public static List<Mission> Missions;
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
                Missions = GarrisonApi.GetAllAvailableMissions();
                Followers = GarrisonApi.GetAllFollowers();
                Buildings = GarrisonApi.GetAllBuildings();
                Check = true;
            }
            catch (Exception e)
            {
                GarrisonButler.Err(e.ToString());
            }
        }

        internal static void OnStop()
        {
            //Gear_OnStop();
        }

        public static async Task<bool> RootLogic()
        {
            CheckPulseTime();
            AnitAfk();
            if (await RestoreUiIfNeeded())
                return true;
            // Is bot dead? if so, release and run back to corpse
            if (await DeathBehavior.ExecuteCoroutine())
                return true;

            if (StyxWoW.Me.Combat && await CombatBehavior.ExecuteCoroutine())
            {
                // reset the autoBlacklist timer 
                //MoveToPoolTimer.Reset();
                return true;
            }

            if (await VendorBehavior.ExecuteCoroutine())
                return true;

            if (!StyxWoW.Me.IsAlive || StyxWoW.Me.Combat || RoutineManager.Current.NeedRest)
                return false;

            //if (BotPoi.Current.Type == PoiType.None && LootTargeting.Instance.FirstObject != null)
            //    SetLootPoi(LootTargeting.Instance.FirstObject);

            // Fishing Logic

            //if (await DoFishing())
            //    return true;

            //var poiGameObject = BotPoi.Current.AsObject as WoWGameObject;

            //// only loot when POI is not set to a fishing pool.
            //if (!StyxWoW.Me.IsFlying
            //    && (BotPoi.Current.Type != PoiType.Harvest
            //    || (poiGameObject != null && poiGameObject.SubType != WoWGameObjectType.FishingHole))
            //    && await LootBehavior.ExecuteCoroutine())
            //{
            //    return true;
            //}

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
            
            return false;
        }

        public static DateTime nextCheck = DateTime.Now;
        public static async Task<bool> RestoreUiIfNeeded()
        {
            if (RestoreCompletedMission && GarrisonApi.GetNumberCompletedMissions() == 0)
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
            //GarrisonButler.Debug("DoTurnInCompletedMissions");
            // Is there mission to turn in?
            if (GarrisonApi.GetNumberCompletedMissions() == 0)
                return false;
            GarrisonButler.Log("Found " + GarrisonApi.GetNumberCompletedMissions() + "completed missions to turn in.");

            // are we at the action table?
            if (await MoveToTable())
                return true;

            GarrisonApi.TurnInAllCompletedMissions();
            RestoreCompletedMission = true;
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }

        public static List<KeyValuePair<Mission, Follower[]>> ToStart = new List<KeyValuePair<Mission, Follower[]>>();
        

        public static bool Check = true;
        public static async Task<bool> DoCheckAvailableMissions()
        {
            //GarrisonButler.Debug("DoCheckAvailableMissions");
            if (!Check)
                return false;

            // Is there mission to turn in?
            if (GarrisonApi.GetNumberAvailableMissions() == 0)
                return false;
            GarrisonButler.Log("Found " + GarrisonApi.GetNumberAvailableMissions()  + " available missions to complete.");
            var tempFollowers = Followers.Select(x => x).ToList();
            var temp = new List<KeyValuePair<Mission, Follower[]>>();
            foreach (Mission mission in GarrisonApi.GetAllAvailableMissions())
            {
                Follower[] match =
                    mission.FindMatch(tempFollowers.Where(f => f.IsCollected && f.Status == "nil").ToList());
                if (match != null)
                {
                    GarrisonButler.Log("Found a match for mission: " + mission.MissionId + " - " + mission.Name);
                    temp.Add(new KeyValuePair<Mission, Follower[]>(mission, match));
                    foreach (string ability in mission.Enemies)
                    {
                        GarrisonButler.Log("    Ability: " + ability);
                    }
                    foreach (Follower follower in match)
                    {
                        GarrisonButler.Log("    Match: " + follower.FollowerId + " - " + follower.Name);
                        foreach (string ability in follower.Counters)
                        {
                            GarrisonButler.Log("        Ability: " + ability);
                        }
                    }
                    tempFollowers.RemoveAll(match.Contains);
                }
            }
            ToStart.AddRange(temp.Where(x => ToStart.All(y => y.Key.MissionId != x.Key.MissionId)));
            GarrisonButler.Log("Can succesfully complete: " + ToStart.Count + " missions.");
            Check = false;
            return true;
        }

        public static async Task<bool> DoStartMissions()
        {
            //GarrisonButler.Debug("DoStartMissions");
            if (ToStart.Count <= 0)
                return false;
            var match = ToStart.First();

            if (await MoveToTable())
                return true;

            if (!GarrisonApi.IsGarrisonMissionTabVisible())
            {
                GarrisonButler.Debug("Mission tab not visible, clicking.");
                GarrisonApi.ClickTabMission();
                return true;
            }
            if (!GarrisonApi.IsGarrisonMissionVisible())
            {
                GarrisonButler.Debug("Mission not visible, opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                GarrisonApi.OpenMission(match.Key);
                return true;

            }
            else if (!GarrisonApi.IsGarrisonMissionVisibleAndValid(match.Key.MissionId))
            {
                GarrisonButler.Debug("Mission not visible or not valid, close and then opening mission: " + match.Key.MissionId + " - " + match.Key.Name);
                GarrisonApi.ClickCloseMission();
                GarrisonApi.OpenMission(match.Key);
                return true;
            }
            //GarrisonButler.Debug("Wait for 1 seconds");
            //await Buddy.Coroutines.Coroutine.Sleep(1000);
            match.Key.AddFollowersToMission(match.Value.ToList());
            //GarrisonButler.Debug("Wait for 1 seconds");
            GarrisonApi.StartMission(match.Key.MissionId);
            GarrisonApi.ClickCloseMission();
            ToStart.Remove(match);
            await Buddy.Coroutines.Coroutine.Sleep(1500);
            return true;
        }
        public static async Task<bool> MoveToTable()
        {
            //move to table

            // TO DO

            //
            if (GarrisonApi.IsGarrisonMissionFrameOpen())
                return false;

            WoWObject table = GarrisonApi.GetCommandTableOrDefault();
            try
            {
                table.Interact();
                await CommonCoroutines.SleepForLagDuration();
            }
            catch (Exception e)
            {
                GarrisonButler.Err(e.ToString());
            }
            return true;
        }


        public static async Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
        {
            if (destination.DistanceSqr(_lastMoveTo) > 5*5)
            {
                if (MoveToLogTimer.IsFinished)
                {
                    if (string.IsNullOrEmpty(destinationName))
                        destinationName = destination.ToString();
                    //AutoAnglerBot.Log("Moving to {0}", destinationName);
                    MoveToLogTimer.Reset();
                }
                _lastMoveTo = destination;
            }
            MoveResult moveResult = Navigator.MoveTo(destination);
            return moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
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
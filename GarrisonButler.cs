using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using GarrisonLua;

namespace GarrisonBuddy
{
    public enum PathingType
    {
        Circle,
        Bounce
    }

    public class GarrisonButler : BotBase
    {
        private static WaitTimer _loadProfileTimer = new WaitTimer(TimeSpan.FromSeconds(1));
        private static DateTime _botStartTime;

        private static DateTime _startTime = DateTime.Now;
        private static DateTime _nextCheck = DateTime.Now;
        private static bool _missionNpcOpened = false;

        private static bool init;
        private static readonly List<Mission> CacheCompletedList = new List<Mission>();

        internal static readonly Version Version = new Version(0, 1);
        private readonly List<uint> _poolsToFish = new List<uint>();
        public List<Follower> Followers;
        public List<Mission> Missions;
        private PathingType _pathingType = PathingType.Circle;
        private string _prevProfilePath;

        public GarrisonButler()
        {
            Instance = this;
            BotEvents.Profile.OnNewOuterProfileLoaded += Profile_OnNewOuterProfileLoaded;
            Profile.OnUnknownProfileElement += Profile_OnUnknownProfileElement;
        }

        internal bool LootFrameIsOpen { get; private set; }

        internal bool ShouldFaceWaterNow { get; set; }

        internal Dictionary<string, uint> FishCaught { get; private set; }

        // internal AutoAnglerProfile Profile { get; private set; }
        internal static GarrisonButler Instance { get; private set; }


        private void GARRISON_MISSION_BONUS_ROLL_COMPLETE(object sender, LuaEventArgs args)
        {
            GarrisonButler.Debug("LuaEvent: GARRISON_MISSION_BONUS_ROLL_COMPLETE ");
            if (args.Args[1].ToString() == "nil")
            {
                GarrisonButler.Debug("GARRISON_MISSION_BONUS_ROLL_COMPLETE: Received a failure.");
            }
            else
            {
                if (args.Args[0] == null)
                {
                    GarrisonButler.Err("ERROR: Arg0 null in GARRISON_MISSION_BONUS_ROLL_COMPLETE");
                }
                else
                {
                    string idstring = args.Args[0].ToString();

                    Mission mission = CacheCompletedList.FirstOrDefault(m => m.MissionId == idstring);
                    if (mission != null)
                    {
                        mission.PrintCompletedMission();
                    }
                    else
                    {
                        GarrisonButler.Log("Uknown mission completed.");
                    }
                }
            }
        }

        private void GARRISON_MISSION_COMPLETE_RESPONSE(object sender, LuaEventArgs args)
        {
            GarrisonButler.Debug("LuaEvent: GARRISON_MISSION_COMPLETE_RESPONSE ");
            // Store the success of the mission
            if (args.Args[1].ToString() == "nil")
            {
                GarrisonButler.Debug("GARRISON_MISSION_COMPLETE_RESPONSE: Received a failure.");
            }
            else
            {
                if (args.Args[0] == null)
                {
                    GarrisonButler.Err("ERROR: Arg0 null in GARRISON_MISSION_COMPLETE_RESPONSE");
                }
                else
                {
                    string idstring = args.Args[0].ToString();
                    var canComplete = Lua.ParseLuaValue<Boolean>(args.Args[1].ToString());
                    var Success = Lua.ParseLuaValue<Boolean>(args.Args[1].ToString());

                    Mission mission = MissionLua.GetCompletedMissionById(args.Args[0].ToString());
                    // Update sucess
                    String lua =
                        "local cm = C_Garrison.GetCompleteMissions(); local RetInfo; local success;" +
                        String.Format(
                            "for idx = 1, #cm do " +
                            "if cm[idx].missionID == {0} then " +
                            "RetInfo = cm[idx].success;" +
                            "success = cm[idx].success;" +
                            "if cm[idx].success then missions[mission_index].state = 0 end;" +
                            "end;" +
                            "end;" +
                            "return tostring(success);", idstring);
                    mission.Success = Success;
                    if (Success)
                    {
                        CacheCompletedList.Add(mission);
                    }
                    else
                    {
                        mission.PrintCompletedMission();
                    }
                }
            }
        }

        private WoWSpell GetWoWSpellFromSpellCastFailedArgs(LuaEventArgs args)
        {
            if (args.Args.Length < 5)
                return null;
            return WoWSpell.FromId((int) ((double) args.Args[4]));
        }


        // scans bags for offhand weapon if mainhand isn't 2h and none are equipped and uses the highest ilvl one


        internal static void Log(string format, params object[] args)
        {
            Logging.Write(Colors.DodgerBlue, String.Format("[GarrisonButler] {0}: {1}", Version, format), args);
        }

        internal static void Err(string format, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("[GarrisonButler] {0}: {1}", Version, format), args);
        }

        internal static void Debug(string format, params object[] args)
        {
            Logging.WriteDiagnostic(Colors.DodgerBlue, String.Format("[GarrisonButler] {0}: {1}", Version, format), args);
        }

        private void DumpConfiguration()
        {
            //Debug("AvoidLava: {0}", AutoAnglerSettings.Instance.AvoidLava);
            //Debug("Fly: {0}", AutoAnglerSettings.Instance.Fly);
            //Debug("LootNPCs: {0}", AutoAnglerSettings.Instance.LootNPCs);

            //Debug("Hat Id: {0}", AutoAnglerSettings.Instance.Hat);
            //Debug("MainHand Id: {0}", AutoAnglerSettings.Instance.MainHand);
            //Debug("OffHand Id: {0}", AutoAnglerSettings.Instance.OffHand);

            //Debug("MaxFailedCasts: {0}", AutoAnglerSettings.Instance.MaxFailedCasts);
            //Debug("MaxTimeAtPool: {0}", AutoAnglerSettings.Instance.MaxTimeAtPool);
            //Debug("NinjaNodes: {0}", AutoAnglerSettings.Instance.NinjaNodes);
            //Debug("PathPrecision: {0}", AutoAnglerSettings.Instance.PathPrecision);
            //Debug("Poolfishing: {0}", AutoAnglerSettings.Instance.Poolfishing);
            //Debug("TraceStep: {0}", AutoAnglerSettings.Instance.TraceStep);
            //Debug("UseWaterWalking: {0}", AutoAnglerSettings.Instance.UseWaterWalking);

            //Debug("RandomGarrisonBaits: {0}", AutoAnglerSettings.Instance.RandomGarrisonBaits);
            //Debug("JawlessSkulkerBait: {0}", AutoAnglerSettings.Instance.JawlessSkulkerBait);
            //Debug("FatSleeperBait: {0}", AutoAnglerSettings.Instance.FatSleeperBait);
            //Debug("BlindLakeSturgeonBait: {0}", AutoAnglerSettings.Instance.BlindLakeSturgeonBait);
            //Debug("FireAmmoniteBait: {0}", AutoAnglerSettings.Instance.FireAmmoniteBait);
            //Debug("SeaScorpionBait: {0}", AutoAnglerSettings.Instance.SeaScorpionBait);
            //Debug("AbyssalGulperEelBaits: {0}", AutoAnglerSettings.Instance.AbyssalGulperEelBaits);
            //Debug("BlindLakeSturgeonBait: {0}", AutoAnglerSettings.Instance.BlindLakeSturgeonBait);
        }

        #region overrides

        private readonly InventoryType[] _2HWeaponTypes =
        {
            InventoryType.TwoHandWeapon,
            InventoryType.Ranged
        };

        private Composite _root;

        public override string Name
        {
            get { return "GarrisonButler"; }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All & (~PulseFlags.CharacterManager); }
        }

        public override Composite Root
        {
            get { return _root ?? (_root = new ActionRunCoroutine(ctx => Coroutine.RootLogic())); }
        }

        public override bool IsPrimaryType
        {
            get { return true; }
        }

        //public override Form ConfigurationForm
        //{
        //    get { return new MainForm(); }
        //}

        public override void Pulse()
        {
        }

        public override void Initialize()
        {
            try
            {
                //    WoWItem mainhand = (AutoAnglerSettings.Instance.MainHand != 0
                //        ? StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == AutoAnglerSettings.Instance.MainHand)
                //        : null) ?? FindMainHand();

                //    WoWItem offhand = AutoAnglerSettings.Instance.OffHand != 0
                //        ? StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == AutoAnglerSettings.Instance.OffHand)
                //        : null;

                //    if ((mainhand == null || !_2HWeaponTypes.Contains(mainhand.ItemInfo.InventoryType)) && offhand == null)
                //        offhand = FindOffhand();

                //    if (mainhand != null)
                //        Log("Using {0} for mainhand weapon", mainhand.Name);

                //    if (offhand != null)
                //        Log("Using {0} for offhand weapon", offhand.Name);


                //    _prevProfilePath = ProfileManager.XmlLocation;

                //    if (AutoAnglerSettings.Instance.Poolfishing && File.Exists(AutoAnglerSettings.Instance.LastLoadedProfile))
                //        ProfileManager.LoadNew(AutoAnglerSettings.Instance.LastLoadedProfile);
                //    else
                //        ProfileManager.LoadEmpty();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public override void Start()
        {
            DumpConfiguration();
            _botStartTime = DateTime.Now;
            FishCaught = new Dictionary<string, uint>();
            //LootTargeting.Instance.IncludeTargetsFilter += LootFilters.IncludeTargetsFilter;


            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            Lua.Events.AttachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);


            Lua.Events.AttachEvent("GARRISON_HIDE_LANDING_PAGE", GARRISON_HIDE_LANDING_PAGE);
            Lua.Events.AttachEvent("GARRISON_INVASION_AVAILABLE", GARRISON_INVASION_AVAILABLE);
            Lua.Events.AttachEvent("GARRISON_INVASION_UNAVAILABLE", GARRISON_INVASION_UNAVAILABLE);
            Lua.Events.AttachEvent("GARRISON_LANDINGPAGE_SHIPMENTS", GARRISON_LANDINGPAGE_SHIPMENTS);
            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_LOOT", GARRISON_MISSION_BONUS_ROLL_LOOT);
            Lua.Events.AttachEvent("GARRISON_MISSION_FINISHED", GARRISON_MISSION_FINISHED);
            Lua.Events.AttachEvent("GARRISON_MISSION_LIST_UPDATE", GARRISON_MISSION_LIST_UPDATE);
            Lua.Events.AttachEvent("GARRISON_MISSION_NPC_CLOSED", GARRISON_MISSION_NPC_CLOSED);
            Lua.Events.AttachEvent("GARRISON_MISSION_NPC_OPENED", GARRISON_MISSION_NPC_OPENED);
            Lua.Events.AttachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);
            Lua.Events.AttachEvent("GARRISON_MONUMENT_CLOSE_UI", GARRISON_MONUMENT_CLOSE_UI);
            Lua.Events.AttachEvent("GARRISON_MONUMENT_LIST_LOADED", GARRISON_MONUMENT_LIST_LOADED);
            Lua.Events.AttachEvent("GARRISON_MONUMENT_REPLACED", GARRISON_MONUMENT_REPLACED);
            Lua.Events.AttachEvent("GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED", GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED);
            Lua.Events.AttachEvent("GARRISON_MONUMENT_SHOW_UI", GARRISON_MONUMENT_SHOW_UI);
            Lua.Events.AttachEvent("GARRISON_RECALL_PORTAL_LAST_USED_TIME", GARRISON_RECALL_PORTAL_LAST_USED_TIME);
            Lua.Events.AttachEvent("GARRISON_RECALL_PORTAL_USED", GARRISON_RECALL_PORTAL_USED);
            Lua.Events.AttachEvent("GARRISON_RECRUITMENT_FOLLOWERS_GENERATED", GARRISON_RECRUITMENT_FOLLOWERS_GENERATED);
            Lua.Events.AttachEvent("GARRISON_RECRUITMENT_NPC_CLOSED", GARRISON_RECRUITMENT_NPC_CLOSED);
            Lua.Events.AttachEvent("GARRISON_RECRUITMENT_NPC_OPENED", GARRISON_RECRUITMENT_NPC_OPENED);
            Lua.Events.AttachEvent("GARRISON_RECRUITMENT_READY", GARRISON_RECRUITMENT_READY);
            Lua.Events.AttachEvent("GARRISON_RECRUIT_FOLLOWER_RESULT", GARRISON_RECRUIT_FOLLOWER_RESULT);
            Lua.Events.AttachEvent("GARRISON_SHOW_LANDING_PAGE", GARRISON_SHOW_LANDING_PAGE);
            Lua.Events.AttachEvent("GARRISON_TRADESKILL_NPC_CLOSED", GARRISON_TRADESKILL_NPC_CLOSED);
            Lua.Events.AttachEvent("GARRISON_UPDATE", GARRISON_UPDATE);

            Lua.Events.AttachEvent("LOOT_OPENED", LootFrameOpenedHandler);
            Lua.Events.AttachEvent("LOOT_CLOSED", LootFrameClosedHandler);
            Lua.Events.AttachEvent("UNIT_SPELLCAST_FAILED", UnitSpellCastFailedHandler);

            Coroutine.OnStart();
        }




        private void GARRISON_HIDE_LANDING_PAGE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_HIDE_LANDING_PAGE ");
        }

        private void GARRISON_INVASION_AVAILABLE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_INVASION_AVAILABLE ");
        }

        private void GARRISON_INVASION_UNAVAILABLE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_INVASION_UNAVAILABLE ");
        }

        private void GARRISON_LANDINGPAGE_SHIPMENTS(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_LANDINGPAGE_SHIPMENTS ");
        }

        private void GARRISON_MISSION_BONUS_ROLL_LOOT(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MISSION_BONUS_ROLL_LOOT ");
        }


        private void GARRISON_MISSION_FINISHED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MISSION_FINISHED ");
        }

        private void GARRISON_MISSION_LIST_UPDATE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MISSION_LIST_UPDATE ");
        }

        private void GARRISON_MISSION_NPC_CLOSED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MISSION_NPC_CLOSED ");
        }

        private void GARRISON_MISSION_NPC_OPENED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MISSION_NPC_OPENED ");
        }


        private void GARRISON_MONUMENT_CLOSE_UI(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MONUMENT_CLOSE_UI ");
        }

        private void GARRISON_MONUMENT_LIST_LOADED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MONUMENT_LIST_LOADED ");
        }

        private void GARRISON_MONUMENT_REPLACED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MONUMENT_REPLACED ");
        }

        private void GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED ");
        }

        private void GARRISON_MONUMENT_SHOW_UI(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_MONUMENT_SHOW_UI ");
        }

        private void GARRISON_RECALL_PORTAL_LAST_USED_TIME(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECALL_PORTAL_LAST_USED_TIME ");
        }

        private void GARRISON_RECALL_PORTAL_USED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECALL_PORTAL_USED ");
        }

        private void GARRISON_RECRUITMENT_FOLLOWERS_GENERATED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECRUITMENT_FOLLOWERS_GENERATED ");
        }

        private void GARRISON_RECRUITMENT_NPC_CLOSED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECRUITMENT_NPC_CLOSED ");
        }

        private void GARRISON_RECRUITMENT_NPC_OPENED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECRUITMENT_NPC_OPENED ");
        }

        private void GARRISON_RECRUITMENT_READY(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECRUITMENT_READY ");
        }

        private void GARRISON_RECRUIT_FOLLOWER_RESULT(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_RECRUIT_FOLLOWER_RESULT ");
        }

        private void GARRISON_SHOW_LANDING_PAGE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_SHOW_LANDING_PAGE ");
        }

        private void GARRISON_TRADESKILL_NPC_CLOSED(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_TRADESKILL_NPC_CLOSED ");
        }

        private void GARRISON_UPDATE(object sender, LuaEventArgs args)
        {
            Logging.Write("LuaEvent: GARRISON_UPDATE ");
        }












        public override void Stop()
        {
            Coroutine.OnStop();

            Log("In {0} days, {1} hours and {2} minutes we have caught",
                (DateTime.Now - _botStartTime).Days,
                (DateTime.Now - _botStartTime).Hours,
                (DateTime.Now - _botStartTime).Minutes);

            foreach (var kv in FishCaught)
            {
                Log("{0} x{1}", kv.Key, kv.Value);
            }

            //LootTargeting.Instance.IncludeTargetsFilter -= LootFilters.IncludeTargetsFilter;
            Lua.Events.DetachEvent("LOOT_OPENED", LootFrameOpenedHandler);
            Lua.Events.DetachEvent("LOOT_CLOSED", LootFrameClosedHandler);
            Lua.Events.DetachEvent("UNIT_SPELLCAST_FAILED", UnitSpellCastFailedHandler);
            Lua.Events.DetachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            Lua.Events.DetachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);

            if (!string.IsNullOrEmpty(_prevProfilePath) && File.Exists(_prevProfilePath))
                ProfileManager.LoadNew(_prevProfilePath);
        }

        #endregion

        #region Handlers

        private void LootFrameClosedHandler(object sender, LuaEventArgs args)
        {
            LootFrameIsOpen = false;
        }

        private void LootFrameOpenedHandler(object sender, LuaEventArgs args)
        {
            LootFrameIsOpen = true;
        }

        private void UnitSpellCastFailedHandler(object sender, LuaEventArgs args)
        {
            WoWSpell spell = GetWoWSpellFromSpellCastFailedArgs(args);
            if (spell != null && spell.IsValid && spell.Name == "Fishing")
                ShouldFaceWaterNow = true;
        }

        #endregion

        #region Profile

        private void Profile_OnNewOuterProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            try
            {
                //Profile = new AutoAnglerProfile(args.NewProfile, _pathingType, _poolsToFish);
                //if (!string.IsNullOrEmpty(ProfileManager.XmlLocation))
                //{
                //    AutoAnglerSettings.Instance.LastLoadedProfile = ProfileManager.XmlLocation;
                //    AutoAnglerSettings.Instance.Save();
                //}
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public void Profile_OnUnknownProfileElement(object sender, UnknownProfileElementEventArgs e)
        {
            //// hackish way to set variables to default states before loading new profile... wtb OnNewOuterProfileLoading event
            //if (_loadProfileTimer.IsFinished)
            //{
            //    _poolsToFish.Clear();
            //    _pathingType = PathingType.Circle;
            //    _loadProfileTimer.Reset();
            //}

            //if (e.Element.Name == "FishingSchool")
            //{
            //    XAttribute entryAttrib = e.Element.Attribute("Entry");
            //    if (entryAttrib != null)
            //    {
            //        uint entry;
            //        UInt32.TryParse(entryAttrib.Value, out entry);
            //        if (!_poolsToFish.Contains(entry))
            //        {
            //            _poolsToFish.Add(entry);
            //            XAttribute nameAttrib = e.Element.Attribute("Name");
            //            if (nameAttrib != null)
            //                Log("Adding Pool Entry: {0} to the list of pools to fish from", nameAttrib.Value);
            //            else
            //                Log("Adding Pool Entry: {0} to the list of pools to fish from", entry);
            //        }
            //    }
            //    else
            //    {
            //        Err(
            //            "<FishingSchool> tag must have the 'Entry' Attribute, e.g <FishingSchool Entry=\"202780\"/>\nAlso supports 'Name' attribute but only used for display purposes");
            //    }
            //    e.Handled = true;
            //}
            //else if (e.Element.Name == "Pathing")
            //{
            //    XAttribute typeAttrib = e.Element.Attribute("Type");
            //    if (typeAttrib != null)
            //    {
            //        _pathingType = (PathingType)
            //            Enum.Parse(typeof(PathingType), typeAttrib.Value, true);

            //        Log("Setting Pathing Type to {0} Mode", _pathingType);
            //    }
            //    else
            //    {
            //        Err(
            //            "<Pathing> tag must have the 'Type' Attribute, e.g <Pathing Type=\"Circle\"/>");
            //    }
            //    e.Handled = true;
            //}
        }

        #endregion
    }
}
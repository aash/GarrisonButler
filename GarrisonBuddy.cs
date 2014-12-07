using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using CommonBehaviors.Actions;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    public class GarrisonBuddy : BotBase
    {
        internal static readonly Version Version = new Version(0, 5, 2);
        internal static List<Follower> Followers;
        internal static List<Mission> Missions;
        internal static readonly List<Mission> CacheCompletedList = new List<Mission>();

        public GarrisonBuddy()
        {
            Instance = this;
        }

        // internal AutoAnglerProfile Profile { get; private set; }
        internal static GarrisonBuddy Instance { get; private set; }


        private void GARRISON_MISSION_BONUS_ROLL_COMPLETE(object sender, LuaEventArgs args)
        {
            Diagnostic("LuaEvent: GARRISON_MISSION_BONUS_ROLL_COMPLETE ");
            if (args.Args[1].ToString() == "nil")
            {
                Diagnostic("GARRISON_MISSION_BONUS_ROLL_COMPLETE: Received a failure.");
            }
            else
            {
                if (args.Args[0] == null)
                {
                    Warning("ERROR: Arg0 null in GARRISON_MISSION_BONUS_ROLL_COMPLETE");
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
                        Log("Unknown mission completed.");
                    }
                }
            }
        }

        private void GARRISON_MISSION_COMPLETE_RESPONSE(object sender, LuaEventArgs args)
        {
            Diagnostic("LuaEvent: GARRISON_MISSION_COMPLETE_RESPONSE ");
            // Store the success of the mission
            if (args.Args[1].ToString() == "nil")
            {
                Diagnostic("GARRISON_MISSION_COMPLETE_RESPONSE: Received a failure.");
            }
            else
            {
                if (args.Args[0] == null)
                {
                    Warning("ERROR: Arg0 null in GARRISON_MISSION_COMPLETE_RESPONSE");
                }
                else
                {
                    var success = Lua.ParseLuaValue<Boolean>(args.Args[1].ToString());

                    Mission mission = MissionLua.GetCompletedMissionById(args.Args[0].ToString());
                    mission.Success = success;
                    if (success)
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

        private static String LogBak = "";
        private static WaitTimer logTimer=new WaitTimer(TimeSpan.FromSeconds(5));
        internal static void Log(string message, params object[] args)
        {
            var messFormat = String.Format("[GarrisonBuddy] {0}: {1}", Version, message);
            if (LogBak == messFormat && !logTimer.IsFinished) return;

            Logging.Write(Colors.DeepSkyBlue, messFormat, args);
            LogBak = messFormat;
            logTimer.Reset();
        }

        internal static void Warning(string message, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("[GarrisonBuddy] {0}: {1}", Version, message), args);
        }

        internal static void Diagnostic(string message, params object[] args)
        {
            Logging.WriteDiagnostic(Colors.DeepPink, String.Format("[GarrisonBuddy] {0}: {1}", Version, message), args);
        }

        #region overrides

        internal static bool LootIsOpen = false;
        private Composite _root;

        public override string Name
        {
            get { return "GarrisonBuddy"; }
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
            get { return false; }
        }

        private DateTime lastRunTime = DateTime.MinValue;
        public override bool RequirementsMet
        {
            get
            {
                TimeSpan timeElapsed = DateTime.Now - lastRunTime;
                if (timeElapsed.TotalMinutes > GaBSettings.Mono.TimeMinBetweenRun)
                {
                    var anyToDo = Coroutine.AnythingTodo();
                    if (anyToDo)
                    {
                        lastRunTime = DateTime.Now;
                        Coroutine.ReadyToSwitch = false;
                    }
                    return anyToDo;
                }
                return false;
            }
        }

        public override Form ConfigurationForm
        {
            get { return new ConfigForm(); }
        }

        public override void Pulse()
        {
        }

        public override void Initialize()
        {
        }

        public override void Start()
        {
            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            Lua.Events.AttachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);
            //Lua.Events.AttachEvent("GARRISON_HIDE_LANDING_PAGE", GARRISON_HIDE_LANDING_PAGE);
            //Lua.Events.AttachEvent("GARRISON_INVASION_AVAILABLE", GARRISON_INVASION_AVAILABLE);
            //Lua.Events.AttachEvent("GARRISON_INVASION_UNAVAILABLE", GARRISON_INVASION_UNAVAILABLE);
            //Lua.Events.AttachEvent("GARRISON_LANDINGPAGE_SHIPMENTS", GARRISON_LANDINGPAGE_SHIPMENTS);
            //Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_LOOT", GARRISON_MISSION_BONUS_ROLL_LOOT);
            //Lua.Events.AttachEvent("GARRISON_MISSION_FINISHED", GARRISON_MISSION_FINISHED);
            //Lua.Events.AttachEvent("GARRISON_MISSION_LIST_UPDATE", GARRISON_MISSION_LIST_UPDATE);
            //Lua.Events.AttachEvent("GARRISON_MISSION_NPC_CLOSED", GARRISON_MISSION_NPC_CLOSED);
            //Lua.Events.AttachEvent("GARRISON_MISSION_NPC_OPENED", GARRISON_MISSION_NPC_OPENED);
            Lua.Events.AttachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);
            //Lua.Events.AttachEvent("GARRISON_MONUMENT_CLOSE_UI", GARRISON_MONUMENT_CLOSE_UI);
            //Lua.Events.AttachEvent("GARRISON_MONUMENT_LIST_LOADED", GARRISON_MONUMENT_LIST_LOADED);
            //Lua.Events.AttachEvent("GARRISON_MONUMENT_REPLACED", GARRISON_MONUMENT_REPLACED);
            //Lua.Events.AttachEvent("GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED",
            //    GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED);
            //Lua.Events.AttachEvent("GARRISON_MONUMENT_SHOW_UI", GARRISON_MONUMENT_SHOW_UI);
            //Lua.Events.AttachEvent("GARRISON_RECALL_PORTAL_LAST_USED_TIME", GARRISON_RECALL_PORTAL_LAST_USED_TIME);
            //Lua.Events.AttachEvent("GARRISON_RECALL_PORTAL_USED", GARRISON_RECALL_PORTAL_USED);
            //Lua.Events.AttachEvent("GARRISON_RECRUITMENT_FOLLOWERS_GENERATED", GARRISON_RECRUITMENT_FOLLOWERS_GENERATED);
            //Lua.Events.AttachEvent("GARRISON_RECRUITMENT_NPC_CLOSED", GARRISON_RECRUITMENT_NPC_CLOSED);
            //Lua.Events.AttachEvent("GARRISON_RECRUITMENT_NPC_OPENED", GARRISON_RECRUITMENT_NPC_OPENED);
            //Lua.Events.AttachEvent("GARRISON_RECRUITMENT_READY", GARRISON_RECRUITMENT_READY);
            //Lua.Events.AttachEvent("GARRISON_RECRUIT_FOLLOWER_RESULT", GARRISON_RECRUIT_FOLLOWER_RESULT);
            //Lua.Events.AttachEvent("GARRISON_SHOW_LANDING_PAGE", GARRISON_SHOW_LANDING_PAGE);
            //Lua.Events.AttachEvent("GARRISON_TRADESKILL_NPC_CLOSED", GARRISON_TRADESKILL_NPC_CLOSED);
            //Lua.Events.AttachEvent("GARRISON_UPDATE", GARRISON_UPDATE);
            Lua.Events.AttachEvent("LOOT_OPENED", LootOpened);
            Lua.Events.AttachEvent("LOOT_CLOSED", LootClosed);
            Coroutine.InitializeCoroutines();
            Coroutine.OnStart();
        }

        private static void LootClosed(object sender, LuaEventArgs args)
        {
            LootIsOpen = false;
        }

        private static void LootOpened(object sender, LuaEventArgs args)
        {
            LootIsOpen = true;
        }

        #endregion

        #region Events

        //private void GARRISON_HIDE_LANDING_PAGE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_HIDE_LANDING_PAGE ");
        //}

        //private void GARRISON_INVASION_AVAILABLE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_INVASION_AVAILABLE ");
        //}

        //private void GARRISON_INVASION_UNAVAILABLE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_INVASION_UNAVAILABLE ");
        //}

        //private void GARRISON_LANDINGPAGE_SHIPMENTS(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_LANDINGPAGE_SHIPMENTS ");
        //}

        //private void GARRISON_MISSION_BONUS_ROLL_LOOT(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MISSION_BONUS_ROLL_LOOT ");
        //}


        //private void GARRISON_MISSION_FINISHED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MISSION_FINISHED ");
        //}

        //private void GARRISON_MISSION_LIST_UPDATE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MISSION_LIST_UPDATE ");
        //}

        //private void GARRISON_MISSION_NPC_CLOSED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MISSION_NPC_CLOSED ");
        //}

        //private void GARRISON_MISSION_NPC_OPENED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MISSION_NPC_OPENED ");
        //}


        //private void GARRISON_MONUMENT_CLOSE_UI(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MONUMENT_CLOSE_UI ");
        //}

        //private void GARRISON_MONUMENT_LIST_LOADED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MONUMENT_LIST_LOADED ");
        //}

        //private void GARRISON_MONUMENT_REPLACED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MONUMENT_REPLACED ");
        //}

        //private void GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MONUMENT_SELECTED_TROPHY_ID_LOADED ");
        //}

        //private void GARRISON_MONUMENT_SHOW_UI(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_MONUMENT_SHOW_UI ");
        //}

        //private void GARRISON_RECALL_PORTAL_LAST_USED_TIME(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECALL_PORTAL_LAST_USED_TIME ");
        //}

        //private void GARRISON_RECALL_PORTAL_USED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECALL_PORTAL_USED ");
        //}

        //private void GARRISON_RECRUITMENT_FOLLOWERS_GENERATED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECRUITMENT_FOLLOWERS_GENERATED ");
        //}

        //private void GARRISON_RECRUITMENT_NPC_CLOSED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECRUITMENT_NPC_CLOSED ");
        //}

        //private void GARRISON_RECRUITMENT_NPC_OPENED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECRUITMENT_NPC_OPENED ");
        //}

        //private void GARRISON_RECRUITMENT_READY(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECRUITMENT_READY ");
        //}

        //private void GARRISON_RECRUIT_FOLLOWER_RESULT(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_RECRUIT_FOLLOWER_RESULT ");
        //}

        //private void GARRISON_SHOW_LANDING_PAGE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_SHOW_LANDING_PAGE ");
        //}

        //private void GARRISON_TRADESKILL_NPC_CLOSED(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_TRADESKILL_NPC_CLOSED ");
        //}

        //private void GARRISON_UPDATE(object sender, LuaEventArgs args)
        //{
        //    Diagnostic("LuaEvent: GARRISON_UPDATE ");
        //}


        public override void Stop()
        {
            Coroutine.OnStop();
            Lua.Events.DetachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            Lua.Events.DetachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);
        }

        #endregion
    }
}
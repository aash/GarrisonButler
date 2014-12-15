using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using CommonBehaviors.Actions;
using GarrisonBuddy.Config;
using GarrisonBuddy.Libraries;
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
        internal static readonly ModuleVersion Version = new ModuleVersion(0, 7, 0);
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

            Logging.Write(Colors.LightSeaGreen, messFormat, args);
            LogBak = messFormat;
            logTimer.Reset();
        }

        internal static void Warning(string message, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("[GarrisonBuddy] {0}: {1}", Version, message), args);
        }

        internal static void Diagnostic(string message, params object[] args)
        {
            Logging.WriteDiagnostic(Colors.Orange, String.Format("[GarrisonBuddy] {0}: {1}", Version, message), args);
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
                if (timeElapsed.TotalMinutes > GaBSettings.Get().TimeMinBetweenRun)
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


        public override void Initialize()
        {
            // Loading configuration from file or default
            GaBSettings.Load();

            GarrisonBuddy.Diagnostic("Attaching to GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);

            GarrisonBuddy.Diagnostic("Attaching to GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.AttachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);

            GarrisonBuddy.Diagnostic("Attaching to GARRISON_MISSION_STARTED");
            Lua.Events.AttachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);

            GarrisonBuddy.Diagnostic("Attaching to LOOT_OPENED");
            Lua.Events.AttachEvent("LOOT_OPENED", LootOpened);

            GarrisonBuddy.Diagnostic("Attaching to LOOT_CLOSED");
            Lua.Events.AttachEvent("LOOT_CLOSED", LootClosed);
        }

        public override void OnDeselected()
        {
            GarrisonBuddy.Diagnostic("Detaching from GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.DetachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            GarrisonBuddy.Diagnostic("Detaching from GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.DetachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);
            GarrisonBuddy.Diagnostic("Detaching from GARRISON_MISSION_STARTED");
            Lua.Events.DetachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);
            GarrisonBuddy.Diagnostic("Detaching from LOOT_OPENED");
            Lua.Events.DetachEvent("LOOT_OPENED", LootOpened);
            GarrisonBuddy.Diagnostic("Detaching from LOOT_CLOSED");
            Lua.Events.DetachEvent("LOOT_CLOSED", LootClosed);
            base.OnDeselected();
        }

        public override void Start()
        {
            try
            {
                GarrisonBuddy.Diagnostic("Coroutine OnStart");
                Coroutine.OnStart();
            }
            catch (Exception e)
            {

                GarrisonBuddy.Diagnostic(e.ToString());
            }
        }

        public override void Stop()
        {
            Coroutine.OnStop();
        }

        #endregion
        private static void LootClosed(object sender, LuaEventArgs args)
        {
            LootIsOpen = false;
        }

        private static void LootOpened(object sender, LuaEventArgs args)
        {
            LootIsOpen = true;
        }
    }
}
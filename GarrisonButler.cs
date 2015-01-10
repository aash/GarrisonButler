#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using CommonBehaviors.Actions;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler
{
    public class GarrisonButler : BotBase
    {
        internal static readonly ModuleVersion Version = new ModuleVersion(1, 3, 30, 0);

        internal static List<Follower> Followers;
        internal static List<Mission> Missions;
        internal static readonly List<Mission> CacheCompletedList = new List<Mission>();
        private static String LogBak = "";
        private static readonly WaitTimer logTimer = new WaitTimer(TimeSpan.FromSeconds(5));
        public static StyxLog CurrentHonorbuddyLog = default(StyxLog);

        public GarrisonButler()
        {
            Instance = this;

            CurrentHonorbuddyLog = StyxLog.GetLogs().GetEmptyIfNull().FirstOrDefault();

            //IEnumerable<StyxLog> logs = StyxLog.GetLogs();

            //foreach (StyxLog curLog in logs)
            //{
            //    System.Windows.MessageBox.Show(curLog.LogFilePath);
            //}
        }

        public static string NameStatic
        {
            get { return "GarrisonButler ICE"; }
        }

        // internal AutoAnglerProfile Profile { get; private set; }
        internal static GarrisonButler Instance { get; private set; }


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

        internal static void Log(string message, params object[] args)
        {
            string messFormat = String.Format("[{0}] {1}: {2}", NameStatic, Version, message);
            Logging.Write(Colors.LightSeaGreen, messFormat, args);
        }

        internal static void Warning(string message, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("[{0}] {1}: {2}", NameStatic, Version, message), args);
        }

        // SortedList
        //  key = function name
        //  value = List of messages
        //private static string[,] DiagnosticSlots = new string[100, 100];

        internal static void Diagnostic(string message, params object[] args)
        {
            //string callingFunction = new System.Diagnostics.StackFrame(1, true).GetMethod().Name;
            //string toLog = String.Format(String.Format("[{0}] {1}: {2}", NameStatic, Version, message), args);

            //for (int i = 0; i < DiagnosticSlots.GetLength(0); i++)
            //{
            //    for(int j = 0; j < DiagnosticSlots.GetLength(1); j++)
            //    {

            //    }
            //}

            Logging.WriteDiagnostic(Colors.Orange, String.Format("[{0}] {1}: {2}", NameStatic, Version, message), args);
        }

        private static void LootClosed(object sender, LuaEventArgs args)
        {
            LootIsOpen = false;
        }

        private static void LootOpened(object sender, LuaEventArgs args)
        {
            LootIsOpen = true;
        }

        #region overrides

        internal static bool LootIsOpen = false;
        private Composite _root;
        private DateTime lastRunTime = DateTime.MinValue;

        public override string Name
        {
            get { return NameStatic; }
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

        public override bool RequirementsMet
        {
            get
            {
                TimeSpan timeElapsed = DateTime.Now - lastRunTime;
                if (timeElapsed.TotalMinutes > GaBSettings.Get().TimeMinBetweenRun)
                {
                    bool anyToDo = Coroutine.AnythingTodo();
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

            Diagnostic("Attaching to GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);

            Diagnostic("Attaching to GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.AttachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);

            Diagnostic("Attaching to GARRISON_MISSION_STARTED");
            Lua.Events.AttachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);

            Diagnostic("Attaching to LOOT_OPENED");
            Lua.Events.AttachEvent("LOOT_OPENED", LootOpened);

            Diagnostic("Attaching to LOOT_CLOSED");
            Lua.Events.AttachEvent("LOOT_CLOSED", LootClosed);
        }

        public override void OnDeselected()
        {
            Diagnostic("Detaching from GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.DetachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);
            Diagnostic("Detaching from GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.DetachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);
            Diagnostic("Detaching from GARRISON_MISSION_STARTED");
            Lua.Events.DetachEvent("GARRISON_MISSION_STARTED", Coroutine.GARRISON_MISSION_STARTED);
            Diagnostic("Detaching from LOOT_OPENED");
            Lua.Events.DetachEvent("LOOT_OPENED", LootOpened);
            Diagnostic("Detaching from LOOT_CLOSED");
            Lua.Events.DetachEvent("LOOT_CLOSED", LootClosed);
            base.OnDeselected();
        }

        public override void Start()
        {
            try
            {
                Diagnostic("Coroutine OnStart");
                Coroutine.OnStart();
            }
            catch (Exception e)
            {
                Diagnostic(e.ToString());
            }
        }

        public override void Stop()
        {
            Coroutine.OnStop();
        }

        #endregion
    }
}
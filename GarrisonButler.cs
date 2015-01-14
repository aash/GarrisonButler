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
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler
{
    public class GarrisonButler : BotBase
    {
        internal static readonly ModuleVersion Version = new ModuleVersion(1, 4, 0, 19);

        internal static List<Follower> Followers;
        internal static List<Mission> Missions;
        internal static readonly List<Mission> CacheCompletedList = new List<Mission>();
        public static StyxLog CurrentHonorbuddyLog = default(StyxLog);

        public GarrisonButler()
        {
            Instance = this;
            CurrentHonorbuddyLog = StyxLog.GetLogs().GetEmptyIfNull().FirstOrDefault();
        }

        public static string NameStatic
        {
            get { return "GarrisonButler ICE"; }
        }

        // internal AutoAnglerProfile Profile { get; private set; }
        internal static GarrisonButler Instance { get; private set; }


        private static void GARRISON_MISSION_BONUS_ROLL_COMPLETE(object sender, LuaEventArgs args)
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
                    var idstring = args.Args[0].ToString();

                    var mission = CacheCompletedList.FirstOrDefault(m => m.MissionId == idstring);
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

        private static void GARRISON_MISSION_COMPLETE_RESPONSE(object sender, LuaEventArgs args)
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

                    var mission = MissionLua.GetCompletedMissionById(args.Args[0].ToString());
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
            var messFormat = String.Format("[{0}] {1}: {2}", NameStatic, Version, message);
            Logging.Write(Colors.LightSeaGreen, messFormat, args);
        }

        internal static void Warning(string message, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("[{0}] {1}: {2}", NameStatic, Version, message), args);
        }

        internal static void DiagnosticLogTimeTaken(string activity, DateTime startedAt)
        {
            DiagnosticLogTimeTaken(activity, (DateTime.Now - startedAt).TotalMilliseconds);
        }

        internal static void DiagnosticLogTimeTaken(string activity, int elapsedTimeInMs)
        {
            DiagnosticLogTimeTaken(activity, (double) elapsedTimeInMs);
        }

        internal static void DiagnosticLogTimeTaken(string activity, double elapsedTimeInMs)
        {
            DiagnosticLogTimeTaken(activity, TimeSpan.FromMilliseconds(elapsedTimeInMs));
        }

        internal static void DiagnosticLogTimeTaken(string activity, TimeSpan timeTaken)
        {
            var formattedTime = String.Format("{0:mm\\:ss\\:fff}", timeTaken);
            var count = formattedTime.Count(c => c == ':');

            if (count == 2)
            {
                var firstIndex = formattedTime.IndexOf(':');

                formattedTime = formattedTime.Substring(0, firstIndex)
                                + "m:"
                                + formattedTime.Substring(firstIndex + 1);

                var lastIndex = formattedTime.LastIndexOf(':');

                formattedTime = formattedTime.Substring(0, lastIndex)
                                + "s:"
                                + formattedTime.Substring(lastIndex + 1, formattedTime.Length - lastIndex - 1);

                formattedTime += "ms";
            }

            Diagnostic(activity + " took " + formattedTime);
        }

        internal static void Diagnostic(string message, params object[] args)
        {
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

        internal static bool LootIsOpen;
        private Composite _root;
        public DateTime _lastRunTime = DateTime.MinValue;

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

        /// <summary>
        /// Returns false in 2 conditions - #1 time is less than 60s from last run or #2 nothing to do
        /// </summary>
        public override bool RequirementsMet
        {
            get
            {
                if (Coroutine.ReadyToSwitch)
                {
                    var timeElapsed = DateTime.Now - _lastRunTime;
                    if (!(timeElapsed.TotalSeconds > GaBSettings.Get().TimeMinBetweenRun)) return false;
                    _lastRunTime = DateTime.Now;
                    int timeBetweenRuns = GaBSettings.Get().TimeMinBetweenRun;
                    uint remainingTime = GarrisonButler.Instance._lastRunTime == DateTime.MinValue
                        ? (uint)timeBetweenRuns
                        : (uint)(timeBetweenRuns - (DateTime.Now - GarrisonButler.Instance._lastRunTime).TotalSeconds);

                    GarrisonButler.Log("One more check and then taking a break for {0}s", timeBetweenRuns);
                }

                var anyToDo = Coroutine.AnythingTodo();
                if (!anyToDo) return false;
                Coroutine.ReadyToSwitch = false;
                return true;
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
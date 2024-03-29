﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Coroutines;
using CommonBehaviors.Actions;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using GarrisonButler.LuaObjects;
using Styx.Common;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals;
using LuaEvents = GarrisonButler.LuaObjects.LuaEvents;

#endregion

namespace GarrisonButler
{
    public class GarrisonButler : BotBase
    {
        internal static readonly ModuleVersion Version = new ModuleVersion(2, 7, 15, 1);

        internal static List<Follower> Followers;
        internal static List<Mission> Missions;
        internal static readonly List<Mission> CacheCompletedList = new List<Mission>();
        public static StyxLog CurrentHonorbuddyLog = default(StyxLog);

        public GarrisonButler()
        {
            CurrentHonorbuddyLog = StyxLog.GetLogs().GetEmptyIfNull().FirstOrDefault();
        }

        public static string NameStatic
        {
            get { return "GarrisonButler ICE"; }
        }

        internal static bool IsIceVersion()
        {
            return NameStatic.ToLower().Contains("ice");
        }

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

      

        #region overrides
        private Composite _root;
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
            get { return _root ?? (_root = new ActionRunCoroutine(ctx => ButlerCoroutine.RootLogic())); }
        }

        public override bool IsPrimaryType
        {
            get { return false; }
        }

        public override bool RequirementsMet
        {
            get
            {
                return true;
            }
        }

        public override Form ConfigurationForm
        {
            get { return new ConfigForm(); }
        }


        public override void Initialize()
        {
            Diagnostic("Attaching to GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.AttachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);

            Diagnostic("Attaching to GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.AttachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);

            Diagnostic("Attaching to GARRISON_MISSION_STARTED");
            Lua.Events.AttachEvent("GARRISON_MISSION_STARTED", ButlerCoroutine.GARRISON_MISSION_STARTED);

            Diagnostic("Attaching to LOOT_OPENED");
            Lua.Events.AttachEvent("LOOT_OPENED", LuaEvents.LootOpened);

            Diagnostic("Attaching to LOOT_CLOSED");
            Lua.Events.AttachEvent("LOOT_CLOSED", LuaEvents.LootClosed);

            Diagnostic("Attaching to SHIPMENT_CRAFTER_INFO");
            Lua.Events.AttachEvent("SHIPMENT_CRAFTER_INFO", ButlerCoroutine.SHIPMENT_CRAFTER_INFO);

            CapacitiveDisplayFrame.Initialize();
        }

        public override void OnDeselected()
        {
            Diagnostic("Detaching from GARRISON_MISSION_BONUS_ROLL_COMPLETE");
            Lua.Events.DetachEvent("GARRISON_MISSION_BONUS_ROLL_COMPLETE", GARRISON_MISSION_BONUS_ROLL_COMPLETE);

            Diagnostic("Detaching from GARRISON_MISSION_COMPLETE_RESPONSE");
            Lua.Events.DetachEvent("GARRISON_MISSION_COMPLETE_RESPONSE", GARRISON_MISSION_COMPLETE_RESPONSE);

            Diagnostic("Detaching from GARRISON_MISSION_STARTED");
            Lua.Events.DetachEvent("GARRISON_MISSION_STARTED", ButlerCoroutine.GARRISON_MISSION_STARTED);

            Diagnostic("Detaching from LOOT_OPENED");
            Lua.Events.DetachEvent("LOOT_OPENED", LuaEvents.LootOpened);

            Diagnostic("Detaching from LOOT_CLOSED");
            Lua.Events.DetachEvent("LOOT_CLOSED", LuaEvents.LootClosed);

            CapacitiveDisplayFrame.OnDeselected();

            base.OnDeselected();
        }

        public override void Start()
        {
            try
            {
                GarrisonButler.Diagnostic("Initializing Shipments");
                ButlerCoroutine.InitializeShipments();

                GarrisonButler.Diagnostic("Initializing Missions");
                ButlerCoroutine.InitializeMissions();
            }
            catch (Exception e)
            {
                if (e is CoroutineStoppedException)
                    throw;

                Diagnostic(e.ToString());
            }
        }

        public override void Stop()
        {
            ButlerCoroutine.OnStop();
        }

        #endregion
    }
}
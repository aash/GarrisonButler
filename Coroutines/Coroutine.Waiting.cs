#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonButler.Config;
using GarrisonLua;
using NewMixedMode;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
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
                throw new NotImplementedException(
                    "This level of garrison is not supported! Please upgrade at least to level 2 the main building.");
            }

            var hbRelogApi = new HBRelogApi();

            if (hbRelogApi.IsConnected && GaBSettings.Get().HBRelogMode)
            {
                GarrisonButler.Diagnostic("[HBRelogMode] Skipping current task.");
                hbRelogApi.SkipCurrentTask(hbRelogApi.CurrentProfileName);
                await Buddy.Coroutines.Coroutine.Sleep(1000);
            }
            else if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (botBase.PrimaryBot.Name.ToLower().Contains("angler"))
                {
                    WoWPoint fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                    GarrisonButler.Log(
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
                GarrisonButler.Log("You Garrison has been taken care of! Waiting for orders...");

                /*
                 * if (await MoveTo(myFactionWaitingPoints[townHallLevel - 1]))
                    return true;
                */
            }
            return false;
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
            if (helperTriggerWithTimer(ShouldRunDailies, ref DailiesWaitTimer, ref DailiesTriggered,
                DailiesWaitTimerValue))
                return true;
            // Cache
            if (helperTriggerWithTimer(ShouldRunCache, ref CacheWaitTimer, ref CacheTriggered, CacheWaitTimerValue))
                return true;

            // Mine
            if (helperTriggerWithTimer(ShouldRunMine, ref MineWaitTimer, ref MineTriggered, MineWaitTimerValue))
                return true;

            // gardenla
            if (helperTriggerWithTimer(ShouldRunGarden, ref GardenWaitTimer, ref GardenTriggered, GardenWaitTimerValue))
                return true;

            // Start or pickup work orders
            if (helperTriggerWithTimer(ShouldRunPickUpOrStartShipment, ref StartOrderWaitTimer, ref StartOrderTriggered,
                StartOrderWaitTimerValue))
                return true;

            // Missions
            if (helperTriggerWithTimer(ShouldRunTurnInMissions, ref TurnInMissionWaitTimer, ref TurnInMissionsTriggered,
                TurnInMissionWaitTimerValue))
                return true;

            // Missions completed 
            if (helperTriggerWithTimer(ShouldRunStartMission, ref StartMissionWaitTimer, ref StartMissionTriggered,
                StartMissionWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(ShouldRunSalvage, ref SalvageWaitTimer, ref SalvageTriggered,
                SalvageWaitTimerValue))
                return true;

            // Salvage
            if (helperTriggerWithTimer(CanRunLastRound, ref LastRoundWaitTimer, ref LastRoundTriggered,
                LastRoundWaitTimerValue))
                return true;

            return AnythingLeftToDoBeforeEnd();
        }


        // The trigger must be set off by someone else to avoid pauses in the behavior! 
        private static bool helperTriggerWithTimer(Func<bool> condition, ref WaitTimer timer, ref bool toModify,
            int timerValueInSeconds)
        {
            if (timer != null && !timer.IsFinished)
                return toModify;

            if (timer == null)
                timer = new WaitTimer(TimeSpan.FromSeconds(timerValueInSeconds));
            timer.Reset();

            if (condition())
                toModify = true;
            else toModify = false;

            return toModify;
        }

        // The trigger must be set off by someone else to avoid pauses in the behavior! 
    }

    internal class ActionsSequence : Action
    {
        private readonly List<Action> Actions;

        public ActionsSequence(params Action[] actions)
        {
            Actions = actions.ToList();
        }

        public ActionsSequence(ActionsSequence actionsSequence)
        {
            Actions = actionsSequence.Actions;
        }

        public ActionsSequence()
        {
            Actions = new List<Action>();
        }

        public void AddAction(Action action)
        {
            Actions.Add(action);
        }

        public override async Task<bool> ExecuteAction()
        {
            //GarrisonButler.Diagnostic("Starting main sequence.");
            foreach (Action actionBasic in Actions)
            {
                //GarrisonButler.Diagnostic("Starting main sequence: executing action");
                if (await actionBasic.ExecuteAction())
                    return true;
            }
            return false;
        }
    }

    internal class ActionOnTimer<T> : Action
    {
        private readonly Func<T, Task<bool>> _action;
        private readonly bool _caching;
        private readonly Func<Tuple<bool, T>> _condition;
        private readonly Action[] _preActions;
        private readonly WaitTimer _waitTimer;
        private bool _lastResult;
        private bool _needToCache = false;
        private Tuple<bool, T> _tempStorage;

        public ActionOnTimer(Func<T, Task<bool>> action, Func<Tuple<bool, T>> condition, int waitTimeMs = 3000,
            bool instantStart = false, bool caching = false, params Action[] preAction)
        {
            _action = action;
            _condition = condition;
            _tempStorage = default(Tuple<bool, T>);
            _waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeMs));
            _lastResult = instantStart;
            _preActions = preAction;
            _caching = caching;
        }

        public override async Task<bool> ExecuteAction()
        {
            //GarrisonButler.Diagnostic("Execute ActionOnTimer.");
            if (!_lastResult && !_waitTimer.IsFinished)
            {
                //GarrisonButler.Diagnostic("Execute ExecuteAction: Return false : {0} || {1}", !_lastResult, !_waitTimer.IsFinished); 
                return false;
            }
            //GarrisonButler.Diagnostic("Execute ExecuteAction: Return true : {0} || {1}", !_lastResult, !_waitTimer.IsFinished); 
            Tuple<bool, T> result;
            if (_caching && !_lastResult)
                _tempStorage = _condition();

            result = _caching ? _tempStorage : _condition();

            if (result.Item1)
            {
                foreach (Action preAction in _preActions)
                {
                    if (await preAction.ExecuteAction())
                        await Buddy.Coroutines.Coroutine.Yield();
                }
                _lastResult = await _action(result.Item2);
            }
            else
                _lastResult = result.Item1;

            _waitTimer.Reset();
            return _lastResult;
        }
    }

    internal class ActionBasic : Action
    {
        private readonly Func<Task<bool>> _action;
        private readonly Func<bool> _condition;
        protected bool _lastResult;
        protected WaitTimer _waitTimer;

        public ActionBasic(Func<Task<bool>> action, int waitTimeMs = 3000, bool instantStart = false)
        {
            _action = action;
            _waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeMs));
            _lastResult = instantStart;
        }

        public ActionBasic()
        {
        }

        public override async Task<bool> ExecuteAction()
        {
            //GarrisonButler.Diagnostic("Execute ExecuteAction.");
            if (!_lastResult && !_waitTimer.IsFinished)
            {
                //GarrisonButler.Diagnostic("Execute ExecuteAction: Return false : {0} || {1}", !_lastResult, !_waitTimer.IsFinished);
                return false;
            }

            //GarrisonButler.Diagnostic("Execute ExecuteAction: Executing");
            _lastResult = await _action();
            //GarrisonButler.Diagnostic("Execute ExecuteAction: Result: " + _lastResult);

            _waitTimer.Reset();
            return _lastResult;
        }
    }

    internal abstract class Action
    {
        public abstract Task<bool> ExecuteAction();
    }
}
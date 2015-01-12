#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx.Common.Helpers;
using Styx.CommonBot;

#endregion

namespace GarrisonButler.Coroutines
{
    internal class ActionHelpers
    {
        internal abstract class Action
        {
            public abstract Task<ActionResult> ExecuteAction();
        }

        internal class ActionBasic : Action
        {
            private readonly Func<Task<ActionResult>> _action;
            private readonly Func<bool> _condition;
            protected ActionResult _lastResult;
            protected WaitTimer _waitTimer;

            public override string ToString()
            {
                string action = "";
                string condition = "";

                if (_action != default(Func<Task<ActionResult>>))
                    if (_action.Method != default(System.Reflection.MethodInfo))
                        action = _action.Method.Name;

                if (_condition != default(Func<bool>))
                    if (_condition.Method != default(System.Reflection.MethodInfo))
                        condition = _condition.Method.Name;

                return "ActionBasic: _action=" + action + "; _condition=" + condition;
            }

            public ActionBasic(Func<Task<ActionResult>> action, int waitTimeMs = 3000)
            {
                _action = action;
                _waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeMs));
                _lastResult = ActionResult.Done;
            }

            public ActionBasic()
            {
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("Execute ExecuteAction.");
                if (_lastResult == ActionResult.Done && !_waitTimer.IsFinished)
                {
                    //GarrisonButler.Diagnostic("Execute ExecuteAction: Return false : {0} || {1}", !_lastResult, !_waitTimer.IsFinished);
                    return ActionResult.Done;
                }

                //GarrisonButler.Diagnostic("Execute ExecuteAction: Executing");
                _lastResult = await _action();
                //GarrisonButler.Diagnostic("Execute ExecuteAction: Result: " + _lastResult);

                _waitTimer.Reset();
                return _lastResult;
            }
        }

        internal class ActionOnTimer<T> : Action
        {
            protected readonly Func<T, Task<ActionResult>> _action;
            protected readonly Func<Tuple<bool, T>> _condition;
            protected readonly Action[] _preActions;
            protected readonly WaitTimer _waitTimerAction;
            protected WaitTimer _waitTimerAntiSpamRunning;
            protected WaitTimer _waitTimerCondition;
            protected ActionResult _lastResult;
            protected bool _needToCache = false;
            protected Tuple<bool, T> _resCondition;

            public override string ToString()
            {
                string action = "";
                string condition = "";

                if (_action != default(Func<T, Task<ActionResult>>))
                    if (_action.Method != default(System.Reflection.MethodInfo))
                        action = _action.Method.Name;

                if (_condition != default(Func<Tuple<bool, T>>))
                    if (_condition.Method != default(System.Reflection.MethodInfo))
                        condition = _condition.Method.Name;

                return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
            }

            public ActionOnTimer(Func<T, Task<ActionResult>> action, Func<Tuple<bool, T>> condition, int waitTimeActionMs = 3000,
                int waitTimeConditionMs = 3500, params Action[] preAction)
            {
                _action = action;
                _condition = condition;
                _resCondition = default(Tuple<bool, T>);
                _waitTimerAction = new WaitTimer(TimeSpan.FromMilliseconds(3000));
                _waitTimerCondition = new WaitTimer(TimeSpan.FromMilliseconds(10000));
                _waitTimerAntiSpamRunning = new WaitTimer(TimeSpan.FromMilliseconds(100));
                _lastResult = ActionResult.Init;
                _preActions = preAction;
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                // Check time between a recheck once done
                if (_lastResult == ActionResult.Done && !_waitTimerAction.IsFinished)
                    return ActionResult.Done;

                // Check time while running to not overload
                if (_lastResult == ActionResult.Running && !_waitTimerAntiSpamRunning.IsFinished)
                    return ActionResult.Running;

                _waitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if timer is finished
                if (_lastResult == ActionResult.Refresh 
                    || _lastResult == ActionResult.Init
                    || _waitTimerCondition.IsFinished)
                {
                    _resCondition = _condition();
                    _waitTimerCondition.Reset();
                }

                // If condition of action verified
                if (_resCondition.Item1)
                {
                    // Running preactions with a tick between some
                    foreach (Action preAction in _preActions)
                    {
                        if (await preAction.ExecuteAction() == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    _lastResult = await _action(_resCondition.Item2);
                }
                else
                    _lastResult = ActionResult.Done;

                _waitTimerAction.Reset();
                return
                    (_lastResult == ActionResult.Refresh || _lastResult == ActionResult.Running)
                    ? ActionResult.Running
                    : ActionResult.Done;
            }
        }

        internal class ActionOnTimerCached<T> : ActionOnTimer<T>
        {
            public ActionOnTimerCached(Func<T, Task<ActionResult>> action, Func<Tuple<bool, T>> condition,
                int waitTimeActionMs = 1000, int waitTimeConditionMs = 150,
                bool instantStart = false, params Action[] preAction)
                : base(action, condition, waitTimeActionMs, waitTimeConditionMs, preAction)
            {
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                // Check time between a recheck once done
                if (_lastResult == ActionResult.Done && !_waitTimerAction.IsFinished)
                    return ActionResult.Done;

                // Check time while running to not overload
                if (_lastResult == ActionResult.Running && !_waitTimerAntiSpamRunning.IsFinished)
                    return ActionResult.Running;

                _waitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if done and timer finished.
                if (_lastResult == ActionResult.Refresh
                    || _lastResult == ActionResult.Init
                    || ( _lastResult == ActionResult.Done))
                {
                    _resCondition = _condition();
                    _waitTimerCondition.Reset();
                }

                // If condition of action verified
                if (_resCondition.Item1)
                {
                    // Running preactions with a tick between some
                    foreach (Action preAction in _preActions)
                    {
                        if (await preAction.ExecuteAction() == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    _lastResult = await _action(_resCondition.Item2);
                }
                else
                    _lastResult = ActionResult.Done;
                
                _waitTimerAction.Reset();
                return
                    (_lastResult == ActionResult.Refresh || _lastResult == ActionResult.Running)
                    ? ActionResult.Running
                    : ActionResult.Done;
            }
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

            public override async Task<ActionResult> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction(): count=" + Actions.Count);
                //int count = 0;
                foreach (Action actionBasic in Actions)
                {
                    //count++;
                    //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction():   #" + count + " - " + actionBasic.ToString());
                    var result = await actionBasic.ExecuteAction();
                    if (result == ActionResult.Running)
                        return ActionResult.Running;
                    if (result == ActionResult.Refresh)
                        return ActionResult.Refresh;
                }
                return ActionResult.Done;
            }
        }

    }
    internal enum ActionResult
    {
        Refresh,
        Done,
        Failed,
        Running,
        Init
    }
}
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx.Common.Helpers;

#endregion

namespace GarrisonButler.Coroutines
{
    internal class ActionHelpers
    {
        internal abstract class Action
        {
            public abstract Task<bool> ExecuteAction();
        }

        internal class ActionBasic : Action
        {
            private readonly Func<Task<bool>> _action;
            private readonly Func<bool> _condition;
            protected bool _lastResult;
            protected WaitTimer _waitTimer;

            public override string ToString()
            {
                string action = "";
                string condition = "";

                if (_action != default(Func<Task<bool>>))
                    if (_action.Method != default(System.Reflection.MethodInfo))
                        action = _action.Method.Name;

                if (_condition != default(Func<bool>))
                    if (_condition.Method != default(System.Reflection.MethodInfo))
                        condition = _condition.Method.Name;

                return "ActionBasic: _action=" + action + "; _condition=" + condition;
            }

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

        internal class ActionOnTimer<T> : Action
        {
            protected readonly Func<T, Task<bool>> _action;
            protected readonly Func<Tuple<bool, T>> _condition;
            protected readonly Action[] _preActions;
            protected readonly WaitTimer _waitTimerAction;
            protected readonly WaitTimer _waitTimerCondition;
            protected bool _lastResult;
            protected bool _needToCache = false;
            protected Tuple<bool, T> _tempStorage;

            public override string ToString()
            {
                string action = "";
                string condition = "";

                if (_action != default(Func<T, Task<bool>>))
                    if (_action.Method != default(System.Reflection.MethodInfo))
                        action = _action.Method.Name;

                if (_condition != default(Func<Tuple<bool, T>>))
                    if (_condition.Method != default(System.Reflection.MethodInfo))
                        condition = _condition.Method.Name;

                return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
            }

            public ActionOnTimer(Func<T, Task<bool>> action, Func<Tuple<bool, T>> condition, int waitTimeActionMs = 3000,
                int waitTimeConditionMs = 3500,
                bool instantStart = false, params Action[] preAction)
            {
                _action = action;
                _condition = condition;
                _tempStorage = default(Tuple<bool, T>);
                _waitTimerAction = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeActionMs));
                _waitTimerCondition = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeConditionMs));
                _lastResult = instantStart;
                _preActions = preAction;
            }

            public override async Task<bool> ExecuteAction()
            {
                if (!_lastResult && !_waitTimerAction.IsFinished)
                    return false;

                if (!_lastResult && _waitTimerCondition.IsFinished)
                {
                    _tempStorage = _condition();
                    _waitTimerCondition.Reset();
                }

                Tuple<bool, T> result = _tempStorage;
                if (result.Item1)
                {
                    foreach (Action preAction in _preActions)
                    {
                        if (await preAction.ExecuteAction())
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    _lastResult = await _action(result.Item2);
                    if (!_lastResult)
                    {
                        _tempStorage = _condition();
                        _lastResult = _tempStorage.Item1;
                        _waitTimerCondition.Reset();
                    }
                }
                else
                    _lastResult = result.Item1;

                _waitTimerAction.Reset();
                return _lastResult;
            }
        }

        internal class ActionOnTimerCached<T> : ActionOnTimer<T>
        {
            public ActionOnTimerCached(Func<T, Task<bool>> action, Func<Tuple<bool, T>> condition,
                int waitTimeActionMs = 1000, int waitTimeConditionMs = 200,
                bool instantStart = false, params Action[] preAction)
                : base(action, condition, waitTimeActionMs, waitTimeConditionMs, instantStart, preAction)
            {
            }

            public override async Task<bool> ExecuteAction()
            {
                if (!_lastResult && !_waitTimerAction.IsFinished)
                    return false;

                if (!_lastResult && _waitTimerCondition.IsFinished)
                {
                    _tempStorage = _condition();
                    _waitTimerCondition.Reset();
                }

                Tuple<bool, T> result = _tempStorage;
                if (result.Item1)
                {
                    foreach (Action preAction in _preActions)
                    {
                        if (await preAction.ExecuteAction())
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    _lastResult = await _action(result.Item2);
                    if (!_lastResult)
                    {
                        _tempStorage =_condition();
                        _lastResult = _tempStorage.Item1;
                        _waitTimerCondition.Reset();
                    }
                }
                else
                    _lastResult = result.Item1;

                _waitTimerAction.Reset();
                return _lastResult;
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

            public override async Task<bool> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction(): count=" + Actions.Count);
                //int count = 0;
                foreach (Action actionBasic in Actions)
                {
                    //count++;
                    //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction():   #" + count + " - " + actionBasic.ToString());
                    if (await actionBasic.ExecuteAction())
                        return true;
                }
                return false;
            }
        }
    }
}
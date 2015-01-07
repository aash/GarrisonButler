using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Styx.Common.Helpers;

namespace GarrisonButler.Coroutines
{
    class ActionHelpers
    {
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

        internal class ActionOnTimerCached<T> : ActionOnTimer<T>
        {
            public ActionOnTimerCached(Func<T, Task<bool>> action, Func<Tuple<bool, T>> condition, int waitTimeActionMs = 3000, int waitTimeConditionMs = 200,
                bool instantStart = false, params Action[] preAction)
                :base(action, condition,waitTimeActionMs,waitTimeConditionMs,instantStart,preAction)
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
                }
                else
                    _lastResult = result.Item1;

                _waitTimerAction.Reset();
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

            public ActionOnTimer(Func<T, Task<bool>> action, Func<Tuple<bool, T>> condition, int waitTimeActionMs = 3000, int waitTimeConditionMs = 200,
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

                if (_waitTimerCondition.IsFinished)
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
                }
                else
                    _lastResult = result.Item1;

                _waitTimerAction.Reset();
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
}

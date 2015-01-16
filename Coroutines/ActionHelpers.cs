#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Styx.Common.Helpers;

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
            protected ActionResult LastResult;
            protected WaitTimer WaitTimer;

            public override string ToString()
            {
                var action = "";

                if (_action == default(Func<Task<ActionResult>>)) return "ActionBasic: _action=" + action;
                if (_action.Method != default(MethodInfo))
                    action = _action.Method.Name;

                return "ActionBasic: _action=" + action;
            }

            public ActionBasic(Func<Task<ActionResult>> action, int waitTimeMs = 3000)
            {
                _action = action;
                WaitTimer = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeMs));
                LastResult = ActionResult.Done;
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("Execute ExecuteAction.");
                if (LastResult == ActionResult.Done && !WaitTimer.IsFinished)
                {
                    //GarrisonButler.Diagnostic("Execute ExecuteAction: Return false : {0} || {1}", !_lastResult, !_waitTimer.IsFinished);
                    return ActionResult.Done;
                }

                //GarrisonButler.Diagnostic("Execute ExecuteAction: Executing");
                LastResult = await _action();
                //GarrisonButler.Diagnostic("Execute ExecuteAction: Result: " + _lastResult);

                WaitTimer.Reset();
                return LastResult;
            }
        }

        internal class ActionOnTimer<T> : Action
        {
            protected readonly Func<T, Task<ActionResult>> CustomAction;
            protected readonly Func<Tuple<bool, T>> Condition;
            protected readonly Action[] PreActions;
            protected readonly WaitTimer WaitTimerAction;
            protected WaitTimer WaitTimerAntiSpamRunning;
            protected WaitTimer WaitTimerCondition;
            protected ActionResult LastResult;
            protected bool NeedToCache = false;
            protected Tuple<bool, T> ResCondition;

            public override string ToString()
            {
                var action = "";
                var condition = "";

                if (CustomAction != default(Func<T, Task<ActionResult>>))
                    if (CustomAction.Method != default(MethodInfo))
                        action = CustomAction.Method.Name;

                if (Condition == default(Func<Tuple<bool, T>>))
                    return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
                if (Condition.Method != default(MethodInfo))
                    condition = Condition.Method.Name;

                return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
            }

            public ActionOnTimer(Func<T, Task<ActionResult>> customAction, Func<Tuple<bool, T>> condition,
                int waitTimeActionMs = 3000,
                int waitTimeConditionMs = 3500, params Action[] preAction)
            {
                CustomAction = customAction;
                Condition = condition;
                ResCondition = default(Tuple<bool, T>);
                WaitTimerAction = new WaitTimer(TimeSpan.FromMilliseconds(5000));
                WaitTimerCondition = new WaitTimer(TimeSpan.FromMilliseconds(10000));
                WaitTimerAntiSpamRunning = new WaitTimer(TimeSpan.FromMilliseconds(1.0/15.0));
                LastResult = ActionResult.Init;
                PreActions = preAction;
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                // Check time between a recheck once done
                if (LastResult == ActionResult.Done && !WaitTimerAction.IsFinished)
                    return ActionResult.Done;

                // Check time while running to not overload
                if (LastResult == ActionResult.Running && !WaitTimerAntiSpamRunning.IsFinished)
                    return ActionResult.Running;

                WaitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if timer is finished
                if (LastResult == ActionResult.Refresh
                    || LastResult == ActionResult.Init
                    || WaitTimerCondition.IsFinished)
                {
                    ResCondition = Condition();
                    WaitTimerCondition.Reset();
                }

                // If condition of action verified
                if (ResCondition.Item1)
                {
                    // Running preactions with a tick between some
                    foreach (var preAction in PreActions)
                    {
                        if (await preAction.ExecuteAction() == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    LastResult = await CustomAction(ResCondition.Item2);
                }
                else
                    LastResult = ActionResult.Done;

                WaitTimerAction.Reset();
                return
                    (LastResult == ActionResult.Refresh || LastResult == ActionResult.Running)
                        ? ActionResult.Running
                        : ActionResult.Done;
            }
        }

        internal class ActionOnTimerCached<T> : ActionOnTimer<T>
        {
            public ActionOnTimerCached(Func<T, Task<ActionResult>> customAction, Func<Tuple<bool, T>> condition,
                int waitTimeActionMs = 1000, int waitTimeConditionMs = 150, params Action[] preAction)
                : base(customAction, condition, waitTimeActionMs, waitTimeConditionMs, preAction)
            {
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                // Check time between a recheck once done
                if (LastResult == ActionResult.Done && !WaitTimerAction.IsFinished)
                    return ActionResult.Done;

                // Check time while running to not overload
                if (LastResult == ActionResult.Running && !WaitTimerAntiSpamRunning.IsFinished)
                    return ActionResult.Running;

                WaitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if done and timer finished.
                if (LastResult == ActionResult.Refresh
                    || LastResult == ActionResult.Init
                    || (LastResult == ActionResult.Done))
                {
                    ResCondition = Condition();
                    WaitTimerCondition.Reset();
                }

                // If condition of action verified
                if (ResCondition.Item1)
                {
                    // Running preactions with a tick between some
                    foreach (var preAction in PreActions)
                    {
                        if (await preAction.ExecuteAction() == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    LastResult = await CustomAction(ResCondition.Item2);
                }
                else
                    LastResult = ActionResult.Done;

                WaitTimerAction.Reset();
                return
                    (LastResult == ActionResult.Refresh || LastResult == ActionResult.Running)
                        ? ActionResult.Running
                        : ActionResult.Done;
            }
        }

        internal class ActionsSequence : Action
        {
            private readonly List<Action> _actions;

            public ActionsSequence(params Action[] actions)
            {
                _actions = actions.ToList();
            }

            public ActionsSequence(ActionsSequence actionsSequence)
            {
                _actions = actionsSequence._actions;
            }

            public ActionsSequence()
            {
                _actions = new List<Action>();
            }

            public void AddAction(Action action)
            {
                _actions.Add(action);
            }

            public override async Task<ActionResult> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction(): count=" + Actions.Count);
                //int count = 0;
                foreach (var actionBasic in _actions)
                {
                    //count++;
                    //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction():   #" + count + " - " + actionBasic.ToString());
                    var result = await actionBasic.ExecuteAction();
                    switch (result)
                    {
                        case ActionResult.Running:
                            return ActionResult.Running;
                        case ActionResult.Refresh:
                            return ActionResult.Refresh;
                    }
                }
                return ActionResult.Done;
            }
        }
    }

    public enum ActionResult
    {
        Refresh,
        Done,
        Failed,
        Running,
        Init
    }
}
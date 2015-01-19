﻿#region

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
            public abstract Task<Result> ExecuteAction();
            internal Func<Task<Result>> Condition;
        }

        internal class ActionBasic : Action
        {
            private readonly Func<Task<Result>> _action;
            protected Result LastResult;
            protected WaitTimer WaitTimer;

            public override string ToString()
            {
                var action = "";

                if (_action == default(Func<Task<Result>>)) return "ActionBasic: _action=" + action;
                if (_action.Method != default(MethodInfo))
                    action = _action.Method.Name;

                return "ActionBasic: _action=" + action;
            }

            public ActionBasic(Func<Task<Result>> action, int waitTimeMs = 3000)
            {
                _action = action;
                WaitTimer = new WaitTimer(TimeSpan.FromMilliseconds(waitTimeMs));
                LastResult = new Result(ActionResult.Done);
            }

            public override async Task<Result> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("Execute ExecuteAction.");
                if (LastResult.Status == ActionResult.Done && !WaitTimer.IsFinished)
                {
                    //GarrisonButler.Diagnostic("Execute ExecuteAction: Return false : {0} || {1}", !_lastResult, !_waitTimer.IsFinished);
                    return new Result(ActionResult.Done);
                }

                //GarrisonButler.Diagnostic("Execute ExecuteAction: Executing");
                LastResult = await _action();
                //GarrisonButler.Diagnostic("Execute ExecuteAction: Result: " + _lastResult);

                WaitTimer.Reset();
                return new Result(LastResult);
            }
        }

        internal class ActionOnTimer : Action
        {
            protected readonly Func<object, Task<Result>> CustomAction;
            protected readonly Action[] PreActions;
            protected readonly WaitTimer WaitTimerAction;
            protected WaitTimer WaitTimerAntiSpamRunning;
            protected WaitTimer WaitTimerCondition;
            protected Result LastResult;
            protected bool NeedToCache = false;
            protected Result ResCondition;

            public override string ToString()
            {
                var action = "";
                var condition = "";

                if (CustomAction != default(Func<object, Task<Result>>))
                    if (CustomAction.Method != default(MethodInfo))
                        action = CustomAction.Method.Name;

                if (Condition == default(Func<Task<Result>>))
                    return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
                if (Condition.Method != default(MethodInfo))
                    condition = Condition.Method.Name;

                return "ActionOnTimer: _action=" + action + "; _condition=" + condition;
            }

            public ActionOnTimer(Func<object, Task<Result>> customAction, Func<Task<Result>> condition,
                int waitTimeActionMs = 3000,
                int waitTimeConditionMs = 3500, params Action[] preAction)
            {
                CustomAction = customAction;
                Condition = condition;
                ResCondition = default(Result);
                WaitTimerAction = new WaitTimer(TimeSpan.FromMilliseconds(5000));
                WaitTimerCondition = new WaitTimer(TimeSpan.FromMilliseconds(10000));
                WaitTimerAntiSpamRunning = new WaitTimer(TimeSpan.FromMilliseconds(1.0/15.0));
                LastResult = new Result(ActionResult.Init);
                PreActions = preAction;
            }

            public override async Task<Result> ExecuteAction()
            {
                // Check time between a recheck once done
                if (LastResult.Status == ActionResult.Done && !WaitTimerAction.IsFinished)
                    return new Result(ActionResult.Done);

                // Check time while running to not overload
                if (LastResult.Status == ActionResult.Running && !WaitTimerAntiSpamRunning.IsFinished)
                    return new Result(ActionResult.Running);

                WaitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if timer is finished
                if (LastResult.Status == ActionResult.Refresh
                    || LastResult.Status == ActionResult.Init
                    || WaitTimerCondition.IsFinished)
                {
                    ResCondition = await Condition();
                    WaitTimerCondition.Reset();
                }

                // If condition of action verified
                if (ResCondition.Status == ActionResult.Running)
                {
                    // Running preactions with a tick between some
                    foreach (var preAction in PreActions)
                    {
                        if ((await preAction.ExecuteAction()).Status == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    LastResult = await CustomAction(ResCondition.Result1);
                }
                else
                    LastResult.Status = ActionResult.Done;

                WaitTimerAction.Reset();
                return
                    (LastResult.Status == ActionResult.Refresh || LastResult.Status == ActionResult.Running)
                        ? new Result(ActionResult.Running)
                        : new Result(ActionResult.Done);
            }
        }

        internal class ActionOnTimerCached : ActionOnTimer
        {
            public ActionOnTimerCached(Func<object, Task<Result>> customAction, Func<Task<Result>> condition,
                int waitTimeActionMs = 1000, int waitTimeConditionMs = 150, params Action[] preAction)
                : base(customAction, condition, waitTimeActionMs, waitTimeConditionMs, preAction)
            {
            }

            public override async Task<Result> ExecuteAction()
            {
                // Check time between a recheck once done
                if (LastResult.Status == ActionResult.Done && !WaitTimerAction.IsFinished)
                    return new Result(ActionResult.Done);

                // Check time while running to not overload
                if (LastResult.Status == ActionResult.Running && !WaitTimerAntiSpamRunning.IsFinished)
                    return new Result(ActionResult.Running);

                WaitTimerAntiSpamRunning.Reset();

                // Check action condition, refresh if asked, if not yet ran once, if done and timer finished.
                if (LastResult.Status == ActionResult.Refresh
                    || LastResult.Status == ActionResult.Init
                    || (LastResult.Status == ActionResult.Done))
                {
                    ResCondition = await Condition();
                    WaitTimerCondition.Reset();
                }

                if (WaitTimerCondition.IsFinished)
                {
                    var tempRes = await Condition();
                    if (tempRes.Status == ResCondition.Status)
                        ResCondition = tempRes;
                    WaitTimerCondition.Reset();
                }

                // If condition of action verified
                if (ResCondition.Status == ActionResult.Running)
                {
                    // Running preactions with a tick between some
                    foreach (var preAction in PreActions)
                    {
                        if ((await preAction.ExecuteAction()).Status == ActionResult.Running)
                            await Buddy.Coroutines.Coroutine.Yield();
                    }
                    // Running action
                    LastResult = await CustomAction(ResCondition.Result1);
                }
                else
                    LastResult = new Result(ActionResult.Done);

                WaitTimerAction.Reset();
                return
                    (LastResult.Status == ActionResult.Refresh || LastResult.Status == ActionResult.Running)
                        ? new Result(ActionResult.Running)
                        : new Result(ActionResult.Done);
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

            public async Task<Result> AtLeastOneTrue()
            {
                foreach (var action in _actions)
                {
                    if ((await action.Condition()).Status == ActionResult.Running)
                        return new Result(ActionResult.Running);
                }
                return new Result(ActionResult.Failed);
            }

            public override async Task<Result> ExecuteAction()
            {
                //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction(): count=" + Actions.Count);
                //int count = 0;
                foreach (var actionBasic in _actions)
                {
                    //count++;
                    //GarrisonButler.Diagnostic("ActionSequence.ExecuteAction():   #" + count + " - " + actionBasic.ToString());
                    var result = await actionBasic.ExecuteAction();
                    switch (result.Status)
                    {
                        case ActionResult.Running:
                            return new Result(ActionResult.Running);
                        case ActionResult.Refresh:
                            return new Result(ActionResult.Refresh);
                    }
                }
                return new Result(ActionResult.Done);
            }
        }
    }

    public class Result
    {
        private ActionResult _status;
        private object _result;

        public Result(ActionResult status, object result = null)
        {
            _status = status;
            _result = result;
        }
        public Result(Result result)
        {
            _status = result.Status;
            _result = result.Result1;
        }

        public ActionResult Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public object Result1
        {
            get { return _result; }
            set { _result = value; }
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
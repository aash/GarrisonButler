#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
   public class Result
    {
        public Result(ActionResult state, object result = null)
        {
            State = state;
            Content = result;
        }

        public Result(Result result)
        {
            State = result.State;
            Content = result.Content;
        }

        public ActionResult State { get; set; }

        public object Content { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Enum.GetName(typeof(ActionResult), State), Content);
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
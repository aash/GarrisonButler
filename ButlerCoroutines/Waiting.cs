#region

using GarrisonButler.API;

#region

using System;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonButler.Config;
using NewMixedMode;
using Styx;
using Styx.CommonBot;

#endregion

#endregion

// ReSharper disable once CheckNamespace

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {
        private static bool _hbRelogSkipped;
        private static int _hbRelogSkippedCounter;
        private static WoWPoint waitingSpot;
        private static bool waitingSpotInit;

// ReSharper disable once CSharpWarnings::CS1998
        private static async Task<Result> Waiting()
        {
            var townHallLevel = BuildingsLua.GetTownHallLevel();
            if (townHallLevel < 1)
                return new Result(ActionResult.Failed);

            var myFactionWaitingPoints = Me.IsAlliance ? AllyWaitingPoints : HordeWaitingPoints;

            if (myFactionWaitingPoints[townHallLevel - 1] == new WoWPoint())
            {
                throw new NotImplementedException(
                    "This level of garrison is not supported! Please upgrade at least to level 2 the main building.");
            }
            if (!waitingSpotInit)
            {
                var r = new Random(DateTime.Now.Second);
                var randomX = (float) (r.NextDouble() - 0.5)*5;
                var randomY = (float) (r.NextDouble() - 0.5)*5;
                var toAdd = Me.IsAlliance ? TableAlliance : TableHorde;
                toAdd.X = toAdd.X + randomX;
                toAdd.Y = toAdd.Y + randomY;
                waitingSpot = Dijkstra.ClosestToNodes(toAdd);
                waitingSpotInit = true;
            }

            if ((await
                MoveTo(waitingSpot, "Moving to random waiting spot next to mission table.")).State ==
                ActionResult.Running)
                return new Result(ActionResult.Running);

            GarrisonButler.Log("You Garrison has been taken care of! Waiting for orders...");
            return new Result(ActionResult.Done);
        }

        /// <summary>
        /// If enabled, does HBRelog ... If MixedMode, takes care of angler (returns true) .... otherwise returns true
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> JobDoneSwitch()
        {
            var hbRelogApi = new HBRelogApi();

            if (hbRelogApi.IsConnected && GaBSettings.Get().HbRelogMode)
            {
                if (_hbRelogSkipped == false)
                {
                    GaBSettings.Save();
                    GarrisonButler.Log("[HBRelogMode] Skipping current task.");
                    hbRelogApi.SkipCurrentTask(hbRelogApi.CurrentProfileName);
                    _hbRelogSkipped = true;
                }
                else if (_hbRelogSkippedCounter > 10)
                {
                    GarrisonButler.Diagnostic(
                        "[HBRelogMode] Still not closed by HBRelog after 10 ticks, shutting down honorbuddy.");
                    TreeRoot.Shutdown();
                    _hbRelogSkippedCounter = 0;
                }
                else
                {
                    _hbRelogSkippedCounter++;
                    GarrisonButler.Diagnostic("[HBRelogMode] Task skipped, waiting...");
                }
            }
            else if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (!botBase.PrimaryBot.Name.ToLower().Contains("angler")) return true;
                var fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                GarrisonButler.Log(
                    "You Garrison has been taken care of, bot safe. AutoAngler with Mixed Mode has been detected, moving to fishing area. Happy catch! :)");
                if (!(Me.Location.Distance(fishingSpot) > 2)) return true;
                if ((await MoveTo(fishingSpot, "[Waiting] Moving to fishing spot.")).State == ActionResult.Running)
                    return true;
            }
            return true;
        }

        public static async Task SomethingToDo()
        {
            if (!ReadyToSwitch)
            {
                AnyTodo =  false;
            }
            AnyTodo = await _mainSequence.AtLeastOneTrue();
        }

        internal static bool AnyTodo = false;
    }
}
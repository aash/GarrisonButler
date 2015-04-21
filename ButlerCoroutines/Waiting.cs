#region

using Styx.Pathing;
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
                return true; 
            }
            
            if (BotManager.Current.Name == "Mixed Mode")
            {
                var botBase = (MixedModeEx) BotManager.Current;
                if (!botBase.PrimaryBot.Name.ToLower().Contains("angler")) return true;
                var fishingSpot = Me.IsAlliance ? FishingSpotAlly : FishingSpotHorde;
                GarrisonButler.Log(
                    "You Garrison has been taken care of, bot safe. AutoAngler with Mixed Mode has been detected, moving to fishing area. Happy catch! :)");
                if (Me.Location.Distance(fishingSpot) < 2) return true;
                Navigator.MoveTo(fishingSpot);
                return true;
            }
            return false;
        }
    }
}
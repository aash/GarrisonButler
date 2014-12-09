using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly WaitTimer RefreshBuildingsTimer = new WaitTimer(TimeSpan.FromMinutes(5));

        internal static readonly List<uint> FinalizeGarrisonPlotIds = new List<uint>
        {
            231217,
            231964,
            233248,
            233249,
            233250,
            233251,
            232651,
            232652,
            236261,
            236262,
            236263,
            236175,
            236176,
            236177,
            236185,
            236186,
            236187,
            236188,
            236190,
            236191,
            236192,
            236193,
        };

        private static async Task<bool> DoBuildingRelated()
        {
            RefreshBuildings();

            // Check followers for buildings

            // To do

            // Mine
            if (await CleanMine())
                return true;

            // Garden 
            if (await CleanGarden())
                return true;

            if (await PickUpAllWorkOrder())
                return true;

            // Garrison Cache
            if (await PickUpGarrisonCache())
                return true;

            if (await ActivateFinishedBuildings())
                return true;

            if (await StartOrder())
                return true;

            return false;
        }

        private static void RefreshBuildings(bool forced = false)
        {
            if (!RefreshBuildingsTimer.IsFinished && _buildings != null && !forced) return;

            GarrisonBuddy.Log("Refreshing Buildings and shipments databases.");
            _buildings = BuildingsLua.GetAllBuildings();
            RefreshBuildingsTimer.Reset();
        }

        private static async Task<bool> ActivateFinishedBuildings()
        {
            if (!GaBSettings.Mono.ActivateBuildings)
                return false;

            IOrderedEnumerable<WoWGameObject> allToActivate =
                ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .ToList()
                    .OrderBy(o => o.Location.X);
            if (!allToActivate.Any())
            {
                return false;
            }

            WoWGameObject toActivate = allToActivate.First();
            GarrisonBuddy.Log("Found building to activate(" + toActivate.Name + "), moving to building.");
            GarrisonBuddy.Diagnostic("Building  " + toActivate.SafeName + " - " + toActivate.Entry + " - " +
                                     toActivate.DisplayId + ": " + toActivate.Location);
            if (await MoveTo(toActivate.Location, "Building activation"))
                return true;


            await Buddy.Coroutines.Coroutine.Sleep(300);
            toActivate.Interact();
            GarrisonBuddy.Log("Activating " + toActivate.SafeName + ", waiting...");
            if (await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                toActivate.Interact();
                return
                    !ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Any(o => FinalizeGarrisonPlotIds.Contains(o.Entry) && o.Guid == toActivate.Guid);
            }))
            {
                GarrisonBuddy.Warning("Failed to activate building: " + toActivate.Name);
            }
            return true;
        }
    }
}
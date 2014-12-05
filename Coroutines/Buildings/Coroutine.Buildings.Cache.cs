using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly List<uint> GarrisonCaches = new List<uint>
        {
            236916,
            237191,
            237724,
            237723,
            237722,
            237720
        };

        private static int _attemptCache;
        private static bool Found;
        private static WoWPoint cacheCachedLocation;
        private static bool CacheRunning = false;

        private static bool IsToDoCache()
        {
            return cacheWaitTimer == null || cacheWaitTimer.IsFinished || CacheRunning;
        }

        private static WaitTimer cacheWaitTimer;
        private static async Task<bool> PickUpGarrisonCache()
        {
            if (cacheWaitTimer != null && !cacheWaitTimer.IsFinished && !CacheRunning)
                return false;
            if (cacheWaitTimer == null)
                cacheWaitTimer = new WaitTimer(TimeSpan.FromMinutes(1));
            cacheWaitTimer.Reset();


            if (!IsToDoCache())
            {
                CacheRunning = false;
                return false;
            }

            if (_attemptCache > 2)
            {
                GarrisonBuddy.Log("Not picking up available Cache since too many failed attempts in the past.");
                CacheRunning = false;
                return false;
            }

            WoWGameObject cacheFound =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cacheFound == null && !Found)
            {
                CacheRunning = false;
                return false;
            }
            Found = true;
            CacheRunning = true;
            if (cacheFound != null)
            {
                cacheCachedLocation = cacheFound.Location;
                GarrisonBuddy.Log("Detected garrison cache available, moving to collect.");
                GarrisonBuddy.Diagnostic("Shipment " + cacheFound.SafeName + " - " + cacheFound.Entry + " - " + cacheFound.DisplayId + ": " +
                                         cacheFound.Location);
                if (await MoveTo(cacheFound.Location, "Collecting garrison cache"))
                    return true;

                GarrisonBuddy.Log("Collecting Garrison cache.");
                if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
                {
                    cacheFound.Interact();
                    ObjectManager.Update();
                    return !ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o => GarrisonCaches.Contains(o.Entry));
                }))
                {
                    GarrisonBuddy.Warning("Failed to collect Garrison cache. Already " + _attemptCache + " attempts failed.");
                    _attemptCache++;
                }
                else
                {
                    GarrisonBuddy.Log("Succesfully collected Garrison cache.");
                    _attemptCache = 0;
                }
            }
            else
            {
                if (await MoveTo(cacheCachedLocation, "Collecting garrison cache"))
                    return true;
            }

            CacheRunning = false;
            Found = false;
            return true;
        }
    }
}
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
        private static WaitTimer cacheWaitTimer;

        private static bool CanRunCache(ref WoWGameObject cache)
        {
            if (!GaBSettings.Mono.GarrisonCache)
                return false;
            // Check
            cache =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null && !Found)
                return false;

            Found = true;

            if (cache != null) cacheCachedLocation = cache.Location;
            return true;
        }
        private static bool ShouldRunCache()
        {
            WoWGameObject cacheFound = null;
            return CanRunCache(ref cacheFound);
        }

        private static async Task<bool> PickUpGarrisonCache()
        {
            WoWGameObject cacheFound = null;

            if (!CanRunCache(ref cacheFound))
                return false;

            if (cacheFound != null)
            {
                cacheCachedLocation = cacheFound.Location;
                GarrisonBuddy.Log("Detected garrison cache available, moving to collect.");
                GarrisonBuddy.Diagnostic("Shipment " + cacheFound.SafeName + " - " + cacheFound.Entry + " - " + cacheFound.DisplayId + ": " +
                                         cacheFound.Location);


                await HarvestWoWGameOject(cacheFound);
            }
            else
            {
                if (await MoveTo(cacheCachedLocation, "Collecting garrison cache"))
                    return true;
            }

            Found = false;
            CacheTriggered = false;
            return true;
        }
    }
}
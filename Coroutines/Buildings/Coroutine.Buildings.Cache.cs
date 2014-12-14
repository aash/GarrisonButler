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

        private static Tuple<bool,WoWGameObject> CanRunCache()
        {
            if (!GaBSettings.Get().GarrisonCache)
                return new Tuple<bool, WoWGameObject>(false,null);
            // Check
            var cache =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null && !Found)
                return new Tuple<bool, WoWGameObject>(false, null);
            
            Found = true;
            if (cache != null) cacheCachedLocation = cache.Location;
            return new Tuple<bool, WoWGameObject>(true, cache);
        }
        private static bool ShouldRunCache()
        {
            return CanRunCache().Item1;
        }

        private static async Task<bool> PickUpGarrisonCache(WoWGameObject cacheFound)
        {
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
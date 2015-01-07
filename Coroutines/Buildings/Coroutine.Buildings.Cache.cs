#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
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

        private static Tuple<bool, WoWGameObject> CanRunCache()
        {
            if (!GaBSettings.Get().GarrisonCache)
            {
                GarrisonButler.Diagnostic("[Cache] Cache deactivated in user settings"); 
                return new Tuple<bool, WoWGameObject>(false, null);
            }
            // Check
            WoWGameObject cache =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .FirstOrDefault(o => GarrisonCaches.GetEmptyIfNull().Contains(o.Entry));
            if (cache == default(WoWGameObject))
            {
                GarrisonButler.Diagnostic("[Cache] Cache not found, skipping...");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            return new Tuple<bool, WoWGameObject>(true, cache);
        }

        private static bool ShouldRunCache()
        {
            return CanRunCache().Item1;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx;
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

        private static bool IsToDoCache()
        {
            return GaBSettings.Mono.GarrisonCache &&
                   (ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o => GarrisonCaches.Contains(o.Entry) || Found));
        }

        private static async Task<bool> PickUpGarrisonCache()
        {
            if (!IsToDoCache())
                return false;

            if (_attemptCache > 2)
            {
                GarrisonBuddy.Log("Not picking up available Cache since too many failed attempts in the past.");
                return false;
            }

            WoWGameObject cacheFound =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cacheFound == null && !Found)
                return false;
            Found = true;
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

            Found = false;
            return true;
        }
    }
}
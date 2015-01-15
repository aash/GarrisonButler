using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Libraries
{
    static class PlayerExtensions
    {

        internal static readonly List<uint> GarrisonsZonesId = new List<uint>
        {
            7078, // Lunarfall - Ally
            7004 // Frostwall - Horde
        };

        /// <summary>
        /// Check if a localPlayer is inside the garrison.
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static bool IsInGarrison(this LocalPlayer me)
        {
            try
            {
                if (me != null)
                    return GarrisonsZonesId.Contains(me.ZoneId);
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning(
                    "[PlayerExtensions] Error while checking if LocalPlayer is in Garrison.");
                GarrisonButler.Diagnostic("[PlayerExtensions] Error of type: ", e.GetType());
            }
            return false;
        }
    }
}

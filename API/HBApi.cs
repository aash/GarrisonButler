
#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler;
using GarrisonButler.Libraries;
using Styx;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.API
{
    class HbApi
    {
        internal static readonly List<uint> GarrisonsZonesId = new List<uint>
        {
            7078, // Lunarfall - Ally
            7004, // Frostwall - Horde
        };

        internal static LocalPlayer Me = StyxWoW.Me;

        internal static bool IsInGarrison()
        {
            return GarrisonsZonesId.Contains(Me.ZoneId);
        }
    }
}

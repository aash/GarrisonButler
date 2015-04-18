using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
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

        /// <summary>        
        /// Check if localPlayer has a way to disenchant an item
        /// </summary>
        /// <param name="me"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool CanDisenchant(this LocalPlayer me, WoWItem item)
        {
            try
            {
                if (me != null && item != null)
                {
                    // Check if not possible right now
                    if (!SpellManager.HasSpell(13262) ||
                        StyxWoW.Me.IsDead ||
                        StyxWoW.Me.IsActuallyInCombat)
                    {
                        return false;
                    }

                    // Check if the item is in ignored list
                    if (ButlerCoroutines.AtomsLibrary.Garrison.DisenchantItems.IgnoredItem.Contains(item.Guid))
                        return false;

                    // Seems it's a go!
                    return true;
                }

            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning(
                    "[PlayerExtensions] Error while checking if LocalPlayer can disenchant an item.");
                GarrisonButler.Diagnostic("[PlayerExtensions] Error of type: ", e.GetType());
            }
            return false;
        }

        public static bool IsInGarrisonMine(this LocalPlayer me)
        { 
            return MinesId.Contains(me.SubZoneId);
        }

        private static readonly List<uint> MinesId = new List<uint>
        {
            7324, //ally 1
            7325, // ally 2
            7326, // ally 3
            7327, // horde 1
            7328, // horde 2
            7329 // horde 3
        };

    }
}

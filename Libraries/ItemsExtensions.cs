using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Libraries
{
    public static class ItemsExtensions
    {

        /// <summary>
        /// Returns if an item can be mailed. Check for Null included
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsMailable(this WoWItem item)
        {
            return item != null
                   && item.IsValid
                   && !item.IsConjured
                   && !item.IsMe;
        }

        /// <summary>
        /// Check if an item is included in the protected lists of items of HB
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsProtected(this WoWItem item)
        {
            var isProtected = false;
            try
            {
                if(item != null)
                    isProtected = ProtectedItemsManager.Contains(item.Entry);
            }
            catch (Exception e)
            {
                GarrisonButler.Warning("[ItemsExtensions] Error while checking ProtectedItemsManager. Will consider item as protected to be safe.");
                GarrisonButler.Diagnostic("[ItemsExtensions] Error of type: ", e.GetType());
                isProtected = true;
            }
            return isProtected;
        }
    }
}
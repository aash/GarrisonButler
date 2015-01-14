using System;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals.WoWObjects;
using GarrisonButler.API;

namespace GarrisonButler.Libraries
{
    public static class ObjectsExtensions
    {
        /// <summary>
        /// Returns if an item can be mailed. Check for Null included
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static void InteractAndCloseGarrisonWindow(this WoWObject theObject)
        {
            InterfaceLua.CloseLandingPage();
            theObject.Interact();
        }
    }
}
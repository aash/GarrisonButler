using System;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals.WoWObjects;
using GarrisonButler.API;

namespace GarrisonButler.Libraries
{
    public static class ObjectsExtensions
    {
        /// <summary>
        /// Close garrison window and interact with object
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
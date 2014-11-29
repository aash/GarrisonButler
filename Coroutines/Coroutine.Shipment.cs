using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {

        private static List<uint> GarrisonCaches = new List<uint>()
        {
            236916,
            237191,
            237724,
            237723,
            237722,
            237720
        }; 

        public static async Task<bool> PickUpGarrisonCache()
        {
            var cache = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null)
                return false;
            
            if(await MoveTo(cache.Location))
                return true;

            cache.Interact();
            return true;
        }
    }
}

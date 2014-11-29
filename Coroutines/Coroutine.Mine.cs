using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {

        private static List<uint> mineItems = new List<uint>()
        {
            232541, // Mine cart
            232542, // Blackrock Deposit 
            232543, // Rich Blackrock Deposit 
            232544, // True iron deposit
            232545 // Rich True iron deposit
        }; 

        public static async Task<bool> CleanMine()
        {
            var cache = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => mineItems.Contains(o.Entry)).ToList();
            if (!cache.Any())
                return false;

            var itemToCollect = cache.OrderBy(i => i.Distance).First();
            if(await MoveTo(itemToCollect.Location))
                return true;

            itemToCollect.Interact();
            await Buddy.Coroutines.Coroutine.Sleep(1000);
            return true;
        }
    }
}

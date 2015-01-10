
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
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

        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        internal static void StackItems()
        {
            Lua.DoString(@"
            local items={}  
            local done = 1  
            for bag = 0,4 do  
                for slot=1,GetContainerNumSlots(bag) do  
                    local id = GetContainerItemID(bag,slot)  
                    local _,c,l = GetContainerItemInfo(bag, slot)  
                    if id ~= nil then  
                        local n,_,_,_,_,_,_, maxStack = GetItemInfo(id)  
                        if c < maxStack then  
                            if items[id] == nil then  
                                items[id] = {left=maxStack-c,bag=bag,slot=slot,locked = l or 0}  
                            else  
                                if items[id].locked == 0 then  
                                    PickupContainerItem(bag, slot)  
                                    PickupContainerItem(items[id].bag, items[id].slot)  
                                    items[id] = nil  
                                else  
                                    items[id] = {left=maxStack-c,bag=bag,slot=slot,locked = l or 0}  
                                end  
                                done = 0  
                            end  
                        end  
                    end  
                end  
            end  
            return done 
        ");
        }
        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        internal async static Task<bool> StackAllItemsIfPossible()
        {
            if (!AnyItemsStackable())
                return false;

            await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                if (AnyItemsStackable())
                {
                    StackItems();
                    return false;
                }
                return true;
            });
            return true;
        }

        /// <summary>
        /// Returns if any items in bags can be stacked with another one.
        /// </summary>
        /// <returns></returns>
        internal static bool AnyItemsStackable()
        {
            List<WoWItem> stackable =
                Me.BagItems
                .Where(i => i.StackCount < ApiLua.GetMaxStackItem(i.Entry))
                .GetEmptyIfNull()
                .ToList();

            while (stackable.Count > 0)
            {
                WoWItem currentItem = stackable[0];
                stackable.RemoveAt(0);

                if (stackable.Any(d => d.Entry == currentItem.Entry))
                    return true;
            }

            return false;
        }

    }
}

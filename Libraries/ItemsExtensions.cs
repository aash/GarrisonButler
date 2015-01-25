using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.Coroutines;
using Styx;
using Styx.CommonBot.Profiles;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Libraries
{
    public static class ItemsExtensions
    {
        /// <summary>
        ///     Returns if an item can be mailed. Check for Null included
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsMailable(this WoWItem item)
        {
            return item != null
                   && item.IsValid
                   && !item.IsConjured
                   && !item.IsMe
                   && !item.IsSoulbound;
        }

        public static bool IsDisenchantable(this WoWItem item)
        {
            var armorTypeString = ApiLua.GetArmorTypeString();
            var weaponTypeString = ApiLua.GetWeaponTypeString();
            var itemType = ApiLua.GetItemTypeString((int) item.Entry);

            return item != null
                   && item.IsValid
                   && !item.IsConjured
                   && !item.IsMe
                   && !item.IsAccountBound
                   && (itemType == armorTypeString || itemType == weaponTypeString)
                   && (item.Quality == WoWItemQuality.Uncommon
                       || item.Quality == WoWItemQuality.Rare
                       || item.Quality == WoWItemQuality.Epic)
                   && !item.IsProtected()
                   && !item.IsOpenable;
        }

        /// <summary>
        ///     Check if an item is included in the protected lists of items of HB
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsProtected(this WoWItem item)
        {
            var isProtected = false;
            try
            {
                if (item != null)
                    isProtected = ProtectedItemsManager.Contains(item.Entry);
            }
            catch (Exception e)
            {
                if (e is CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning(
                    "[ItemsExtensions] Error while checking ProtectedItemsManager. Will consider item as protected to be safe.");
                GarrisonButler.Diagnostic("[ItemsExtensions] Error of type: ", e.GetType());
                isProtected = true;
            }
            return isProtected;
        }

        /// <summary>
        ///     Split an item to the desired amount to the desired free slot
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        public static async Task<ActionResult> Split(this WoWItem item, int amount)
        {
            var freeBagIndex = int.MinValue;
            var freeBagSlot = int.MinValue;
            var Backpack = StyxWoW.Me.Inventory.Backpack;
            for (uint i = 0; i < Backpack.Slots; i++)
            {
                var slotItem = Backpack.GetItemBySlot(i);
                if (slotItem == null)
                {
                    freeBagIndex = -1;
                    freeBagSlot = (int) i;
                    break;
                }
            }
            for (var index = 0U; index < 4U; ++index)
            {
                WoWBag bagAtIndex = StyxWoW.Me.GetBagAtIndex(index);
                if (bagAtIndex != null)
                {
                    for (uint i = 0; i < bagAtIndex.Slots; i++)
                    {
                        var slotItem = bagAtIndex.GetItemBySlot(i);
                        if (slotItem == null)
                        {
                            freeBagIndex = (int) index;
                            freeBagSlot = (int) i;
                            break;
                        }
                    }

                    if (freeBagIndex != int.MinValue
                        && freeBagSlot != int.MinValue)
                        break;
                }
                if (freeBagIndex != int.MinValue
                    && freeBagSlot != int.MinValue)
                    break;
            }

            // No free slots
            if (freeBagIndex == int.MinValue
                || freeBagSlot == int.MinValue)
            {
                GarrisonButler.Diagnostic("[Items] Split - No free bag slot found. item={0}, amount={1}", item.Entry, amount);
                return ActionResult.Failed;
            }

            GarrisonButler.Diagnostic("[Items] Split - item={0}, amount={1}, freeBag={2}, freeSlot={3}", item.Entry, amount, freeBagIndex, freeBagSlot);

            return await ButlerLua.SplitItem(item.BagIndex, item.BagSlot, amount, freeBagIndex, freeBagSlot);
        }

        /// <summary>
        /// Return the number in bags.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static long GetNumberItemInBags(this WoWItem item)
        {
            return HbApi.GetNumberItemInBags(item.Entry);
        }

        /// <summary>
        /// Return the number in reagent bank.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static long GetNumberItemInReagentBank(this WoWItem item)
        {
            return HbApi.GetNumberItemInReagentBank(item.Entry);
        }

        /// <summary>
        /// Return the list of this item in bags
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static IEnumerable<WoWItem> GetInBags(this WoWItem item)
        {
            return HbApi.GetItemInBags(item.Entry);
        }

        public static async Task<ActionResult> Mill(this WoWItem item)
        {
            return await HbApi.MillHerbFromBags(item.Entry);
        }
    }
}
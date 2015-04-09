using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines
{
    class Enchanting
    {
        internal static List<WoWGuid> IgnoredItem = new List<WoWGuid>(); 

        /// <summary>
        /// Check if the player has any item which he can disenchant.
        /// </summary>
        /// <returns></returns>
        public async static Task<Result> CanDisenchantItems()
        {
            if (!GaBSettings.Get().ShouldDisenchant)
                return new Result(ActionResult.Failed);

            if (GetItemsToDisenchant().Any())
                return new Result(ActionResult.Running);

            return new Result(ActionResult.Failed);
        }

        /// <summary>
        /// Disenchant one item then ask for a refresh.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static async Task<Result> DisenchantItems(object o)
        {
            // For all items disenchantable, disenchant
            var itemToDisenchant = GetItemsToDisenchant().FirstOrDefault();
            if (itemToDisenchant == default(WoWItem))
            {
                GarrisonButler.Diagnostic("[DisenchantItems] Called but failed to get an item to disenchant.");
                return new Result(ActionResult.Failed, "Failed to get an item to disenchant.");
            }
            var result = await itemToDisenchant.Disenchant();
            if (result.Status == ActionResult.Failed)
            {
                GarrisonButler.Diagnostic("DisenchantItems failed to disenchant item {0}, error: {1}", itemToDisenchant.Name, result.content);
                return new Result(ActionResult.Failed);
            }
            // Successfuly disenchanted, adding to ignore list
            IgnoredItem.Add(itemToDisenchant.Guid);

            return new Result(ActionResult.Refresh);
        }

        /// <summary>
        /// Get all items that the user can disenchant based on settings, ignored list, skill
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetItemsToDisenchant()
        {
            var myItems = StyxWoW.Me.BagItems;
            return
                myItems.Where(
                    i =>
                        CheckAgainstSettings(i)
                    );

        }

        /// <summary>
        /// Check an item to disenchant against the settings of the user
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static bool CheckAgainstSettings(WoWItem item)
        {
            var settings = GaBSettings.Get();

            if (item.Quality > (WoWItemQuality) settings.MaxDisenchantQuality)
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because quality: {1} > {2}", item.SafeName, item.Quality, settings.MaxDisenchantQuality.ToString());
                return false;
            }

            if (item.ItemInfo.Level > settings.MaxDisenchantIlvl)
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because ilvl: {1} > {2}", item.SafeName, item.ItemInfo.Level, item.ItemInfo.Level);
                return false;
            }


            if ((item.ItemInfo.Bond == WoWItemBondType.OnEquip) && !settings.ShouldDisenchantBoE)
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because BOE {1} - {2}", item.SafeName, (item.ItemInfo.Bond == WoWItemBondType.OnEquip), settings.ShouldDisenchantBoE);
                return false;
            }

            if ((item.ItemInfo.Bond == WoWItemBondType.OnPickup) && !settings.ShouldDisenchantBoP)
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because BOP {1} - {2}", item.SafeName, (item.ItemInfo.Bond == WoWItemBondType.OnPickup), settings.ShouldDisenchantBoP);
                return false;
            }

            if (!item.IsDisenchantable())
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because engine thinks item is not disenchantable.", item.Name);
                return false;
            }

            if (!StyxWoW.Me.CanDisenchant(item))
            {
                GarrisonButler.Diagnostic("Cannot disenchant {0} because engine think I can't disenchant it.", item.Name);
                return false;
            }
            GarrisonButler.Diagnostic("Can disenchant {0}", item.SafeName);

            return true;

        }

    }
}

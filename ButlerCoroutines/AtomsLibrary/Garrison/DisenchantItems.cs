#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    internal class DisenchantItems : Atom
    {
        internal static readonly List<WoWGuid> IgnoredItem = new List<WoWGuid>();
        //public new bool ShouldRepeat = true;


        public DisenchantItems()
        {
        }

        public override bool RequirementsMet()
        {
            return true;
        }

        public override bool IsFulfilled()
        {
            return GaBSettings.Get().ShouldDisenchant && !GetItemsToDisenchant().Any();
        }

        public async override Task Action()
        {
            // For all items disenchantable, disenchant
            var itemToDisenchant = GetItemsToDisenchant().FirstOrDefault();
            if (itemToDisenchant == default(WoWItem))
            {
                GarrisonButler.Diagnostic("[DisenchantItems] Called but failed to get an item to disenchant.");
                Status = new Result(ActionResult.Failed, "Failed to get an item to disenchant.");

                return;
            }
            var result = await itemToDisenchant.Disenchant();
            if (result.State == ActionResult.Failed)
            {
                GarrisonButler.Diagnostic("DisenchantItems failed to disenchant item {0}, error: {1}", itemToDisenchant.Name, result.Content);
                Status = new Result(ActionResult.Failed, string.Format("DisenchantItems failed to disenchant item {0}, error: {1}", itemToDisenchant.Name, result.Content));

                return;
            }
            // Successfuly disenchanted, adding to ignore list
            IgnoredItem.Add(itemToDisenchant.Guid);
        }

        public override string Name()
        {
            return "[DisenchantingItems]";
        }

        /// <summary>
        /// Get all items that the user can disenchant based on settings, ignored list, skill
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetItemsToDisenchant()
        {
            var myItems = StyxWoW.Me.BagItems;
            return myItems.Where(CheckAgainstSettings);
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
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because quality: {1} > {2}", item.SafeName,
                    //item.Quality, settings.MaxDisenchantQuality.ToString());
                return false;
            }

            if (item.ItemInfo.Level > settings.MaxDisenchantIlvl)
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because ilvl: {1} > {2}", item.SafeName,
                    //item.ItemInfo.Level, settings.MaxDisenchantIlvl);
                return false;
            }

            if (item.ItemInfo.Level < settings.MinDisenchantIlvl)
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because ilvl: {1} < {2}", item.SafeName,
                    //item.ItemInfo.Level, settings.MinDisenchantIlvl);
                return false;
            }


            if ((item.ItemInfo.Bond == WoWItemBondType.OnEquip) && !settings.ShouldDisenchantBoE)
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because BOE {1} - {2}", item.SafeName,
                    //(item.ItemInfo.Bond == WoWItemBondType.OnEquip), settings.ShouldDisenchantBoE);
                return false;
            }

            if ((item.ItemInfo.Bond == WoWItemBondType.OnPickup) && !settings.ShouldDisenchantBoP)
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because BOP {1} - {2}", item.SafeName,
                    //(item.ItemInfo.Bond == WoWItemBondType.OnPickup), settings.ShouldDisenchantBoP);
                return false;
            }

            if (!item.IsDisenchantable())
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because engine thinks item is not disenchantable.",
                    //item.Name);
                return false;
            }

            if (!StyxWoW.Me.CanDisenchant(item))
            {
                //GarrisonButler.Diagnostic("Cannot disenchant {0} because engine think I can't disenchant it.", item.Name);
                return false;
            }
            GarrisonButler.Diagnostic("Can disenchant {0}", item.SafeName);

            return true;
        }
    }
}
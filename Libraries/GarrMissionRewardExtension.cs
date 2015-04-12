using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.Objects;
using Styx.WoWInternals;
using Styx.WoWInternals.DB;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Libraries
{
    public static class GarrMissionRewardExtension
    {
        public static MissionReward.MissionRewardCategory Category(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.Category: !!! Passed in GarrMissionReward was null !!!");
                return MissionReward.MissionRewardCategory.Unknown;
            }

            var foundRewardObject = MissionReward.AllRewards.FirstOrDefault(r => r.Id == source.GarrisonButlerRewardId());
            var returnValue = foundRewardObject == null
                ? MissionReward.MissionRewardCategory.Unknown
                : foundRewardObject.Category;

            return returnValue;
        }

        public static bool IsGarrisonResources(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsGarrisonResources: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return source.CurrencyType == WoWCurrencyType.GarrisonResources;
        }

        public static bool IsCurrencyReward(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsCurrencyReward: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return source.CurrencyType > 0;
        }

        public static bool IsGold(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsGold: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return !source.IsFollowerXP()
                && !source.IsCurrencyReward()
                && !source.IsItemReward();
        }

        // ReSharper disable once InconsistentNaming
        public static bool IsFollowerXP(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsFollowerXP: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return source.FollowerXP > 0;
        }

        public static bool IsItemReward(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsItemReward: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return source.ItemId > 0;
        }

        public static bool IsRushOrder(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.IsRushOrder: !!! Passed in GarrMissionReward was null !!!");
                return false;
            }

            return source.ItemId == (int)MissionReward.RushOrderIds.Forge
                || source.ItemId == (int)MissionReward.RushOrderIds.Tannery
                || source.ItemId == (int)MissionReward.RushOrderIds.EnchanterStudy
                || source.ItemId == (int)MissionReward.RushOrderIds.EngineeringWorks
                || source.ItemId == (int)MissionReward.RushOrderIds.TailoringEmporium
                || source.ItemId == (int)MissionReward.RushOrderIds.GemBoutique
                || source.ItemId == (int)MissionReward.RushOrderIds.ScribeQuarter
                || source.ItemId == (int)MissionReward.RushOrderIds.AlchemyLab;
        }

        public static int GarrisonButlerRewardId(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.GarrisonButlerRewardId: !!! Passed in GarrMissionReward was null !!!");
                return -1;
            }

            if (source.IsFollowerXP())
            {
                return (int)MissionReward.MissionRewardCategory.FollowerExperience;
            }
            else if (source.IsItemReward())
            {
                var itemInfo = ItemInfo.FromId((uint)source.ItemId);
                if (itemInfo == null)
                {
                    GarrisonButler.Diagnostic(
                        "Error retrieving item info for GarrMissionReward GarrisonButler Id.  MissionId={0}",
                        source.GarrMissionId);
                    return -1;
                }
                return (int)itemInfo.Id;
            }
            else if (source.IsCurrencyReward())
            {
                var currencyInfo = WoWCurrency.GetCurrencyByType(source.CurrencyType);
                if (currencyInfo == null)
                {
                    GarrisonButler.Diagnostic(
                        "Error retrieving currency info for GarrMissionReward's name.  MissionId={0}",
                        source.GarrMissionId);
                    return -1;
                }
                return (int)currencyInfo.Entry;
            }
            else
            {
                return (int)MissionReward.MissionRewardCategory.Gold;
            }
        }

        public static int Quantity(this GarrMissionReward source)
        {
            return source == null
                ? 0
                : Math.Max(Math.Max(source.ItemQuantity, source.FollowerXP), source.CurrencyQuantity);
        }

        public static string Name(this GarrMissionReward source)
        {
            if (source == null)
            {
                GarrisonButler.Diagnostic("GarrMissionRewardExtension.Name: !!! Passed in GarrMissionReward was null !!!");
                return "Error";
            }
                
            if (source.IsFollowerXP())
            {
                return "FollowerXP";
            }
            else if (source.IsItemReward())
            {
                var itemInfo = ItemInfo.FromId((uint) source.ItemId);
                if (itemInfo == null)
                {
                    GarrisonButler.Diagnostic(
                        "Error retrieving item info for GarrMissionReward name.  MissionId={0}",
                        source.GarrMissionId);
                    return "Error";
                }
                return itemInfo.Name;
            }
            else if (source.IsCurrencyReward())
            {
                var currencyInfo = WoWCurrency.GetCurrencyByType(source.CurrencyType);
                if (currencyInfo == null)
                {
                    GarrisonButler.Diagnostic(
                        "Error retrieving currency info for GarrMissionReward's name.  MissionId={0}",
                        source.GarrMissionId);
                    return "Error";
                }
                return currencyInfo.Name;
            }
            else
            {
                return "Gold";
            }
        }
    }
}

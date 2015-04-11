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
            var foundRewardObject = MissionReward.AllRewards.FirstOrDefault(r => r.Id == source.Id);
            var returnValue = foundRewardObject == null
                ? MissionReward.MissionRewardCategory.Unknown
                : foundRewardObject.Category;

            return returnValue;
        }

        public static bool IsGarrisonResources(this GarrMissionReward source)
        {
            return source.CurrencyType == WoWCurrencyType.GarrisonResources;
        }

        public static bool IsCurrencyReward(this GarrMissionReward source)
        {
            return source.CurrencyType > 0;
        }

        public static bool IsGold(this GarrMissionReward source)
        {
            return !source.IsFollowerXP()
                && !source.IsCurrencyReward()
                && !source.IsItemReward();
        }

        public static bool IsFollowerXP(this GarrMissionReward source)
        {
            return source.FollowerXP > 0;
        }

        public static bool IsItemReward(this GarrMissionReward source)
        {
            return source.ItemId > 0;
        }

        public static string Name(this GarrMissionReward source)
        {
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

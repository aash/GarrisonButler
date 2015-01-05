#region

using System;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot.POI;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static async Task<bool> HarvestWoWGameOject(WoWGameObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }

            if (node != toHarvest)
                BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);

            return true;
        }

        private static WoWPoint CachedToHarvestLocation = WoWPoint.Empty;
        private static async Task<bool> HarvestWoWGameObjectCachedLocation(WoWGameObject toHarvest)
        {
            if (toHarvest != null && toHarvest.IsValid)
            {
                CachedToHarvestLocation = toHarvest.Location;
                return await HarvestWoWGameOject(toHarvest);
            }

            // First moving to cached location
            if(CachedToHarvestLocation != WoWPoint.Empty)
                if (await MoveTo(CachedToHarvestLocation))
                    return true;

            CachedToHarvestLocation = WoWPoint.Empty;

            return await HarvestWoWGameOject(toHarvest);
        }
    }
}
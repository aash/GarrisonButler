using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly List<uint> GarrisonCaches = new List<uint>
        {
            236916,
            237191,
            237724,
            237723,
            237722,
            237720
        };

        internal static List<uint> FinalizeGarrisonPlotIds = new List<uint>
        {
            232651,
            232652,
            233250,
            233251,
            236190,
            236191,
            236192,
            236193,
            236261,
            236263
        };

        public static async Task<bool> PickUpGarrisonCache()
        {
            if (!GaBSettings.Mono.GarrisonCache)
                return false;

            WoWGameObject cache =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null)
                return false;

            if (await MoveTo(cache.Location))
                return true;

            cache.Interact();
            return true;
        }

        public static async Task<bool> PickUpMineWorkOrders()
        {
            if (!GaBSettings.Mono.ShipmentsMine)
                return false;

            Building mine = _buildings.FirstOrDefault(b => MinesId.Contains(b.id));
            if (mine == null)
                return false;

            if (BuildingsLua.GetNumberShipmentReadyByBuildingId(mine.id) == 0)
                return false;

            WoWGameObject mineShipment =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235886);
            if (mineShipment == null)
            {
                //GarrisonBuddy.Diagnostic("Seems there's a problem, shipment for mine ready but can't find on map");
                return false;
            }

            if (await MoveTo(mineShipment.Location))
                return true;

            mineShipment.Interact();
            return true;
        }


        public static async Task<bool> PickUpGardenWorkOrders()
        {
            if (!GaBSettings.Mono.ShipmentsGarden)
                return false;

            Building garden = _buildings.FirstOrDefault(b => GardensId.Contains(b.id));
            if (garden == null)
                return false;

            if (BuildingsLua.GetNumberShipmentReadyByBuildingId(garden.id) == 0)
                return false;

            WoWGameObject gardenShipment =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235885);
            if (gardenShipment == null)
            {
                //GarrisonBuddy.Diagnostic("Seems there's a problem, shipment for garden ready but can't find on map");
                return false;
            }

            if (await MoveTo(gardenShipment.Location))
                return true;

            gardenShipment.Interact();
            return true;
        }

        public static async Task<bool> ActivateFinishedBuildings()
        {
            if (!GaBSettings.Mono.ActivateBuildings)
                return false;

            IOrderedEnumerable<WoWGameObject> toActivate =
                ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .ToList()
                    .OrderBy(o => o.Location.X);
            if (!toActivate.Any())
                return false;

            if (await MoveTo(toActivate.First().Location))
                return true;

            toActivate.First().Interact();
            await Buddy.Coroutines.Coroutine.Sleep(5000);
            return true;
        }


        // DOESNT WORK SINCE SOME WORK ORDER ARE ALWAYS THERE! 
        //public static async Task<bool> PickUpAllWorkOrders()
        //{
        //    var shipments = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.SubType == WoWGameObjectType.GarrisonShipment).OrderBy(o=>o.Entry);

        //    foreach (var shipment in shipments)
        //    {
        //        if (await MoveTo(shipment.Location))
        //            return true;
        //        shipment.Interact();

        //    }
        //    return true;
        //}
    }
}
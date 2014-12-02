using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using GarrisonLua;
using Styx;
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

        internal static readonly List<uint> FinalizeGarrisonPlotIds = new List<uint>
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
            236262,
            236263
        };
        private static readonly WoWPoint MineShipmentAlly = new WoWPoint(1901.799, 103.2309, 83.52671);
        private static readonly WoWPoint MineShipmentHorde = new WoWPoint(5474.07, 4451.756, 144.5106);

        private static readonly WoWPoint GardenShipmentAlly = new WoWPoint(1901.799, 103.2309, 83.52671);
        private static readonly WoWPoint GardenShipmentHorde = new WoWPoint(5414.973, 4574.003, 137.4256);

        private static readonly List<uint> MineShipmentIds = new List<uint>()
        {
            239237,
            235886,
        };

        private static async Task<bool> PickUpGarrisonCache()
        {
            if (!GaBSettings.Mono.GarrisonCache)
                return false;

            WoWGameObject cache =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null)
                return false;

            GarrisonBuddy.Diagnostic("Shipment: Detected garrison cache available to collect.");

            if (await MoveTo(cache.Location, "Collecting garrison cache"))
                return true;

            cache.Interact();
            return true;
        }

        private static async Task<bool> PickUpMineWorkOrders()
        {
            if (!GaBSettings.Mono.ShipmentsMine)
                return false;

            Building mine = _buildings.FirstOrDefault(b => MinesId.Contains(b.id));
            if (mine == null)
                return false;

            var numShipments = BuildingsLua.GetNumberShipmentReadyByBuildingId(mine.id);
            if (numShipments < 1)
                return false;

            GarrisonBuddy.Diagnostic("Shipment: Detected " + numShipments + " shipments to collect from mine.");

            WoWGameObject mineShipment =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235886);
            if (mineShipment == null)
            {
                GarrisonBuddy.Diagnostic("Seems there's a problem, shipment for mine ready but can't find on map. Trying to move to default location.");
                return await MoveTo(Me.IsAlliance ? MineShipmentAlly : MineShipmentHorde, "Default location for mine shipments");
            }

            if (await MoveTo(mineShipment.Location, "Collecting mine shipments"))
                return true;

            mineShipment.Interact();
            return true;
        }


        private static async Task<bool> PickUpGardenWorkOrders()
        {
            if (!GaBSettings.Mono.ShipmentsGarden)
                return false;

            Building garden = _buildings.FirstOrDefault(b => GardensId.Contains(b.id));
            if (garden == null)
                return false;


            var numShipments = BuildingsLua.GetNumberShipmentReadyByBuildingId(garden.id);
            if (numShipments < 1)
                return false;

            GarrisonBuddy.Diagnostic("Shipment: Detected " + numShipments + " shipments to collect from garden.");
            
            WoWGameObject gardenShipment =
                ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235885);
            if (gardenShipment == null)
            {
                GarrisonBuddy.Diagnostic("Seems there's a problem, shipment for garden ready but can't find on map. Trying to move to known location.");
                return await MoveTo(Me.IsAlliance ? GardenShipmentAlly : GardenShipmentHorde, "Default location for garden shipments");
            }

            if (await MoveTo(gardenShipment.Location, "Collecting garden shipments"))
                return true;

            gardenShipment.Interact();
            return true;
        }

        private static async Task<bool> ActivateFinishedBuildings()
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

            GarrisonBuddy.Diagnostic("Shipment: Found " + toActivate.Count() + " buildings to activate.");

            if (await MoveTo(toActivate.First().Location,"Building activation"))
                return true;


            await Buddy.Coroutines.Coroutine.Sleep(300); 
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
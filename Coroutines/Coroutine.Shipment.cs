using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {

        private static List<uint> GarrisonCaches = new List<uint>()
        {
            236916,
            237191,
            237724,
            237723,
            237722,
            237720
        };

        public static async Task<bool> PickUpGarrisonCache()
        {
            var cache = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => GarrisonCaches.Contains(o.Entry));
            if (cache == null)
                return false;

            if (await MoveTo(cache.Location))
                return true;

            cache.Interact();
            return true;
        }
        public static async Task<bool> PickUpMineWorkOrders()
        {
            var mine = _buildings.FirstOrDefault(b => MinesId.Contains(b.id));
            if (mine == null)
                return false;

            if (mine.shipmentsReady == 0)
                return false;

            var mineShipment = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235886);
            if (mineShipment == null)
            {
                GarrisonBuddy.Debug("Seems there's a problem, shipment for mine ready but can't find on map");
                return false;                
            }

            if (await MoveTo(mineShipment.Location))
                return true;

            mineShipment.Interact();
            return true;
        }


        public static async Task<bool> PickUpGardenWorkOrders()
        {
            var garden = _buildings.FirstOrDefault(b => GardensId.Contains(b.id));
            if (garden == null)
                return false;

            if (garden.shipmentsReady == 0)
                return false;

            var gardenShipment = ObjectManager.GetObjectsOfType<WoWGameObject>().FirstOrDefault(o => o.Entry == 235885);
            if (gardenShipment == null)
            {
                GarrisonBuddy.Debug("Seems there's a problem, shipment for garden ready but can't find on map");
                return false;
            }

            if (await MoveTo(gardenShipment.Location))
                return true;

            gardenShipment.Interact();
            return true;
        }


        public static async Task<bool> PickUpAllWorkOrders()
        {
            var shipments = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.SubType == WoWGameObjectType.GarrisonShipment).OrderBy(o=>o.Entry);

            foreach (var shipment in shipments)
            {
                if (await MoveTo(shipment.Location))
                    return true;
                shipment.Interact();

            }
            return true;
        }
    }
}

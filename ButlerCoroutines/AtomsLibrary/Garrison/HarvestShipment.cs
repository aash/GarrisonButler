using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using Styx;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    class HarvestShipment : HarvestObject
    {
        private readonly Building _building;

        public HarvestShipment(Building building, WoWGameObject shipment)
            : base(shipment)
        {
            _building = building; 
        }

        // no pick up left to do
        public override bool IsFulfilled()
        {
            GarrisonButler.Log("[HarvestShipment] IsFulfilled called.");
            _building.Refresh();
            var canUseNow = false;
            try
            {
                canUseNow = Toharvest.CanUseNow();
            }
            catch (Exception)
            {}

            return _building.ShipmentsReady == 0 
                && (LootFrame.Instance == null || !LootFrame.Instance.IsVisible)
                && !canUseNow; // In case shipment has been cleared but not looted (seems that the 2 mechanisms are not linked) 
        }
    }
}

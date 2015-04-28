using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    class PickUpWorkOrders : Atom
    {
        public override string Name()
        {
            return "[PickUpWorkOrders|" + _building.Name + "]";
        }
        protected Building _building;
        private WoWGameObject _buildingAsObject;
        private WoWPoint _locationToLookAt;
        private WoWGameObject _shipmentToCollect;
        private Atom _currentAction;

        public PickUpWorkOrders(Building building)
        {
            _building = building;
        }
        public PickUpWorkOrders()
        {
            _building = null;
        }
        public override bool RequirementsMet()
        {
            _building.Refresh();
            if (_building.ShipmentsReady <= 0)
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] No shipment left to pickup: {0}", _building.Name);
                return false;
            }

            // Get the list of the building objects
            _buildingAsObject =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => _building.Displayids.Contains(o.DisplayId))
                    .OrderBy(o => o.DistanceSqr)
                    .FirstOrDefault();

            if (_buildingAsObject == default(WoWGameObject))
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] Building could not be found in the area: {0}",
                    _building.Name);
                foreach (var id in _building.Displayids)
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp]     ID {0}", id);
                }
                return false;
            }

            // Fix for the mine position
            var minesIds = ButlerCoroutine._buildings.Where(
                b =>
                    (b.Id == (int)Buildings.MineLvl1) ||
                    (b.Id == (int)Buildings.MineLvl2) ||
                    (b.Id == (int)Buildings.MineLvl3)).SelectMany(b => b.Displayids);
            if (minesIds.Contains(_buildingAsObject.DisplayId))
                _locationToLookAt = StyxWoW.Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
            else
                _locationToLookAt = _buildingAsObject.Location;

            GarrisonButler.Diagnostic("[ShipmentPickUp] Found {0} shipments to collect: {1}",
                _building.ShipmentsReady,
                _building.Name);
            return true;
        }

        public override bool IsFulfilled()
        {
            _building.Refresh();


            // Activated by user ?
            var buildingsettings = GaBSettings.Get().GetBuildingSettings(_building.Id);
            if (buildingsettings == null)
                return true;

            if (!buildingsettings.CanCollectOrder)
            {
                GarrisonButler.Diagnostic("[ShipmentPickUp] Deactivated in user settings: {0}", _building.Name);
                return true;
            }

            return _building.ShipmentsReady == 0 && (_currentAction == null || _currentAction.IsFulfilled());
        }

        public async override Task Action()
        {
            if(_shipmentToCollect == null || _shipmentToCollect == default(WoWGameObject))
            {
                // Fix for the mine position
                var minesIds = ButlerCoroutine._buildings.Where(
                    b =>
                        (b.Id == (int)Buildings.MineLvl1) ||
                        (b.Id == (int)Buildings.MineLvl2) ||
                        (b.Id == (int)Buildings.MineLvl3)).SelectMany(b => b.Displayids);
                if (minesIds.Contains(_buildingAsObject.DisplayId))
                    _locationToLookAt = StyxWoW.Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
                else
                    _locationToLookAt = _buildingAsObject.Location;

                // Search for shipment next to building
                _shipmentToCollect =
                    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .Where(
                            o =>
                                o.SubType == WoWGameObjectType.GarrisonShipment &&
                                o.Location.DistanceSqr(_locationToLookAt) < 2500f)
                        .OrderBy(o => o.Location.DistanceSqr(_locationToLookAt))
                        .FirstOrDefault();

                if (_shipmentToCollect != default(WoWGameObject))
                    _currentAction = new HarvestShipment(_building, _shipmentToCollect);

                else if (_buildingAsObject != null)
                    _currentAction = new MoveTo(_locationToLookAt, 10);

                else
                    _currentAction = new MoveTo(_building.Pnj, 10);
            }

            if (_currentAction != null)
                await _currentAction.Execute();
        }
    }
}

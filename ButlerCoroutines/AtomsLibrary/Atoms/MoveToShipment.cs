#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class MoveToShipment : MoveTo
    {
        private WoWGameObject _shipmentToCollect = default(WoWGameObject);
        private readonly Building _building;

        public MoveToShipment(Building building)
            : base(building.Pnj)
        {
            _building = building;
        }

        public override async Task Action()
        {
            // refresh location if needed 
            if (_shipmentToCollect == default(WoWGameObject))
            {
                // Get the list of the building objects
                var buildingAsObject =
                    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .Where(o => _building.Displayids.Contains(o.DisplayId))
                        .OrderBy(o => o.DistanceSqr)
                        .FirstOrDefault();

                // If can't find building we keep default lcoation
                if (buildingAsObject == default(WoWGameObject))
                {
                    GarrisonButler.Diagnostic("[ShipmentPickUp] Building could not be found in the area: {0}",
                        _building.Name);
                    foreach (var id in _building.Displayids)
                    {
                        GarrisonButler.Diagnostic("[ShipmentPickUp]     ID {0}", id);
                    }
                }
                else // building found let's look for the shipment now
                {
                    // Fix for the mine position
                    WoWPoint locationToLookAt;
                    var minesIds = ButlerCoroutine._buildings.Where(
                        b =>
                            (b.Id == (int)Buildings.MineLvl1) ||
                            (b.Id == (int)Buildings.MineLvl2) ||
                            (b.Id == (int)Buildings.MineLvl3)).SelectMany(b => b.Displayids);

                    if (minesIds.Contains(buildingAsObject.DisplayId))
                        locationToLookAt = StyxWoW.Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
                    else
                        locationToLookAt = buildingAsObject.Location;

                    _shipmentToCollect = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .Where(
                            o =>
                                o.SubType == WoWGameObjectType.GarrisonShipment &&
                                o.Location.DistanceSqr(locationToLookAt) < 2500f)
                        .OrderBy(o => o.Location.DistanceSqr(locationToLookAt))
                        .FirstOrDefault();

                    if (_shipmentToCollect != default(WoWGameObject))
                    {
                        Location.X = _shipmentToCollect.X;
                        Location.Y = _shipmentToCollect.Y;
                        Location.Z = _shipmentToCollect.Z;
                    }
                    else
                    {
                        Location.X = locationToLookAt.X;
                        Location.Y = locationToLookAt.Y;
                        Location.Z = locationToLookAt.Z;
                    }
                }
            }
            await base.Action();
        }

        public override string Name()
        {
            return "[MoveToShipment|" + _building.Name + "]";
        }
    }
}
#region

using System;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    internal class StartWorkOrders : Atom
    {
        public override string Name()
        {
            return "[StartWorkOrders|" + _building.Name + "]";
        }

        protected Building _building;
        private Atom _currentAction;
        private Atom _freshAction = null;
        private Atom _preOrderOps; 

        public StartWorkOrders(Building building)
        {
            _building = building;
            Dependencies.Add(new RefreshOrderReagents(_building));
        }

        public StartWorkOrders()
        {
            _building = null;
        }

        public override bool RequirementsMet()
        {
            if (_building == null)
            {
                Status = new Result(ActionResult.Failed, "Building is null, either not built or not properly scanned.");
                return false;
            }
            //GarrisonButler.Diagnostic("[StartWorkOrders] Checking Requirements for {0}", _building.Name);

            _building.Refresh();
            var buildingsettings = GaBSettings.Get().GetBuildingSettings(_building.Id);
            if (buildingsettings == null)
                return false;


            if (_building.WorkFrameWorkAroundTries >= Building.WorkFrameWorkAroundMaxTriesUntilBlacklist)
            {
                Status = new Result(ActionResult.Failed,
                    string.Format("Building has been blacklisted due to reaching maximum Blizzard Workframe Bug workaround tries ({0})", Building.WorkFrameWorkAroundMaxTriesUntilBlacklist));
                return false;
            }

            // No Shipment left to start
            if (_building.NumberShipmentLeftToStart <= 0)
            {
                Status = new Result(ActionResult.Failed, "No shipment left to start");
                return false;
            }

            // Under construction
            if (_building.IsBuilding || _building.CanActivate)
            {
                Status = new Result(ActionResult.Failed, "Building under construction");
                return false;
            }

            // Structs cannot be null
            var shipmentObjectFound =
                ButlerCoroutine.ShipmentsMap.FirstOrDefault(s => s.BuildingIds.Contains(_building.Id));

            if (!shipmentObjectFound.CompletedPreQuest)
            {
                Status = new Result(ActionResult.Failed,String.Format("Cannot start shipment until preQuest is completed. A={1} H={2}: {0}", _building.Name, shipmentObjectFound.ShipmentPreQuestIdAlliance,shipmentObjectFound.ShipmentPreQuestIdHorde));
                return false;
            }

            // Reached limit of tries?
            if (_building.StartWorkOrderTries >= Building.StartWorkOrderMaxTries)
            {
                Status = new Result(ActionResult.Failed, String.Format("Cannot start shipments due to reaching max tries: {0}", Building.StartWorkOrderMaxTries));
                return false;
            }
            // If need to do an action to know maxShipment to start
            if (_building.IsActionForRefreshNeeded())
            {
                // Add refresh action
                Status = new Result(ActionResult.Running, String.Format(" Refresh needed!"));
                return true;
            }
            // max start by user ?
            var maxSettings = GaBSettings.Get().GetBuildingSettings(_building.Id).MaxCanStartOrder;

            var maxInProgress = maxSettings == 0
                ? _building.ShipmentCapacity
                : Math.Min(_building.ShipmentCapacity, maxSettings);

            var maxToStart = maxInProgress - _building.ShipmentsTotal;
            var maxCanComplete = _building.maxCanComplete();

            maxToStart = Math.Min(maxCanComplete, maxToStart);
            //GarrisonButler.Diagnostic(
            //    "[GetMaxShipmentToStart,{5}]: maxSettings={0} maxInProgress={1} ShipmentCapacity={2} CanCompleteOrder={3} maxToStart={4}",
            //    maxSettings, maxInProgress, _building.ShipmentCapacity, maxCanComplete, maxToStart, _building.Id);
            
            if (maxToStart <= 0)
            {
                Status = new Result(ActionResult.Failed, String.Format("Can't start more work orders. ShipmentsTotal={0}, MaxCanStartOrder={1}",
                    _building.ShipmentsTotal,
                    buildingsettings.MaxCanStartOrder)); 
                return false;
            }
            
            Status = new Result(ActionResult.Running, String.Format("Found {0} new work orders to start", maxToStart));
            return true;
        }

        public override bool IsFulfilled()
        {
            if (_building == null)
                return true;

            var buildingsettings = GaBSettings.Get().GetBuildingSettings(_building.Id);

            if (buildingsettings == null)
                return true;

            // Activated by user ?
            if (!buildingsettings.CanStartOrder)
            {
                Status = new Result(ActionResult.Done, String.Format("Deactivated in user settings"));
                return true;
            } 
            
            _building.Refresh();
            if (_building.IsActionForRefreshNeeded())
            {
                Status = new Result(ActionResult.Running, String.Format("Refresh reagents Needed"));
                return false;
            }
            if (_building.NumberShipmentLeftToStart > 0)
            {
                Status = new Result(ActionResult.Running, String.Format("Still orders left to start"));
                return false;
            }

            Status = new Result(ActionResult.Done, String.Format("Fulfilled")); 
            return true;
        }

        public override async Task Action()
        {
            if (_currentAction == null)
            {
                if (_preOrderOps == null)
                {
                            // max start by user ?
                    var maxSettings = GaBSettings.Get().GetBuildingSettings(_building.Id).MaxCanStartOrder;

                    var maxInProgress = maxSettings == 0
                        ? _building.ShipmentCapacity
                        : Math.Min(_building.ShipmentCapacity, maxSettings);

                    var maxToStart = maxInProgress - _building.ShipmentsTotal;
                    var maxCanComplete = _building.maxCanComplete();

                    maxToStart = Math.Min(maxCanComplete, maxToStart);

                    _preOrderOps = new PreCraftOperation(_building.ReagentId, maxToStart*_building.NumberReagent);
                }

                Status = new Result(ActionResult.Running, String.Format("Executing prep order!")); 
                
                if (!_preOrderOps.IsFulfilled())
                {
                    await _preOrderOps.Execute(); 
                    return;
                }

                Status = new Result(ActionResult.Running, String.Format("prep order fulfilled!")); 

                _currentAction = new StartShipment(_building);
            }

            await _currentAction.Execute();
        }
    }
}
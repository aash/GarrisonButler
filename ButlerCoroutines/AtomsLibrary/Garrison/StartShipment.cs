#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.Config;
using GarrisonButler.LuaObjects;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
{
    internal class StartShipment : Atom
    {
        public override string Name()
        {
            return "[StartShipment|" + _building.Name + "]";
        }
        private readonly Building _building;

        public StartShipment(Building building)
        {
            _building = building;
            Dependencies = new List<Atom>
            {
                new InteractWithOrderNpc(building)
            };
        }

        public override bool RequirementsMet()
        {
            return true;
        }

        public override bool IsFulfilled()
        {
            return _building.maxCanComplete() == 0;
        }

        public override async Task Action()
        {
            if (IsCreateAllOrders())
            {
                if (!await CapacitiveDisplayFrame.StartAllOrder(_building))
                {
                    Status = new Result(ActionResult.Failed);
                    return;
                }
                GarrisonButler.Log("Successfully started all work orders at {0}.", _building.Name);

                Status = new Result(ActionResult.Done);
                return;
            }
                // Otherwise we create them one by one
            var maxSettings = GaBSettings.Get().GetBuildingSettings(_building.Id).MaxCanStartOrder;

            var maxInProgress = maxSettings == 0
                ? _building.ShipmentCapacity
                : Math.Min(_building.ShipmentCapacity, maxSettings);

            var maxToStart = maxInProgress - _building.ShipmentsTotal;
            var maxCanComplete = _building.maxCanComplete();

            maxToStart = Math.Min(maxCanComplete, maxToStart);

            for (var i = 0; i < maxToStart; i++)
            {
                if (!await CapacitiveDisplayFrame.ClickStartOrderButton(_building))
                {
                    GarrisonButler.Diagnostic(
                        "[ShipmentStart,{0}] Max number of tries ({1}) reached to start shipment at {2}",
                        _building.Id, Building.StartWorkOrderMaxTries, _building.Name);

                    Status = new Result(ActionResult.Failed);
                    return;
                }
                await Coroutine.Yield();
            }
            //}
            var timeout = new WaitTimer(TimeSpan.FromMilliseconds(10000));
            timeout.Reset();

            while (!timeout.IsFinished)
            {
                _building.Refresh();
                maxSettings = GaBSettings.Get().GetBuildingSettings(_building.Id).MaxCanStartOrder;

                maxInProgress = maxSettings == 0
                    ? _building.ShipmentCapacity
                    : Math.Min(_building.ShipmentCapacity, maxSettings);

                maxToStart = maxInProgress - _building.ShipmentsTotal;
                maxCanComplete = _building.maxCanComplete();

                var max = Math.Min(maxCanComplete, maxToStart);
                if (max == 0)
                {
                    GarrisonButler.Log("[ShipmentStart{1}] Finished starting work orders at {0}.",
                        _building.Name, _building.Id);
                    Status = new Result(ActionResult.Done);
                    return;
                }
                GarrisonButler.Diagnostic("[ShipmentStart,{0}] Waiting for shipment to update.", _building.Id);
                await CommonCoroutines.SleepForRandomReactionTime();
                await Coroutine.Yield();
            }
        }

        private bool IsCreateAllOrders()
        {
            var maxSettings = GaBSettings.Get().GetBuildingSettings(_building.Id).MaxCanStartOrder;
            var maxInProgress = maxSettings == 0
                ? _building.ShipmentCapacity
                : Math.Min(_building.ShipmentCapacity, maxSettings);
            var maxWishedToStart = maxInProgress - _building.ShipmentsTotal;

            int maxCanComplete = _building.maxCanComplete();

            var maxToStart = Math.Min(maxCanComplete, maxWishedToStart);
            GarrisonButler.Diagnostic(
                "[IsCreateAllOrders,{5}]: maxSettings={0} maxInProgress={1} ShipmentCapacity={2} CanCompleteOrder={3} maxToStart={4}",
                maxSettings, maxInProgress, _building.ShipmentCapacity, maxCanComplete, maxToStart, _building.Id);

            if (maxToStart > 0 // Is there anything to start
                && maxCanComplete > 0 // and Can we start anything
                &&
                (maxCanComplete == maxToStart
                    // and (Is the number we want to start is the max we can start with our materials ?
                 || maxToStart == _building.ShipmentCapacity - _building.ShipmentsTotal))
                // Or is it the max the building allow us to start?)
            {
                return true;
            }
            return false;
        }
    }
}
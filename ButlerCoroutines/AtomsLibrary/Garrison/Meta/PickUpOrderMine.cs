using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Libraries;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class PickUpOrderMine : PickUpWorkOrders
    {
        public override string Name()
        {
            return "[PickUpOrderMine]";
        }
        public PickUpOrderMine() : base()
        {
            var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                 b =>
                     (b.Id == (int)Buildings.MineLvl1) ||
                     (b.Id == (int)Buildings.MineLvl2) ||
                     (b.Id == (int)Buildings.MineLvl3));
            if (mine != default(Building))
            {
                _building = mine;
                Dependencies.Add(new MoveToShipment(_building));
            }
        }
        public override bool RequirementsMet()
        {
            if (_building == null)
            {
                GarrisonButler.Diagnostic("[PickUpOrderMine] Building null, looking for building...");
                var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.Id == (int)Buildings.MineLvl1) ||
                    (b.Id == (int)Buildings.MineLvl2) ||
                    (b.Id == (int)Buildings.MineLvl3));
                if (mine != default(Building))
                {
                    GarrisonButler.Diagnostic("[PickUpOrderMine] Building Found!");
                    _building = mine;
                    Dependencies.Add(new MoveToShipment(_building));
                }
                else
                {
                    GarrisonButler.Diagnostic("[PickUpOrderMine] Building not Found!");
                    return false;
                }
            }
            return base.RequirementsMet();
        }
    }
}

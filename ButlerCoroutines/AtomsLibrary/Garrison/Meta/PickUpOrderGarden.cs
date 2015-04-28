using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.Libraries;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class PickUpOrderGarden : PickUpWorkOrders
    {
        public override string Name()
        {
            return "[PickUpOrderGarden]";
        }
        public PickUpOrderGarden()
            : base()
        {
            var garden = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                 b =>
                     (b.Id == (int)Buildings.GardenLvl1) ||
                     (b.Id == (int)Buildings.GardenLvl2) ||
                     (b.Id == (int)Buildings.GardenLvl3));
            if (garden != default(Building))
            {
                _building = garden;
            }
        }
        public override bool RequirementsMet()
        {
            if (_building == null)
            {
                GarrisonButler.Diagnostic("[PickUpOrderGarden] Building null, looking for building...");

                var garden = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                     b =>
                         (b.Id == (int)Buildings.GardenLvl1) ||
                         (b.Id == (int)Buildings.GardenLvl2) ||
                         (b.Id == (int)Buildings.GardenLvl3));
                if (garden != default(Building))
                {
                    GarrisonButler.Diagnostic("[PickUpOrderGarden] Building Found!");
                    _building = garden;
                }
                else
                {
                    GarrisonButler.Diagnostic("[PickUpOrderGarden] Building not Found!");
                    return false;
                }
            }
            return base.RequirementsMet();
        }
    }
}

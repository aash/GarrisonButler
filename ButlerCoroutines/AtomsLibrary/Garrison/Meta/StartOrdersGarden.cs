using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.Libraries;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class StartOrderGarden : StartWorkOrders
    {
        public override string Name()
        {
            return "[StartOrderGarden]";
        }
        public StartOrderGarden()
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
                var garden = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                    b =>
                        (b.Id == (int)Buildings.GardenLvl1) ||
                        (b.Id == (int)Buildings.GardenLvl2) ||
                        (b.Id == (int)Buildings.GardenLvl3));
                if (garden != default(Building))
                {
                    _building = garden;
                }
                else
                {
                    return false;
                }
            }
            return base.RequirementsMet();
        }
    }
}

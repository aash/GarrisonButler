using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.Libraries;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class StartOrdersMine : StartWorkOrders
    {
        public override string Name()
        {
            return "[StartOrdersMine]";
        }
        public StartOrdersMine() : base()
        {
            var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.Id == (int)Buildings.MineLvl1) ||
                    (b.Id == (int)Buildings.MineLvl2) ||
                    (b.Id == (int)Buildings.MineLvl3));
            if (mine != default(Building))
            {
                _building = mine;
            }
        }

        public override bool RequirementsMet()
        {
            if (_building == null)
            {
                var mine = ButlerCoroutine._buildings.GetEmptyIfNull().FirstOrDefault(
                b =>
                    (b.Id == (int)Buildings.MineLvl1) ||
                    (b.Id == (int)Buildings.MineLvl2) ||
                    (b.Id == (int)Buildings.MineLvl3));
                if (mine != default(Building))
                {
                    _building = mine;
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

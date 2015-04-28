//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Buddy.Coroutines;
//using GarrisonButler.API;
//using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
//using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta;
//using GarrisonButler.Config;
//using GarrisonButler.Libraries;
//using Styx;
//using Styx.CommonBot.Coroutines;
//using Styx.WoWInternals;
//using Styx.WoWInternals.WoWObjects;

//namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison
//{
//    class MillBeforeOrder : Atom
//    {
//        private Building _building;
//        private Func<int> _numToStart;

//        public MillBeforeOrder(Building building, Func<int> numToStart)
//        {
//            _building = building;
//            _numToStart = numToStart;

//                Dependencies = new List<Atom>()
//                {
//                    new Mill()
//                };
            
//        }
//        public override bool RequirementsMet()
//        {
//            return Dependencies.All(d => d.RequirementsMet());
//        }

//        public override bool IsFulfilled()
//        {
//            // Do we need to mill
//            // get number in bags
//            var count = HbApi.GetNumberItemInBags(_building.ReagentId);
//            // add number in reagent banks
//            count += HbApi.GetNumberItemInReagentBank(_building.ReagentId);
//            bool needToMill = count/_building.NumberReagent < _numToStart();

//            if (!needToMill) return true;

//            var millable = HbApi.GetAllItemsToMillFrom(_building.ReagentId, GaBSettings.Get().Pigments).ToArray();
//            return !millable.Any();
//        }

//        public async override Task Action()
//        {
//        }

//        public override string Name()
//        {
//            return "[MillBeforeOrder|" + _building.Name + ", " + _numToStart() + "]";
//        }
//    }
//}

//#region

//using System.Threading.Tasks;
//using Styx.CommonBot.POI;
//using Styx.WoWInternals.WoWObjects;

//#endregion

//namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
//{
//    internal class HarvestObjectCached : HarvestObject
//    {
//        private WoWObject _toharvest;
//        private bool _harvested;

//        public HarvestObjectCached(WoWGameObject toHarvest)
//            : base(toHarvest)
//        {
//        }


//        public override bool IsFulfilled()
//        {
//            return _harvested;
//        }

//        public override async Task Action()
//        {
//            // We should rewrite interaction code ourselves! 
//            // 2 scenarios, instant pickup and delayed interaction (example: ores in mine)
//            if (_toharvest == null || !_toharvest.IsValid)
//            {
//                Status = new Result(ActionResult.Done);
//                _harvested = true;
//                return;
//            }

//            var node = BotPoi.Current.AsObject as WoWGameObject;
//            if (node == null || !node.IsValid)
//            {
//                BotPoi.Clear();
//            }

//            if (node != _toharvest)
//                BotPoi.Current = new BotPoi(_toharvest, PoiType.Harvest);
//        }
//    }
//}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.Config;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class SellJunk : Atom
    {
        public override bool RequirementsMet()
        {
            return true; 
        }

        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().ForceJunkSell)
                return true;

            if (HbApi.GetItemsInBags(i =>
            {
                var res = false;
                try
                {
                    res = i.Quality == WoWItemQuality.Poor;
                }
                    // ReSharper disable once EmptyGeneralCatchClause
                catch (Exception e)
                {
                    if (e is CoroutineStoppedException)
                        throw;
                }
                return res;
            }).Any())
                return false;
            return true; 
        }

        public async override Task Action()
        {
            Vendors.ForceSell = true;
            await ButlerCoroutine.VendorBehavior.ExecuteCoroutine();
        }

        public override string Name()
        {
            return "[SellJunk]";
        }
    }
}

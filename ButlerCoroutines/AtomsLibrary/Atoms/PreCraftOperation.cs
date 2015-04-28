using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    /// <summary>
    /// Prepare a reagent to be ready to craft an item
    /// </summary>
    public class PreCraftOperation : Atom
    {
        private uint _reagentEntry;
        private int _reagentCount;
        private Atom _action;

        public PreCraftOperation(uint reagentEntry, int reagentCount)
        {
            _reagentEntry = reagentEntry;
            _reagentCount = reagentCount;

            // Search a correspondance in our tree of items
            var pigments =
                GaBSettings.Get()
                    .Pigments.GetEmptyIfNull()
                    .Where(p => p.Id == reagentEntry && p.MilledFrom.Any(i => i.Activated))
                    .ToArray();
            if (pigments.Any())
            {
                var itemsToMillFrom =
                    pigments.SelectMany(p => p.MilledFrom).Select(p => p.ItemId).ToArray();
                
                if (itemsToMillFrom.Any())
                {
                    _action = new Mill(itemsToMillFrom.ToList());
                }}
                        // If we found items which can be milled to get the one we want, then let's add them as a milling order with a list of items
            // if none found, there's nothing we can do to get that item
        }

        /// <summary>
        /// Check if we found an action to get the reagent and if the action's requirements are met.
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return _action != null && _action.RequirementsMet();
        }

        /// <summary>
        /// Check if the player has enough of the reagent in Bags + Reagents bank
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            if (_reagentEntry == 0)
                return true;
            // get number in bags
            var numInBags = HbApi.GetNumberItemInBags(_reagentEntry);
            // add number in reagent banks
            numInBags += HbApi.GetNumberItemInReagentBank(_reagentEntry);
            return numInBags >= _reagentCount;
        }

        public async override Task Action()
        {
            await _action.Execute();
        }

        public override string Name()
        {
            return "[PreCraftOperation|" 
                + _reagentEntry 
                + ",#" 
                + _reagentCount 
                + ","
                + (_action == null ? "none" : _action.Name())
                + "]";
        }
    }
}
#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using Styx;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    public class Stack : Atom
    {
        private readonly List<uint> _entries;
        private readonly int _size;

        public Stack(List<uint> entries, int size)
        {
            _entries = entries;
            _size = size;
        }

        /// <summary>
        /// We must have at least _size of one of _entries in bag to make a stack.
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return _entries.Any(entry => HbApi.GetNumberItemInBags(entry) >= _size);
        }

        /// <summary>
        /// Fulfilled if we have a stack of the required size in bags
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            return StyxWoW.Me.BagItems.Any(i => _entries.Contains(i.Entry) && i.StackCount >= _size);
        }

        /// <summary>
        /// Stack all the items which have more than the count required
        /// </summary>
        /// <returns></returns>
        public override async Task Action()
        {
            foreach (var entry in _entries.Where(e => HbApi.GetNumberItemInBags(e) >= _size))
            {
                await HbApi.StackItemsIfPossible(entry);
                if (IsFulfilled())
                    return;
            }
        }

        public override string Name()
        {
            return "[Stack]";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    public class UseItem : Atom
    {
        private readonly uint _entry;
        private bool _done = false;
        private readonly Func<bool> _terminationCondition; 


        /// <summary>
        /// If no terminationCondition given, will be executed only once.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="terminationCondition"></param>
        public UseItem(uint entry, Func<bool> terminationCondition = null)
        {
            _entry = entry;
            if (terminationCondition != null)
                _terminationCondition = terminationCondition;
        }
        public override bool RequirementsMet()
        {
            var item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == _entry);
            if (item == default(WoWItem))
                return false;

            if (!item.IsValid)
                return false;

            if (item.CooldownTimeLeft.TotalMilliseconds > 100)
                return false;

            return true;
        }

        public override bool IsFulfilled()
        {
            return _terminationCondition != null ? _terminationCondition() : _done;
        }

        public async override Task Action()
        {
            var firstOrDefault = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == _entry);
            if (firstOrDefault != null)
            {
                firstOrDefault.Use();
                await CommonCoroutines.SleepForLagDuration();
                _done = true;
            }
        }

        public override string Name()
        {
            return "[UseItem]";
        }
    }
}

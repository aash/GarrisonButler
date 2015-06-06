using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    public class UseItem : Atom
    {
        private readonly uint _entry;
        private bool _done = false;
        private readonly Func<bool> _terminationCondition;
        private bool _stopMovement;


        /// <summary>
        /// If no terminationCondition given, will be executed only once.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="terminationCondition"></param>
        public UseItem(uint entry, Func<bool> terminationCondition = null, bool stopMovement = false)
        {
            _entry = entry;
            _stopMovement = stopMovement;
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
                if (_stopMovement && StyxWoW.Me.IsMoving)
                {
                    await CommonCoroutines.StopMoving("Using item with entry " + _entry);
                    await CommonCoroutines.SleepForLagDuration();
                }

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

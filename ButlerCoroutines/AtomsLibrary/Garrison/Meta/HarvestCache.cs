using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class HarvestCache : Atom
    {
        private Atom _currentAction;
        
        private static readonly List<uint> GarrisonCaches = new List<uint>
        {
            236916,
            237191,
            237724,
            237723,
            237722,
            237720
        };

        public HarvestCache()
        {
            ShouldRepeat = true;
        }

        public override bool RequirementsMet()
        {
            return true;
        }

        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().GarrisonCache)
            {
                GarrisonButler.Diagnostic("Deactivated in user settings.");
                return true;
            }

            return !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                .GetEmptyIfNull()
                .Any(o => GarrisonCaches.Contains(o.Entry));
        }

        public async override Task Action()
        {
            if (_currentAction == null || _currentAction.IsFulfilled())
            {

                var allToActivate = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => GarrisonCaches.Contains(o.Entry))
                    .OrderBy(o => o.Location.X);

                var closest = allToActivate.First();

                _currentAction = new HarvestObject(closest);
            }
            await _currentAction.Execute();
        }

        public override string Name()
        {
            return "[HarvestCache]";
        }
    }
}

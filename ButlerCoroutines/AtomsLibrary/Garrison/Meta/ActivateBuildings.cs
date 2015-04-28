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
    class ActivateBuildings : Atom
    {
        private Atom _currentAction;

        private static readonly List<uint> FinalizeGarrisonPlotIds = new List<uint>
        {
            231217,
            231964,
            233248,
            233249,
            233250,
            233251,
            232651,
            232652,
            236261,
            236262,
            236263,
            236175,
            236176,
            236177,
            236185,
            236186,
            236187,
            236188,
            236190,
            236191,
            236192,
            236193
        };
        public ActivateBuildings()
        {
            ShouldRepeat = true; 
        }

        public override bool RequirementsMet()
        {
            return true; 
        }

        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().ActivateBuildings)
            {
                GarrisonButler.Diagnostic("Deactivated in user settings.");
                return true;
            }

            return !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                .GetEmptyIfNull()
                .Any(o => FinalizeGarrisonPlotIds.Contains(o.Entry));
        }

        public async override Task Action()
        {
            if (_currentAction == null || _currentAction.IsFulfilled())
            {
                
                var allToActivate = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => FinalizeGarrisonPlotIds.Contains(o.Entry))
                    .OrderBy(o => o.Location.X);

                var closest = allToActivate.First();

                _currentAction = new HarvestObject(closest);
            }
            await _currentAction.Execute();
        }

        public override string Name()
        {
            return "[ActivateBuildings]";
        }
    }
}

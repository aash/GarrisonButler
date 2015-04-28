using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    class CleanGarden : Atom
    {
        private Atom _currentAction;

        internal static readonly List<uint> GardenItems = new List<uint>
        {
            235390, // Nagrand Arrowbloom
            235388, // Gorgrond Flytrap
            235376, // Frostweed 
            235389, // Starflower
            235387, // Fireweed
            235391 // Talador Orchid
        };

        public CleanGarden()
        {
            ShouldRepeat = true; 
        }

        /// <summary>
        /// IS there any item to harvest
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Any(o => GardenItems.Contains(o.Entry));
        }

        /// <summary>
        /// Is there no item left
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().HarvestGarden)
            {
                GarrisonButler.Diagnostic("[Garden] Deactivated in user settings.");
                return true;
            }

            return !ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Any(o => GardenItems.Contains(o.Entry));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async override Task Action()
        {
            if (_currentAction == null || _currentAction.IsFulfilled())
            {
                var allObjects =
                    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                        .GetEmptyIfNull()
                        .Where(o => GardenItems.Contains(o.Entry) && !Objects.Blacklist.IsBlacklisted(o))
                        .ToArray();

                var closest = allObjects.OrderBy(o=> StyxWoW.Me.Location.Distance(o.Location)).First();

                _currentAction = new HarvestObject(closest);
            }
            await _currentAction.Execute();
        }

        public override string Name()
        {
            return "[CleanGarden]";
        }
    }
}

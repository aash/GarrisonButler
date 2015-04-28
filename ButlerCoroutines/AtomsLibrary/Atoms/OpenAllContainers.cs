#region

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class OpenAllContainers : Atom
    {
        private readonly List<uint> _contenairEntries;

        public OpenAllContainers(List<uint> contenairEntries, WoWPoint location = default(WoWPoint),
            float precision = 0.0f)
        {
            _contenairEntries = contenairEntries;
            if (location != default(WoWPoint))
            {
                Dependencies.Add(new MoveTo(location, precision));
            }
        }


        /// <summary>
        /// Always return true
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            return true;
        }

        /// <summary>
        /// Test if the player has any of the containers in bags
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            var containers = GetContainers();
            if (!containers.Any())
            {
                GarrisonButler.Diagnostic("[OpenContainer] None found in bags.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Open container item
        /// </summary>
        /// <returns></returns>
        public override async Task Action()
        {
            if (StyxWoW.Me.Mounted)
            {
                await CommonCoroutines.Dismount("Opening container");
                await CommonCoroutines.SleepForLagDuration();
            }

            var containers = GetContainers();
            foreach (var container in containers)
            {
                container.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();
                await Coroutine.Wait(5000, () => !StyxWoW.Me.IsCasting);
                await Coroutine.Yield();
            }
        }

        public override string Name()
        {            
            return "[OpenAllContainers|" + string.Join(",", _contenairEntries.ToArray()) + "]";
        }

        private IEnumerable<WoWItem> GetContainers()
        {
            return HbApi.GetItemsInBags(_contenairEntries);
        }
    }
}
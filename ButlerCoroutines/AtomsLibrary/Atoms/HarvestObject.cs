#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class HarvestObject : Atom
    {
        protected readonly WoWGameObject Toharvest;

        public HarvestObject(WoWGameObject toHarvest)
        {
            if (toHarvest == null)
            {
                throw new Exception("Error HarvestObject, parameter is null. Should be a WoWObject! Peace.");
            }

            GarrisonButler.Diagnostic("Creating HarvestObject with {0}", toHarvest.SafeName);
            Toharvest = toHarvest;
            Dependencies = new List<Atom>
            {
                new MoveToInteract(Toharvest)
            };
        }

        public override bool RequirementsMet()
        {
            return Toharvest != null;
        }

        public override bool IsFulfilled()
        {
            return Toharvest == null || !Toharvest.IsValid;
        }

        private int _lootCount = 0; 

        public const int MaxLootCount = 3;

        public override async Task Action()
        {
            await Coroutine.Wait(5000, () =>
            {
                WoWMovement.MoveStop();
                return !StyxWoW.Me.IsMoving;
            });

            Toharvest.Interact();
            await CommonCoroutines.SleepForLagDuration();
            await CommonCoroutines.SleepForRandomReactionTime();

            if(await Coroutine.Wait(5000, () => LootFrame.Instance != null && LootFrame.Instance.IsVisible))
            {
                GarrisonButler.Log("[HarvestObject] Looting {0}.", Toharvest.Name);
                if (Toharvest != null && Toharvest.IsValid)
                {
                    if (_lootCount > MaxLootCount)
                    {
                        GarrisonButler.Warning("[HarvestObject] Tried to loot {0}, {1} times => blacklisting.", Toharvest.Name, _lootCount);
                        Objects.Blacklist.Add(Toharvest);
                        return; 
                    }
                    _lootCount++;
                }
                LootFrame.Instance.LootAll();
                await CommonCoroutines.SleepForLagDuration();
                await Coroutine.Yield();
            }
        }

        public override string Name()
        {
            return "[HarvestObject|" + this.Toharvest.Name + "]";
        }
    }
}
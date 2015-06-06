#region

using System.Collections.Generic;
using System.Linq;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    public class UseMiningPick : UseItem
    {
        private const int PreserverdMiningPickItemId = 118903;
        private const int PreserverdMiningPickAura = 176061;
        private const int MaxStack = 1;

        public UseMiningPick()
            : base(PreserverdMiningPickItemId,
                () =>
                {
                    var auras = StyxWoW.Me.Auras.Where(a => a.Value.SpellId == PreserverdMiningPickAura);
                    var pairs = auras as KeyValuePair<string, WoWAura>[] ?? auras.ToArray();
                    if (pairs.Any())
                    {
                        var aura = pairs.First().Value;
                        if (aura == null)
                        {
                            //GarrisonButler.Diagnostic("[Item] Aura null skipping.");
                            return false;
                        }
                        // ReSharper disable once InvertIf
                        if (aura.StackCount >= MaxStack)
                        {
                            GarrisonButler.Diagnostic("[Item] Number of stacks/Max: {0}/{1} - too high to use item {2}",
                                aura.StackCount,
                                MaxStack,
                                aura.Name);
                            return true;
                        }
                        //GarrisonButler.Diagnostic("[Item] AuraCheck: {0} - current stack {1}", aura.Name, aura.StackCount);
                    }
                    return false;
                })
        {
            ShouldRepeat = true;
        }

        public override bool RequirementsMet()
        {
            return true; 
        }

        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().UseMiningPick)
                return true;

            if (!StyxWoW.Me.IsInGarrisonMine())
                return true; 

            var item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == PreserverdMiningPickItemId);
            if (item == default(WoWItem))
                return true;

            if (!base.RequirementsMet())
                return true; 

            return base.IsFulfilled();
        }
    }
}
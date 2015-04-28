using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta
{
    public class UseMinerCoffee : UseItem
    {
        private const int MinersCofeeItemId = 118897;
        private const int MinersCofeeAura = 176049;
        private const int MaxStack = 5;

        public UseMinerCoffee()
            : base(MinersCofeeItemId,
                () =>
                {
                    var auras = StyxWoW.Me.Auras.Where(a => a.Value.SpellId == MinersCofeeAura);
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
            if (!GaBSettings.Get().UseCoffee)
                return false;

            if (!StyxWoW.Me.IsInGarrisonMine())
                return false;

            return base.RequirementsMet();
        }
        public override bool IsFulfilled()
        {
            if (!GaBSettings.Get().UseCoffee)
                return true;

            var item = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == MinersCofeeItemId);
            if (item == default(WoWItem))
                return true;

            return base.IsFulfilled();
        }
    }
}
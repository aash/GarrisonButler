using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Objects;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Libraries
{
    static class SpellExtension
    {
        public static bool CanCraft(this WoWSpell spell)
        {
            try
            {
                if (spell.CooldownTimeLeft.TotalSeconds > 0)
                {
                    GarrisonButler.Diagnostic("[SpellExtension] Cannot craft: Spell cooldown > 0, CD={0}, id={1}, name={2}", spell.CooldownTimeLeft, spell.Id, spell.Name);
                    return false;
                }

                if (!spell.IsValid)
                {
                    GarrisonButler.Diagnostic("[SpellExtension] Cannot craft: Spell is not valid id={0}, name={1}", spell.Id, spell.Name);
                    return false;
                }

                // Check for skill values
                foreach (var skillLineAbility in SkillLineAbility.FromSpellId(spell.Id))
                {
                    var skillLine = skillLineAbility.SkillLine;
                    var skill = StyxWoW.Me.GetSkill(skillLine);
                    if (skill == null
                        || !skill.IsValid
                        || skill.CurrentValue < spell.BaseLevel)
                    {
                        GarrisonButler.Diagnostic("[SpellExtension] Cannot craft ");
                        return false;
                    }
                }

                // Check for reagents
                if (spell.MaxCanCraft() <= 0)
                {
                    GarrisonButler.Diagnostic("[SpellExtension] Cannot craft  maxCanCraft");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning(
                    "[SpellExtension] Error while checking if CanCraft.");
                GarrisonButler.Diagnostic("[SpellExtension] Error of type: ", e.GetType());
            }
            return false;
        }

        public static int MaxCanCraft(this WoWSpell spell)
        {
            var maxRepeat = Int32.MaxValue;
            var spellReagents = spell.InternalInfo.SpellReagents;
            if (spellReagents.Reagent == null)
            {
                GarrisonButler.Diagnostic("[SpellExtension] No reagents found for spell, returned max int value.", spell.Id, spell.Name);
                return maxRepeat;
            }

            for (var index = 0; index < spellReagents.Reagent.Length; index++)
            {
                var reagent = spellReagents.Reagent[index];
                if (reagent == 0)
                    continue;
                var required = spellReagents.ReagentCount[index];
                if (required <= 0)
                    continue;
                // get number in bags
                var numInBags = HbApi.GetNumberItemInBags((uint)reagent);

                // add number in reagent banks
                numInBags += HbApi.GetNumberItemInReagentBank((uint)reagent);

                if (GarrisonButler.IsIceVersion())
                {
                    // If has inscription
                    var inscription = StyxWoW.Me.GetSkill(SkillLine.Inscription);
                    if (inscription != null)
                    {
                        // add number from milling simulation
                        numInBags += HbApi.GetNumberItemByMillingBags((uint)reagent,
                            GaBSettings.Get().Pigments.GetEmptyIfNull().ToList());
                    }
                }
                var repeatNum = (int)(numInBags / required);
                if (repeatNum < maxRepeat)
                    maxRepeat = repeatNum;
            }
            return maxRepeat;
        }

        public static bool NeedAnvil(this WoWSpell spell)
        {
            var skillLineAbilities = SkillLineAbility.FromSpellId(spell.Id);
            return
                skillLineAbilities.Any(
                    sa => sa.SkillLine == SkillLine.Blacksmithing || sa.SkillLine == SkillLine.Engineering);
        }
    }
}

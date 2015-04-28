using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Libraries;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    class CraftItem : Atom
    {
        private readonly uint _itemEntry;
        private readonly WoWSpell _recipeSpell;
        private readonly SkillLine _skillLine;

        public CraftItem(uint itemEntry, SkillLine skillLine, uint spellId = 0)
        {
            _itemEntry = itemEntry;
            _skillLine = skillLine;

            if (spellId == 0)
                _recipeSpell = GetRecipeSpell();
            else
                _recipeSpell = WoWSpell.FromId((int)spellId);

            for (int i = 0; i < _recipeSpell.InternalInfo.SpellReagents.Reagent.Length; i++)
            {
                var reagentEntry = (uint)_recipeSpell.InternalInfo.SpellReagents.Reagent[i];
                var reagentCount = (int)_recipeSpell.InternalInfo.SpellReagents.ReagentCount[i];
                Dependencies.Add(new PreCraftOperation(reagentEntry, reagentCount));
            }

            if (_recipeSpell.NeedAnvil())
            {
                var defaultLocation = StyxWoW.Me.IsAlliance ? new WoWPoint(1907, 93, 83) : new WoWPoint(5473, 4444, 144);
                Dependencies.Add(new MoveToObject(WoWSpellFocus.Anvil, defaultLocation));
            }
        }

        public override bool RequirementsMet()
        {
            
            return _recipeSpell.CanCraft();
        }

        public override bool IsFulfilled()
        {
            GarrisonButler.Diagnostic("[CraftItem] IsFulfilled called: " + _recipeSpell.Name);
            return false;
        }

        public async override Task Action()
        {
            await HbApi.CastSpell(_recipeSpell);
        }

        public override string Name()
        {
            return String.Format("[CraftItem|Entry:{0}|SkillLine:{1}|recipe:{2}]", _itemEntry, _skillLine, _recipeSpell.Name);
        }

        private WoWSpell GetRecipeSpell()
        {
            var skillLine = _skillLine;
            if (!Enum.GetValues(typeof(SkillLine)).Cast<SkillLine>().Contains(skillLine))
            {
                GarrisonButler.Diagnostic("[DailyProfession] TradeSkillId {0} is not a valid tradeskill Id.",
                    _skillLine);
            }

            var skillLineId = (int)_skillLine;

            var skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine)s.ID)
                .ToList();

            var recipes = SkillLineAbility.GetAbilities()
                .Where(
                    a =>
                        skillLineIds.Contains(a.SkillLine) && a.NextSpellId == 0 && a.GreySkillLevel > 0 &&
                        a.TradeSkillCategoryId > 0)
                .Select(a => WoWSpell.FromId(a.SpellId))
                .Where(s => s != null && s.IsValid)
                .ToList();

            return recipes.FirstOrDefault(s => s.CreatesItemId == _itemEntry)
                   ?? recipes.FirstOrDefault(s => s.Id == _itemEntry);
        }

    }
}

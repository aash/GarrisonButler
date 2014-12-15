using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GarrisonBuddy.Config;
using Styx;
using Styx.CommonBot;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using System.Runtime.Serialization;

namespace GarrisonBuddy.Objects
{
    public class DailyProfession
    {
        public string Name {get;set;}
        public tradeskillID TradeskillId { get; set; }
        public uint ItemId { get; set; }
        public bool Activated { get; set; }

        [XmlIgnore] public WoWSpell Spell { get; set; }

        private DailyProfession(string name, uint itemId, DailyProfession.tradeskillID tradeskillId)
        {
            Name = name;
            TradeskillId = tradeskillId;
            ItemId = itemId;
            Spell = null;
        }

        private DailyProfession()
        {}

        public void Initialize()
        {
            if (GaBSettings.Get().DailySettings.FirstOrDefault(d => d.ItemId == ItemId).Activated)
                Spell = HasRecipe();
        }
        public bool needAnvil()
        {
            return TradeskillId == DailyProfession.tradeskillID.Blacksmithing || TradeskillId == DailyProfession.tradeskillID.Engineering;
        }
        public WoWSpell HasRecipe()
        {
            string name = Enum.GetName(typeof(DailyProfession.tradeskillID), TradeskillId);
            var TradeSkillSpell = (DailyProfession.tradeskillSpell)Enum.Parse(typeof(DailyProfession.tradeskillSpell), name);
            GarrisonBuddy.Diagnostic("[Profession] Name:" + name);
            GarrisonBuddy.Diagnostic("[Profession] TradeSkillSpell:" + TradeSkillSpell);

            if (!SpellManager.HasSpell((int)TradeSkillSpell))
                return null;

            int skillLineId = (int)TradeskillId;

            List<SkillLine> skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine)s.ID)
                .ToList();

            List<WoWSpell> recipes = SkillLineAbility.GetAbilities()
                .Where(
                    a =>
                        skillLineIds.Contains(a.SkillLine) && a.NextSpellId == 0 && a.GreySkillLevel > 0 &&
                        a.TradeSkillCategoryId > 0)
                .Select(a => WoWSpell.FromId(a.SpellId))
                .Where(s => s != null && s.IsValid)
                .ToList();

            return recipes.FirstOrDefault(s => s.CreatesItemId == ItemId)
                   ?? recipes.FirstOrDefault(s => s.Id == ItemId);
        }

        public int GetMaxRepeat()
        {
            if (Spell == null)
                Spell = GetRecipeSpell();

            int maxRepeat = Int32.MaxValue;
            WoWSpell.SpellReagentsEntry spellReagents = Spell.InternalInfo.SpellReagents;
            if (spellReagents.Reagent == null)
                return maxRepeat;

            for (int index = 0; index < spellReagents.Reagent.Length; index++)
            {
                int reagent = spellReagents.Reagent[index];
                if (reagent == 0)
                    continue;
                uint required = spellReagents.ReagentCount[index];
                if (required <= 0)
                    continue;
                long numInBags =
                    StyxWoW.Me.BagItems.Sum(i => i != null && i.IsValid && i.Entry == reagent ? i.StackCount : 0);
                numInBags +=
                    StyxWoW.Me.ReagentBankItems.Sum(i => i != null && i.IsValid && i.Entry == reagent ? i.StackCount : 0);
                var repeatNum = (int)(numInBags / required);
                if (repeatNum < maxRepeat)
                    maxRepeat = repeatNum;
            }
            if (Spell.CooldownTimeLeft.TotalSeconds > 0) return 0;
            return maxRepeat;
        }

        public WoWSpell GetRecipeSpell()
        {
            var skillLine = (SkillLine)TradeskillId;
            if (!Enum.GetValues(typeof(SkillLine)).Cast<SkillLine>().Contains(skillLine))
            {
                //QBCLog.ProfileError("TradeSkillId {0} is not a valid tradeskill Id.", TradeSkillId);
            }

            int skillLineId = (int)TradeskillId;

            List<SkillLine> skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine)s.ID)
                .ToList();

            List<WoWSpell> recipes = SkillLineAbility.GetAbilities()
                .Where(
                    a =>
                        skillLineIds.Contains(a.SkillLine) && a.NextSpellId == 0 && a.GreySkillLevel > 0 &&
                        a.TradeSkillCategoryId > 0)
                .Select(a => WoWSpell.FromId(a.SpellId))
                .Where(s => s != null && s.IsValid)
                .ToList();

            return recipes.FirstOrDefault(s => s.CreatesItemId == ItemId)
                   ?? recipes.FirstOrDefault(s => s.Id == ItemId);
        }
        public static readonly List<DailyProfession> AllDailies =
       new List<DailyProfession>()
            {
                new DailyProfession("Alchemical Catalyst",108996, DailyProfession.tradeskillID.Alchemy),
                new DailyProfession("Secret of Draenor Alchemy",118700, DailyProfession.tradeskillID.Alchemy),
                new DailyProfession("Truesteel Ingot",108257, DailyProfession.tradeskillID.Blacksmithing), 
                new DailyProfession("Secret of Draenor Blacksmithing",118720, DailyProfession.tradeskillID.Blacksmithing),
                new DailyProfession("Fractured Temporal Crystal",115504, DailyProfession.tradeskillID.Enchanting),
                new DailyProfession("Secret of Draenor Enchanting",119293, DailyProfession.tradeskillID.Enchanting),
                new DailyProfession("Gearspring Parts",111366, DailyProfession.tradeskillID.Engineering),
                new DailyProfession("Secret of Draenor Engineering",119299, DailyProfession.tradeskillID.Engineering),
                new DailyProfession("War Paints",112377, DailyProfession.tradeskillID.Inscription), // war paint
                new DailyProfession("Secret of Draenor Inscription",119297, DailyProfession.tradeskillID.Inscription), // secrets
                new DailyProfession("Taladite Crystal",115524, DailyProfession.tradeskillID.Jewelcrafting),
                new DailyProfession("Secret of Draenor Jewelcrafting",118723, DailyProfession.tradeskillID.Jewelcrafting), // secret
                new DailyProfession("Burnished Leather",110611, DailyProfession.tradeskillID.Leatherworking),
                new DailyProfession("Secret of Draenor Leatherworking",118721, DailyProfession.tradeskillID.Leatherworking),
                new DailyProfession("Hexweave Cloth",111556, DailyProfession.tradeskillID.Tailoring),
                new DailyProfession("Secret of Draenor Tailoring",118722, DailyProfession.tradeskillID.Tailoring),
            };

        public enum tradeskillID
        {
            Alchemy = 171,
            Blacksmithing = 164,
            Cooking = 185,
            Enchanting = 333,
            Engineering = 202,
            Fishing = 356,
            Herbalism = 182,
            Inscription = 773,
            Jewelcrafting = 755,
            Leatherworking = 165,
            Mining = 186,
            Tailoring = 197,
            Skinning = 393
        }

        public enum tradeskillSpell
        {
            Alchemy = 156606,
            Blacksmithing = 158737,
            Cooking = 185,
            Enchanting = 158716,
            Engineering = 158739,
            //Fishing = 356,
            //Herbalism = 182,
            Inscription = 158748,
            Jewelcrafting = 158750,
            Leatherworking = 158752,
            //Mining = 186,
            Tailoring = 158758,
            //Skinning = 393
        }
    }
}

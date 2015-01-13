#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using GarrisonButler.Config;
using Styx;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;

#endregion

namespace GarrisonButler.Objects
{
    public class DailyProfession
    {
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

        public enum TradeskillSpell
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
            Tailoring = 158758
            //Skinning = 393
        }

        public static readonly List<DailyProfession> AllDailies =
            new List<DailyProfession>
            {
                new DailyProfession("Alchemical Catalyst", 108996, 156587, tradeskillID.Alchemy),
                new DailyProfession("Secret of Draenor Alchemy", 118700, 175880, tradeskillID.Alchemy),
                new DailyProfession("Truesteel Ingot", 108257, 171690, tradeskillID.Blacksmithing),
                new DailyProfession("Secret of Draenor Blacksmithing", 118720, 176090, tradeskillID.Blacksmithing),
                new DailyProfession("Fractured Temporal Crystal", 115504, 169092, tradeskillID.Enchanting),
                new DailyProfession("Secret of Draenor Enchanting", 119293, 177043, tradeskillID.Enchanting),
                new DailyProfession("Gearspring Parts", 111366, 169080, tradeskillID.Engineering),
                new DailyProfession("Secret of Draenor Engineering", 119299, 177054, tradeskillID.Engineering),
                new DailyProfession("War Paints", 112377, 169081, tradeskillID.Inscription), // war paint
                new DailyProfession("Secret of Draenor Inscription", 119297, 177045, tradeskillID.Inscription),
                // secrets
                new DailyProfession("Taladite Crystal", 115524, 170700, tradeskillID.Jewelcrafting),
                new DailyProfession("Secret of Draenor Jewelcrafting", 118723, 176087, tradeskillID.Jewelcrafting),
                // secret
                new DailyProfession("Burnished Leather", 110611, 171391, tradeskillID.Leatherworking),
                new DailyProfession("Secret of Draenor Leatherworking", 118721, 176089, tradeskillID.Leatherworking),
                new DailyProfession("Hexweave Cloth", 111556, 168835, tradeskillID.Tailoring),
                new DailyProfession("Secret of Draenor Tailoring", 118722, 176058, tradeskillID.Tailoring)
            };

        public DailyProfession(string name, uint itemId, uint spellId, tradeskillID tradeskillId, bool activated = false)
        {
            Name = name;
            TradeskillId = tradeskillId;
            ItemId = itemId;
            SpellId = spellId;
            Spell = null;
            Activated = activated;
        }

        public DailyProfession()
        {
        }

        [XmlText]
        public string Name { get; set; }

        [XmlAttribute("TradeskillId")]
        public tradeskillID TradeskillId { get; set; }

        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }

        [XmlAttribute("SpellId")]
        public uint SpellId { get; set; }

        [XmlAttribute("Activated")]
        public bool Activated { get; set; }

        [XmlIgnore]
        public WoWSpell Spell { get; set; }

        public void Initialize()
        {
            var firstOrDefault = GaBSettings.Get().DailySettings.FirstOrDefault(d => d.ItemId == ItemId);
            if (firstOrDefault != null && firstOrDefault.Activated)
            {
                GarrisonButler.Diagnostic("[DailyProfession] {0}: loading spell.", Name);
                Spell = HasRecipe();
            }
            else
                GarrisonButler.Diagnostic("[DailyProfession] {0}: not activated.", Name);
        }

        public bool NeedAnvil()
        {
            return TradeskillId == tradeskillID.Blacksmithing || TradeskillId == tradeskillID.Engineering;
        }

        public WoWSpell HasRecipe()
        {
            var name = Enum.GetName(typeof (tradeskillID), TradeskillId);
            if (name != null)
            {
                var tradeSkillSpell = (TradeskillSpell) Enum.Parse(typeof (TradeskillSpell), name);

                GarrisonButler.Diagnostic("[DailyProfession] Name: " + Name);
                GarrisonButler.Diagnostic("[DailyProfession] TradeSkillSpell: " + tradeSkillSpell);
            }
            else
            {
                GarrisonButler.Diagnostic("[DailyProfession] Name: null");
            }
            return WoWSpell.FromId((int) SpellId);
        }

        public int GetMaxRepeat()
        {
            if (Spell == null)
                Spell = GetRecipeSpell();

            var maxRepeat = Int32.MaxValue;
            var spellReagents = Spell.InternalInfo.SpellReagents;
            if (spellReagents.Reagent == null)
                return maxRepeat;

            for (var index = 0; index < spellReagents.Reagent.Length; index++)
            {
                var reagent = spellReagents.Reagent[index];
                if (reagent == 0)
                    continue;
                var required = spellReagents.ReagentCount[index];
                if (required <= 0)
                    continue;
                var numInBags =
                    StyxWoW.Me.BagItems.Sum(i => i != null && i.IsValid && i.Entry == reagent ? i.StackCount : 0);
                numInBags +=
                    StyxWoW.Me.ReagentBankItems.Sum(i => i != null && i.IsValid && i.Entry == reagent ? i.StackCount : 0);
                var repeatNum = (int) (numInBags/required);
                if (repeatNum < maxRepeat)
                    maxRepeat = repeatNum;
            }
            return Spell.CooldownTimeLeft.TotalSeconds > 0 ? 0 : maxRepeat;
        }

        public WoWSpell GetRecipeSpell()
        {
            var skillLine = (SkillLine) TradeskillId;
            if (!Enum.GetValues(typeof (SkillLine)).Cast<SkillLine>().Contains(skillLine))
            {
                GarrisonButler.Diagnostic("[DailyProfession] TradeSkillId {0} is not a valid tradeskill Id.",
                    TradeskillId);
            }

            var skillLineId = (int) TradeskillId;

            var skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine) s.ID)
                .ToList();

            var recipes = SkillLineAbility.GetAbilities()
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
    }
}
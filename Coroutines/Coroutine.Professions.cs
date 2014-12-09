using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonBuddy.Config;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Patchables;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
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

        private static bool lastCheckCd = false;
        private static List<Helpers.TradeskillHelper> tradeskillHelpers;

        private static List<KeyValuePair<tradeskillID,WoWSpell>> DailyProfessionCD;

        private static readonly List<KeyValuePair<uint, tradeskillID>> AllDailyProfessionCD =
            new List<KeyValuePair<uint, tradeskillID>>
            {
                // Alchemy
                new KeyValuePair<uint, tradeskillID>(108996, tradeskillID.Alchemy),
                new KeyValuePair<uint, tradeskillID>(118700, tradeskillID.Alchemy),
                // BlackSmithing
                // Truesteel
                new KeyValuePair<uint, tradeskillID>(108257, tradeskillID.Blacksmithing),
                // Secrets
                new KeyValuePair<uint, tradeskillID>(118720, tradeskillID.Blacksmithing),
                // Enchanting
                //Fractured Temporal Crystal
                new KeyValuePair<uint, tradeskillID>(115504, tradeskillID.Enchanting),
                // Secret
                new KeyValuePair<uint, tradeskillID>(119293, tradeskillID.Enchanting),
                // Engineering
                // secrets
                new KeyValuePair<uint, tradeskillID>(119299, tradeskillID.Engineering),
                // bolts
                new KeyValuePair<uint, tradeskillID>(111366, tradeskillID.Engineering),
                // Inscription
                new KeyValuePair<uint, tradeskillID>(169081, tradeskillID.Inscription),
                new KeyValuePair<uint, tradeskillID>(119297, tradeskillID.Inscription),
                // Jewelcrafting
                new KeyValuePair<uint, tradeskillID>(115524, tradeskillID.Jewelcrafting),
                new KeyValuePair<uint, tradeskillID>(118723, tradeskillID.Jewelcrafting),
                // Leatherworking
                new KeyValuePair<uint, tradeskillID>(110611, tradeskillID.Leatherworking),
                new KeyValuePair<uint, tradeskillID>(118721, tradeskillID.Leatherworking),
                // Tailoring
                new KeyValuePair<uint, tradeskillID>(111556, tradeskillID.Tailoring),
                new KeyValuePair<uint, tradeskillID>(118722, tradeskillID.Tailoring),
            };

        private static void InitializeDailies()
        {
            if (DailyProfessionCD != null) return;

            DailyProfessionCD = new List<KeyValuePair<tradeskillID,WoWSpell>>();

            GarrisonBuddy.Log("Loading Professions dailies, please wait.");
            foreach (var item in AllDailyProfessionCD)
            {
                if (!CheckDailyAgainstConfig(item.Value))
                    continue;
                WoWSpell spell = HasRecipe((int) item.Key, (int) item.Value);
                if (spell != null)
                {
                    GarrisonBuddy.Log("Adding daily CD: {0} - {1}", (tradeskillID)item.Value, spell.Name);
                    DailyProfessionCD.Add(new KeyValuePair<tradeskillID, WoWSpell>(item.Value,spell));
                }
            }

            GarrisonBuddy.Log("Loading Professions dailies done.");
        }

        private static bool CheckDailyAgainstConfig(tradeskillID id)
        {
            switch (id)
            {
                case tradeskillID.Alchemy:
                    return GaBSettings.Mono.Alchemy;
                case tradeskillID.Blacksmithing:
                    return GaBSettings.Mono.Blacksmithing;
                case tradeskillID.Enchanting:
                    return GaBSettings.Mono.Enchanting;
                case tradeskillID.Engineering:
                    return GaBSettings.Mono.Engineering;
                case tradeskillID.Inscription:
                    return GaBSettings.Mono.Inscription;
                case tradeskillID.Jewelcrafting:
                    return GaBSettings.Mono.Jewelcrafting;
                case tradeskillID.Leatherworking:
                    return GaBSettings.Mono.Leatherworking;
                case tradeskillID.Tailoring:
                    return GaBSettings.Mono.Tailoring;
            }
            return false;
        }

        private static bool ShouldRunDailies()
        {
            tradeskillID tradeskillId = new tradeskillID();
            WoWSpell spell = null;
            return CanRunDailies(ref spell, ref tradeskillId);
        }

        private static bool CanRunDailies(ref WoWSpell spellOut, ref tradeskillID tradeskillId)
        {
            // Check
            InitializeDailies();

            if (!DailyProfessionCD.Any())
            {
                GarrisonBuddy.Diagnostic("[Profession] DailyProfessionCD is empty."); 
                return false;
            }

            if (AllDailyProfessionCD == null)
            {
                GarrisonBuddy.Diagnostic("[Profession] AllDailyProfessionCD not initialized.");
                return false;
            }

            foreach (KeyValuePair<tradeskillID,WoWSpell> spellProfession in DailyProfessionCD)
            {
                //ADD CHECK WITH OPTIONS, can use itemID
                if (spellProfession.Value.CooldownTimeLeft.TotalSeconds == 0)
                {
                    tradeskillID tradeskill = spellProfession.Key;
                    WoWSpell Spell = spellProfession.Value;

                    spellOut = Spell;
                    tradeskillId = tradeskill;
                    GarrisonBuddy.Diagnostic("[Profession] Found possible daily CD - TS {0} - {1}", (tradeskillID)tradeskillId, Spell.Name);
                    return true;
                }
            }
            GarrisonBuddy.Diagnostic("[Profession] No possible daily CD found.");
            return false;
        }

        public static async Task<bool> DoDailyCd()
        {
            WoWSpell spell = null;
            tradeskillID tradeskillId = 0;

            if (!CanRunDailies(ref spell, ref tradeskillId))
                return false;

            if (tradeskillId == tradeskillID.Blacksmithing || tradeskillId == tradeskillID.Engineering)
            {
                if (await FindAnvilAndDoCd(spell, (int)tradeskillId))
                    return true;
            }
            else
            {
                if (await DoCd(spell, (int)tradeskillId))
                    return true;
            }

            DailiesTriggered = false;
            return true;
        }

        private static async Task<bool> FindAnvilAndDoCd(WoWSpell spell, int skillLineId)
        {
            WoWGameObject anvil =
                ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SpellFocus == WoWSpellFocus.Anvil)
                    .OrderBy(o => o.Location.DistanceSqr(Dijkstra.ClosestToNodes(o.Location))) // The closest to a known waypoint
                    .FirstOrDefault();
            if (anvil == null)
            {
                GarrisonBuddy.Warning("Can't find an Anvil around, skipping for now.");
            }
            else
            {
                GarrisonBuddy.Warning("[Profession] Current CD requires an anvil, moving to the safest one.");
                if (await MoveTo(anvil.Location))
                    return true;
                
                if (await DoCd(spell, skillLineId))
                    return true;
            }
            return false;
        }

        private static async Task<bool> DoCd(WoWSpell spell, int skillLineId)
        {
            GarrisonBuddy.Log("[Profession] Realizing daily CD: " + spell.Name);
                if (Me.IsMoving)
                    WoWMovement.MoveStop();
                if (Me.Mounted)
                    await CommonCoroutines.LandAndDismount();
                await CommonCoroutines.SleepForLagDuration();
                spell.Cast();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);
                return true;
        }

        private static WoWSpell HasRecipe(int itemOrSpellId, int TradeSkillId)
        {
            string name = Enum.GetName(typeof (tradeskillID), TradeSkillId);
            var TradeSkillSpell = (tradeskillSpell) Enum.Parse(typeof (tradeskillSpell), name);
            GarrisonBuddy.Diagnostic("[Profession] Name:" + name);
            GarrisonBuddy.Diagnostic("[Profession] TradeSkillSpell:" + TradeSkillSpell);

            if (!SpellManager.HasSpell((int) TradeSkillSpell))
                return null;

            int skillLineId = TradeSkillId;

            List<SkillLine> skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine) s.ID)
                .ToList();

            List<WoWSpell> recipes = SkillLineAbility.GetAbilities()
                .Where(
                    a =>
                        skillLineIds.Contains(a.SkillLine) && a.NextSpellId == 0 && a.GreySkillLevel > 0 &&
                        a.TradeSkillCategoryId > 0)
                .Select(a => WoWSpell.FromId(a.SpellId))
                .Where(s => s != null && s.IsValid)
                .ToList();

            return recipes.FirstOrDefault(s => s.CreatesItemId == itemOrSpellId)
                   ?? recipes.FirstOrDefault(s => s.Id == itemOrSpellId);
        }

        private static int GetMaxRepeat(int itemOrSpellId, int TradeSkillId)
        {
            WoWSpell recipe = GetRecipeSpell(itemOrSpellId, TradeSkillId);

            int maxRepeat = int.MaxValue;
            WoWSpell.SpellReagentsEntry spellReagents = recipe.InternalInfo.SpellReagents;
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
                var repeatNum = (int) (numInBags/required);
                if (repeatNum < maxRepeat)
                    maxRepeat = repeatNum;
            }
            if (recipe.CooldownTimeLeft.TotalSeconds > 0) return 0;
            return maxRepeat;
        }

        private static WoWSpell GetRecipeSpell(int itemOrSpellId, int TradeSkillId)
        {
            var skillLine = (SkillLine) TradeSkillId;
            if (!Enum.GetValues(typeof (SkillLine)).Cast<SkillLine>().Contains(skillLine))
            {
                //QBCLog.ProfileError("TradeSkillId {0} is not a valid tradeskill Id.", TradeSkillId);
            }

            int skillLineId = TradeSkillId;

            List<SkillLine> skillLineIds = StyxWoW.Db[ClientDb.SkillLine]
                .Select(r => r.GetStruct<SkillLineInfo.SkillLineEntry>())
                .Where(s => s.ID == skillLineId || s.ParentSkillLineId == skillLineId)
                .Select(s => (SkillLine) s.ID)
                .ToList();

            List<WoWSpell> recipes = SkillLineAbility.GetAbilities()
                .Where(
                    a =>
                        skillLineIds.Contains(a.SkillLine) && a.NextSpellId == 0 && a.GreySkillLevel > 0 &&
                        a.TradeSkillCategoryId > 0)
                .Select(a => WoWSpell.FromId(a.SpellId))
                .Where(s => s != null && s.IsValid)
                .ToList();

            return recipes.FirstOrDefault(s => s.CreatesItemId == itemOrSpellId)
                   ?? recipes.FirstOrDefault(s => s.Id == itemOrSpellId);
        }
    }
}
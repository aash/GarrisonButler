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

        private static List<WoWSpell> DailyProfessionCD;

        private static readonly List<KeyValuePair<uint, tradeskillID>> AllDailyProfessionCD =
            new List<KeyValuePair<uint, tradeskillID>>
            {
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

            DailyProfessionCD = new List<WoWSpell>();
            GarrisonBuddy.Log("Loading Professions dailies, please wait.");
            foreach (var item in AllDailyProfessionCD)
            {
                if (!CheckDailyAgainstConfig(item.Value))
                    continue;
                WoWSpell spell = HasRecipe((int) item.Key, (int) item.Value);
                if (spell != null)
                {
                    GarrisonBuddy.Log("Adding daily CD: " + spell.Name);
                    DailyProfessionCD.Add(spell);
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
            int id = 0;
            return CanRunDailies(ref tradeskillId, ref id);
        }

        private static bool CanRunDailies(ref tradeskillID tradeskillId, ref int id)
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

            foreach (WoWSpell spell in DailyProfessionCD)
            {
                //ADD CHECK WITH OPTIONS, can use itemID
                if (spell.CooldownTimeLeft.TotalSeconds == 0)
                {
                    GarrisonBuddy.Diagnostic("[Profession] Detected available daily profession cd: " + spell.Name);
                    KeyValuePair<uint, tradeskillID> cd = AllDailyProfessionCD.FirstOrDefault(c => c.Key == spell.Id);
                    if (cd.Value == null || cd.Key == null)
                    {
                        GarrisonBuddy.Diagnostic("[Profession] Unable to find a match in DB for spell:" + spell.Name + " value:" + (cd.Value == null).ToString() + " key " + (cd.Key == null).ToString());
                    }
                    else
                    {
                        id = (int)cd.Key;
                        tradeskillId = cd.Value;
                        GarrisonBuddy.Diagnostic("[Profession] Found possible daily CD:" + spell.Name);
                        return true;
                    }
                }
            }
            GarrisonBuddy.Diagnostic("[Profession] No possible daily CD found.");
            return false;
        }

        public static async Task<bool> DoDailyCd()
        {
            int IdSpell = 0;
            tradeskillID tradeskillId = 0;

            if (!CanRunDailies(ref tradeskillId, ref IdSpell))
                return false;

            if (tradeskillId == tradeskillID.Blacksmithing || tradeskillId == tradeskillID.Engineering)
            {
                if (await FindAnvilAndDoCd((int)tradeskillId, (int)IdSpell))
                    return true;
            }
            else
            {
                if (await DoCd((int)tradeskillId, (int)IdSpell))
                    return true;
            }

            DailiesTriggered = false;
            return true;
        }

        private static async Task<bool> FindAnvilAndDoCd(int id, int skillLineId)
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
                if (await MoveTo(anvil.Location))
                    return true;

                await Buddy.Coroutines.Coroutine.Sleep(500);
                WoWMovement.MoveStop();
                if (await DoCd(id, skillLineId))
                    return true;
            }
            return false;
        }

        private static async Task<bool> DoCd(int id, int skillLineId)
        {
            int max = GetMaxRepeat(id, skillLineId);
            if (max > 0)
            {
                WoWSpell spell = GetRecipeSpell(id, skillLineId);
                GarrisonBuddy.Log("[Profession] Realizing daily CD: " + spell.Name);
                if (Me.IsMoving)
                    WoWMovement.MoveStop();
                await CommonCoroutines.SleepForLagDuration();
                spell.Cast();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);
                return true;
            }
            return false;
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
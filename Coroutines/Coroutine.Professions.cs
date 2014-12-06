using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.Professionbuddy.Dynamic;
using GarrisonBuddy.Config;
using Styx;
using Styx.Common.Helpers;
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

        private static bool lastCheckCd = false;
        private static List<Helpers.TradeskillHelper> tradeskillHelpers;

        private static List<WoWSpell> DailyProfessionCD;

        private static List<KeyValuePair<uint, tradeskillID>> AllDailyProfessionCD =
            new List<KeyValuePair<uint, tradeskillID>>
            {
                new KeyValuePair<uint, tradeskillID>((uint)108996, tradeskillID.Alchemy),
                new KeyValuePair<uint, tradeskillID>((uint)118700,tradeskillID.Alchemy),
                // BlackSmithing
                // Truesteel
                new KeyValuePair<uint, tradeskillID>((uint)108257,tradeskillID.Blacksmithing),
                // Secrets
                new KeyValuePair<uint, tradeskillID>((uint)118720,tradeskillID.Blacksmithing),
                // Enchanting
                //Luminous Shard
                new KeyValuePair<uint, tradeskillID>((uint)169091,tradeskillID.Enchanting),
                // Secret
                new KeyValuePair<uint, tradeskillID>((uint)119293,tradeskillID.Enchanting),
                // Engineering
                // secrets
                new KeyValuePair<uint, tradeskillID>((uint)119299,tradeskillID.Engineering),
                // bolts
                new KeyValuePair<uint, tradeskillID>((uint)111366,tradeskillID.Engineering),
                // Inscription
                new KeyValuePair<uint, tradeskillID>((uint)169081,tradeskillID.Inscription),
                new KeyValuePair<uint, tradeskillID>((uint)119297,tradeskillID.Inscription),
                // Jewelcrafting
                new KeyValuePair<uint, tradeskillID>((uint)115524,tradeskillID.Jewelcrafting),
                new KeyValuePair<uint, tradeskillID>((uint)118723,tradeskillID.Jewelcrafting),
                // Leatherworking
                new KeyValuePair<uint, tradeskillID>((uint)110611,tradeskillID.Leatherworking),
                new KeyValuePair<uint, tradeskillID>((uint)118721,tradeskillID.Leatherworking),
                // Tailoring
                new KeyValuePair<uint, tradeskillID>((uint)111556,tradeskillID.Tailoring),
                new KeyValuePair<uint, tradeskillID>((uint)118722,tradeskillID.Tailoring),
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
                var spell = HasRecipe((int)item.Key, (int)item.Value);
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

        private static bool CanRunDailies()
        {
            // Check
            InitializeDailies();
            if (!DailyProfessionCD.Any())
            {
                return false;
            }
            bool needToRun = false;
            foreach (var spell in DailyProfessionCD)
            {
                //ADD CHECK WITH OPTIONS, can use itemID
                if (spell.CooldownTimeLeft.TotalSeconds == 0)
                {
                    GarrisonBuddy.Diagnostic("Detected available daily profession cd: " + spell.Name);
                    needToRun = true;
                }
            }

            if (needToRun)
            {
                GarrisonBuddy.Log("Available daily cd found.");
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<bool> DoDailyCd()
        {
            if (!CanRunDailies())
                return false;

            foreach (var dailySpell in DailyProfessionCD)
            {
                if (dailySpell.CooldownTimeLeft.TotalSeconds != 0) continue;
                   
                var cd = AllDailyProfessionCD.First(c => c.Key == dailySpell.Id);
                if (cd.Value == tradeskillID.Blacksmithing || cd.Value ==  tradeskillID.Engineering )
                {
                    if(await FindAnvilAndDoCd((int)cd.Key, (int)cd.Value))
                        return true;
                }
                else
                {
                    if (await DoCd((int)cd.Key, (int)cd.Value))
                        return true;
                }
            }
            DailiesTriggered = false;
            return true;
        }

        private static async Task<bool> FindAnvilAndDoCd(int id, int skillLineId)
        {
            WoWGameObject anvil =
                ObjectManager.GetObjectsOfType<WoWGameObject>()
                    .Where(o => o.SpellFocus == WoWSpellFocus.Anvil)
                    .OrderBy(o => Me.Location.DistanceSqr(o.Location))
                    .FirstOrDefault();
            if (anvil == null)
            {
                GarrisonBuddy.Warning("Can't find an Anvil around, skipping for now.");
            }
            else
            {
                if (await MoveTo(anvil.Location))
                    return true;
                WoWMovement.ClickToMove(anvil.Location);
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
            var name = Enum.GetName(typeof(tradeskillID), TradeSkillId);
            var TradeSkillSpell = (tradeskillSpell)System.Enum.Parse(typeof(tradeskillSpell), name);
            GarrisonBuddy.Diagnostic("Name:" + name);
            GarrisonBuddy.Diagnostic("TradeSkillSpell:" + TradeSkillSpell);

            if(!SpellManager.HasSpell((int)TradeSkillSpell))
                return null;

            int skillLineId = TradeSkillId;

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
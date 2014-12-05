using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Patchables;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.DBC;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static bool lastCheckCd = false;
        private static WaitTimer CheckCd = null;

        public static bool IsToDoCd()
        {
            if (CheckCd != null && !CheckCd.IsFinished) return false;
            return true;
        }

        public static async Task<bool> DoDailyCd()
        {
            if (CheckCd != null && !CheckCd.IsFinished) return false;

            // Alchemy
            if (CanUseCd(108996, (int)tradeskillID.Alchemy))
                if (await DoCd(108996, (int)tradeskillID.Alchemy))
                    return true;
            if (CanUseCd(118700, (int)tradeskillID.Alchemy))
                if (await DoCd(118700, (int)tradeskillID.Alchemy))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // BlackSmithing
            // Truesteel
            if (CanUseCd(108257, (int)tradeskillID.Blacksmithing))
                if (await FindAnvilAndDoCd(108257, (int)tradeskillID.Blacksmithing))
                    return true;
            // Secrets
            if (CanUseCd(118720, (int)tradeskillID.Blacksmithing))
                if (await FindAnvilAndDoCd(118720, (int)tradeskillID.Blacksmithing))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // Enchanting
            //Luminous Shard
            if (CanUseCd(169091, (int)tradeskillID.Enchanting))
                if (await DoCd(169091, (int)tradeskillID.Enchanting))
                    return true;
            // Secret
            if (CanUseCd(119293, (int)tradeskillID.Enchanting))
                if (await DoCd(119293, (int)tradeskillID.Enchanting))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // Engineering
            // secrets
            if (CanUseCd(119299, (int) tradeskillID.Engineering))
                if (await DoCd(119299, (int) tradeskillID.Engineering))
                    return true;
            // bolts
            if (CanUseCd(111366, (int) tradeskillID.Engineering))
                if (await FindAnvilAndDoCd(111366, (int) tradeskillID.Engineering))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();
                
            // Inscription
            if (CanUseCd(169081, (int)tradeskillID.Inscription))
                if (await DoCd(169081, (int)tradeskillID.Inscription))
                    return true;
            if (CanUseCd(119297, (int)tradeskillID.Inscription))
                if (await DoCd(119297, (int)tradeskillID.Inscription))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // Jewelcrafting
            if (CanUseCd(115524, (int)tradeskillID.Jewelcrafting))
                if (await DoCd(115524, (int)tradeskillID.Jewelcrafting))
                    return true;
            if (CanUseCd(118723, (int)tradeskillID.Jewelcrafting))
                if (await DoCd(118723, (int)tradeskillID.Jewelcrafting))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // Leatherworking
            if (CanUseCd(110611, (int)tradeskillID.Leatherworking))
                if (await DoCd(110611, (int)tradeskillID.Leatherworking))
                    return true;
            if (CanUseCd(118721, (int)tradeskillID.Leatherworking))
                if (await DoCd(118721, (int)tradeskillID.Leatherworking))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();

            // Tailoring
            if (CanUseCd(111556, (int)tradeskillID.Tailoring))
                if (await DoCd(111556, (int)tradeskillID.Tailoring))
                    return true;
            if (CanUseCd(118722, (int)tradeskillID.Tailoring))
                if (await DoCd(118722, (int)tradeskillID.Tailoring))
                    return true;
            await Buddy.Coroutines.Coroutine.Yield();
            
            // Checked all cd
            if(CheckCd == null) CheckCd = new WaitTimer(TimeSpan.FromMinutes(1));
            CheckCd.Reset();
            return false;
        }

        private static async Task<bool> FindAnvilAndDoCd(int id, int skillLineId)
        {
            WoWGameObject anvil =
                    ObjectManager.GetObjectsOfType<WoWGameObject>()
                        .Where(o => o.SpellFocus == WoWSpellFocus.Anvil).OrderBy(o => Me.Location.DistanceSqr(o.Location)).FirstOrDefault();
            if (anvil == null)
            {
                GarrisonBuddy.Warning("Can't find an Anvil around, skipping for now.");
            }
            else
            {
                if (await MoveToSafe(anvil.Location))
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
                spell.Cast();
                await CommonCoroutines.SleepForLagDuration();
                await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);
                return true;
            }
            return false;
        }

        private static bool CanUseCd(int itemOrSpellId, int TradeSkillId)
        {
            WoWSpell recipe = GetRecipeSpell(itemOrSpellId, TradeSkillId);
            return SpellManager.HasSpell(recipe.Id) && recipe.IsValid && Math.Abs(recipe.CooldownTimeLeft.TotalSeconds) < 0.1;
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

        private enum tradeskillID
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

        //itemToCollect = ores.OrderBy(i => i.Distance).First();
        //        // Do I have a mining pick to use
        //        WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
        //        if (miningPick != null && miningPick.Usable
        //            && !Me.HasAura(PreserverdMiningPickAura)
        //            && miningPick.CooldownTimeLeft.TotalSeconds == 0)
        //        {
        //            GarrisonBuddy.Diagnostic("Using Mining pick");
        //            miningPick.Use();
        //        }
        //        // Do I have a cofee to use
        //        WoWItem coffee = Me.BagItems.Where(o => o.Entry == MinersCofeeItemId).ToList().FirstOrDefault();
        //        if (coffee != null && coffee.Usable &&
        //            (!Me.HasAura(MinersCofeeAura) ||
        //             Me.Auras.FirstOrDefault(a => a.Value.SpellId == MinersCofeeAura).Value.StackCount < 2)
        //            && coffee.CooldownTimeLeft.TotalSeconds == 0)
        //        {
        //            GarrisonBuddy.Diagnostic("Using coffee");
        //            coffee.Use();
        //        }

        //        if (await MoveTo(itemToCollect.Location))
        //            return true;

        //        itemToCollect.Interact();
        //        SetLootPoi(itemToCollect);
        //        await Buddy.Coroutines.Coroutine.Sleep(3500);
        //        return true;

        // DOESNT WORK SINCE SOME WORK ORDER ARE ALWAYS THERE! 
        //public static async Task<bool> PickUpAllWorkOrders()
        //{
        //    var shipments = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => o.SubType == WoWGameObjectType.GarrisonShipment).OrderBy(o=>o.Entry);

        //    foreach (var shipment in shipments)
        //    {
        //        if (await MoveTo(shipment.Location))
        //            return true;
        //        shipment.Interact();

        //    }
        //    return true;
        //}
    }
}
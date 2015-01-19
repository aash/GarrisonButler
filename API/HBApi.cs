#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.API
{
    public class HbApi
    {



        internal static LocalPlayer Me = StyxWoW.Me;

        #region Items
        /// <summary>
        /// Return the number of item with entry itemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static long GetNumberItemInBags(uint itemId)
        {
            return StyxWoW.Me.BagItems.GetEmptyIfNull().Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
        }

        /// <summary>
        /// Return the number of item with entry itemId in reagent bank.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static long GetNumberItemInReagentBank(uint itemId)
        {
            return StyxWoW.Me.ReagentBankItems.Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
        }


        /// <summary>
        /// Return the number of item with entry itemId which can get by milling all activated items in bags. 
        /// Doesn't check if the player can mill.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="PigmentsFromSettings"></param>
        /// <returns></returns>
        public static long GetNumberItemByMillingBags(uint itemId, List<Pigment> PigmentsFromSettings)
        {
            // TO DO Deams auto detection of spell's profession to not check milling when doing blacksmithing recipe.
            // add number that we could get from milling
            // Find all pigments corresponding to current reagent id and where at least one source is activated
            var pigments = PigmentsFromSettings.GetEmptyIfNull().Where(p => p.Id == itemId && p.MilledFrom.Any(i => i.Activated)).ToArray();
            if (pigments.Any())
            {
                GarrisonButler.Diagnostic("PigmentsFromSettings selected and corresponding to {0}, #=.", itemId, pigments.Count());
                ObjectDumper.WriteToHb(pigments, 3);

                var numByMilling = 0;
                var itemsToMillFrom = pigments.SelectMany(p => p.MilledFrom).ToArray();
                if (itemsToMillFrom.Any())
                {
                    GarrisonButler.Diagnostic("itemsToMillFrom selected and corresponding to {0}, #:{1}.", itemId, itemsToMillFrom.Count());
                    int sum = 0;
                    foreach (var source in itemsToMillFrom)
                    {
                        var itemsInBags = StyxWoW.Me.BagItems.Where(i => i.Entry == source.ItemId && source.Activated);
                        GarrisonButler.Diagnostic("itemsInBags selected and corresponding to {0}, #:{1}.", itemId, itemsInBags.Count());

                        int sum1 = 0;
                        foreach (var item in itemsInBags)
                        {
                            GarrisonButler.Diagnostic("item selected and corresponding to {0}, #:{1}.", itemId, item.SafeName);
                            var possibleMillings = item.StackCount/5;
                            sum1 += (int)possibleMillings*4;
                        }
                        sum += sum1;
                    }
                    numByMilling += sum;
                }
                return numByMilling;
            }
            GarrisonButler.Diagnostic("PigmentsFromSettings selected and corresponding to {0} = 0.", itemId);
            return 0;
        }

        public static IEnumerable<WoWItem> GetAllItemsToMillFrom(uint pigmentId, List<Pigment> pigmentsSettings)
        {
            return pigmentsSettings.Where(p=> p.Id == pigmentId).SelectMany(p => p.MilledFrom).SelectMany(p => GetItemInBags(p.ItemId).Where(i=> i.StackCount >= 5));
        }

        /// <summary>
        /// Return all items with Entry equal to itemId or null
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static IEnumerable<WoWItem> GetItemInBags(uint itemId)
        {
            return Me.BagItems.GetEmptyIfNull().Where(i => i.Entry == itemId).GetEmptyIfNull();
        }
        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        internal static void StackItems()
        {
            Lua.DoString("SortBags()");
        }

        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        internal static async Task<bool> StackAllItemsIfPossible()
        {
            if (!AnyItemsStackable())
                return false;

            await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                if (!AnyItemsStackable()) return true;
                StackItems();
                return false;
            });
            return true;
        }

        /// <summary>
        /// Returns if any items in bags can be stacked with another one.
        /// </summary>
        /// <returns></returns>
        internal static bool AnyItemsStackable()
        {
            var stackable =
                Me.BagItems
                    .Where(i => i.StackCount < ApiLua.GetMaxStackItem(i.Entry))
                    .GetEmptyIfNull()
                    .ToList();

            while (stackable.Count > 0)
            {
                var currentItem = stackable[0];
                stackable.RemoveAt(0);

                if (stackable.Any(d => d.Entry == currentItem.Entry))
                    return true;
            }

            return false;
        }

        #endregion 

        /// <summary>
        /// Safe spell casting. Wait for it to be done or if anything wrong happened return failed. 
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        internal static async Task<ActionResult> CastSpell(WoWSpell spell)
        {
            if (Me.Mounted)
            {
                await CommonCoroutines.Dismount("Dismounting to cast " + spell.Name + ".");
                await CommonCoroutines.SleepForLagDuration();
            }

            if (Me.IsMoving)
            {
                WoWMovement.MoveStop();
                await CommonCoroutines.SleepForLagDuration();
            }

            if (Me.IsCasting)
            {
                GarrisonButler.Diagnostic("Error casting {0}, already casting at the moment.", spell.Name);
                return ActionResult.Failed;
            }

            try
            {
                spell.Cast();
                await CommonCoroutines.SleepForLagDuration();
                var timeOutLimit = (int)spell.CastTime + 10000;
                if (!await Buddy.Coroutines.Coroutine.Wait(timeOutLimit, () => !Me.IsCasting))
                {
                    GarrisonButler.Diagnostic("Timed out while waiting for spell={0}, castTime={1}, timeOutLimit={2}.", spell.Name, spell.CastTime, timeOutLimit);
                    return ActionResult.Failed;
                }
                await CommonCoroutines.SleepForLagDuration();
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic(e.ToString());
                GarrisonButler.Log("Failed casting {0}.", spell.Name);
                return ActionResult.Failed;
            }
            GarrisonButler.Log("Successful cast of {0}.", spell.Name);
            return ActionResult.Done;
        }

        /// <summary>
        /// Find and mill one of a stack of ItemId in bags
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal static async Task<ActionResult> MillHerbFromBags(uint itemId)
        {
            // Conditions to check
            // Must have inscription
            var inscription = Me.GetSkill(SkillLine.Inscription);
            if (inscription == null)
            {
                GarrisonButler.Diagnostic("[Milling] Inscription == null. operation failed.");
                return ActionResult.Failed;
            }

            if (!inscription.IsValid)
            {
                GarrisonButler.Diagnostic("[Milling] Inscription is not valid. operation failed.");
                return ActionResult.Failed;
            }

            if (inscription.CurrentValue <= 0)
            {
                GarrisonButler.Diagnostic("[Milling] Inscription value <= 0. operation failed.");
                return ActionResult.Failed;
            }

            // Search for a stack in bags
            var stackToMill = Me.BagItems.GetEmptyIfNull().FirstOrDefault(i => i.Entry == itemId);
            if (stackToMill == default(WoWItem))
            {
                GarrisonButler.Diagnostic("[Milling] No item found in bags, id={0}", itemId);
                return ActionResult.Failed;
            }

            // Search for milling spell
            var millingSpell = WoWSpell.FromId(51005);

            if (millingSpell == null)
            {
                GarrisonButler.Diagnostic("[Milling] Milling spell not found(==null).");
                return ActionResult.Failed;
            }

            if (!millingSpell.IsValid)
            {
                GarrisonButler.Diagnostic("[Milling] Milling spell not valid.");
                return ActionResult.Failed;
            }

            if (!millingSpell.CanCast)
            {
                GarrisonButler.Diagnostic("[Milling] Can't cast milling spell.");
                return ActionResult.Failed;
            }
            var bagIndex = stackToMill.BagIndex;
            var bagSlot = stackToMill.BagSlot;
            var stackSize = stackToMill.StackCount;
            var itemName = stackToMill.SafeName;

            // Mill once
            await HbApi.CastSpell(millingSpell);
            stackToMill.UseContainerItem();
            await CommonCoroutines.SleepForLagDuration();

            // Wait for Cast
            await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);

            // Verification process
            // Refresh of the current state
            var bagWithMilledItem = default(WoWContainer);
            var itemMilled = default(WoWItem);
            var waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(3000));
            waitTimer.Reset();

            while (!waitTimer.IsFinished)
            {
                ObjectManager.Update();
                await Buddy.Coroutines.Coroutine.Yield();

                bagWithMilledItem = Me.GetBagAtIndex((uint)bagIndex);
                if (bagWithMilledItem != null)
                {
                    itemMilled = bagWithMilledItem.GetItemBySlot((uint)bagSlot);
                    if (itemMilled == null || itemMilled.Entry != itemId || itemMilled.StackCount < stackSize)
                    {
                        GarrisonButler.Diagnostic("[Milling] Confirmed milled, break.");
                        break;
                    }
                }
            }

            if (bagWithMilledItem == null)
            {
                GarrisonButler.Diagnostic("[Milling] wrong bag index. index={0}, slot={1}, itemId={2}", bagIndex, bagSlot, itemId);
                return ActionResult.Failed;
            }

            if (itemMilled != null && itemMilled.Entry == itemId && itemMilled.StackCount >= stackSize)
            {
                GarrisonButler.Diagnostic("[Milling] itemMilled not null, and stackCount didn't change. index={0}, slot={1}, itemId={2}, oldSize={3}, newSize={4}", bagIndex, bagSlot, itemId, stackSize, itemMilled.StackCount);
                return ActionResult.Failed;
            }

            GarrisonButler.Log("[Milling] Succesfully milled {0}.", itemName);
            return ActionResult.Done;
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.ButlerCoroutines;
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
            var inBags =
                StyxWoW.Me.BagItems.GetEmptyIfNull()
                    .Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
            GarrisonButler.Diagnostic("[HBApi] Get number of item in bags: item={0}, #={1}", itemId, inBags);
            return inBags;
        }

        /// <summary>
        /// Return the number of item with entry itemId that the player has (items & currency).
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static long GetNumberItemCarried(uint itemId)
        {
            var carried = Me.GetCarriedItemCount(itemId);
                //StyxWoW.Me.BagItems.GetEmptyIfNull()
                //    .Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
            GarrisonButler.Diagnostic("[HBApi] Get number of item carried: item={0}, #={1}", itemId, carried);
            return carried;
        }

        /// <summary>
        /// Return the number of item with entry itemId in reagent bank.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static long GetNumberItemInReagentBank(uint itemId)
        {
            var inReagentBank =
                StyxWoW.Me.ReagentBankItems.Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
            GarrisonButler.Diagnostic("[HBApi] Get number of item in reagent bank: item={0}, #={1}", itemId,
                inReagentBank);
            return inReagentBank;
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
            var pigments =
                PigmentsFromSettings.GetEmptyIfNull()
                    .Where(p => p.Id == itemId && p.MilledFrom.Any(i => i.Activated))
                    .ToArray();
            if (pigments.Any())
            {
                GarrisonButler.Diagnostic("[HBApi] PigmentsFromSettings selected and corresponding to {0}, #={1}.",
                    itemId, pigments.Count());
                ObjectDumper.WriteToHb(pigments, 3);

                var numByMilling = 0;
                var itemsToMillFrom = pigments.SelectMany(p => p.MilledFrom).ToArray();
                if (itemsToMillFrom.Any())
                {
                    GarrisonButler.Diagnostic("[HBApi] itemsToMillFrom selected and corresponding to {0}, #:{1}.",
                        itemId, itemsToMillFrom.Count());
                    int sum = 0;
                    foreach (var source in itemsToMillFrom)
                    {
                        var itemsInBags = StyxWoW.Me.BagItems.Where(i => i.Entry == source.ItemId && source.Activated);
                        GarrisonButler.Diagnostic("[HBApi] itemsInBags selected and corresponding to {0}, #:{1}.",
                            itemId, itemsInBags.Count());

                        int sum1 = 0;
                        foreach (var item in itemsInBags)
                        {
                            GarrisonButler.Diagnostic("[HBApi] item selected and corresponding to {0}, StackSize:{1}.",
                                itemId, item.StackCount);
                            var possibleMillings = item.StackCount/5;
                            sum1 += (int) possibleMillings*4;
                        }
                        sum += sum1;
                    }
                    numByMilling += sum;
                }
                GarrisonButler.Diagnostic(
                    "[HBApi] Found {0} possible pigments from milling bags simulation for pigmentId={1}.", numByMilling,
                    itemId);
                return numByMilling;
            }
            GarrisonButler.Diagnostic("[HBApi] Found {0} match in pigments list for ItemId={1}, is it a WoD pigment?", 0,
                itemId);
            return 0;
        }

        public static IEnumerable<WoWItem> GetAllItemsToMillFrom(uint pigmentId, List<Pigment> pigmentsSettings)
        {
            return
                pigmentsSettings.Where(p => p.Id == pigmentId)
                    .SelectMany(p => p.MilledFrom)
                    .SelectMany(p => GetItemInBags(p.ItemId).Where(i => i.StackCount >= 5));
        }

        /// <summary>
        /// Return all items with Entry equal to itemId or null
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static IEnumerable<WoWItem> GetItemInBags(uint itemId)
        {
            var inBags =
                Me.BagItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && i.Entry == itemId)
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get item in bags: item={0}, #Found={1}", itemId, inBags.Count());
            return inBags;
        }

        public static IEnumerable<WoWItem> GetItemsInBags(List<uint> ids)
        {
            var inBags =
                Me.BagItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && ids.Contains(i.Entry))
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get items in bags: #Found={0}, from list:", inBags.Count());
            ObjectDumper.WriteToHb(ids, 3);
            return inBags;
        }

        public static IEnumerable<WoWItem> GetItemsInBags(Func<WoWItem, bool> predicate)
        {
            var inBags =
                Me.BagItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && predicate(i))
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get items in bags: #Found={0}, from predicate.", inBags.Count());
            return inBags;
        }

        /// <summary>
        /// Return all items (currency included) with Entry equal to itemId or null
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static IEnumerable<WoWItem> GetItemCarried(uint itemId)
        {
            var carried =
                Me.CarriedItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && i.Entry == itemId)
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get item carried: item={0}, #Found={1}", itemId, carried.Count());
            return carried;
        }

        public static IEnumerable<WoWItem> GetItemsCarried(List<uint> ids)
        {
            var carried =
                Me.CarriedItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && ids.Contains(i.Entry))
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get items carried: #Found={0}, from list:", carried.Count());
            ObjectDumper.WriteToHb(ids, 3);
            return carried;
        }

        public static IEnumerable<WoWItem> GetItemsCarried(Func<WoWItem, bool> predicate)
        {
            var carried =
                Me.CarriedItems.GetEmptyIfNull()
                    .Where(i => i != null && i.IsValid && predicate(i))
                    .GetEmptyIfNull()
                    .ToList();
            GarrisonButler.Diagnostic("[HBApi] Get items carried: #Found={0}, from predicate.", carried.Count());
            return carried;
        }

        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        //internal static void StackItems()
        //{
        //    Lua.DoString("SortBags()");
        //}

        /// <summary>
        /// Stacks all items in bags.
        /// </summary>
        internal static async Task<bool> StackAllItemsIfPossible()
        {
            if (!AnyItemsStackable())
                return false;
    
            var stackable =
                Me.BagItems
                    .Where(i => i.StackCount < ApiLua.GetMaxStackItem(i.Entry))
                    .GetEmptyIfNull()
                    .ToList().Select(i=> i.Entry).Distinct();
            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                foreach (var entry in stackable)
                {
                    await StackItemsIfPossible(entry);
                }
            }
            return false;
           
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
                var timeOutLimit = (int) spell.CastTime + 10000;
                if (!await Buddy.Coroutines.Coroutine.Wait(timeOutLimit, () => !Me.IsCasting))
                {
                    GarrisonButler.Diagnostic("Timed out while waiting for spell={0}, castTime={1}, timeOutLimit={2}.",
                        spell.Name, spell.CastTime, timeOutLimit);
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
        /// Find and mill one of a stack of ItemId in bags. Uses milling spell or draenic mortar.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        internal static async Task<ActionResult> MillHerbFromBags(uint itemId)
        {
            // Conditions to check

            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                // Search for a stack in bags
                var stackToMill = Me.BagItems.GetEmptyIfNull().FirstOrDefault(i => i.Entry == itemId);
                if (stackToMill == default(WoWItem))
                {
                    GarrisonButler.Diagnostic("[Milling] No item found in bags, id={0}", itemId);
                    return ActionResult.Failed;
                }

                // Must have inscription or item
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

                WoWSpell millingSpell = default(WoWSpell);
                var millingItem = default(WoWItem);
                if (inscription.CurrentValue <= 0)
                {
                    millingItem = GetItemInBags(114942).FirstOrDefault();
                    if (millingItem == default(WoWItem) || millingItem.StackCount <= 0)
                    {
                        GarrisonButler.Diagnostic(
                            "[Milling] Inscription value <= 0 and no milling item in bags found. operation failed.");
                        return ActionResult.Failed;
                    }
                    if (!millingItem.Usable)
                    {
                        GarrisonButler.Diagnostic(
                            "[Milling] Milling item unusable. operation failed.");
                        return ActionResult.Failed;
                    }
                }
                else
                {
                    // We use spell to mill
                    // Search for milling spell
                    millingSpell = WoWSpell.FromId(51005);

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
                }


                var bagIndex = stackToMill.BagIndex;
                var bagSlot = stackToMill.BagSlot;
                var stackSize = stackToMill.StackCount;
                var itemName = stackToMill.SafeName;

                GarrisonButler.Diagnostic(
                    "[Milling] In bags before milling at (bagIndex={0},bagSlot={1}): stackSize={2}, itemName={3}",
                    bagIndex,
                    bagSlot, stackSize, itemName);

                // Mill once using spell
                if (millingSpell != default(WoWSpell))
                    await CastSpell(millingSpell);
                else
                {
                    millingItem.Use();
                    await CommonCoroutines.SleepForLagDuration();
                }

                stackToMill.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();

                //// Wait for Cast
                //await Buddy.Coroutines.Coroutine.Wait(10000, () => !Me.IsCasting);

                // Verification process
                // Refresh of the current state
                var bagWithMilledItem = default(WoWContainer);
                var itemMilled = default(WoWItem);
                var waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(1500)); // cast time supposed to be 1000s

                waitTimer.Reset();

                while (!waitTimer.IsFinished)
                {
                    await Buddy.Coroutines.Coroutine.Yield();

                    //If casting
                    if (Me.IsCasting)
                        continue;

                    ObjectManager.Update();
                    try
                    {
                        bagWithMilledItem = Me.GetBagAtIndex((uint) bagIndex);
                        if (bagWithMilledItem != null)
                        {
                            itemMilled = bagWithMilledItem.GetItemBySlot((uint) bagSlot);
                            if (itemMilled != null)
                                GarrisonButler.Diagnostic(
                                    "[Milling] In bags after milling at (bagIndex={0},bagSlot={1}): stackSize={2}, itemName={3}",
                                    bagIndex, bagSlot, itemMilled.StackCount, itemMilled.Name);
                            if (itemMilled == null || itemMilled.Entry != itemId || itemMilled.StackCount < stackSize)
                            {
                                GarrisonButler.Diagnostic("[Milling] Confirmed milled, break.");
                                break;
                            }
                        }

                    }
                    catch (Exception)
                    {

                        break;
                    }
                }
                await CommonCoroutines.SleepForLagDuration();

                if (bagWithMilledItem == null)
                {
                    GarrisonButler.Diagnostic("[Milling] wrong bag index. index={0}, slot={1}, itemId={2}", bagIndex,
                        bagSlot, itemId);
                    return ActionResult.Failed;
                }

                if (itemMilled != null && itemMilled.Entry == itemId && itemMilled.StackCount >= stackSize)
                {
                    GarrisonButler.Diagnostic(
                        "[Milling] itemMilled not null, and stackCount didn't change. index={0}, slot={1}, itemId={2}, oldSize={3}, newSize={4}",
                        bagIndex, bagSlot, itemId, stackSize, itemMilled.StackCount);
                    return ActionResult.Failed;
                }
                GarrisonButler.Log("[Milling] Succesfully milled {0}.", itemName);
            }
            return ActionResult.Done;
        }

        public static async Task StackItemsIfPossible(uint itemId)
        {

            var maxStackSize = ApiLua.GetMaxStackItem(itemId);
            if (maxStackSize == 0)
                return;

            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                var allNotFull = GetItemInBags(itemId).Where(i => i.StackCount < maxStackSize).ToList();
                if (allNotFull.Any())
                {
                    for (int index = allNotFull.Count-1; index >= 0; index--)
                    {
                        var item = allNotFull[index];
// Add this item to all the other
                        for (int i = allNotFull.Count-1; i >= 0; i--)
                        {
                            var subItem = allNotFull[i];
                            if (subItem == item)
                                continue;

                            var availableSpace = maxStackSize - subItem.StackCount;
                            var sizeToMove = Math.Min(availableSpace, item.StackCount);

                            if (sizeToMove == item.StackCount)
                            {
                                if (sizeToMove == availableSpace)
                                {
                                    allNotFull.RemoveAt(i);
                                    index--;
                                    GarrisonButler.Diagnostic("[StackItemsIfPossible] SizeToMove == available space, removing bag={0}, slot={1}, size={2}", subItem.BagIndex, subItem.BagSlot, sizeToMove);
                                }
                                allNotFull.RemoveAt(index);
                                GarrisonButler.Diagnostic("[StackItemsIfPossible] SizeToMove == SizeItemToMove, removing bag={0}, slot={1}, size={2}", item.BagIndex, item.BagSlot, sizeToMove);
                                // move sizeToMove
                                int bagSource;
                                int slotSource;
                                var resSource = item.GetLuaContainerPosition(out bagSource, out slotSource);
                                int bagTarget;
                                int slotTarget;
                                var resTarget = subItem.GetLuaContainerPosition(out bagTarget, out slotTarget);
                                GarrisonButler.Diagnostic("[StackItemsIfPossible] Split bag={0}, slot={1}, size={2}", bagSource, slotSource, sizeToMove);
                                await ButlerLua.DoString(String.Format("SplitContainerItem({0}, {1}, {2})", bagSource, slotSource,
                                    sizeToMove));
                                await CommonCoroutines.SleepForLagDuration();
                                await
                                    ButlerLua.DoString(
                                        (String.Format("PickupContainerItem({0}, {1})", bagTarget, slotTarget)));
                                await CommonCoroutines.SleepForLagDuration();
                                subItem.UseContainerItem();
                                break;
                            }
                            else
                            {
                                if (sizeToMove == availableSpace)
                                {
                                    allNotFull.RemoveAt(i); 
                                    GarrisonButler.Diagnostic("[StackItemsIfPossible] SizeToMove == available space, removing bag={0}, slot={1}, size={2}", subItem.BagIndex, subItem.BagSlot, sizeToMove);
                                }
                                // move
                                int bagSource;
                                int slotSource;
                                var resSource = item.GetLuaContainerPosition(out bagSource, out slotSource);
                                int bagTarget;
                                int slotTarget;
                                var resTarget = subItem.GetLuaContainerPosition(out bagTarget, out slotTarget);
                                GarrisonButler.Diagnostic("[StackItemsIfPossible] Split bag={0}, slot={1}, size={2}", bagSource, slotSource, sizeToMove);
                                await ButlerLua.DoString(String.Format("SplitContainerItem({0}, {1}, {2})", bagSource, slotSource,
                                    sizeToMove));
                                await CommonCoroutines.SleepForLagDuration();
                                await
                                    ButlerLua.DoString(
                                        (String.Format("PickupContainerItem({0}, {1})", bagTarget, slotTarget)));
                                await CommonCoroutines.SleepForLagDuration();
                                subItem.UseContainerItem();
                            }
                        }
                    }
                }
                ObjectManager.Update();
            }
        }
    }
}
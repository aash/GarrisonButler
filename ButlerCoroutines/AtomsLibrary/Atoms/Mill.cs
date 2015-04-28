#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison.Meta;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines.AtomsLibrary.Atoms
{
    internal class Mill : Atom
    {
        private readonly List<uint> _entries;
        private List<int> _oldEntriesCount;
        private readonly Atom _craftMortar;

        public Mill(List<uint> entries)
        {
            _entries = new List<uint>();
            _entries.AddRange(entries);

            var inscription = StyxWoW.Me.GetSkill(SkillLine.Inscription);
            if (inscription == null || inscription.CurrentValue <= 0)
            {
                _craftMortar = new CraftDraenicMortarAtNpc();
                Dependencies.Add(_craftMortar);
            }

            Dependencies.Add(new Stack(_entries, 5));
        }

        public Mill(uint entry)
            : this(new List<uint> {entry})
        {
        }

        /// <summary>
        /// We need at least a count of 5 of one of the entries in bags.
        /// We also need to have the inscription profession or a draenic mortar
        /// </summary>
        /// <returns></returns>
        public override bool RequirementsMet()
        {
            if (_craftMortar != null && !_craftMortar.RequirementsMet())
                return false;

            return _entries.Any(entry => HbApi.GetNumberItemInBags(entry) >= 5);
        }

        /// <summary>
        /// Fulfilled when number of items before first execution and now has changed
        /// </summary>
        /// <returns></returns>
        public override bool IsFulfilled()
        {
            // Has never ran
            //if (_oldEntriesCount == null)
            //    return false;

            //for (int i = 0; i < _entries.Count; i++)
            //{
            //    var entry = _entries[i];
            //    var oldCount = _oldEntriesCount[i];
            //    if (HbApi.GetNumberItemInBags(entry) != oldCount)
            //        return true;
            //}

            return false;
        }

        public override async Task Action()
        {
            using (var myLock = StyxWoW.Memory.AcquireFrame())
            {
                // Record number of items in bags if first execution
                if (_oldEntriesCount == null)
                {
                    _oldEntriesCount = new List<int>();
                    foreach (var entry in _entries)
                    {
                        _oldEntriesCount.Add((int) HbApi.GetNumberItemInBags(entry));
                    }
                }

                // take a stack of size 5 or more and mill
                var stackToMill = StyxWoW.Me.BagItems.GetEmptyIfNull()
                    .FirstOrDefault(i => _entries.Contains(i.Entry) && i.StackCount >= 5);

                if (stackToMill == default(WoWItem))
                {
                    Status = new Result(ActionResult.Failed, "Couldn't find a proper stack to mill in bags.");
                    return;
                }
                var itemId = stackToMill.Entry;
                var itemName = stackToMill.Name;
                var bagIndex = stackToMill.BagIndex;
                var bagSlot = stackToMill.BagSlot;
                var stackSize = stackToMill.StackCount;

                WoWMovement.MoveStop();
                await CommonCoroutines.SleepForLagDuration();

                if (_craftMortar != null)
                {
                    var millingItem = HbApi.GetItemInBags(114942).FirstOrDefault();
                    if (millingItem == default(WoWItem))
                    {
                        Status = new Result(ActionResult.Failed,
                            "[Mill] no draenic mortar item in bags found. operation failed.");
                        return;
                    }

                    millingItem.Use();
                    await CommonCoroutines.SleepForLagDuration();
                }
                else
                {
                    // Search for milling spell
                    var millingSpell = WoWSpell.FromId(51005);
                    await HbApi.CastSpell(millingSpell);
                }

                stackToMill.UseContainerItem();
                await CommonCoroutines.SleepForLagDuration();


                // Verification process
                // Refresh of the current state
                var bagWithMilledItem = default(WoWContainer);
                var itemMilled = default(WoWItem);
                var waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(1500)); // cast time supposed to be 1000s

                waitTimer.Reset();

                while (!waitTimer.IsFinished)
                {
                    await Coroutine.Yield();

                    //If casting
                    if (StyxWoW.Me.IsCasting)
                        continue;

                    ObjectManager.Update();
                    try
                    {
                        bagWithMilledItem = StyxWoW.Me.GetBagAtIndex((uint) bagIndex);
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
                    Status = new Result(ActionResult.Failed,
                        string.Format("[Milling] wrong bag index. index={0}, slot={1}, itemId={2}", bagIndex,
                            bagSlot, itemId));
                    return;
                }

                if (itemMilled != null && itemMilled.Entry == itemId && itemMilled.StackCount >= stackSize)
                {
                    Status = new Result(ActionResult.Failed,
                        string.Format(
                            "[Milling] itemMilled not null, and stackCount didn't change. index={0}, slot={1}, itemId={2}, oldSize={3}, newSize={4}",
                            bagIndex, bagSlot, itemId, stackSize, itemMilled.StackCount));
                    return;
                }
                GarrisonButler.Log("[Milling] Succesfully milled {0}.", itemName);
            }
        }

        public override string Name()
        {
            return "[Mill]";
        }
    }
}
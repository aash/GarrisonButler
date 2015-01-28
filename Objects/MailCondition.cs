using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GarrisonButler.API;
using GarrisonButler.Libraries;
using JetBrains.Annotations;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Objects
{
    /// <summary>
    ///     Represents a condition for a mail item
    /// </summary>
    public class MailCondition : INotifyPropertyChanged, IComparable
    {
        public enum Conditions
        {
            NumberInBagsSuperiorTo = 0,
            NumberInBagsSuperiorOrEqualTo = 1,
            KeepNumberInBags = 2,
            None = 99
        }

        private static List<MailCondition> _allPossibleConditions;
        private int _checkValue;
        private Conditions _condition;
        private string _name;

        public MailCondition(Conditions condition, int checkValue)
        {
            _condition = condition;
            _checkValue = checkValue;
            _name = GetName(_condition);
        }

        public MailCondition()
        {
            _condition = Conditions.None;
            _checkValue = 0;
            _name = GetName(_condition);
        }

        public int CompareTo(object obj)
        {
            var mailCondition = obj as MailCondition;
            return mailCondition != null
                ? String.Compare(Name, mailCondition.Name, StringComparison.Ordinal)
                : Name.CompareTo(obj);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Retrieve the name from condition, to be used for initialization only.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static string GetName(Conditions condition)
        {
            switch (condition)
            {
                case Conditions.NumberInBagsSuperiorTo:
                    return "if > in Bags";

                case Conditions.NumberInBagsSuperiorOrEqualTo:
                    return "if >= in Bags";

                case Conditions.KeepNumberInBags:
                    return "Keep in Bags at least";

                case Conditions.None:
                    return "Deactivated";
                default:
                    GarrisonButler.Diagnostic("This rule has not been implemented!");
                    break;
            }
            return "Not Implemented";
        }

        private static Conditions GetCondition(String name)
        {
            switch (name)
            {
                case "if > in Bags":
                    return Conditions.NumberInBagsSuperiorTo;

                case "if >= in Bags":
                    return Conditions.NumberInBagsSuperiorOrEqualTo;

                case "Keep in Bags at least":
                    return Conditions.KeepNumberInBags;

                case "Deactivated":
                    return Conditions.None;

                default:
                    GarrisonButler.Diagnostic("This mail rule has not been implemented! Defaulting to None.");
                    break;
            }
            return Conditions.None;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Returns value of the condition
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool GetCondition(uint itemId)
        {
            switch (_condition)
            {
                case Conditions.NumberInBagsSuperiorTo:
                    return IsNumberInBagsSuperiorTo(itemId, _checkValue);

                case Conditions.NumberInBagsSuperiorOrEqualTo:
                    return IsNumberInBagsSuperiorOrEqualTo(itemId, _checkValue);

                case Conditions.KeepNumberInBags:
                    return IsKeepNumberInBags(itemId, _checkValue);

                case Conditions.None:
                    return false;

                default:
                    GarrisonButler.Diagnostic("This mail rule has not been implemented!");
                    break;
            }
            return false;
        }

        /// <summary>
        ///     Returns a static list of all the possible conditions implemented
        /// </summary>
        /// <returns></returns>
        public static List<MailCondition> GetAllPossibleConditions()
        {
            if (_allPossibleConditions != null)
                return _allPossibleConditions;

            _allPossibleConditions =
                (from object condition in Enum.GetValues(typeof (Conditions))
                    select new MailCondition((Conditions) condition, 0)).ToList();
            return _allPossibleConditions;
        }

        /// <summary>
        ///     Returns items to send or null if none.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<WoWItem>> GetItemsOrNull(uint itemId)
        {
            switch (_condition)
            {
                case Conditions.NumberInBagsSuperiorTo:
                    return GetNumberInBagsSuperiorTo(itemId);

                case Conditions.NumberInBagsSuperiorOrEqualTo:
                    return GetNumberInBagsSuperiorOrEqualTo(itemId);

                case Conditions.KeepNumberInBags:
                    return await GetNumberKeepNumberInBags(itemId, _checkValue);

                case Conditions.None:
                    return null;

                default:
                    GarrisonButler.Diagnostic("This mail rule has not been implemented!");
                    break;
            }
            return null;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged1([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region GetSet

        /// <summary>
        ///     Name of the condition as displayed in UI
        /// </summary>
        [XmlAttribute("Name")]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                _condition = GetCondition(_name);
                OnPropertyChanged1();
            }
        }

        /// <summary>
        ///     Condition of the item
        /// </summary>
        [XmlAttribute("Rule")]
        public Conditions Condition
        {
            get { return _condition; }
            set
            {
                if (value == _condition) return;
                _condition = value;
                OnPropertyChanged1();
            }
        }

        /// <summary>
        ///     Value to check against for condition
        /// </summary>
        [XmlAttribute("CheckValue")]
        public int CheckValue
        {
            get { return _checkValue; }
            set
            {
                if (value == _checkValue) return;
                _checkValue = value;
                OnPropertyChanged1();
            }
        }

        #endregion

        #region Rules

        // A rule must have a method returning a bool and a method returning the list of items

        /// <summary>
        ///     Return true if the specified character have more than x count of ItemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberInBagsSuperiorTo(uint itemId, int x)
        {
            var numInBags = HbApi.GetNumberItemInBags(itemId);
            return numInBags > x;
        }

        /// <summary>
        ///     Return array of items to send to respect the following rule: if the specified character have more than x count of
        ///     ItemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorTo(uint itemId)
        {
            return HbApi.GetItemInBags(itemId);
        }

        /// <summary>
        ///     Return true if the specified character have x or more than x count of ItemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberInBagsSuperiorOrEqualTo(uint itemId, int x)
        {
            var numInBags = HbApi.GetNumberItemInBags(itemId);
            return numInBags != 0 && numInBags >= x;
        }

        /// <summary>
        ///     Return array of items to send to respect the following rule: if the specified character have x or more than x count
        ///     of itemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorOrEqualTo(uint itemId)
        {
            return HbApi.GetItemInBags(itemId);
        }

        /// <summary>
        ///     Returns if there is more of the specified ItemId than the threshold.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private static bool IsKeepNumberInBags(uint itemId, int threshold)
        {
            return IsNumberInBagsSuperiorTo(itemId, threshold);
        }

        /// <summary>
        ///     Return array of items to send to respect the following rule: if the specified character have x or more than x count
        ///     of itemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<WoWItem>> GetNumberKeepNumberInBags(uint itemId, int x)
        {
            var items = HbApi.GetItemInBags(itemId).ToArray();

            // No items corresponding in bags
            if (!items.Any())
                return new List<WoWItem>();

            //if min amount is 0 returning all corresponding items
            if (x == 0)
                return items;

            // Max size of a stack of itemId
            var maxStackSize = ApiLua.GetMaxStackItem(itemId);
            var sizeToCut = x;
            var stacksToKeep = new List<WoWItem>();
            var rest = (x%maxStackSize);
            var numberOfStacks = x/maxStackSize;

            // Stacking item

            // Stacking all items
            await HbApi.StackItemsIfPossible(itemId);

            if (x > maxStackSize)
            {
                // everything is supposed to be stacked, we just need to split one stack for the rest.

                // There should be number Of full Stacks 
                var getStacks = GetStacks(itemId, numberOfStacks, maxStackSize).ToArray();
                if (!getStacks.Any())
                {
                    GarrisonButler.Diagnostic(
                        "[MailCondition] Couldn't find enough full stacks for split over maxStack. [Id:{0}/#:{1}]",
                        itemId, x);
                    return new List<WoWItem>();
                }

                if (rest == 0)
                    return items.Where(i => !getStacks.Contains(i));

                sizeToCut = rest;
            }

            var possibleStacksToCut =
                items.Where(i => i.StackCount >= sizeToCut && !stacksToKeep.Contains(i));

            if (!possibleStacksToCut.Any())
            {
                GarrisonButler.Diagnostic("[MailCondition] Couldn't find a valid stack to cut. [Id:{0}/sizeToCut:{1}/#stacksToKeep:{2}]", itemId, sizeToCut, stacksToKeep.Count);
                return new List<WoWItem>();
            }

            var stackCut = await CutAndGetStack(sizeToCut, itemId);
            if (stackCut == default(WoWItem))
            {
                GarrisonButler.Diagnostic("[MailCondition] Couldn't find resulting stack of split. [Id:{0}/sizeToCut:{1}/#stacksToKeep:{2}]", itemId, sizeToCut, stacksToKeep.Count);
                return new List<WoWItem>();
            }
            stacksToKeep.Add(stackCut);
            var fullStacks = GetStacks(itemId, numberOfStacks, maxStackSize).ToArray();
            if (fullStacks.Count() < numberOfStacks)
            {
                // it means we splited one of the full stacks
                var splitedFull = GetStacks(itemId, 1, maxStackSize - sizeToCut).ToArray();
                if (!splitedFull.Any())
                {
                    GarrisonButler.Diagnostic("[MailCondition] Couldn't find resulting stack of split from full stack splitting. [Id:{0}/sizeToCut:{1}/#stacksToKeep:{2}]", itemId, sizeToCut, stacksToKeep.Count);
                    return new List<WoWItem>();
                }
                stacksToKeep.Add(splitedFull.First());
            }
            stacksToKeep.AddRange(fullStacks);
            return HbApi.GetItemsInBags(i => i.Entry == itemId && !stacksToKeep.Contains(i));
        }


        /// <summary>
        ///     Cut a stack and get returns the part of the size asked if found otherwise default WoWItem.
        /// </summary>
        /// <param name="sizeToCut"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static async Task<WoWItem> CutAndGetStack(int sizeToCut, uint itemId)
        {
            // TO DO check for the action to be done before returning
            await SplitOneStack(sizeToCut, itemId);
            await CommonCoroutines.SleepForLagDuration();
            await CommonCoroutines.SleepForRandomUiInteractionTime();
            await Buddy.Coroutines.Coroutine.Yield(); // This should refresh the object manager.

            // let's find this stack
            var stacksCutSize = GetStacks(itemId, 1, sizeToCut).ToList();
            if (!stacksCutSize.Any())
            {
                return default(WoWItem); // Error finding correct stack size
            }
            return stacksCutSize.First();
        }


        /// <summary>
        ///     Return a number of stacks. if not enough return what exists.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="number"></param>
        /// <param name="sizeStacks"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetStacks(uint itemId, int number, int sizeStacks)
        {
            if (sizeStacks == 0)
                return new List<WoWItem>();

            var stacks =
                HbApi.GetItemsInBags(i => i.Entry == itemId && i.StackCount == sizeStacks).ToList();

            if (stacks.Count() >= number)
                return stacks.Take(number);

            GarrisonButler.Diagnostic("Error getting from bags {1}x stack(s) of itemId:{0} and stackSize:{3}. Returning {2} stacks", itemId,
                number, stacks.Count, sizeStacks);
            return stacks;
        }

        #endregion

        #region Helpers

        ///// <summary>
        /////     Returns all the stacks of the specified itemId in bags which can be mailed (isMailable extension).
        ///// </summary>
        ///// <param name="itemId"></param>
        ///// <returns></returns>
        //private static IEnumerable<WoWItem> GetAllItems(uint itemId)
        //{
        //    return StyxWoW.Me.BagItems.GetEmptyIfNull().Where(i => i.IsMailable() && i.Entry == itemId);
        //}

        ///// <summary>
        /////     Returns number of specified itemId in bags which can be mailed (isMailable extension).
        ///// </summary>
        ///// <param name="itemId"></param>
        ///// <returns></returns>
        //private static long GetNumberItemInBags(uint itemId)
        //{
        //    return
        //        StyxWoW.Me.BagItems.GetEmptyIfNull().Sum(i => i.IsMailable() && i.Entry == itemId ? i.StackCount : 0);
        //}

        ///// <summary>
        ///// Split a stack of itemId in a stack of amount and the rest. 
        ///// Will stack items if found ItemId in bags.
        ///// if amount is superior to max stack size ex, (wants 300, got 330, max 200) cuts in (200/100/30)
        ///// </summary>
        ///// <param name="amount">The amount you want to be able to pick from bags</param>
        ///// <param name="itemdId"></param>


        /// <summary>
        ///     Split a stack of itemId in stack size-amount and amount.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="itemId"></param>
        private static async Task SplitOneStack(int amount, uint itemId)
        {
            var item = HbApi.GetItemsInBags(i => i.Entry == itemId && i.StackCount > amount).FirstOrDefault();

            if (item != default(WoWItem))
            {
                await item.Split(amount);
            }
        }

        #endregion
    }
}
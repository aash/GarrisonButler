using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GarrisonButler.API;
using JetBrains.Annotations;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Config.OldSettings
{
    public class MailCondition : INotifyPropertyChanged
    {
        public enum Conditions
        {
            NumberInBagsSuperiorTo = 0,
            NumberInBagsSuperiorOrEqualTo = 1,
            KeepNumberInBags = 2,
            None = 99
        }

        private string _name;
        private Conditions _condition;
        private int _checkValue;
        private static List<MailCondition> _allPossibleConditions;

        #region GetSet

        /// <summary>
        /// Name of the condition as displayed in UI
        /// </summary>
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
        /// Condition of the item
        /// </summary>
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
        /// Value to check against for condition
        /// </summary>
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

        /// <summary>
        /// Retrieve the name from condition, to be used for initialization only.
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
                    GarrisonButler.Diagnostic("This rule has not been implemented! Defaulting to None.");
                    break;
            }
            return Conditions.None;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns value of the condition 
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
                    GarrisonButler.Diagnostic("This rule has not been implemented!");
                    break;
            }
            return false;
        }

        /// <summary>
        /// Returns a static list of all the possible conditions implemented
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
        /// Returns items to send or null if none.
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
                    GarrisonButler.Diagnostic("This rule has not been implemented!");
                    break;
            }
            return null;
        }

        #region Rules

        // A rule must have a method returning a bool and a method returning the list of items

        /// <summary>
        /// Return true if the specified character have more than x count of ItemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberInBagsSuperiorTo(uint itemId, int x)
        {
            var numInBags = GetNumberItemInBags(itemId);
            return numInBags > x;
        }

        /// <summary>
        /// Return array of items to send to respect the following rule: if the specified character have more than x count of ItemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorTo(uint itemId)
        {
            return GetAllItems(itemId);
        }

        /// <summary>
        /// Return true if the specified character have x or more than x count of ItemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberInBagsSuperiorOrEqualTo(uint itemId, int x)
        {
            var numInBags = GetNumberItemInBags(itemId);
            return numInBags >= x;
        }

        /// <summary>
        /// Return array of items to send to respect the following rule: if the specified character have x or more than x count of itemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorOrEqualTo(uint itemId)
        {
            return GetAllItems(itemId);
        }

        /// <summary>
        /// Returns if there is more of the specified ItemId than the threshold.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        private static bool IsKeepNumberInBags(uint itemId, int threshold)
        {
            return IsNumberInBagsSuperiorTo(itemId, threshold);
        }

        /// <summary>
        /// Return array of items to send to respect the following rule: if the specified character have x or more than x count of itemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<WoWItem>> GetNumberKeepNumberInBags(uint itemId, int x)
        {
            for (var i = 0; i < 15; i++)
            {
                HbApi.StackItems();
                await CommonCoroutines.SleepForLagDuration();
            }
            await Buddy.Coroutines.Coroutine.Yield();
            SplitItemStack(x, itemId);
            await CommonCoroutines.SleepForRandomUiInteractionTime();

            // This is the pain... Not sure it will work
            var items = GetAllItems(itemId);
            var woWItems = items as WoWItem[] ?? items.ToArray();
            var toKeep = woWItems.FirstOrDefault(i => i.StackCount == x);
            return toKeep == default(WoWItem) ? new List<WoWItem>() : woWItems.Where(i => i != toKeep);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns all the stacks of the specified itemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetAllItems(uint itemId)
        {
            return StyxWoW.Me.BagItems.Where(i => i != null && i.IsValid && i.Entry == itemId);
        }

        /// <summary>
        /// Returns number of specified itemId in bags.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static long GetNumberItemInBags(uint itemId)
        {
            return
                StyxWoW.Me.BagItems.Sum(i => i != null && i.IsValid && i.Entry == itemId ? i.StackCount : 0);
        }

        /// <summary>
        /// Split a stack of itemId in a stack of amount and the rest.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="itemdId"></param>
        private static void SplitItemStack(int amount, uint itemdId)
        {
            var possibleStacks = StyxWoW.Me.BagItems.Where(i => i.Entry == itemdId && i.StackCount >= amount);
            if (!possibleStacks.Any())
                return;

            Lua.DoString(
                string.Format(
                    "local amount = {0}; ", amount) +
                string.Format(
                    "local item = {0}; ", itemdId) +
                "local ItemBagNr = 0; " +
                "local ItemSlotNr = 1; " +
                "local EmptyBagNr = 0; " +
                "local EmptySlotNr = 1; " +
                "for b=0,4 do " +
                "for s=1,GetContainerNumSlots(b) do " +
                "if ((GetContainerItemID(b,s) == item)) " + /*"and (select(3, GetContainerItemInfo(b,s)) == nil)) */
                "then " +
                "ItemBagNr = b; " +
                "ItemSlotNr = s; " +
                "end; " +
                "end; " +
                "end; " +
                "for b=0,4 do " +
                "for s=1,GetContainerNumSlots(b) do " +
                "if GetContainerItemID(b,s) == nil then " +
                "EmptyBagNr = b; " +
                "EmptySlotNr = s; " +
                "end; " +
                "end; " +
                "end; " +
                "ClearCursor(); " +
                "SplitContainerItem(ItemBagNr,ItemSlotNr,amount); " +
                "if CursorHasItem() then " +
                "PickupContainerItem(EmptyBagNr,EmptySlotNr); " +
                "ClearCursor(); " +
                "end;"
                );
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged1([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public Objects.MailCondition FromOld()
        {
            var condition = new Objects.MailCondition((Objects.MailCondition.Conditions) Condition, CheckValue);
            return condition;
        }
    }
}
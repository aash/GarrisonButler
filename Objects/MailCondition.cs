using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using JetBrains.Annotations;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Objects
{
    /// <summary>
    /// Represents a condition for a mail item
    /// </summary>
    public class MailCondition : INotifyPropertyChanged
    {
        public enum Conditions
        {
            NumberInBagsSuperiorTo = 0,
            NumberInBagsSuperiorOrEqualTo = 1,
            KeepNumberInBags = 2,
            None = 99,
        }

        private string _name;
        private Conditions _condition;
        private int _checkValue;

        #region GetSet
        /// <summary>
        /// Name of the condition as displayed in UI
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged1();
                }
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
                if (value != _condition)
                {
                    _condition = value;
                    OnPropertyChanged1();
                }
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
                if (value != _checkValue)
                {
                    _checkValue = value;
                    OnPropertyChanged1();
                }
            }
        }
        #endregion

        public MailCondition(Conditions condition, int checkValue)
        {
            _condition = condition;
            _checkValue = checkValue;
            _name = getName(_condition);
        }

        public MailCondition()
        {
            _condition = Conditions.None;
            _checkValue = 0;
            _name = getName(_condition);
        }

        /// <summary>
        /// Retrieve the name from condition, to be used for initialization only.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private string getName(Conditions condition)
        {
            switch (condition)
            {
                case Conditions.NumberInBagsSuperiorTo:
                    return "if > to in Bags";

                case Conditions.NumberInBagsSuperiorOrEqualTo:
                    return "if >= to in Bags";

                case Conditions.KeepNumberInBags:
                    return "Keep in Bags at least";
                    
                case Conditions.None:
                    return "None/Deactivated";
                default:
                    GarrisonButler.Diagnostic("This rule has not been implemented!");
                    break;
            }
            return "Not Implemented";
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

                default:
                    GarrisonButler.Diagnostic("This rule has not been implemented!");
                    break;
            }
            return false;
        }

        /// <summary>
        /// Returns a list of all the possible conditions implemented
        /// </summary>
        /// <returns></returns>
        public static List<MailCondition> GetAllPossibleConditions()
        {
            return (from object condition in Enum.GetValues(typeof(Conditions)) select new MailCondition((Conditions)condition, 0)).ToList();
        }

        /// <summary>
        /// Returns items to send or null if none.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public IEnumerable<WoWItem> GetItemsOrNull(uint itemId)
        {
            switch (_condition)
            {
                case Conditions.NumberInBagsSuperiorTo:
                    return GetNumberInBagsSuperiorTo(itemId, _checkValue);

                case Conditions.NumberInBagsSuperiorOrEqualTo:
                    return GetNumberInBagsSuperiorOrEqualTo(itemId, _checkValue);

                case Conditions.KeepNumberInBags:
                    return GetNumberKeepNumberInBags(itemId, _checkValue);

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
            long numInBags = GetNumberItemInBags(itemId);
            return numInBags > x;
        }

        /// <summary>
        /// Return array of items to send to respect the following rule: if the specified character have more than x count of ItemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorTo(uint itemId, int x)
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
            long numInBags = GetNumberItemInBags(itemId);
            return numInBags >= x;
        }
        /// <summary>
        /// Return array of items to send to respect the following rule: if the specified character have x or more than x count of itemId in bags send everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static IEnumerable<WoWItem> GetNumberInBagsSuperiorOrEqualTo(uint itemId, int x)
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
        private static IEnumerable<WoWItem> GetNumberKeepNumberInBags(uint itemId, int x)
        {
            // This is the pain...
            throw new NotImplementedException("To do");
            return null;
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

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged1([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));

        }
    }
}

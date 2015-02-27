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
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler.Objects
{
    /// <summary>
    ///     Represents a condition for a mail item
    /// </summary>
    public class MissionCondition : INotifyPropertyChanged, IComparable
    {
        [XmlType(TypeName = "MissionConditions")]
        public enum Conditions
        {
            // Inventory Conditions
            NumberPlayerHasSuperiorTo = 0,
            NumberPlayerHasSuperiorOrEqualTo = 1,
            NumberPlayerHasInferiorTo = 2,
            NumberPlayerHasInferiorOrEqualTo = 3,
            // Reward Conditions
            NumberRewardSuperiorTo = 4,
            NumberRewardSuperiorOrEqualTo = 5,
            NumberRewardInferiorTo = 6,
            NumberRewardInferiorOrEqualTo = 7,
            None = 99
        }

        private static List<MissionCondition> _allPossibleConditions;
        private int _checkValue;
        private Conditions _condition;
        private string _name;

        public MissionCondition(Conditions condition, int checkValue, Mission mission = null)
        {
            _condition = condition;
            _checkValue = checkValue;
            _name = GetName(_condition);
            //_mission = mission;
            //if (_mission != null)
            //    _missionId = _mission.MissionId;
        }

        public MissionCondition()
        {
            _condition = Conditions.None;
            _checkValue = 0;
            _name = GetName(_condition);
            //_mission = null;
        }

        public int CompareTo(object obj)
        {
            var missionCondition = obj as MissionCondition;
            return missionCondition != null
                ? String.Compare(Name, missionCondition.Name, StringComparison.Ordinal)
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
                case Conditions.NumberPlayerHasSuperiorTo:
                    return "if I have > #";

                case Conditions.NumberPlayerHasSuperiorOrEqualTo:
                    return "if I have >= #";

                case Conditions.NumberPlayerHasInferiorTo:
                    return "if I have < #";

                case Conditions.NumberPlayerHasInferiorOrEqualTo:
                    return "if I have <= #";

                case Conditions.NumberRewardSuperiorTo:
                    return "if rewards > #";

                case Conditions.NumberRewardSuperiorOrEqualTo:
                    return "if rewards >= #";

                case Conditions.NumberRewardInferiorTo:
                    return "if rewards < #";

                case Conditions.NumberRewardInferiorOrEqualTo:
                    return "if rewards <= #";

                case Conditions.None:
                    return "Deactivated";
                default:
                    GarrisonButler.Diagnostic("This mission rule has not been implemented!");
                    break;
            }
            return "Not Implemented";
        }

        private static Conditions GetCondition(String name)
        {
            switch (name)
            {
                case "if I have > #":
                    return Conditions.NumberPlayerHasSuperiorTo;

                case "if I have >= #":
                    return Conditions.NumberPlayerHasSuperiorOrEqualTo;

                case "if I have < #":
                    return Conditions.NumberPlayerHasInferiorTo;

                case "if I have <= #":
                    return Conditions.NumberPlayerHasInferiorOrEqualTo;

                case "if rewards > #":
                    return Conditions.NumberRewardSuperiorTo;

                case "if rewards >= #":
                    return Conditions.NumberRewardSuperiorOrEqualTo;

                case "if rewards < #":
                    return Conditions.NumberRewardInferiorTo;

                case "if rewards <= #":
                    return Conditions.NumberRewardInferiorOrEqualTo;

                case "Deactivated":
                    return Conditions.None;

                default:
                    GarrisonButler.Diagnostic("This mission rule has not been implemented! Defaulting to None.");
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
        public bool GetCondition(MissionReward reward)
        {
            switch (_condition)
            {
                case Conditions.NumberPlayerHasSuperiorTo:
                    return IsNumberPlayerHasSuperiorTo(reward, _checkValue);

                case Conditions.NumberPlayerHasSuperiorOrEqualTo:
                    return IsNumberPlayerHasSuperiorOrEqualTo(reward, _checkValue);

                case Conditions.NumberPlayerHasInferiorTo:
                    return IsNumberPlayerHasInferiorTo(reward, _checkValue);

                case Conditions.NumberPlayerHasInferiorOrEqualTo:
                    return IsNumberPlayerHasInferiorOrEqualTo(reward, _checkValue);

                case Conditions.NumberRewardSuperiorTo:
                    return IsNumberRewardSuperiorTo(reward, _checkValue);

                case Conditions.NumberRewardSuperiorOrEqualTo:
                    return IsNumberRewardSuperiorOrEqualTo(reward, _checkValue);

                case Conditions.NumberRewardInferiorTo:
                    return IsNumberRewardInferiorTo(reward, _checkValue);

                case Conditions.NumberRewardInferiorOrEqualTo:
                    return IsNumberRewardInferiorOrEqualTo(reward, _checkValue);

                case Conditions.None:
                    return true;

                default:
                    GarrisonButler.Diagnostic("GetCondition: This mission rule has not been implemented! Id: " + reward.Id + " Name: " + reward.Name);
                    break;
            }
            return true;
        }

        /// <summary>
        ///     Returns a static list of all the possible conditions implemented
        /// </summary>
        /// <returns></returns>
        public static List<MissionCondition> GetAllPossibleConditions()
        {
            if (_allPossibleConditions != null)
                return _allPossibleConditions;

            _allPossibleConditions =
                (from object condition in Enum.GetValues(typeof(Conditions))
                 select new MissionCondition((Conditions)condition, 0)).ToList();
            return _allPossibleConditions;
        }

        /// <summary>
        ///     Returns items to send or null if none.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<object>> GetItemsOrNull(MissionReward reward)
        {
            switch (_condition)
            {
                case Conditions.NumberPlayerHasSuperiorTo:
                    return GetNumberPlayerHasSuperiorTo(reward);

                case Conditions.NumberPlayerHasSuperiorOrEqualTo:
                    return GetNumberPlayerHasSuperiorOrEqualTo(reward);

                case Conditions.NumberPlayerHasInferiorTo:
                    return GetNumberPlayerHasInferiorTo(reward);

                case Conditions.NumberPlayerHasInferiorOrEqualTo:
                    return GetNumberPlayerHasInferiorOrEqualTo(reward);

                case Conditions.NumberRewardSuperiorTo:
                    return (IEnumerable<object>)GetNumberRewardSuperiorTo(reward);

                case Conditions.NumberRewardSuperiorOrEqualTo:
                    return GetNumberRewardSuperiorOrEqualTo(reward);

                case Conditions.NumberRewardInferiorTo:
                    return GetNumberRewardInferiorTo(reward);

                case Conditions.NumberRewardInferiorOrEqualTo:
                    return GetNumberRewardInferiorOrEqualTo(reward);

                //case Conditions.NumberInBagsSuperiorTo:
                //    return GetNumberInBagsSuperiorTo(itemId);

                //case Conditions.NumberInBagsSuperiorOrEqualTo:
                //    return GetNumberInBagsSuperiorOrEqualTo(itemId);

                //case Conditions.KeepNumberInBags:
                //    return await GetNumberKeepNumberInBags(itemId, _checkValue);

                case Conditions.None:
                    return null;

                default:
                    GarrisonButler.Diagnostic("GetItemsOrNull: This mission rule has not been implemented! Id: " + reward.Id + " Name: " + reward.Name);
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
        ///     Return true if the specified character has more than x count of ItemId carried.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberPlayerHasSuperiorTo(MissionReward reward, int x)
        {
            uint numCarried = 0;

            if (reward.IsItemReward)
            {
                numCarried = (uint)HbApi.GetNumberItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                numCarried = reward._CurrencyInfo.Amount;
            }
            else if (reward.IsGold)
            {
                numCarried = (uint)StyxWoW.Me.Gold;
            }
            //else if (reward.IsFollowerXP)
            //{
            //    numCarried = x;
            //}
            return numCarried > x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified character has more than x count of
        ///     ItemId carried, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberPlayerHasSuperiorTo(MissionReward reward)
        {
            var numCarried = Enumerable.Empty<object>();
            if (reward.IsItemReward)
            {
                numCarried = HbApi.GetItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                var currency = WoWCurrency.GetCurrencyById((uint)reward.Id);
                numCarried = new List<WoWCurrency>();
                ((List<WoWCurrency>) numCarried).Add(currency);
            }
            else if (reward.IsGold)
            {
                var gold = (int)StyxWoW.Me.Gold;
                numCarried = new List<object>();
                ((List<object>)numCarried).Add((object)gold);
            }
            return numCarried;
        }

        /// <summary>
        ///     Return true if the specified character have x or more than x count of ItemId carried.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberPlayerHasSuperiorOrEqualTo(MissionReward reward, int x)
        {
            uint numCarried = 0;

            if (reward.IsItemReward)
            {
                numCarried = (uint)HbApi.GetNumberItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                numCarried = reward._CurrencyInfo.Amount;
            }
            else if (reward.IsGold)
            {
                numCarried = (uint)StyxWoW.Me.Gold;
            }

            return numCarried != 0 && numCarried >= x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified character have x or more than x count
        ///     of itemId carried, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberPlayerHasSuperiorOrEqualTo(MissionReward reward)
        {
            var numCarried = Enumerable.Empty<object>();
            if (reward.IsItemReward)
            {
                numCarried = HbApi.GetItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                var currency = WoWCurrency.GetCurrencyById((uint)reward.Id);
                numCarried = new List<WoWCurrency>();
                ((List<WoWCurrency>)numCarried).Add(currency);
            }
            else if (reward.IsGold)
            {
                var gold = (int)StyxWoW.Me.Gold;
                numCarried = new List<object>();
                ((List<object>)numCarried).Add((object)gold);
            }
            return numCarried;
        }

        /// <summary>
        ///     Return true if the specified character has less than x count of ItemId carried.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberPlayerHasInferiorTo(MissionReward reward, int x)
        {
            uint numCarried = 0;

            if (reward.IsItemReward)
            {
                numCarried = (uint)HbApi.GetNumberItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                numCarried = reward._CurrencyInfo.Amount;
            }
            else if (reward.IsGold)
            {
                numCarried = (uint)StyxWoW.Me.Gold;
            }
            return numCarried < x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified character has less than x count of
        ///     ItemId carried, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberPlayerHasInferiorTo(MissionReward reward)
        {
            var numCarried = Enumerable.Empty<object>();
            if (reward.IsItemReward)
            {
                numCarried = HbApi.GetItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                var currency = WoWCurrency.GetCurrencyById((uint)reward.Id);
                numCarried = new List<WoWCurrency>();
                ((List<WoWCurrency>)numCarried).Add(currency);
            }
            else if (reward.IsGold)
            {
                var gold = (int)StyxWoW.Me.Gold;
                numCarried = new List<object>();
                ((List<object>)numCarried).Add((object)gold);
            }
            return numCarried;
        }

        /// <summary>
        ///     Return true if the specified character have x or less than x count of ItemId carried.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberPlayerHasInferiorOrEqualTo(MissionReward reward, int x)
        {
            uint numCarried = 0;

            if (reward.IsItemReward)
            {
                numCarried = (uint)HbApi.GetNumberItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                numCarried = reward._CurrencyInfo.Amount;
            }
            else if (reward.IsGold)
            {
                numCarried = (uint)StyxWoW.Me.Gold;
            }
            return numCarried != 0 && numCarried <= x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified character have x or less than x count
        ///     of itemId carried, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberPlayerHasInferiorOrEqualTo(MissionReward reward)
        {
            var numCarried = Enumerable.Empty<object>();
            if (reward.IsItemReward)
            {
                numCarried = HbApi.GetItemCarried((uint)reward.Id);
            }
            else if (reward.IsCurrencyReward)
            {
                var currency = WoWCurrency.GetCurrencyById((uint)reward.Id);
                numCarried = new List<WoWCurrency>();
                ((List<WoWCurrency>)numCarried).Add(currency);
            }
            else if (reward.IsGold)
            {
                var gold = (int)StyxWoW.Me.Gold;
                numCarried = new List<object>();
                ((List<object>)numCarried).Add((object)gold);
            }
            return numCarried;
        }

        //*****************************
        //*****************************
        //*****************************
        /// <summary>
        ///     Return true if the specified mission has reward more than x count of ItemId.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberRewardSuperiorTo(MissionReward reward, int x)
        {
            return reward.Quantity > x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified mission has more than x count of
        ///     ItemId as a reward, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberRewardSuperiorTo(MissionReward reward)
        {
            var retval = new List<object>();
            retval.Add(reward.Quantity);
            return retval;
        }

        /// <summary>
        ///     Return true if the specified mission has x or more than x count of ItemId as a reward.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberRewardSuperiorOrEqualTo(MissionReward reward, int x)
        {
            return reward.Quantity >= x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified mission has x or more than x count
        ///     of itemId as a reward, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberRewardSuperiorOrEqualTo(MissionReward reward)
        {
            var retval = new List<object>();
            retval.Add(reward.Quantity);
            return retval;
        }

        /// <summary>
        ///     Return true if the specified mission has less than x count of ItemId as a reward.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberRewardInferiorTo(MissionReward reward, int x)
        {
            return reward.Quantity < x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified mission has less than x count of
        ///     ItemId as a reward, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberRewardInferiorTo(MissionReward reward)
        {
            var retval = new List<object>();
            retval.Add(reward.Quantity);
            return retval;
        }

        /// <summary>
        ///     Return true if the specified mission has x or less than x count of ItemId as a reward.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsNumberRewardInferiorOrEqualTo(MissionReward reward, int x)
        {
            return reward.Quantity <= x;
        }

        /// <summary>
        ///     Return array of items to respect the following rule: if the specified mission has x or less than x count
        ///     of itemId as a reward, return everything.
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetNumberRewardInferiorOrEqualTo(MissionReward reward)
        {
            var retval = new List<object>();
            retval.Add(reward.Quantity);
            return retval;
        }

        /// <summary>
        ///     Cut a stack and get returns the part of the size asked if found otherwise default object.
        /// </summary>
        /// <param name="sizeToCut"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private static async Task<object> CutAndGetStack(int sizeToCut, uint itemId)
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
                return default(object); // Error finding correct stack size
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
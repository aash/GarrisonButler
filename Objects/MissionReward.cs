using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using JetBrains.Annotations;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System.Xml.Serialization;
using GarrisonButler.Config;
using GarrisonButler.Libraries;

namespace GarrisonButler.Objects
{
    public class MissionReward : INotifyPropertyChanged
    {
        private int _id;
        [XmlAttribute("Id")]
        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                if(IsItemReward)
                    _ItemInfo = ItemInfo.FromId((uint)value);
                else if (IsCurrencyReward)
                    _CurrencyInfo = WoWCurrency.GetCurrencyById((uint) value);
            }
        }
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAttribute("Category")]
        public MissionRewardCategory Category { get; set; }
        [XmlAttribute("IndividualSuccessChanceEnabled")]
        public bool IndividualSuccessChanceEnabled { get; set; }
        [XmlAttribute("DisallowMissionsWithThisReward")]
        public bool DisallowMissionsWithThisReward { get; set; }
        [XmlAttribute("RequiredLevel")]
        public int RequiredLevel { get; set; }
        // Set by the user with the slider for this reward
        [XmlAttribute("RequiredSuccessChance")]
        public int RequiredSuccessChance { get; set; }
        [XmlIgnore]
        public ItemInfo _ItemInfo { get; set; }
        [XmlIgnore]
        public WoWCurrency _CurrencyInfo { get; set; }
        [XmlIgnore]
        public int Quantity { get; set; }
        [XmlIgnore]
        public string Icon { get; set; }

        private MissionCondition _missionCondition;
        private string _missionConditionComment;
        private MissionCondition _playerCondition;
        private string _playerConditionComment;

        [XmlElement("MissionCondition")]
        public MissionCondition ConditionForMission
        {
            get { return _missionCondition; }
            set
            {
                if (value == _missionCondition) return;
                _missionCondition = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int CheckValueForMission
        {
            get { return _missionCondition != null ? _missionCondition.CheckValue : 0; }
            set
            {
                if (value == _missionCondition.CheckValue) return;
                _missionCondition.CheckValue = value;
                OnPropertyChanged();
            }
        }

        public string CommentForMissionCondition
        {
            get { return _missionConditionComment; }
            set
            {
                if (value == _missionConditionComment) return;
                _missionConditionComment = value;
                OnPropertyChanged();
            }
        }

        [XmlElement("PlayerCondition")]
        public MissionCondition ConditionForPlayer
        {
            get { return _playerCondition; }
            set
            {
                if (value == _playerCondition) return;
                _playerCondition = value;
                OnPropertyChanged();
            }
        }

        [XmlIgnore]
        public int CheckValueForPlayer
        {
            get { return _playerCondition != null ? _playerCondition.CheckValue : 0; }
            set
            {
                if (value == _playerCondition.CheckValue) return;
                _playerCondition.CheckValue = value;
                OnPropertyChanged();
            }
        }

        public string CommentForPlayerCondition
        {
            get { return _playerConditionComment; }
            set
            {
                if (value == _playerConditionComment) return;
                _playerConditionComment = value;
                OnPropertyChanged();
            }
        }

        //(string title, int quantity, int currencyID, int itemID, int followerXP, string name, string icon)

        public enum MissionRewardCategory
        {
            Currency = 0,           // currency
            PlayerExperience = 1,   // item
            PlayerGear = 2,         // item
            FollowerExperience = 3, // NONE
            FollowerGear = 4,       // item
            FollowerItem = 5,       // item
            FollowerContract = 6,   // item
            LegendaryQuestItem = 7, // item
            ReputationToken = 8,    // item
            VanityItem = 9,         // item
            MiscItem = 10,          // item
            Profession = 11,        // item
            UnknownItem = 12,       // item
            Unknown = 13,           // NONE
            Gold = 14               // NONE
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        //public void SetCondition(string name)
        //{
        //    _condition.Name = name;
        //    OnPropertyChanged("Condition");
        //}

        //public bool CanConsiderReward()
        //{
        //    return _condition.GetCondition(this);
        //}

        //public async Task<IEnumerable<object>> GetRewardObjectList()
        //{
        //    return await _condition.GetItemsOrNull(this);
        //}


        /*
        var currentIndex = cpt + 7 * i;
                var rewardTitle = mission[currentIndex + 1] == "nil" ? "" : mission[currentIndex + 1];
                var rewardQuantity = mission[currentIndex + 2].ToInt32();
                var rewardCurrencyID = mission[currentIndex + 3].ToInt32();
                var rewardItemID = mission[currentIndex + 4].ToInt32();
                var rewardFollowerXP = mission[currentIndex + 5].ToInt32();
                var rewardName = mission[currentIndex + 6] == "nil" ? "" : mission[currentIndex + 6];
                var rewardIcon = mission[currentIndex + 7] == "nil" ? "" : mission[currentIndex + 7];
        */

        public MissionReward()
        {
            this.Name = string.Empty;
            this.Category = MissionRewardCategory.Unknown;
        }

        public MissionReward(MissionReward other)
        {
            this.Category = other.Category;
            this.Name = other.Name;
            this.Id = other.Id;
            this.RequiredLevel = other.RequiredLevel;
            this.RequiredSuccessChance = other.RequiredSuccessChance;
            this._ItemInfo = other._ItemInfo;
            this._CurrencyInfo = other._CurrencyInfo;
        }

        public void CopyFrom(MissionReward other)
        {
            this.Category = other.Category;
            this.Name = other.Name;
            this.Id = other.Id;
            this.RequiredLevel = other.RequiredLevel;
            this.RequiredSuccessChance = other.RequiredSuccessChance;
            this._ItemInfo = other._ItemInfo;
            this._CurrencyInfo = other._CurrencyInfo;
        }

        /// <summary>
        /// Create object from LUA values returned in mission rewards
        /// </summary>
        /// <param name="title"></param>
        /// <param name="quantity"></param>
        /// <param name="currencyID"></param>
        /// <param name="itemID"></param>
        /// <param name="followerXP"></param>
        /// <param name="name"></param>
        /// <param name="icon"></param>
        public MissionReward(string title, int quantity, int currencyID, int itemID, int followerXP, string name, string icon)
        {
            Icon = icon;

            if(itemID > 0)
            {
                // Item Reward
                // 120205 item ID = player experience
                var reward = GaBSettings.Get().MissionRewardSettings.FirstOrDefault(r => r.Id == itemID);
                if(reward != null)
                    this.CopyFrom(reward);
                else
                {
                    this.Category = MissionRewardCategory.UnknownItem;
                    PopulateItemInfo(itemID);
                }
                this.Quantity = quantity;
            }
            else if(quantity > 0)
            {
                // Gold Reward
                if(currencyID == 0)
                {
                    var reward = GaBSettings.Get().MissionRewardSettings.FirstOrDefault(r => r.Id == (int)MissionRewardCategory.Gold);
                    if (reward != null)
                    {
                        this.CopyFrom(reward);
                    }
                    else
                    {
                        this.Category = MissionRewardCategory.Gold;
                        this.Id = (int) MissionRewardCategory.Gold;
                    }
                    this.Quantity = quantity;
                    this.Name = "Gold";
                }
                // Currency Reward
                else
                {
                    var reward = GaBSettings.Get().MissionRewardSettings.FirstOrDefault(r => r.Id == currencyID);
                    if (reward != null)
                    {
                        this.CopyFrom(reward);
                        PopulateCurrencyInfo(currencyID);
                    }
                    else
                    {
                        this.Category = MissionRewardCategory.Currency;
                        PopulateCurrencyInfo(currencyID);
                        this.Id = currencyID;
                    }
                    this.Quantity = quantity;
                }
            }
            else
            {
                // Follower XP Reward
                if(followerXP > 0)
                {
                    var reward =
                        GaBSettings.Get()
                            .MissionRewardSettings.FirstOrDefault(r => r.Id == (int)MissionRewardCategory.FollowerExperience);
                    if (reward != null)
                    {
                        this.CopyFrom(reward);
                    }
                    else
                    {
                        this.Category = MissionRewardCategory.FollowerExperience;
                        this.Id = (int)MissionRewardCategory.FollowerExperience;
                    }
                    this.Quantity = followerXP;
                    this.Name = "FollowerXP";
                }
                // ???
                else
                {
                    this.Category = MissionRewardCategory.Unknown;
                }
            }
        }

        [XmlIgnore]
        public bool IsItemReward
        {
            get
            {
                return Category == MissionRewardCategory.PlayerGear
                       || Category == MissionRewardCategory.FollowerGear
                       || Category == MissionRewardCategory.FollowerItem
                       || Category == MissionRewardCategory.MiscItem
                       || Category == MissionRewardCategory.LegendaryQuestItem
                       || Category == MissionRewardCategory.ReputationToken
                       || Category == MissionRewardCategory.Profession
                       || Category == MissionRewardCategory.FollowerContract
                       || Category == MissionRewardCategory.VanityItem
                       || Category == MissionRewardCategory.UnknownItem
                       || Category == MissionRewardCategory.PlayerExperience;
            }
        }

        [XmlIgnore]
        public bool IsCurrencyReward
        {
            get { return Category == MissionRewardCategory.Currency; }
        }

        [XmlIgnore]
        public bool IsFollowerXP
        {
            get { return Category == MissionRewardCategory.FollowerExperience; }
        }

        [XmlIgnore]
        public bool IsGold
        {
            get { return Category == MissionRewardCategory.Gold; }
        }

        /// <summary>
        /// Populate an object from known values at compile time for use in AllRewards list
        /// </summary>
        /// <param name="id"></param>
        /// <param name="category"></param>
        /// <param name="reqLevel"></param>
        public MissionReward(int id, MissionRewardCategory category, int reqLevel = 0)
        {
            Category = category;
            RequiredLevel = reqLevel;
            Id = id;

            if (IsCurrencyReward)
            {
                PopulateCurrencyInfo(id);
            } else if (IsItemReward)
            {
                PopulateItemInfo(Id);
            }
            else if(id == (int)category)
            {
                Name = category.ToString();
            }

            if (reqLevel != 0)
                RequiredLevel = reqLevel;
        }

        public void PopulateItemInfo(int itemID)
        {
            Id = itemID;
            _ItemInfo = ItemInfo.FromId((uint)itemID);
            if (_ItemInfo != null)
            {
                Name = _ItemInfo.Name;
                RequiredLevel = _ItemInfo.RequiredLevel;
            }
        }

        public void PopulateCurrencyInfo(int currencyID)
        {
            _CurrencyInfo = WoWCurrency.GetCurrencyById((uint)Id);

            if(_CurrencyInfo != null)
                Name = _CurrencyInfo.Name;

            if (Name.IsNullOrEmpty())
            {
                var currencyItemLink = API.ApiLua.GetCurrencyItemLink(currencyID);
                var name = API.ApiLua.GetNameFromItemLink(currencyItemLink);
                Name = name;
            }
        }

        [XmlIgnore]
        public static readonly List<MissionReward> AllRewards =
            new List<MissionReward>
            {
                // Special Items
                new MissionReward((int)MissionRewardCategory.FollowerExperience, MissionRewardCategory.FollowerExperience),
                new MissionReward((int)MissionRewardCategory.Gold, MissionRewardCategory.Gold),
                // Currency - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#rewards
                new MissionReward(823, MissionRewardCategory.Currency),             // Currency - Apexis Crystal - http://www.wowhead.com/currency=823/apexis-crystal
                new MissionReward(824, MissionRewardCategory.Currency),             // Currency - Garrison Resources - http://www.wowhead.com/currency=824/garrison-resources
                new MissionReward(390, MissionRewardCategory.Currency),             // Currency - Conquest Points - http://www.wowhead.com/currency=390/conquest-points
                new MissionReward(994, MissionRewardCategory.Currency),             // Currency - Seal of Tempered Fate - http://www.wowhead.com/currency=994/seal-of-tempered-fate
                new MissionReward(392, MissionRewardCategory.Currency),             // Currency - Honor Points - http://www.wowhead.com/currency=392/honor-points
                new MissionReward(824, MissionRewardCategory.Currency),             // Currency - Garrison Resources - http://www.wowhead.com/currency=824/garrison-resources
                // Archaeology Currency
                new MissionReward(829, MissionRewardCategory.Currency),             // Currency - Arakkoa Archaeology Fragment - http://www.wowhead.com/currency=829/arakkoa-archaeology-fragment
                new MissionReward(828, MissionRewardCategory.Currency),             // Currency - Ogre Archaeology Fragment - http://ptr.wowhead.com/currency=828/ogre-archaeology-fragment
                new MissionReward(821, MissionRewardCategory.Currency),             // Currency - Draenor Clans Archaeology Fragment - http://ptr.wowhead.com/currency=821/draenor-clans-archaeology-fragment
                // Player Experience - http://www.wowhead.com/item=120205/xp
                new MissionReward(120205, MissionRewardCategory.PlayerExperience),  // Item automatically awards XP to the player
                // Player Gear
                new MissionReward(114053, MissionRewardCategory.PlayerGear, 90),    // PlayerGear - Shimmering Gauntlets - Level 90 - ilvl 512 - http://www.wowhead.com/item=114053/shimmering-gauntlets
                new MissionReward(114052, MissionRewardCategory.PlayerGear, 90),    // PlayerGear - Gleaming Ring - Level 90 - ilvl 519 - http://www.wowhead.com/item=114052/gleaming-ring
                new MissionReward(114101, MissionRewardCategory.PlayerGear, 91),    // PlayerGear - Tormented Girdle - Level 91 - ilvl 530 - http://www.wowhead.com/item=114101/tormented-girdle
                new MissionReward(114098, MissionRewardCategory.PlayerGear, 92),    // PlayerGear - Tormented Hood - Level 92 - ilvl 540 - http://www.wowhead.com/item=114098/tormented-hood
                new MissionReward(114096, MissionRewardCategory.PlayerGear, 93),    // PlayerGear - Tormented Treads - Level 93 - ilvl 550 - http://www.wowhead.com/item=114096/tormented-treads
                new MissionReward(114108, MissionRewardCategory.PlayerGear, 94),    // PlayerGear - Tormented Armament - Level 94 - ilvl 560 - http://www.wowhead.com/item=114108/tormented-armament
                new MissionReward(114094, MissionRewardCategory.PlayerGear, 95),    // PlayerGear - Tormented Bracers - Level 95 - ilvl 570 - http://www.wowhead.com/item=114094/tormented-bracers
                new MissionReward(114099, MissionRewardCategory.PlayerGear, 96),    // PlayerGear - Tormented Leggings - Level 96 - ilvl 580 - http://www.wowhead.com/item=114099/tormented-leggings
                new MissionReward(114097, MissionRewardCategory.PlayerGear, 97),    // PlayerGear - Tormented Gauntlets - Level 97 - ilvl 590 - http://www.wowhead.com/item=114097/tormented-gauntlets
                new MissionReward(114105, MissionRewardCategory.PlayerGear, 98),    // PlayerGear - Tormented Trinket - Level 98 - ilvl 600 - http://www.wowhead.com/item=114105/tormented-trinket
                new MissionReward(114100, MissionRewardCategory.PlayerGear, 99),    // PlayerGear - Tormented Spaulders - Level 99 - ilvl 610 - http://www.wowhead.com/item=114100/tormented-spaulders
                // ilvl Missions - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#item-level-missions
                new MissionReward(114063, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Spaulders - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114063/munificent-spaulders
                new MissionReward(114057, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Bracers - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114057/munificent-bracers
                new MissionReward(114058, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Robes - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114058/munificent-robes
                new MissionReward(114068, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Trinket - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114068/munificent-trinket
                new MissionReward(114066, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Choker - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114066/munificent-choker
                new MissionReward(114109, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Armament - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114109/munificent-armament
                new MissionReward(114059, MissionRewardCategory.PlayerGear),        // PlayerGear - Munificent Treads - Level 100 - ilvl 615 - Requires level 100 followers - http://www.wowhead.com/item=114059/munificent-treads
                new MissionReward(114080, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Trinket - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114080/turbulent-trinket
                new MissionReward(114071, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Treads - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114071/turbulent-treads
                new MissionReward(114075, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Spaulders - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114075/turbulent-spaulders
                new MissionReward(114078, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Choker - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114078/turbulent-choker
                new MissionReward(114110, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Armament - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114110/turbulent-armament
                new MissionReward(114070, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulent Robes - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114070/turbulent-robes
                new MissionReward(114069, MissionRewardCategory.PlayerGear),        // PlayerGear - Turbulet Bracers - Level 100 - ilvl 630 - Requires ilvl 615 followers - http://www.wowhead.com/item=114069/turbulent-bracers
                new MissionReward(114085, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Spaulders - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114085/grandiose-spaulders
                new MissionReward(114083, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Robes - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114083/grandiose-robes
                new MissionReward(114084, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Treads - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114084/grandiose-treads
                new MissionReward(114086, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Choker - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114086/grandiose-choker
                new MissionReward(114087, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Trinket - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114087/grandiose-trinket
                new MissionReward(114082, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Bracers - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114082/grandiose-bracers
                new MissionReward(114112, MissionRewardCategory.PlayerGear),        // PlayerGear - Grandiose Armament - Level 100 - ilvl 645 - Requires ilvl 630 followers - http://www.wowhead.com/item=114112/grandiose-armament
                // HighMaul gear - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#highmaul-missions
                // http://www.wowhead.com/guides/garrisons/buildings/garrison-buildings-for-raiding
                new MissionReward(118529, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - Cache of Highmaul Treasures (Normal) - Level 100 - ilvl 655 - Requires 2x ilvl645 Follower - http://www.wowhead.com/item=118529/cache-of-highmaul-treasures
                new MissionReward(118530, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - Cache of Highmaul Treasures (Heroic) - Level 100 - ilvl 670 - Normal version req + 15x boss kills from normal Highmaul - http://www.wowhead.com/item=118530/cache-of-highmaul-treasures
                new MissionReward(118531, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - Cache of Highmaul Treasures (Mythic) - Level 100 - ilvl 685 - Heroic version req + 15x boss kilils from heroic Highmaul  http://www.wowhead.com/item=118531/cache-of-highmaul-treasures
                // Blackrock Foundry Gear - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#6-1-ptr-missions
                new MissionReward(122484, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - PATCH 6.1 - Blackrock Foundry Spoils (Normal) - Level 100 - ilvl 665 - ??? - http://ptr.wowhead.com/item=122484/blackrock-foundry-spoils
                new MissionReward(122485, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - PATCH 6.1 - Blackrock Foundry Spoils (Heroic) - Level 100 - ilvl 680 - ??? - http://ptr.wowhead.com/item=122485/blackrock-foundry-spoils
                new MissionReward(122486, MissionRewardCategory.PlayerGear, 100),   // PlayerGear - PATCH 6.1 - Blackrock Foundry Spoils (Mythic) - Level 100 - ilvl 695 - ??? - http://ptr.wowhead.com/item=122486/blackrock-foundry-spoils
                // Follower contracts - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#follower-items
                new MissionReward(112848, MissionRewardCategory.FollowerContract),  // FollowerContract - Contract: Daleera Moonfang - http://www.wowhead.com/item=112848/contract-daleera-moonfang
                new MissionReward(114826, MissionRewardCategory.FollowerContract),  // FollowerContract - Contract: Bruma Swiftstone - http://www.wowhead.com/item=114826/contract-bruma-swiftstone
                new MissionReward(112737, MissionRewardCategory.FollowerContract),  // FollowerContract - Contract: Ka'la of the Frostwolves - http://www.wowhead.com/item=112737/contract-kala-of-the-frostwolves
                new MissionReward(114825, MissionRewardCategory.FollowerContract),  // FollowerContract - Contract: Ulna Thresher - http://www.wowhead.com/item=114825/contract-ulna-thresher
                // Follower gear - http://www.wowhead.com/guides/garrisons/warlords-of-draenor-garrison-missions-guide#follower-items
                new MissionReward(114807, MissionRewardCategory.FollowerGear),      // FollowerGear - War Ravaged Armor Set - ivl 615 (Green) - Can come from Armor Enhancement Token - http://www.wowhead.com/item=114807/war-ravaged-armor-set
                new MissionReward(114806, MissionRewardCategory.FollowerGear),      // FollowerGear - Blackrock Armor Set - ilvl 630 (Blue) - Can come from Armor Enhancement Token - http://www.wowhead.com/item=114806/blackrock-armor-set
                new MissionReward(114746, MissionRewardCategory.FollowerGear),      // FollowerGear - Goredrenched Armor Set - ilvl 645 (Purple) - Can come from Armor Enhancement Token - http://www.wowhead.com/item=114746/goredrenched-armor-set
                new MissionReward(114616, MissionRewardCategory.FollowerGear),      // FollowerGear - War Ravaged Weaponry - ilvl 615 (Green) - Can come from Weapon Enhancement Token - http://www.wowhead.com/item=114616/war-ravaged-weaponry
                new MissionReward(114081, MissionRewardCategory.FollowerGear),      // FollowerGear - Blackrock Weaponry - ilvl 630 (Blue) - Can come from Weapon Enhancement Token - http://www.wowhead.com/item=114081/blackrock-weaponry
                new MissionReward(114622, MissionRewardCategory.FollowerGear),      // FollowerGear - Goredrenched Weaponry - ilvl 645 (Purple) - Can come from Weapon Enhancement Token - http://www.wowhead.com/item=114622/goredrenched-weaponry
                // Follower "upgrade" items
                new MissionReward(120301, MissionRewardCategory.FollowerItem),      // FollowerItem - Armor Enhancement Token - Green - Can turn in to Green/Blue/Purple armor enhancement tokens - http://www.wowhead.com/item=120301/armor-enhancement-token
                new MissionReward(120302, MissionRewardCategory.FollowerItem),      // FollowerItem - Weapon Enhancement Token - Green - Can turn in to Green/Blue/Purple weapon enhancement tokens - http://www.wowhead.com/item=120302/weapon-enhancement-token
                new MissionReward(114745, MissionRewardCategory.FollowerItem),      // FollowerItem - Braced Armor Enhancement - Green - Upgrade armor by 3 ilvls - http://www.wowhead.com/item=114745/braced-armor-enhancement
                new MissionReward(114808, MissionRewardCategory.FollowerItem),      // FollowerItem - Fortified Armor Enhancement - Blue - Upgrade armor by 6 ilvls - http://www.wowhead.com/item=114808/fortified-armor-enhancement
                new MissionReward(114822, MissionRewardCategory.FollowerItem),      // FollowerItem - Heavily Reinforced Armor Enhancement - Purple - Upgrade armor by 9 ilvls - http://www.wowhead.com/item=114822/heavily-reinforced-armor-enhancement
                new MissionReward(114128, MissionRewardCategory.FollowerItem),      // FollowerItem - Balanced Weapon Enhancement - Green - Upgrade weapon by 3 ilvls - http://www.wowhead.com/item=114128/balanced-weapon-enhancement
                new MissionReward(114129, MissionRewardCategory.FollowerItem),      // FollowerItem - Striking Weapon Enhancement - Blue - Upgrade weapon by 6 ilvls - http://www.wowhead.com/item=114129/striking-weapon-enhancement
                new MissionReward(114131, MissionRewardCategory.FollowerItem),      // FollowerItem - Power Overrun Weapon Enhancement - Purple - Upgrade weapon by 9 ilvls - http://www.wowhead.com/item=114131/power-overrun-weapon-enhancement
                // Follower "ability/trait" items
                new MissionReward(118354, MissionRewardCategory.FollowerItem),      // FollowerItem - Follower Re-training Certificate - Blue - Reroll the abilities and traits on a follower - http://www.wowhead.com/item=118354/follower-re-training-certificate
                new MissionReward(118475, MissionRewardCategory.FollowerItem),      // FollowerItem - Hearthstone Strategy Guide - Blue - Teaches a follower the Hearthstone Pro trait - http://www.wowhead.com/item=118475/hearthstone-strategy-guide
                new MissionReward(118474, MissionRewardCategory.FollowerItem),      // FollowerItem - Supreme Manual of Dance - Blue - Teaches a follower the Dancer trait - http://www.wowhead.com/item=118474/supreme-manual-of-dance
                new MissionReward(122272, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Follower Ability Retraining Guide - Reroll the abilities on a follower - http://ptr.wowhead.com/item=122272/follower-ability-retraining-guide
                new MissionReward(122273, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Follower Trait Retraining Guide - Reroll the traits on a follower - http://ptr.wowhead.com/item=122273/follower-trait-retraining-guide
                new MissionReward(122582, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Guide to Arakkoa Relations - Blue - Replaces follower trait with the Bird Watcher trait - http://ptr.wowhead.com/item=122582/guide-to-arakkoa-relations
                new MissionReward(122580, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Ogre Buddy Handbook - Blue - Replaces follower trait with Ogre Buddy trait - http://ptr.wowhead.com/item=122580/ogre-buddy-handbook
                new MissionReward(122584, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Winning with Wildlings - Blue - Replaces follower trait with Wildling trait - http://ptr.wowhead.com/item=122584/winning-with-wildlings
                new MissionReward(122583, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Grease Monkey Guide - Blue - Replaces follower trait with Mechano Affictionado trait - http://ptr.wowhead.com/item=122583/grease-monkey-guide
                new MissionReward(122275, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Sun-touched Feather of Rukhmar - Gives follower epic speed - http://ptr.wowhead.com/item=122275/sun-touched-feather-of-rukhmar
                // Follower experience boost
                new MissionReward(122274, MissionRewardCategory.FollowerItem),      // FollowerItem - PATCH 6.1 - Tome of Knowledge - Blue - Max stack of 20 - http://ptr.wowhead.com/item=122274/tome-of-knowledge
                // Legendary questline
                new MissionReward(115280, MissionRewardCategory.LegendaryQuestItem),// LegendaryQuestItem - Abrogator Stone - Orange - http://www.wowhead.com/item=115280/abrogator-stone
                new MissionReward(115510, MissionRewardCategory.LegendaryQuestItem),// LegendaryQuestItem - Elemental Rune - Orange - http://www.wowhead.com/item=115510/elemental-rune
                // Reputation Tokens
                new MissionReward(117492, MissionRewardCategory.ReputationToken),   // ReputationToken - Relic of Rukhmar - Blue - Arakkoa Outcasts - ON USE - http://www.wowhead.com/item=117492/relic-of-rukhmar
                new MissionReward(118100, MissionRewardCategory.ReputationToken),   // ReputationToken - Highmaul Relic - Blue - Steamwheedle Preservation Society - TRADE IN - http://www.wowhead.com/item=118100/highmaul-relic
                new MissionReward(26045, MissionRewardCategory.ReputationToken),    // ReputationToken - Halaa Battle Token - Green - http://www.wowhead.com/item=26045/halaa-battle-token
                                                                                    // http://www.wowhead.com/mission=332/mysteries-of-lok-rath rewards 100 of these & 40 Halaa Research tokens which is enough to buy
                                                                                    // http://www.wowhead.com/item=28915/reins-of-the-dark-riding-talbuk or http://www.wowhead.com/item=29228/reins-of-the-dark-war-talbuk
                new MissionReward(26044, MissionRewardCategory.ReputationToken),    // ReputationToken - Halaa Research Token - Green - http://www.wowhead.com/item=26044/halaa-research-token
                                                                                    // http://www.wowhead.com/mission=332/mysteries-of-lok-rath rewards 40 of these & 100 Halaa Battle tokens which is enough to buy
                                                                                    // http://www.wowhead.com/item=28915/reins-of-the-dark-riding-talbuk or http://www.wowhead.com/item=29228/reins-of-the-dark-war-talbuk
                // Vanity Items
                new MissionReward(118193, MissionRewardCategory.VanityItem),        // VanityItem - Mysterious Shining Lockbox - Blue - Contains 118191 (Archmage Vargoth's Spare Staff - Toy) - ON USE - http://www.wowhead.com/item=118193/mysterious-shining-lockbox
                new MissionReward(27944, MissionRewardCategory.VanityItem),         // VanityItem - Talisman of True Treasure Tracking - White - USELSS - http://www.wowhead.com/item=27944/talisman-of-true-treasure-tracking
                new MissionReward(118427, MissionRewardCategory.VanityItem),        // VanityItem - Autographed Hearthstone Card - Blue - TOY - http://www.wowhead.com/item=118427/autographed-hearthstone-card
                new MissionReward(122637, MissionRewardCategory.VanityItem),        // VanityItem - PATCH 6.1 - S.E.L.F.I.E. Camera - Blue - http://ptr.wowhead.com/item=122637/s-e-l-f-i-e-camera
                new MissionReward(122661, MissionRewardCategory.VanityItem),        // VanityItem - PATCH 6.1 - S.E.L.F.I.E. Lens Upgrade Kit - Blue - http://ptr.wowhead.com/item=122661/s-e-l-f-i-e-lens-upgrade-kit
                // Profession
                new MissionReward(120945, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Primal Spirit - Green - Profession Reagent - http://www.wowhead.com/item=120945/primal-spirit
                new MissionReward(122595, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: The Forge - http://ptr.wowhead.com/item=122595/rush-order-the-forge
                new MissionReward(122596, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: The Tannery - http://ptr.wowhead.com/item=122596/rush-order-the-tannery
                new MissionReward(122590, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Enchanter's Study - http://ptr.wowhead.com/item=122590/rush-order-enchanters-study
                new MissionReward(122591, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Engineering Works - http://ptr.wowhead.com/item=122591/rush-order-engineering-works
                new MissionReward(122594, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Tailoring Emporium - http://ptr.wowhead.com/item=122594/rush-order-tailoring-emporium
                new MissionReward(122592, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Gem Boutique - http://ptr.wowhead.com/item=122592/rush-order-gem-boutique
                new MissionReward(122593, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Scribe's Quarters - http://ptr.wowhead.com/item=122593/rush-order-scribes-quarters
                new MissionReward(122576, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Rush Order: Alchemy Lab - http://ptr.wowhead.com/item=122576/rush-order-alchemy-lab
                new MissionReward(118472, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Savage Blood - Green - Profession Reagent - http://ptr.wowhead.com/item=118472/savage-blood
                // Archaeology Keystones
                new MissionReward(108439, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Draenor Clan Orator Cane - Green - Draenor Clan Keystone - http://ptr.wowhead.com/item=108439/draenor-clan-orator-cane
                new MissionReward(109585, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Arakkoa Cipher - Green - Arakkoa Keystone - http://ptr.wowhead.com/item=109585/arakkoa-cipher
                new MissionReward(109584, MissionRewardCategory.Profession),        // Profession - PATCH 6.1 - Ogre Missive - Green - Ogre Keystone - http://ptr.wowhead.com/item=109584/ogre-missive
                // Misc Items
                new MissionReward(79249, MissionRewardCategory.MiscItem),           // MiscItem - Tome of the Clear Mind - White - For redoing talents - http://www.wowhead.com/item=79249/tome-of-the-clear-mind
                new MissionReward(118428, MissionRewardCategory.MiscItem),          // MiscItem - Legion Chili - White - Personal 100 stat food - http://www.wowhead.com/item=118428/legion-chili
                new MissionReward(6662, MissionRewardCategory.MiscItem),            // MiscItem - Elixir of Giant Growth - White - Lower level potion - http://www.wowhead.com/item=6662/elixir-of-giant-growth
                new MissionReward(115012, MissionRewardCategory.MiscItem),          // MiscItem - Shattered Stone - GREY - Junk - http://www.wowhead.com/item=115012/shattered-stone
                new MissionReward(33449, MissionRewardCategory.MiscItem),           // MiscItem - PATCH 6.1 - Crusty Flatbread - White - Health replinishing food - http://ptr.wowhead.com/item=33449/crusty-flatbread
                new MissionReward(118632, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Focus Augment Rune - Blue - Increases Intellect by 50 for 1 hour - http://ptr.wowhead.com/item=118632/focus-augment-rune
                new MissionReward(118630, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Hyper Augment Rune - Blue - Increases Agility by 50 for 1 hour - http://ptr.wowhead.com/item=118630/hyper-augment-rune
                new MissionReward(118631, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Stout Augment Rune - Blue - Increases Strength by 50 for 1 hour - http://ptr.wowhead.com/item=118631/stout-augment-rune
                new MissionReward(122481, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Talador - Blue - http://ptr.wowhead.com/item=122481/scouting-report-talador
                new MissionReward(122482, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Spires of Arak - Blue - http://ptr.wowhead.com/item=122482/scouting-report-spires-of-arak
                new MissionReward(122479, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Shadowmoon Valley - Blue - http://ptr.wowhead.com/item=122479/scouting-report-shadowmoon-valley
                new MissionReward(122483, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Nagrand - Blue - http://ptr.wowhead.com/item=122483/scouting-report-nagrand
                new MissionReward(122480, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Gorgrond - Blue - http://ptr.wowhead.com/item=122480/scouting-report-gorgrond
                new MissionReward(122478, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Scouting Report: Frostfire Ridge - Blue - http://ptr.wowhead.com/item=122478/scouting-report-frostfire-ridge
                new MissionReward(118729, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Gorgrond Treasure Map - Blue - http://ptr.wowhead.com/item=118729/gorgrond-treasure-map
                new MissionReward(118727, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Frostfire Treasure Map - Blue - http://ptr.wowhead.com/item=118727/frostfire-treasure-map
                new MissionReward(118728, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Shadowmoon Valley Treasure Map - Blue - http://ptr.wowhead.com/item=118728/shadowmoon-valley-treasure-map
                new MissionReward(118730, MissionRewardCategory.MiscItem),          // MiscItem - PATCH 6.1 - Talador Treasure Map - Blue - http://ptr.wowhead.com/item=118730/talador-treasure-map
                new MissionReward(118731, MissionRewardCategory.MiscItem)          // MiscItem - PATCH 6.1 - Spires of Arak Treasure Map - Blue - http://ptr.wowhead.com/item=118731/spires-of-arak-treasure-map
            };

        public override string ToString()
        {
            return Name;
        }
    }
}

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows;
using System.Xml.Serialization;
using GarrisonButler.API;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using JetBrains.Annotations;
using Styx.Helpers;
using GarrisonButler.Libraries.JSON;

#endregion

namespace GarrisonButler.Config
{
    [ComVisible(true)]
    [XmlRoot("GarrisonButlerSettings")]
    public class GaBSettings : INotifyPropertyChanged
    {
        private List<Pigment> _pigments;
        private List<MissionReward> _rewards;
        private int _defaultMissionSuccessChance = 100;
        private int _maxNumberOfEpicMaxLevelFollowersToUseWhenBoosting = 2;
        private bool _useEpicMaxLevelFollowersToBoostLowerFollowers = true;

        private GaBSettings()
        {
            GreensToChar = new SafeString();
        }


        #region interface

        [ComVisible(true)]
        public bool GetBooleanValue(string propertyName)
        {
            var prop = GetType().GetProperty(propertyName);
            GarrisonButler.Diagnostic("GetValue called for {0}, old value={1}", propertyName, prop.GetValue(this));
            return (bool)prop.GetValue(this);
        }
        public void UpdateBooleanValue(string propertyName, bool value)
        {
            var prop = GetType().GetProperty(propertyName);
            GarrisonButler.Diagnostic("Update called for {0}, old value={1}, new value={2}", propertyName, prop.GetValue(this), value);
            prop.SetValue(this, value);
        }
        public int GetIntValue(string propertyName)
        {
            var prop = GetType().GetProperty(propertyName);
            GarrisonButler.Diagnostic("GetValue called for {0}, old value={1}", propertyName, prop.GetValue(this));
            return (int)prop.GetValue(this);
        }
        public void UpdateIntValue(string propertyName, int value)
        {
            var prop = GetType().GetProperty(propertyName);
            GarrisonButler.Diagnostic("Update called for {0}, old value={1}, new value={2}", propertyName, prop.GetValue(this), value);
            prop.SetValue(this, value);
        }

        public void UpdateGreenToCharRecipient(string value)
        {
            GreensToChar.Value = value;
        }
        public string GetGreenToCharRecipient()
        {
            return GreensToChar.Value;
        }




        public bool IsIceVersion()
        {
            return GarrisonButler.IsIceVersion();
        }

        [ComVisible(true)]
        public string GetVersionNumber()
        {
            return GarrisonButler.Version.ToString();
        }

        public struct BuildingJs
        {
            public int id;
            public string name;
            public string displayicon;
            public bool canStartOrder;
            public int maxCanStartOrder;
            public bool canCollectOrder;
            public bool available;
        }

        [ComVisible(true)]
        //public string getBuildingsJs()
        //{
        //    return "";
        //}

        private const bool Debug = true;

        public string getBuildingsJs()
        {
            if(Debug)
            GarrisonButler.Diagnostic("Sending buildings to interface.");
            var buildingsJs = new ArrayList();

            var buildingsWithOrders = BuildingsSettings.GetEmptyIfNull()
                .Where(bs => Building.HasOrder((Buildings) bs.BuildingIds.GetEmptyIfNull().FirstOrDefault())).ToList();
            for (int i = 0; i < buildingsWithOrders.Count; i++)
            {
                buildingsJs.Add(buildingsWithOrders[i].BuildingIds.First());
            }
            var res = JSON.JsonEncode(buildingsJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json buildings: " + res);
            return res;
        }

        public string getBuildingById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var buildingJs = new ArrayList();
            if (Debug)
                GarrisonButler.Diagnostic("Init bsettings");

            var bSettings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (bSettings == default(BuildingSettings) || bSettings == null)
            {
                if (Debug)
                    GarrisonButler.Diagnostic("bsettings null");
                return "";
            }
            if (Debug)
                GarrisonButler.Diagnostic("buildingJs add name");
            buildingJs.Add(bSettings.Name);
            if (Debug)
                GarrisonButler.Diagnostic("buildingJs add collect");
            buildingJs.Add(bSettings.CanCollectOrder);
            if (Debug)
                GarrisonButler.Diagnostic("buildingJs add max");
            buildingJs.Add(bSettings.MaxCanStartOrder);
            if (Debug)
                GarrisonButler.Diagnostic("buildingJs add start");
            buildingJs.Add(bSettings.CanStartOrder);

            if (Debug)
                GarrisonButler.Diagnostic("buildingJs check available");
            ButlerCoroutines.ButlerCoroutine.RefreshBuildings(true);
            var available = false;
            var buildingsInGame = ButlerCoroutines.ButlerCoroutine._buildings;
            if (buildingsInGame != null)
            {
                available = buildingsInGame.Any(
                b => bSettings.BuildingIds != null && bSettings.BuildingIds.Contains(b.Id));
            }
            else
            {
                if (Debug)
                    GarrisonButler.Diagnostic("buildingJs add available: No buildings detected");
            }
            if (Debug)
                GarrisonButler.Diagnostic("buildingJs add available");
            buildingJs.Add(available);

            var res = JSON.JsonEncode(buildingJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json building: " + res);
            return res;
        }

        public void saveBuildingCanCollectOrder(int id, bool canCollect)
        {
            if (Debug)
                GarrisonButler.Diagnostic("Save Value for canCollect, new value={0}", canCollect);
            var bSettings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (bSettings == default(BuildingSettings))
                return;

            if (Debug)
                GarrisonButler.Diagnostic("Save Value for canCollect, old value={0}", bSettings.CanCollectOrder);
            bSettings.CanCollectOrder = canCollect;
        }
        public void saveBuildingCanStartOrder(int id, bool canStart)
        {
            if (Debug)
                GarrisonButler.Diagnostic("Save Value for canStart, new value={0}", canStart);
            var bSettings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (bSettings == default(BuildingSettings))
                return;

            if (Debug)
                GarrisonButler.Diagnostic("Save Value for CanStartOrder, old value={0}", bSettings.CanStartOrder);
            bSettings.CanStartOrder = canStart;
        }
        public void saveBuildingMaxCanStart(int id, int maxCanStart)
        {
            if (Debug)
                GarrisonButler.Diagnostic("Save Value for maxCanStart, new value={0}", maxCanStart);
            var bSettings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (bSettings == default(BuildingSettings))
                return;

            if (Debug)
                GarrisonButler.Diagnostic("Save Value for MaxCanStartOrder, old value={0}", bSettings.MaxCanStartOrder);
            bSettings.MaxCanStartOrder = maxCanStart;
        }

        public string getDailyCdJs()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending Daily CDs to interface.");
            var dailiesJs = new ArrayList();

            for (int i = 0; i < DailySettings.Count; i++)
            {
                dailiesJs.Add(DailySettings[i].ItemId);
            }
            var res = JSON.JsonEncode(dailiesJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json dailies: " + res);
            return res;
        }

        public string getDailyCdById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var dailyJs = new ArrayList();
            var dailySettings = DailySettings.FirstOrDefault(d => d.ItemId == id);
            if (dailySettings == default(DailyProfession))
                return "";

            dailyJs.Add(dailySettings.Name);
            dailyJs.Add(dailySettings.TradeskillId.ToString());
            dailyJs.Add(dailySettings.Activated);

            var res = JSON.JsonEncode(dailyJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json daily: " + res);
            return res;
        }

        public void saveDailyCd(int itemId, bool activated)
        {
            var cdSettings = DailySettings.FirstOrDefault(d => d.ItemId == itemId);
            if (cdSettings == default(DailyProfession))
            {
                if (Debug)
                    GarrisonButler.Diagnostic("Could not find settings for {0}.", itemId);
                return;
            }
            if (Debug)
                GarrisonButler.Diagnostic("Save dailyCD {0}, old={1}, new={2}", itemId, cdSettings.Activated, activated);
            cdSettings.Activated = activated;
        }





        public string getMillingJs()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending Milling items to interface.");
            var millingJs = new ArrayList();

            for (int i = 0; i < Pigments.FirstOrDefault().MilledFrom.Count; i++)
            {
                millingJs.Add(Pigments.FirstOrDefault().MilledFrom[i].ItemId);
            }
            var res = JSON.JsonEncode(millingJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json milling: " + res);
            return res;
        }

        public string getMillingById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var millingJs = new ArrayList();
            var millingSetting = Pigments.FirstOrDefault().MilledFrom.FirstOrDefault(d => d.ItemId == id);
            if (millingSetting == default(SourcePigment))
                return "";

            millingJs.Add(millingSetting.Name);
            millingJs.Add(millingSetting.Activated);

            var res = JSON.JsonEncode(millingJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json milling item: " + res);
            return res;
        }

        public void saveMillingItem(int itemId, bool activated)
        {
            var millingSettings = Pigments.FirstOrDefault().MilledFrom.FirstOrDefault(d => d.ItemId == itemId);
            if (millingSettings == default(SourcePigment))
            {
                if (Debug)
                    GarrisonButler.Diagnostic("Could not find settings for milling item {0}.", itemId);
                return;
            }
            if (Debug)
                GarrisonButler.Diagnostic("Save milling item {0}, old={1}, new={2}", itemId, millingSettings.Activated, activated);
            millingSettings.Activated = activated;
        }



        public string getTPJs()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending TP items to interface.");
            var tpJs = new ArrayList();

            for (int i = 0; i < TradingPostReagentsSettings.Count; i++)
            {
                tpJs.Add(TradingPostReagentsSettings[i].ItemId);
            }
            var res = JSON.JsonEncode(tpJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json tp: " + res);
            return res;
        }

        public string getTPById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var tpJs = new ArrayList();
            var tpSettings = TradingPostReagentsSettings.FirstOrDefault(d => d.ItemId == id);
            if (tpSettings == default(BItem))
                return "";

            tpJs.Add(tpSettings.Name);
            tpJs.Add(tpSettings.Activated);

            var res = JSON.JsonEncode(tpJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json tp item: " + res);
            return res;
        }

        public void saveTPItem(int itemId, bool activated)
        {
            var tpSettings = TradingPostReagentsSettings.FirstOrDefault(d => d.ItemId == itemId);
            if (tpSettings == default(BItem))
            {
                GarrisonButler.Diagnostic("Could not find settings for tp item {0}.", itemId);
                return;
            }
            if (Debug)
                GarrisonButler.Diagnostic("Save tp item {0}, old={1}, new={2}", itemId, tpSettings.Activated, activated);
            tpSettings.Activated = activated;
        }


        public string getMailsJs()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending mail items to interface.");
            var mailsJs = new ArrayList();

            for (int i = 0; i < MailItems.Count; i++)
            {
                mailsJs.Add(MailItems[i].ItemId);
            }
            var res = JSON.JsonEncode(mailsJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json mail computed.");
            return res;
        }


        public string getMailConditions()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending mail conditions to interface.");
            var conditionsJs = new ArrayList();
            var conditions = MailCondition.GetAllPossibleConditions();
            for (int i = 0; i < conditions.Count; i++)
            {
                conditionsJs.Add(conditions[i].ToString());
            }
            var res = JSON.JsonEncode(conditionsJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json mail conditions: " + res);
            return res;
        }

        public string getMailById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var mailJs = new ArrayList();
            var mailSettings = MailItems.FirstOrDefault(d => d.ItemId == id);
            if (mailSettings == default(MailItem))
                return "";

            mailJs.Add(mailSettings.Recipient.Value);
            mailJs.Add(mailSettings.Condition.Name);
            mailJs.Add(mailSettings.CheckValue);
            mailJs.Add(mailSettings.Comment);

            var res = JSON.JsonEncode(mailJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json mail item: " + res);
            return res;
        }

        public void deleteMailById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var mailSettings = MailItems.FirstOrDefault(d => d.ItemId == id);
            if (mailSettings == default(MailItem))
                return;

            MailItems.Remove(mailSettings);
        }

        public void saveMail(string mailJson)
        {
            if (Debug)
                GarrisonButler.Diagnostic("Save mail element: " + mailJson);
            try
            {
                ArrayList mail = JSON.JsonDecode(mailJson) as ArrayList;
                if (Debug)
                    GarrisonButler.Diagnostic("Save mail element decoded: ");
                ObjectDumper.WriteToHb(mail,3);

                var condition = MailCondition.GetAllPossibleConditions().FirstOrDefault(c => c.Name == mail[2].ToString());

                MailItem mailItem = new MailItem(
                    (uint)mail[0].ToString().ToInt32(),
                    mail[1].ToString(),
                    condition,
                    mail[3].ToString().ToInt32(),
                    (mail[4] ?? "").ToString());

                var current = MailItems.FirstOrDefault(m => m.ItemId == mailItem.ItemId);
                if (current != null)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("Updating mail element from: " + current.ToString());
                    if (Debug)
                        GarrisonButler.Diagnostic("Updating mail element to: " + mailItem.ToString());
                    current.ItemId = mailItem.ItemId;
                    current.Condition = mailItem.Condition;
                    current.Recipient = mailItem.Recipient;
                    current.CheckValue = mailItem.CheckValue;
                    current.Comment = mailItem.Comment;
                }
                else
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("Adding mail element: " + mailItem.ToString());
                    MailItems.Add(mailItem);
                }
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic(e.ToString());
            }
        }

        #region Missions


        public string getRewardsJs()
        {
            if (Debug)
                GarrisonButler.Diagnostic("Sending rewards to interface.");
            var RewardsJs = new ArrayList();

            for (int i = 0; i < MissionRewardSettings.Count; i++)
            {
                RewardsJs.Add(MissionRewardSettings[i].Id);
            }
            var res = JSON.JsonEncode(RewardsJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json rewards: " + res);
            return res;
        }

        public string getRewardsById(string idAsString)
        {
            var id = idAsString.ToInt32();
            var rewardJs = new ArrayList();
            var rewardSettings = MissionRewardSettings.FirstOrDefault(d => d.Id == id);
            if (rewardSettings == default(MissionReward))
                return "";

            rewardJs.Add(rewardSettings.Name);
            rewardJs.Add(Enum.GetName(rewardSettings.Category.GetType(), rewardSettings.Category));
            rewardJs.Add(rewardSettings.DisallowMissionsWithThisReward);
            rewardJs.Add(rewardSettings.IndividualSuccessChanceEnabled);
            rewardJs.Add(rewardSettings.RequiredSuccessChance);
            rewardJs.Add(rewardSettings.RequiredMissionLevel);
            rewardJs.Add(rewardSettings.RequiredPlayerLevel);
            rewardJs.Add(rewardSettings.IsCategoryReward);

            var res = JSON.JsonEncode(rewardJs);
            if (Debug)
                GarrisonButler.Diagnostic("Json reward: " + res);
            return res;
        }

        public void updateRewardById(string rewardJson)
        {
            if (Debug)
                GarrisonButler.Diagnostic("Save reward element: " + rewardJson);
            try
            {
                ArrayList reward = JSON.JsonDecode(rewardJson) as ArrayList;
                if (Debug)
                    GarrisonButler.Diagnostic("Save reward element decoded: ");
                ObjectDumper.WriteToHb(reward, 3);
                if (reward == null)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("updating reward failed, null");
                    return;
                }
                if (reward.Count < 6)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("updating reward failed, missing elements. count:" + reward.Count);
                    return;
                }
                if (reward[5] == null)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("updating reward failed, rewardId null");
                    return;
                }

                var rewardId =  reward[5].ToString().ToInt32();
                var current = MissionRewardSettings.FirstOrDefault(m => m.Id == rewardId);
                if (current != null)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("Updating reward element: " + current.ToString());
                    current.DisallowMissionsWithThisReward = (reward[0] != null) && reward[0].ToString().ToBoolean();
                    current.IndividualSuccessChanceEnabled = (reward[1] != null) && reward[1].ToString().ToBoolean();
                    current.RequiredSuccessChance = (reward[2] != null) ? reward[2].ToString().ToInt32() : 0;
                    current.RequiredMissionLevel = (reward[3] != null) ? reward[3].ToString().ToInt32() : 0;
                    current.RequiredPlayerLevel = (reward[4] != null) ? reward[4].ToString().ToInt32() : 0;
                    current.RequiredPlayerLevel = (reward[4] != null) ? reward[4].ToString().ToInt32() : 0;
                }
                else
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("updating reward failed, id: " + rewardId);
                }
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic(e.ToString());
            }
        }

        public void updateRewardsOrder(string rewardsJson)
        {

            ArrayList rewardsOrderById = JSON.JsonDecode(rewardsJson) as ArrayList;
            if (Debug)
                GarrisonButler.Diagnostic("Update rewards order decoded: ");
            ObjectDumper.WriteToHb(rewardsOrderById, 3);
            if (rewardsOrderById == null)
            {
                if (Debug)
                    GarrisonButler.Diagnostic("Update rewards order failed, id: " + " null");
                return;
            }

            var newMissionRewardsList = new List<MissionReward>();

            foreach (var rewardsIdObject in rewardsOrderById)
            {
                var rewardId = rewardsIdObject.ToString().ToInt32();

                var reward = MissionRewardSettings.FirstOrDefault(r => r.Id == rewardId);
                if (reward == null)
                {
                    if (Debug)
                        GarrisonButler.Diagnostic("Faile to update order for rewards with id: " + rewardId + ", reward null");
                    continue;
                }

                newMissionRewardsList.Add(reward);
            }

            MissionRewardSettings = newMissionRewardsList;
        }

        #endregion



        public void diagnosticJs(string message)
        {
                GarrisonButler.Diagnostic("[UI] " + message);
        }

            #endregion



        [XmlIgnore]
        public static GaBSettings CurrentSettings { get; set; }

        [XmlArrayItem("Building", typeof (BuildingSettings))]
        [XmlArray("BuildingsSettings")]
        public List<BuildingSettings> BuildingsSettings { get; set; }

        [XmlArrayItem("Mail", typeof (MailItem))]
        [XmlArray("MailItems")]
        public List<MailItem> MailItems { get; set; }

        [XmlArrayItem("Daily", typeof (DailyProfession))]
        [XmlArray("DailySettings")]
        public List<DailyProfession> DailySettings { get; set; }

        [XmlArrayItem("Pigment", typeof (Pigment))]
        [XmlArray("PigmentsSettings")]
        public List<Pigment> Pigments
        {
            get { return _pigments; }
            set
            {
                if (Equals(value, _pigments)) return;
                _pigments = value;
                OnPropertyChanged();
            }
        }
        public bool UseGarrisonHearthstone { get; set; }
        public bool RetrieveMail { get; set; }
        public bool SendMail { get; set; }
        public bool SendDisenchantableGreens { get; set; }
        [XmlElement("RecipientGreenDisenchant")]
        public SafeString GreensToChar { get; set; }
        public bool ForceJunkSell { get; set; }
        public bool GarrisonCache { get; set; }
        public bool HarvestGarden { get; set; }
        public bool HarvestMine { get; set; }
        public bool UseCoffee { get; set; }
        public bool UseMiningPick { get; set; }
        public bool DeleteCoffee { get; set; }
        public bool DeleteMiningPick { get; set; }
        public bool ActivateBuildings { get; set; }
        public bool SalvageCrates { get; set; }
        public int TimeMinBetweenRun { get; set; }
        public bool HbRelogMode { get; set; }
        public bool DisableLastRoundCheck { get; set; }


        #region tradingPost

        [XmlArrayItem("Item", typeof(BItem))]
        [XmlArray("TradePostReagents")]
        public List<BItem> TradingPostReagentsSettings { get; set; }

        public DateTime LastCheckTradingPost { get; set; }
        public uint ItemIdTradingPost { get; set; }
        public int NumberReagentTradingPost { get; set; }

        #endregion


        #region missions
        public bool StartMissions { get; set; }
        public bool CompletedMissions { get; set; }
        public bool AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers { get; set; }
        public int DefaultMissionSuccessChance
        {
            get { return _defaultMissionSuccessChance; }
            set { _defaultMissionSuccessChance = value; }
        }

        public bool UseEpicMaxLevelFollowersToBoostLowerFollowers
        {
            get { return _useEpicMaxLevelFollowersToBoostLowerFollowers; }
            set { _useEpicMaxLevelFollowersToBoostLowerFollowers = value; }
        }

        public int MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting
        {
            get { return _maxNumberOfEpicMaxLevelFollowersToUseWhenBoosting; }
            set { _maxNumberOfEpicMaxLevelFollowersToUseWhenBoosting = value; }
        }
        public int MinimumMissionLevel { get; set; }
        public int MinimumGarrisonResourcesToStartMissions { get; set; }
        public bool PreferFollowersWithScavengerForGarrisonResourcesReward { get; set; }
        public bool PreferFollowersWithTreasureHunterForGoldReward { get; set; }
        public bool DisallowScavengerOnNonGarrisonResourcesMissions { get; set; }
        public bool DisallowTreasureHunterOnNonGoldMissions { get; set; }
        public bool DisallowRushOrderRewardIfBuildingDoesntExist { get; set; }

        [XmlArrayItem("Reward", typeof(MissionReward))]
        [XmlArray("MissionRewardSettings")]
        public List<MissionReward> MissionRewardSettings
        {
            get { return _rewards; }
            set
            {
                if (Equals(value, _rewards)) return;
                _rewards = value;
                OnPropertyChanged();
            }
        }

        #endregion


        [XmlElement("Version")]
        public ModuleVersion ConfigVersion { get; set; }

        private static GaBSettings DefaultConfig()
        {
            var ret = new GaBSettings
            {
                ConfigVersion = new ModuleVersion(),
                TimeMinBetweenRun = 60,
                MailItems = new List<MailItem>(),
                BuildingsSettings = new List<BuildingSettings>()
            };
            // Buildings generation, Ugly... but dynamic
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var building in (Buildings[]) Enum.GetValues((typeof (Buildings))))
            {
                var nameCurrent = BuildingSettings.NameFromBuildingId((int) building);
                if (ret.BuildingsSettings.Any(b => b.Name == nameCurrent)) continue;
                var ids =
                    ((Buildings[]) Enum.GetValues((typeof (Buildings)))).Where(
                        b => BuildingSettings.NameFromBuildingId((int) b) == nameCurrent)
                        .Select(x => (int) x)
                        .ToList();
                ret.BuildingsSettings.Add(new BuildingSettings(ids.ToList()));
            }

            // General settings
            // No need, already set by default

            // Profession
            ret.DailySettings = DailyProfession.AllDailies;

            // Trading post / mission rewards
            ret.PopulateMissingSettings();
            
            // Pigments for milling
            ret.Pigments = Pigment.AllPigments;
            return ret;
        }

        private void PopulateMissingSettings()
        {
            
            // populate list for trade post
            if(TradingPostReagentsSettings == null)
                TradingPostReagentsSettings = new List<BItem>();

            foreach (TradingPostReagents tradePostReagent in (TradingPostReagents[])Enum.GetValues(typeof(TradingPostReagents)))
            {
                if (TradingPostReagentsSettings.All(t => t.ItemId != (uint) tradePostReagent))
                {
                    TradingPostReagentsSettings.Add(new BItem((uint)tradePostReagent, EnumHelper.GetDescription(tradePostReagent)));
                }
            }
            // If not filled
            if (_pigments == null)
                _pigments = new List<Pigment>();
            
            var newPigmentsValues = Pigment.AllPigments.Where(p => _pigments.All(pig => pig.Id != p.Id)).ToArray();
            if (newPigmentsValues.Any())
            {
                GarrisonButler.Diagnostic("Updating pigments settings with values:");
                ObjectDumper.WriteToHb(newPigmentsValues, 3);
                _pigments.AddRange(newPigmentsValues);
            }

            if (_rewards == null)
                _rewards = new List<MissionReward>();

            var newRewardValues = MissionReward.AllRewards.Where(r => _rewards.All(rew => rew.Id != r.Id)).ToArray();
            if (newRewardValues.Any())
            {
                GarrisonButler.Diagnostic("Updating reward settings with values:");
                //ObjectDumper.WriteToHb(newRewardValues, 1);
                _rewards.AddRange(newRewardValues);
            }
        }

        private GaBSettings(OldSettings.GaBSettings oldSettings)
        {
            ConfigVersion = GarrisonButler.Version;
            ActivateBuildings = oldSettings.ActivateBuildings;
            BuildingsSettings = oldSettings.BuildingsSettings.Select(b => b.FromOld()).ToList();
            CompletedMissions = oldSettings.CompletedMissions;
            ConfigVersion = oldSettings.ConfigVersion.FromOld();
            DailySettings = oldSettings.DailySettings.Select(d => d.FromOld()).ToList();
            DeleteCoffee = oldSettings.DeleteCoffee;
            DeleteMiningPick = oldSettings.DeleteMiningPick;
            ForceJunkSell = oldSettings.ForceJunkSell;
            GarrisonCache = oldSettings.GarrisonCache;
            HbRelogMode = oldSettings.HBRelogMode;
            HarvestGarden = oldSettings.HarvestGarden;
            HarvestMine = oldSettings.HarvestMine;
            MailItems = oldSettings.MailItems.Select(m => m.FromOld()).ToList();
            RetrieveMail = oldSettings.RetrieveMail;
            SalvageCrates = oldSettings.SalvageCrates;
            SendMail = oldSettings.SendMail;
            StartMissions = oldSettings.StartMissions;
            TimeMinBetweenRun = oldSettings.TimeMinBetweenRun;
            UseCoffee = oldSettings.UseCoffee;
            UseGarrisonHearthstone = oldSettings.UseGarrisonHearthstone;
            UseMiningPick = oldSettings.UseMiningPick;
        }

        public static GaBSettings Get()
        {
            if (CurrentSettings != null) return CurrentSettings;
            GarrisonButler.Diagnostic("No settings loaded, creating default configuration file.");
            Load();
            return CurrentSettings;
        }

        public BuildingSettings GetBuildingSettings(int id)
        {
            var settings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (settings == null)
            {
                GarrisonButler.Diagnostic("Building with id: {0} not found in config.", id);
                //throw new Exception();
            }
            return settings;
        }

        /// <summary>
        /// This function will update the user's settings when we add new features to structures and what not
        /// //TODO Still needs to check all settings, only doing buildings right now
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static bool UpdateSettings(GaBSettings settings)
        {
            if (settings == null)
                return false;

            if (settings.BuildingsSettings == null)
                return false;

            // Make sure no new buildings were added
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var building in (Buildings[]) Enum.GetValues((typeof (Buildings))))
            {
                var nameCurrent = BuildingSettings.NameFromBuildingId((int) building);

                // If the current building settings doesn't contain any records with the
                // building name from the Enum, we need to add it
                if (settings.BuildingsSettings.Any(b => b.Name == nameCurrent)) continue;
                // Find the list of IDs from this building to add
                var ds =
                    ((Buildings[]) Enum.GetValues((typeof (Buildings)))).Where(
                        b => BuildingSettings.NameFromBuildingId((int) b) == nameCurrent)
                        .Select(x => (int) x)
                        .ToList();
                settings.BuildingsSettings.Add(new BuildingSettings(ds.ToList()));
            }

            return true;
        }


        public static void Save()
        {
            Get().ConfigVersion = GarrisonButler.Version;

            try
            {
                var writer =
                    new XmlSerializer(typeof (GaBSettings));

                string charSettingsDirectory = String.Empty;

                try
                {
                    charSettingsDirectory = Settings.CharacterSettingsDirectory;
                }
                catch(Exception e)
                {
                    if (e is Buddy.Coroutines.CoroutineStoppedException)
                        throw;

                    GarrisonButler.Warning("Error saving settings because Honorbuddy failed to return CharacterSettingsDirectory - e:" + e.GetType());
                }

                var file =
                    new StreamWriter(Path.Combine(charSettingsDirectory, "GarrisonButlerSettings.xml"),
                        false);
                writer.Serialize(file, CurrentSettings);
                file.Close();
                GarrisonButler.Log("Settings saved.");
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Diagnostic("Failed to save configuration.");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
            }
        }

        private static OldSettings.GaBSettings LoadOld()
        {
            try
            {
                // try to load as old settings
                var oldSettings = OldSettings.GaBSettings.LoadOnly();
                if (oldSettings == null)
                {
                    GarrisonButler.Diagnostic("Couldn't load as old version");
                }
                else
                {
                    if (oldSettings.ConfigVersion == null)
                        GarrisonButler.Diagnostic("Couldn't load old version number.");

                    var oldVersion = oldSettings.ConfigVersion;
                    if (oldVersion == null || oldVersion.FromOld() >= new ModuleVersion(1, 4, 0, 0)) return null;
                    GarrisonButler.Log("Old settings version detected: {0}", oldVersion);

                    return oldSettings;
                }
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Diagnostic("Couldn't see the settings as an old version.");
            }
            return null;
        }

        private static GaBSettings UpgradeIfPossible()
        {
            var oldSettings = LoadOld();
            return oldSettings == null ? null : new GaBSettings(oldSettings);
        }

        public static void Load()
        {
            GaBSettings upgraded = null;
            GarrisonButler.Log("Loading configuration...");
            try
            {
                //Trying to load and upgrade as old one first.
                upgraded = UpgradeIfPossible();
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Diagnostic("Failed to upgrade configuration, will load as current format.");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
            }
        
            try
            {
                if (upgraded != null)
                {
                    GarrisonButler.Log("Successfully upgraded settings to version {0}.", GarrisonButler.Version);
                    CurrentSettings = upgraded;
                    UpdateSettings(CurrentSettings);
                    Save();
                }
                else
                {
                    GarrisonButler.Log("Loading settings from version {0}.", GarrisonButler.Version);
                    var reader =
                        new XmlSerializer(typeof (GaBSettings));
                    var file =
                        new StreamReader(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"));
                    CurrentSettings = (GaBSettings) reader.Deserialize(file);
                    file.Close();
                    UpdateSettings(CurrentSettings);
                }
                CurrentSettings.PopulateMissingSettings();
                GaBSettings.Save();
                GarrisonButler.Log("Configuration successfully loaded.");
                //ObjectDumper.WriteToHb(CurrentSettings, 5);
            }
            catch (Exception e)
            {
                if (e is Buddy.Coroutines.CoroutineStoppedException)
                    throw;

                GarrisonButler.Warning("Failed to load configuration, creating default configuration. Please configure GarrisonButler!");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
                CurrentSettings = DefaultConfig();
            }
        }

        private static void PrintConfig()
        {
            ObjectDumper.WriteToHb(CurrentSettings, 3); //Deactivated because logs toon char in recipient for now
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
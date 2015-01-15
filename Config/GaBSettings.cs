#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GarrisonButler.Libraries;
using GarrisonButler.Objects;
using Styx.Helpers;

#endregion

namespace GarrisonButler.Config
{
    [XmlRoot("GarrisonButlerSettings")]
    public class GaBSettings
    {
        private GaBSettings()
        {
        }

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

        public bool UseGarrisonHearthstone { get; set; }
        public bool RetrieveMail { get; set; }
        public bool SendMail { get; set; }
        public bool SendDisenchantableGreens { get; set; }
        public string GreensToChar { get; set; }
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
        public bool StartMissions { get; set; }
        public bool CompletedMissions { get; set; }
        public int TimeMinBetweenRun { get; set; }
        public bool HbRelogMode { get; set; }

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
            return ret;
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
                    charSettingsDirectory = Settings.CharacterSettingsDirectory
                }
                catch(Exception e)
                {
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
            catch (Exception)
            {
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
                GarrisonButler.Log("Configuration successfully loaded.");
            }
            catch (Exception e)
            {
                GarrisonButler.Warning("Failed to load configuration, creating default configuration. Please configure GarrisonButler!");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
                CurrentSettings = DefaultConfig();
            }
        }

        private static void PrintConfig()
        {
            ObjectDumper.WriteToHb(CurrentSettings, 3); //Deactivated because logs toon char in recipient for now
        }
    }
}
#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        public static GaBSettings currentSettings { get; set; }

        [XmlArrayItem("Building", typeof(BuildingSettings))]
        [XmlArray("BuildingsSettings")]
        public List<BuildingSettings> BuildingsSettings { get; set; }

        [XmlArrayItem("Mail", typeof(MailItem))]
        [XmlArray("MailItems")]
        public List<MailItem> MailItems { get; set; }

        [XmlArrayItem("Daily", typeof(DailyProfession))]
        [XmlArray("DailySettings")]
        public List<DailyProfession> DailySettings { get; set; }

        public bool UseGarrisonHearthstone { get; set; }
        public bool RetrieveMail { get; set; }
        public bool SendMail { get; set; }
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
        [XmlElement("Version")]
        public ModuleVersion ConfigVersion { get; set; }
        public bool HBRelogMode { get; set; }

        private static GaBSettings DefaultConfig()
        {
            var ret = new GaBSettings();
            ret.ConfigVersion = new ModuleVersion();
            ret.TimeMinBetweenRun = 60;
            ret.MailItems = new List<MailItem>();
            // Buildings generation, Ugly... but dynamic
            ret.BuildingsSettings = new List<BuildingSettings>();
            foreach (buildings building in (buildings[]) Enum.GetValues((typeof (buildings))))
            {
                string nameCurrent = BuildingSettings.nameFromBuildingID((int) building);
                if (ret.BuildingsSettings.All(b => b.Name != nameCurrent))
                {
                    List<int> IDs =
                        ((buildings[]) Enum.GetValues((typeof (buildings)))).Where(
                            b => BuildingSettings.nameFromBuildingID((int) b) == nameCurrent)
                            .Select(x => (int) x)
                            .ToList();
                    ret.BuildingsSettings.Add(new BuildingSettings(IDs.ToList()));
                }
            }

            // General settings
            // No need, already set by default

            // Profession
            ret.DailySettings = DailyProfession.AllDailies;
            return ret;
        }

        public static GaBSettings Get()
        {
            if (currentSettings == null)
            {
                GarrisonButler.Diagnostic("No settings loaded, creating default configuration file.");
                Load();
            }
            return currentSettings;
        }

        public BuildingSettings GetBuildingSettings(int id)
        {
            BuildingSettings settings = BuildingsSettings.FirstOrDefault(b => b.BuildingIds.Contains(id));
            if (settings == null)
            {
                GarrisonButler.Warning("Building with id: {0} not found in config.", id);
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
            foreach (buildings building in (buildings[])Enum.GetValues((typeof(buildings))))
            {
                string nameCurrent = BuildingSettings.nameFromBuildingID((int)building);

                // If the current building settings doesn't contain any records with the
                // building name from the Enum, we need to add it
                if(!settings.BuildingsSettings.Any(b => b.Name == nameCurrent))
                {
                    // Find the list of IDs from this building to add
                    List<int> IDs =
                        ((buildings[])Enum.GetValues((typeof(buildings)))).Where(
                            b => BuildingSettings.nameFromBuildingID((int)b) == nameCurrent)
                            .Select(x => (int)x)
                            .ToList();
                    settings.BuildingsSettings.Add(new BuildingSettings(IDs.ToList()));
                }
            }

            return true;
        }


        public static void Save()
        {
            Get().ConfigVersion = GarrisonButler.Version;

            try
            {
                var writer =
                    new XmlSerializer(typeof(GaBSettings));
                var file =
                    new StreamWriter(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"), false);
                writer.Serialize(file, currentSettings);
                file.Close();
                GarrisonButler.Log("Settings saved.");
            }
            catch(Exception e)
            {
                GarrisonButler.Diagnostic("Failed to save configuration.");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
            }
        }

        public static void Load()
        {
            try
            {
                GarrisonButler.Diagnostic("Loading configuration");
                var reader =
                    new XmlSerializer(typeof (GaBSettings));
                var file =
                    new StreamReader(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"));
                currentSettings = (GaBSettings) reader.Deserialize(file);
                GarrisonButler.Diagnostic("Configuration successfully loaded.");
                UpdateSettings(currentSettings);
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic("Failed to load configuration, creating default configuration.");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
                currentSettings = DefaultConfig();
                Save();
            }
        }

        private static void PrintConfig()
        {
            ObjectDumper.WriteToHB(currentSettings, 3); //Deactivated because logs toon char in recipient for now
        }
    }
}
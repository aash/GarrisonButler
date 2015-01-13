#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GarrisonButler.Libraries;
using Styx.Helpers;

#endregion

namespace GarrisonButler.Config.OldSettings
{
    public class GaBSettings
    {
        private GaBSettings()
        {
        }

        [XmlIgnore]
        public static GaBSettings CurrentSettings { get; set; }

        [XmlArray]
        public List<BuildingSettings> BuildingsSettings { get; set; }

        public List<MailItem> MailItems { get; set; }

        [XmlArray]
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

        public ModuleVersion ConfigVersion { get; set; }
        public bool HbRelogMode { get; set; }

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
            foreach (var building in ((Buildings[]) Enum.GetValues((typeof (Buildings)))))
            {
                var nameCurrent = BuildingSettings.NameFromBuildingId((int) building);
                if (ret.BuildingsSettings.Any(b => b.Name == nameCurrent)) continue;
                var ds =
                    ((Buildings[]) Enum.GetValues((typeof (Buildings)))).Where(
                        b => BuildingSettings.NameFromBuildingId((int) b) == nameCurrent).Select(x => (int) x).ToList();
                ret.BuildingsSettings.Add(new BuildingSettings(ds.ToList()));
            }

            // General settings
            // No need, already set by default

            // Profession
            ret.DailySettings = DailyProfession.AllDailies;
            return ret;
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
            if (settings != default(BuildingSettings)) return settings;
            GarrisonButler.Warning("Building with id: {0} not found in config.", id);
            throw new Exception();
        }


        public static void Save()
        {
            Get().ConfigVersion = GarrisonButler.Version;

            try
            {
                var writer =
                    new XmlSerializer(typeof (GaBSettings));
                var file =
                    new StreamWriter(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"),
                        false);
                writer.Serialize(file, CurrentSettings);
                file.Close();
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic("Failed to save configuration");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
            }
        }

        public static GaBSettings LoadOnly()
        {
            try
            {
                GarrisonButler.Diagnostic("Loading configuration");
                var reader =
                    new XmlSerializer(typeof (GaBSettings));
                var file =
                    new StreamReader(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"));
                CurrentSettings = (GaBSettings) reader.Deserialize(file);
                file.Close();
                GarrisonButler.Diagnostic("Configuration successfully loaded.");
            }
            catch (Exception)
            {
                return null;
            }
            return CurrentSettings;
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
                CurrentSettings = (GaBSettings) reader.Deserialize(file);
                GarrisonButler.Diagnostic("Configuration successfully loaded.");
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic("Failed to load configuration, creating default configuration.");
                GarrisonButler.Diagnostic("Exception: " + e.GetType());
                // TO DELETE
                GarrisonButler.Diagnostic(e.ToString());
                // TO DELETE end
                CurrentSettings = DefaultConfig();
            }
            //ObjectDumper.Write(currentSettings);
            ObjectDumper.WriteToHb(CurrentSettings, 3);
        }
    }
}
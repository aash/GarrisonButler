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
    public class GaBSettings
    {
        private GaBSettings()
        {
        }

        [XmlIgnore]
        public static GaBSettings currentSettings { get; set; }

        public List<BuildingSettings> BuildingsSettings { get; set; }

        public List<DailyProfession> DailySettings { get; set; }

        public bool UseGarrisonHearthstone { get; set; }

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
        public bool HBRelogMode { get; set; }

        private static GaBSettings DefaultConfig()
        {
            var ret = new GaBSettings();
            ret.ConfigVersion = new ModuleVersion();
            ret.TimeMinBetweenRun = 60;
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
            if (settings == default(BuildingSettings))
            {
                GarrisonButler.Warning("Building with id: {0} not found in config.", id);
                throw new Exception();
            }
            return settings;
        }


        public static void Save()
        {
            Get().ConfigVersion = GarrisonButler.Version;

            var writer =
                new XmlSerializer(typeof (GaBSettings));
            var file =
                new StreamWriter(Path.Combine(Settings.CharacterSettingsDirectory, "GarrisonButlerSettings.xml"), false);
            writer.Serialize(file, currentSettings);
            file.Close();
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
            }
            catch (Exception e)
            {
                GarrisonButler.Diagnostic("Failed to load configuration, creating default configuration.");
                currentSettings = DefaultConfig();
            }
            //ObjectDumper.Write(currentSettings);
            ObjectDumper.WriteToHB(currentSettings, 3);
        }
    }
}
using System.ComponentModel;
using System.IO;
using Styx.Helpers;

namespace GarrisonBuddy.Config
{
    public class GaBSettings : Settings
    {
        static GaBSettings()
        {
            Mono = new GaBSettings(Path.Combine(CharacterSettingsDirectory, "GarrisonBuddySettings.xml"));
        }

        private GaBSettings(string path)
            : base(path)
        {
            Mono = this;
            Load();
        }

        #region Dailies

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Alchemy { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Blacksmithing { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Enchanting { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Engineering { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Inscription { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Jewelcrafting { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Leatherworking { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot do your dailies select true."),
         Category("Profession")]
        public bool Tailoring { get; set; }

        #endregion

        public static GaBSettings Mono { get; private set; }

        [Setting, Styx.Helpers.DefaultValue(false),
         Description("To let the bot use the garrison hearthstone if not already in garrison select true."),
         Category("General")]
        public bool UseGarrisonHearthstone { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot collect the garrison cache select true."), Category("General"),]
        public bool GarrisonCache { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot harvest the garden if available select true."), Category("General")]
        public bool HarvestGarden { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot harvest the mine if available select true."), Category("General")]
        public bool HarvestMine { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot activate newly created or upgraded buildings."), Category("General")]
        public bool ActivateBuildings { get; set; }



        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot salvage crates from missions."), Category("General")]
        public bool SalvageCrates { get; set; }

        [Setting, Styx.Helpers.DefaultValue(60),
         Description("The time minimum in minutes between two run at the garrison. Activate hearthstone if using as mixed mode."), Category("General")]
        public int TimeMinBetweenRun { get; set; }





        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot start missions select true. To not do missions, select false."),
         Category("Mission")]
        public bool StartMissions { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot collect completed missions select true."), Category("Mission")]
        public bool CompletedMissions { get; set; }





        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot pick up the completed work orders if available select true."),
         Category("Work Orders")]
        public bool CollectingShipments { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("[EXPERIMENTAL] Might need for you to post the ID of the PNJ on the firum. To let the bot start work orders if available select true."),
         Category("Work Orders")]
        public bool StartOrder { get; set; }
    }
}
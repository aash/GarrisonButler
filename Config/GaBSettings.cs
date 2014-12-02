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

        public static GaBSettings Mono { get; private set; }

        [Setting, Styx.Helpers.DefaultValue(false),
         Description("To let the bot use the garrison hearthstone if not already in garrison select true."),
         Category("General")]
        public bool UseGarrisonHearthstone { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot collect the garrison cache select true."), Category("General")]
        public bool GarrisonCache { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot start missions select true. To not do missions, select false."),
         Category("Mission")]
        public bool DoMissions { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot collect completed missions select true."), Category("Mission")]
        public bool CompletedMissions { get; set; }


        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot harvest the garden if available select true."), Category("Buildings")]
        public bool HarvestGarden { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot pick up the completed work orders from the garden if available select true."),
         Category("Buildings")]
        public bool ShipmentsGarden { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot harvest the mine if available select true."), Category("Buildings")]
        public bool HarvestMine { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot pick up the completed work orders from the mine if available select true."),
         Category("Buildings")]
        public bool ShipmentsMine { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
         Description("To let the bot activate newly created or upgraded buildings."), Category("Buildings")]
        public bool ActivateBuildings { get; set; }
    }
}
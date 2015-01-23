using System.ComponentModel;
using GarrisonButler.API;
using Styx.CommonBot.Frames;
using Styx.WoWInternals;

namespace GarrisonButler.LuaObjects
{
    public class CapacitiveDisplayFrame
    {
        private static CapacitiveDisplayFrame _instance;

        public static CapacitiveDisplayFrame Instance
        {
            get { return _instance; }
            set { _instance = value; }
        }

        public static void Initialize()
        {
            // Attach to open event
            GarrisonButler.Diagnostic("Attaching to SHIPMENT_CRAFTER_OPENED");
            Lua.Events.AttachEvent("SHIPMENT_CRAFTER_OPENED", Opened);

            // Attach to close event
            GarrisonButler.Diagnostic("Attaching to SHIPMENT_CRAFTER_CLOSED");
            Lua.Events.AttachEvent("SHIPMENT_CRAFTER_CLOSED", Closed);
        }

        public static void OnDeselected()
        {
            // Detach open event
            GarrisonButler.Diagnostic("Detaching from SHIPMENT_CRAFTER_OPENED");
            Lua.Events.DetachEvent("SHIPMENT_CRAFTER_OPENED", Opened);

            // Detach close event
            GarrisonButler.Diagnostic("Detaching from SHIPMENT_CRAFTER_CLOSED");
            Lua.Events.DetachEvent("SHIPMENT_CRAFTER_CLOSED", Closed);
        }
        private static void Opened(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("[CapacitiveFrame] Opened.");
            _instance = new CapacitiveDisplayFrame();
        }

        private static void Closed(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("[CapacitiveFrame] Closed.");
            _instance = null;
        }

        public static void ClickStartOrderButton()
        {
            InterfaceLua.ClickStartOrderButtonCapacitiveFrame();
        }

    }
}
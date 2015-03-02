#region

using System.Threading.Tasks;
using GarrisonButler.API;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler.LuaObjects
{
    public class CapacitiveDisplayFrame
    {
        public static CapacitiveDisplayFrame Instance { get; set; }

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
            Instance = new CapacitiveDisplayFrame();
        }

        private static void Closed(object sender, LuaEventArgs args)
        {
            GarrisonButler.Diagnostic("[CapacitiveFrame] Closed.");
            Instance = null;
        }

        public static async Task ClickStartOrderButton(Building building)
        {
            var currentStarted = building.ShipmentsTotal;
            var lua = @"
            local available = GarrisonCapacitiveDisplayFrame.available;
	        if (available and available > 0) then
		        C_Garrison.RequestShipmentCreation(available);
            else
                C_Garrison.RequestShipmentCreation();
	        end
            ";
            await ButlerLua.DoString(lua);
            if (await Buddy.Coroutines.Coroutine.Wait(10000, () => currentStarted != building.ShipmentsTotal))
            {
                GarrisonButler.Log("Successfully started a work order at {0}.", building.Name);
            }
            else
            {
                GarrisonButler.Log("Failed to start a work order at {0}.", building.Name);
            }
        }
    }
}
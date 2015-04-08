#region

using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines;
using Styx.CommonBot.Coroutines;
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

        public static async Task<bool> ClickStartOrderButton(Building building, bool all = false)
        {
            var currentStarted = building.ShipmentsTotal;
            var lua = @"C_Garrison.RequestShipmentCreation();";
            for (int tryCount = 0; tryCount < Building.StartWorkOrderMaxTries; tryCount++)
            {
                Lua.DoString(lua);
                await CommonCoroutines.SleepForRandomReactionTime();
                if (await Buddy.Coroutines.Coroutine.Wait(2000, () =>
                {
                    building.Refresh();
                    return currentStarted != building.ShipmentsTotal;
                }))
                {
                    GarrisonButler.Log("Successfully started a work order at {0}.", building.Name);
                    return true;
                }

                GarrisonButler.Warning("Failed to start a work order at {0}, try #{1}/{2}.", building.Name, tryCount,
                    Building.StartWorkOrderMaxTries);
            }
            return false;
        }

        public static async Task<bool> StartAllOrder(Building building)
        {
            var lua = @"C_Garrison.RequestShipmentCreation(GarrisonCapacitiveDisplayFrame.available);";

            Lua.DoString(lua);
            await CommonCoroutines.SleepForRandomReactionTime();

            if (await Buddy.Coroutines.Coroutine.Wait(5000, () =>
            {
                building.Refresh();
                return building.ShipmentCapacity != building.ShipmentsTotal;
            }))
            {
                return true;
            }

            GarrisonButler.Warning("Failed to start all work order at {0}.", building.Name);

            return false;
        }
    }
}
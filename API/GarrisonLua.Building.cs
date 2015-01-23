#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.Libraries;
using Styx;
using Styx.Helpers;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler.API
{
    public static class BuildingsLua
    {
        // Return the building corresponding to the id

        public static Building GetBuildingById(String buildingId)
        {
            var lua =
                "C_Garrison.RequestLandingPageShipmentInfo();" +
                "local RetInfo = {}; local Temp = {}; local buildings = C_Garrison.GetBuildings();" +
                String.Format(
                    "for i = 1, #buildings do " +
                    "local buildingID = buildings[i].buildingID;" +
                    "if (buildingID == {0}) then " +
                    "local nameShipment, texture, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, timeleftString, itemName, itemIcon, itemQuality, itemID = C_Garrison.GetLandingPageShipmentInfo(buildingID);" +
                    "local id, name, texPrefix, icon, rank, isBuilding, timeStart, buildTime, canActivate, canUpgrade, isPrebuilt = C_Garrison.GetOwnedBuildingInfoAbbrev(buildings[i].plotID);" +
                    "Temp[0] = buildings[i].buildingID;" +
                    "Temp[1] = buildings[i].plotID;" +
                    "Temp[2] = buildings[i].buildingLevel;" +
                    "Temp[3] = name;" +
                    "Temp[4] = rank;" +
                    "if (not isBuilding) then Temp[5] =  0; else Temp[5] = isBuilding;end;" +
                    "Temp[6] = timeStart;" +
                    "Temp[7] = buildTime;" +
                    "if (not canActivate) then Temp[8] =  0; else Temp[8] = canActivate;end;" +
                    "Temp[9] = canUpgrade;" +
                    "Temp[11] = isPrebuilt;" +
                    // Info on shipments
                    "Temp[12] = nameShipment;" +
                    "if (not shipmentCapacity) then Temp[13] =  0; else Temp[13] = shipmentCapacity;end;" +
                    "if (not shipmentsReady) then Temp[14] = 0; else Temp[14] = shipmentsReady;end;" +
                    "if (not shipmentsTotal) then Temp[15] =  0; else Temp[15] = shipmentsTotal;end;" +
                    "Temp[16] = creationTime;" +
                    "Temp[17] = duration;" +
                    "Temp[18] = itemName;" +
                    "Temp[19] = itemQuality;" +
                    "Temp[20] = itemID;" +
                    "end;" +
                    "end;" +
                    "for j_=0,20 do table.insert(RetInfo,tostring(Temp[j_]));end; " +
                    "return unpack(RetInfo)", buildingId);

            var building = Lua.GetReturnValues(lua);

            if (building.IsNullOrEmpty())
                return null;

            var id = building[0].ToInt32();
            var plotId = building[1].ToInt32();
            var buildingLevel = building[2];
            var name = building[3];
            var rank = building[4].ToInt32();
            var isBuilding = building[5].ToBoolean();
            var timeStart = building[6];
            var buildTime = building[7];
            var canActivate = building[8].ToBoolean();
            var canUpgrade = building[9];
            var isPrebuilt = building[11];
            var nameShipment = building[12];
            var shipmentCapacity = building[13].ToInt32();
            var shipmentsReady = building[14].ToInt32();
            var shipmentsTotal = building[15].ToInt32();
            var creationTime = building[16];
            var duration = building[17];
            var itemName = building[18];
            var itemQuality = building[19];
            var itemId = building[20];

            return new Building(StyxWoW.Me.IsAlliance, id, plotId, buildingLevel, name, rank, isBuilding,
                timeStart, buildTime, canActivate, canUpgrade, isPrebuilt, nameShipment,
                shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, itemName, itemQuality, itemId);
        }

        public static List<string> GetListBuildingsId()
        {
            const string lua = "local RetInfo = {}; local buildings = C_Garrison.GetBuildings();" +
                               "for i = 1, #buildings do " +
                               "table.insert(RetInfo,tostring(buildings[i].buildingID));" +
                               "end;" +
                               "return unpack(RetInfo)";

            var followerId = Lua.GetReturnValues(lua);
            return followerId;
        }

        public static List<Building> GetAllBuildings()
        {
            return API.ButlerLua.GetAllFromLua<Building>(GetListBuildingsId, GetBuildingById);
        }

        public static int GetTownHallLevel()
        {
            const string lua = "local level, mapTexture, townHallX, townHallY = C_Garrison.GetGarrisonInfo();" +
                               "if (not level) then return tostring(0);" +
                               "else return tostring(level); end;";

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetNumberShipmentReadyByBuildingId(int buildingId)
        {
            var lua =
                "C_Garrison.RequestLandingPageShipmentInfo();" +
                "local buildings = C_Garrison.GetBuildings();" +
                String.Format(
                    "for i = 1, #buildings do " +
                    "local buildingID = buildings[i].buildingID;" +
                    "if (buildingID == {0} ) then " +
                    "local nameShipment, texture, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, timeleftString, itemName, itemIcon, itemQuality, itemID = C_Garrison.GetLandingPageShipmentInfo(buildingID);" +
                    "if (not shipmentsReady) then " +
                    "return tostring(0); else return tostring(shipmentsReady);" +
                    "end;" +
                    "end;" +
                    "end;" +
                    "return tostring(0);", buildingId);

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetNumberShipmentLeftToStart(int buildingId)
        {
            var lua =
                "C_Garrison.RequestLandingPageShipmentInfo();" +
                "local buildings = C_Garrison.GetBuildings();" +
                String.Format(
                    "for i = 1, #buildings do " +
                    "local buildingID = buildings[i].buildingID;" +
                    "if (buildingID == {0} ) then " +
                    "local nameShipment, texture, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, timeleftString, itemName, itemIcon, itemQuality, itemID = C_Garrison.GetLandingPageShipmentInfo(buildingID);" +
                    "if (not shipmentsTotal) then " +
                    "return tostring(shipmentCapacity); else return tostring(shipmentCapacity-shipmentsTotal);" +
                    "end;" +
                    "end;" +
                    "end;" +
                    "return tostring(0);", buildingId);

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetShipmentTotal(int buildingId)
        {
            var lua =
                "C_Garrison.RequestLandingPageShipmentInfo();" +
                "local buildings = C_Garrison.GetBuildings();" +
                String.Format(
                    "for i = 1, #buildings do " +
                    "local buildingID = buildings[i].buildingID;" +
                    "if (buildingID == {0} ) then " +
                    "local nameShipment, texture, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, timeleftString, itemName, itemIcon, itemQuality, itemID = C_Garrison.GetLandingPageShipmentInfo(buildingID);" +
                    "if (not shipmentsTotal) then " +
                    "return tostring(0); else return tostring(shipmentsTotal);" +
                    "end;" +
                    "end;" +
                    "end;" +
                    "return tostring(0);", buildingId);

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetGarrisonRessources()
        {
            const string lua =
                "local name, amount, texturePath, earnedThisWeek, weeklyMax, totalMax, isDiscovered = GetCurrencyInfo(824)" +
                "return tostring(amount);";

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        // Must be using a capacitive frame!
        public static int GetCapacitiveFrameMaxShipments()
        {
            const string lua = "local amount = 99;" +
                               "for i = 1, C_Garrison.GetNumShipmentReagents() do " +
                               "local name, texture, quality, needed, quantity, itemID = C_Garrison.GetShipmentReagentInfo(i);" +
                               "if i == 1 then " +
                               "amount = quantity / needed;" +
                               "end;" +
                               "local ratio = quantity/needed;" +
                               "if ratio < amount then " +
                               "amount = ratio;" +
                               "end;" +
                               "end;" +
                               "return tostring(amount);";

            var res = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToFloat();
            GarrisonButler.Diagnostic("LUA - GetCapacitiveFrameMaxShipments: " + res);
            return (int) res;
        }
    }
}
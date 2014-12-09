using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonBuddy;
using Styx;
using Styx.Common;
using Styx.Helpers;
using Styx.WoWInternals;

namespace GarrisonLua
{
    public static class BuildingsLua
    {
        // Return the building corresponding to the id

        public static Building GetBuildingById(String buildingId)
        {
            String lua =
                "C_Garrison.RequestLandingPageShipmentInfo();" +
                "local RetInfo = {}; Temp = {}; local buildings = C_Garrison.GetBuildings();" +
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
                    "Temp[5] = isBuilding;" +
                    "Temp[6] = timeStart;" +
                    "Temp[7] = buildTime;" +
                    "Temp[8] = canActivate;" +
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
            List<String> building = Lua.GetReturnValues(lua);
            int id = building[0].ToInt32();
            String plotId = building[1];
            String buildingLevel = building[2];
            String name = building[3];
            int rank = building[4].ToInt32();
            String isBuilding = building[5];
            String timeStart = building[6];
            String buildTime = building[7];
            String canActivate = building[8];
            String canUpgrade = building[9];
            String isPrebuilt = building[11];
            String nameShipment = building[12];
            int shipmentCapacity = building[13].ToInt32();
            int shipmentsReady = building[14].ToInt32();
            int shipmentsTotal = building[15].ToInt32();
            String creationTime = building[16];
            String duration = building[17];
            String itemName = building[18];
            String itemQuality = building[19];
            String itemID = building[20];

            return new Building(StyxWoW.Me.IsAlliance, id, plotId, buildingLevel, name, rank, isBuilding,
                timeStart, buildTime, canActivate, canUpgrade, isPrebuilt, nameShipment,
                shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, itemName, itemQuality, itemID);
        }

        public static List<string> GetListBuildingsId()
        {
            String lua =
                "local RetInfo = {}; local buildings = C_Garrison.GetBuildings();" +
                "for i = 1, #buildings do " +
                "table.insert(RetInfo,tostring(buildings[i].buildingID));" +
                "end;" +
                "return unpack(RetInfo)";
            List<string> followerId = Lua.GetReturnValues(lua);
            return followerId;
        }

        public static List<Building> GetAllBuildings()
        {
            return GetListBuildingsId().Select(GetBuildingById).ToList();
        }

        public static int GetTownHallLevel()
        {
            const string lua = "local level, mapTexture, townHallX, townHallY = C_Garrison.GetGarrisonInfo();" +
                               "if (not level) then return tostring(0);" +
                               "else return tostring(level); end;";
            return Lua.GetReturnValues(lua)[0].ToInt32();
        }

        public static int GetNumberShipmentReadyByBuildingId(int buildingId)
        {
            String lua =
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
            List<String> res = Lua.GetReturnValues(lua);
            return res[0].ToInt32();
        }

        public static int GetNumberShipmentLeftToStart(int buildingId)
        {
            String lua =
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
            List<String> res = Lua.GetReturnValues(lua);
            return res[0].ToInt32();
        }

        public static int GetGarrisonRessources(int buildingId)
        {
            String lua =
                "name, amount, texturePath, earnedThisWeek, weeklyMax, totalMax, isDiscovered = GetCurrencyInfo(824)" +
                "return tostring(amount);";

            List<String> res = Lua.GetReturnValues(lua);
            return res[0].ToInt32();
        }

        // Must be using a capacitive frame!
        public static int GetCapacitiveFrameMaxShipments()
        {
            String lua =
                "local amount = 99;" +
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
            float res = Lua.GetReturnValues(lua)[0].ToFloat();
            GarrisonBuddy.GarrisonBuddy.Diagnostic("LUA - GetCapacitiveFrameMaxShipments: " + res);
            return (int)res;
        }
    }
}
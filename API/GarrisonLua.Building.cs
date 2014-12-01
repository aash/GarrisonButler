using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonBuddy;
using Styx.Helpers;
using Styx.WoWInternals;

namespace GarrisonLua
{
    public static class BuildingsLua
    {
        // Return the building corresponding to the id

        public static Building GetBuildingById(String buildingId)
        {
            GarrisonBuddy.GarrisonBuddy.Diagnostic("GetBuildingById");
            String lua =
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
                    "Temp[13] = shipmentCapacity;" +
                    "if (not shipmentsReady) then Temp[14] = 0; else Temp[14] = shipmentsReady;end;" +
                    "Temp[15] = shipmentsTotal;" +
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
            String shipmentCapacity = building[13];
            int shipmentsReady = building[14].ToInt32();
            String shipmentsTotal = building[15];
            String creationTime = building[16];
            String duration = building[17];
            String itemName = building[18];
            String itemQuality = building[19];
            String itemID = building[20];

            return new Building(id, plotId, buildingLevel, name, rank, isBuilding,
                timeStart, buildTime, canActivate, canUpgrade, isPrebuilt, nameShipment,
                shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, itemName, itemQuality, itemID);
        }

        public static List<string> GetListBuildingsId()
        {
            GarrisonBuddy.GarrisonBuddy.Diagnostic("GetListBuildingsId");
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
                    "if (buildingID == \"{0}\") then " +
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
    }
}
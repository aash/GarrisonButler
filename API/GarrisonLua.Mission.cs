using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonLua
{
    public static class MissionLua
    {
        private static readonly List<uint> CommandTables = new List<uint>
        {
            81661,
            82507,
            82601,
            81546,
            80432,
            84224,
            86062,
            86031,
            84698,
            82600,
            81649,
            85805
        };

        // Return the command table object or a default WoWObject
        public static bool RestoreCompletedMission { get; set; }

        public static WoWObject GetCommandTableOrDefault()
        {
            return ObjectManager.ObjectList.FirstOrDefault(o => CommandTables.Contains(o.Entry));
        }

        // Click on View Accepted Mission button
        public static void ViewCompletedMission()
        {
            Lua.DoString("GarrisonMissionFrameMissions.CompleteDialog.BorderFrame.ViewButton:Click()");
        }

        public static List<string> GetListMissionsId()
        {
            String lua =
                "local available_missions = {}; local RetInfo = {}; C_Garrison.GetAvailableMissions(available_missions);" +
                "for idx = 1, #available_missions do table.insert(RetInfo,tostring(available_missions[idx].missionID));end;" +
                "return unpack(RetInfo)";
            List<string> missionsId = Lua.GetReturnValues(lua);
            return missionsId;
        }

        public static List<string> GetListCompletedMissionsId()
        {
            String lua =
                "local complete_missions = C_Garrison.GetCompleteMissions(); local RetInfo = {};" +
                "for idx = 1, #complete_missions do table.insert(RetInfo,tostring(complete_missions[idx].missionID));end;" +
                "return unpack(RetInfo)";
            List<string> missionsId = Lua.GetReturnValues(lua);
            return missionsId;
        }

        public static int GetNumberCompletedMissions()
        {
            String lua = "return tostring(#(C_Garrison.GetCompleteMissions()))";
            return Lua.GetReturnValues(lua)[0].ToInt32();
        }

        public static int GetNumberAvailableMissions()
        {
            String lua = "local am = {}; C_Garrison.GetAvailableMissions(am); return tostring(#am);";
            return Lua.GetReturnValues(lua)[0].ToInt32();
        }

        public static List<Mission> GetAllCompletedMissions()
        {
            return GetListCompletedMissionsId().Select(GetCompletedMissionById).ToList();
        }

        // Return list of all available missions
        public static List<Mission> GetAllAvailableMissions()
        {
            return GetListMissionsId().Select(GetMissionById).ToList();
        }

        private static List<string> GetEnemies(String missionId)
        {
            String lua =
                String.Format(
                    "local location, xp, environment, environmentDesc, environmentTexture, locPrefix, isExhausting, enemies = C_Garrison.GetMissionInfo(\"{0}\");",
                    missionId) +
                "local RetInfo = {};" +
                "for i = 1, #enemies do " +
                "local enemy = enemies[i];" +
                "for id,mechanic in pairs(enemy.mechanics) do " +
                "table.insert(RetInfo,tostring(id));" +
                "end;" +
                "end;" +
                "return unpack(RetInfo)";
            List<string> enemies = Lua.GetReturnValues(lua);
            return enemies ?? new List<string>();
        }

        public static Mission GetMissionById(String missionId)
        {
            String lua =
                "local b = {}; local am = {}; local RetInfo = {}; local cpt = 0; C_Garrison.GetAvailableMissions(am);" +
                String.Format(
                    "local location, xp, environment, environmentDesc, environmentTexture, locPrefix, isExhausting, enemies = C_Garrison.GetMissionInfo(\"{0}\");" +
                    "for idx = 1, #am do " +
                    "if am[idx].missionID == {0} then " +
                    "b[0] = am[idx].description;" +
                    "b[1] = am[idx].cost;" +
                    "b[2] = am[idx].duration;" +
                    "b[3] = am[idx].durationSeconds;" +
                    "b[4] = am[idx].level;" +
                    "b[5] = am[idx].type;" +
                    "b[6] = am[idx].locPrefix;" +
                    "b[7] = am[idx].state;" +
                    "b[8] = am[idx].iLevel;" +
                    "b[9] = am[idx].name;" +
                    "b[10] = am[idx].location;" +
                    "b[11] = am[idx].isRare;" +
                    "b[12] = am[idx].typeAtlas;" +
                    "b[13] = am[idx].missionID;" +
                    "b[14] = am[idx].numFollowers;" +
                    "b[15] = xp;" +
                    "b[16] = am[idx].numRewards;" +
                    "b[17] = environment;" +
                    "cpt = 17;" +
                    "end;" +
                    "end;"
                    , missionId) +
                "for j_=0,cpt do table.insert(RetInfo,tostring(b[j_]));end; " +
                "return unpack(RetInfo)";
            List<string> mission = Lua.GetReturnValues(lua);

            List<string> enemies = GetEnemies(missionId);
            string description = mission[0];
            int cost = mission[1].ToInt32();
            //mission[2] = this.duration;
            int durationSeconds = mission[3].ToInt32();
            int level = mission[4].ToInt32();
            string type = mission[5];
            //mission[6] = this.locPrefix; 
            int state = mission[7].ToInt32();
            int ilevel = mission[8].ToInt32();
            string name = mission[9];
            string location = mission[10];
            bool isRare = mission[11].ToBoolean();
            //mission[12] = this.typeAtlas; 
            string missionID = mission[13];
            int numFollowers = mission[14].ToInt32();
            string xp = mission[15];
            int numRewards = mission[16].ToInt32();
            string environment = mission[17];

            return new Mission(cost, description,
                durationSeconds, enemies, level, ilevel,
                isRare, location, missionID,
                name, numFollowers, numRewards,
                state, type, xp, environment);
        }

        public static Mission GetCompletedMissionById(String missionId)
        {
            String lua =
                "local b = {}; local am = C_Garrison.GetCompleteMissions(); local RetInfo = {}; local cpt = 0;" +
                String.Format("for idx = 1, #am do " +
                              "local location, xp, environment, environmentDesc, environmentTexture, locPrefix, isExhausting, enemies = C_Garrison.GetMissionInfo(\"{0}\");" +
                              "if am[idx].missionID == {0} then " +
                              "b[0] = am[idx].description;" +
                              "b[1] = am[idx].cost;" +
                              "b[2] = am[idx].duration;" +
                              "b[3] = am[idx].durationSeconds;" +
                              "b[4] = am[idx].level;" +
                              "b[5] = am[idx].type;" +
                              "b[6] = am[idx].locPrefix;" +
                              "b[7] = am[idx].state;" +
                              "b[8] = am[idx].iLevel;" +
                              "b[9] = am[idx].name;" +
                              "b[10] = am[idx].location;" +
                              "b[11] = am[idx].isRare;" +
                              "b[12] = am[idx].typeAtlas;" +
                              "b[13] = am[idx].missionID;" +
                              "b[14] = am[idx].numFollowers;" + //"print (#pairs(am[idx].followers));" +
                              "b[15] = am[idx].numRewards;" +
                              "b[16] = xp;" +
                              "b[17] = am[idx].materialMultiplier;" +
                              "b[18] = am[idx].successChance;" +
                              "b[19] = am[idx].xpBonus;" +
                              "b[20] = am[idx].success;" +
                              "end;" +
                              "end;", missionId) +
                "for j_=0,20 do table.insert(RetInfo,tostring(b[j_]));end; " +
                "return unpack(RetInfo)";
            List<string> mission = Lua.GetReturnValues(lua);

            List<string> enemies = GetEnemies(missionId);
            string description = mission[0];
            int cost = mission[1].ToInt32();
            //mission[2] = this.duration;
            int durationSeconds = mission[3].ToInt32();
            int level = mission[4].ToInt32();
            string type = mission[5];
            //mission[6] = this.locPrefix; 
            int state = mission[7].ToInt32();
            int ilevel = mission[8].ToInt32();
            string name = mission[9];
            string location = mission[10];
            bool isRare = mission[11].ToBoolean();
            //mission[12] = this.typeAtlas; 
            string missionID = mission[13];
            int numFollowers = mission[14].ToInt32();
            int numRewards = mission[15].ToInt32();
            int xp = mission[16].ToInt32();
            int material = mission[17].ToInt32();
            string successChance = mission[18];
            int xpBonus = mission[19].ToInt32();
            bool success = mission[20].ToBoolean();

            return new Mission(cost, description,
                durationSeconds, enemies, level, ilevel,
                isRare, location, missionID,
                name, numFollowers, numRewards,
                state, type, xp, material, successChance, xpBonus, success);
        }

        //public static CompletedMission GetCompletedMissionById(String missionId)
        //{
        //    String lua =
        //        "local cm = C_Garrison.GetCompleteMissions(); local RetInfo = {}; local b = {}; local _;" +
        //        String.Format("for idx = 1, #cm do " +
        //                      "if cm[idx].missionID == {0} then " +
        //                      "_, cm[idx].xp = C_Garrison.GetMissionInfo(cm[idx].missionID);" +
        //                      "_, _, _, cm[idx].successChance, _, _, cm[idx].xpBonus, " +
        //                      "cm[idx].materialMultiplier = C_Garrison.GetPartyMissionInfo(cm[idx].missionID);" +
        //                      "b[0] = cm[idx].missionID;" +
        //                      "b[1] = cm[idx].name;" +
        //                      "b[2] = cm[idx].xp;" +
        //                      "b[3] = cm[idx].successChance;" +
        //                      "b[4] = cm[idx].xpBonus;" +
        //                      "b[5] = cm[idx].materialMultiplier;" +
        //                      "end;" +
        //                      "end;", missionId) +
        //        "for j_=0,5 do table.insert(RetInfo,tostring(b[j_]));end; " +
        //        "return unpack(RetInfo)";
        //    List<string> completedMission = Lua.GetReturnValues(lua);
        //    String id = completedMission[0];
        //    String name = completedMission[1];

        //    int Xp = completedMission[2].ToInt32();
        //    int SuccessChance = completedMission[3].ToInt32();
        //    int XpBonus = completedMission[4].ToInt32();
        //    int MaterialMultiplier = completedMission[5].ToInt32();
        //    return new CompletedMission(id, name, Xp, MaterialMultiplier, SuccessChance, XpBonus);
        //}
        public static void TurnInAllCompletedMissions()
        {
            foreach (Mission completedMission in GetAllCompletedMissions())
            {
                //Mark as complete and call for bonus rolls
                Lua.DoString(
                    "local cm = C_Garrison.GetCompleteMissions();" +
                    "for idx = 1, #cm do " +
                    " if cm[idx] and cm[idx].state and (cm[idx].state < 0) then " +
                    //" print(" + '"' + "MissionComplete, mark as complete: " + '"' + ", " +
                    "C_Garrison.MarkMissionComplete(cm[idx].missionID)" +
                    //") " +
                    " elseif cm[idx] and cm[idx].state then " +
                    //" print(" + '"' + "MissionComplete, bonus roll: " + '"' + ", " +
                    "C_Garrison.MissionBonusRoll(cm[idx].missionID)" +
                    //") " +
                    " end;" +
                    " end;");
            }
        }

        public static void TurnInCompletedMissions(List<Mission> missions)
        {
            foreach (Mission completedMission in missions)
            {
                //Mark as complete and call for bonus rolls
                Lua.DoString(
                    "local cm = C_Garrison.GetCompleteMissions();" +
                    String.Format(
                        "for idx = 1, #cm do if cm[idx].missionID == {0} then" +
                        " if cm[idx] and cm[idx].state and (cm[idx].state < 0) then " +
                        //" print(" + '"' + "MissionComplete, mark as complete: " + '"' + ", " +
                        "C_Garrison.MarkMissionComplete(cm[idx].missionID)" +
                        //") " + 
                        " elseif cm[idx] and cm[idx].state then " +
                        //" print(" + '"' + "MissionComplete, bonus roll: " + '"' + ", " +
                        "C_Garrison.MissionBonusRoll(cm[idx].missionID)" +
                        //") " +
                        //" else print(" + '"' + "MissionComplete, Cancelling" + '"' + ") " +
                        " end;" +
                        " end;end;", completedMission.MissionId));
            }
        }
    }

    public static class FollowersLua
    {
        // Return list of all available followers

        public static List<Follower> GetAllFollowers()
        {
            return GetListFollowersId().Select(GetFollowerById).ToList();
        }

        public static string GetFollowerName(string follower)
        {
            String lua = String.Format("return C_Garrison.GetFollowerName(\"{0}\");", follower);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerNameById(string followerId)
        {
            String lua = String.Format("return C_Garrison.GetFollowerNameByID(\"{0}\");", followerId);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerClassSpecName(string follower)
        {
            String lua = String.Format("return C_Garrison.GetFollowerClassSpecName(\"{0}\");", follower);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerClassSpecById(string followerId)
        {
            String lua = String.Format("return C_Garrison.GetFollowerClassSpecName(\"{0}\");", followerId);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerDisplayIdbyId(string followerId)
        {
            String lua = String.Format("return C_Garrison.GetFollowerDisplayIDByID(\"{0}\");", followerId);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerStatus(string followerId)
        {
            String lua =
                "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format("for i,v in ipairs(followers) do " +
                              "local followerID = (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID;" +
                              "if (followerID == {0}) then table.insert(RetInfo,tostring(v.status)); end;" +
                              "end;" +
                              "return unpack(RetInfo)", followerId);
            return Lua.GetReturnValues(lua).FirstOrDefault() == "nil"
                ? "None"
                : Lua.GetReturnValues(lua).FirstOrDefault();
        }

        public static string GetFollowerInfo(string followerId)
        {
            String lua = String.Format("return {0} and C_Garrison.GetFollowerInfo(\"{0}\");", followerId);
            return Lua.GetReturnValues(lua).FirstOrDefault();
        }

        // Return the follower corresponding to the id

        public static Follower GetFollowerById(String followerId)
        {
            String lua =
                "local RetInfo = {}; Temp = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format(
                    "for i,f in ipairs(followers) do " +
                    "local followerID = (f.garrFollowerID) and tonumber(f.garrFollowerID) or f.followerID;" +
                    "if (followerID == {0}) then " +
                    "Temp[0] = followerID;" +
                    "Temp[1] = f.name;" +
                    "Temp[2] = f.status;" +
                    "Temp[3] = f.ClassSpecName;" +
                    "Temp[4] = f.quality;" +
                    "Temp[5] = f.level;" +
                    "Temp[6] = f.isCollected ;" +
                    "Temp[7] = f.iLevel;" +
                    "Temp[8] = f.levelXP;" +
                    "Temp[9] = f.xp;" +
                    "end;" +
                    "end;" +
                    "for j_=0,9 do table.insert(RetInfo,tostring(Temp[j_]));end; " +
                    "return unpack(RetInfo)", followerId);
            List<String> follower = Lua.GetReturnValues(lua);
            String Name = follower[1];
            String Status = follower[2];
            String ClassSpecName = follower[3];
            String quality = follower[4];
            int level = follower[5].ToInt32();
            bool isCollected = follower[6].ToBoolean();
            int iLevel = follower[7].ToInt32();
            int xp = follower[8].ToInt32();
            int levelXp = follower[9].ToInt32();

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  "for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(counterID));" +
                  "end;" +
                  "end;" +
                  "return unpack(RetInfo)";
            List<String> abilities = Lua.GetReturnValues(lua);

            return new Follower(followerId, Name, Status, ClassSpecName, quality, level, isCollected, iLevel, xp,
                levelXp, abilities);
        }

        // Return list of all available missions

        public static List<string> GetListFollowersId()
        {
            String lua =
                "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                "for i,v in ipairs(followers) do " +
                "table.insert(RetInfo,tostring( (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID));" +
                "end;" +
                //"for idx = 1, #followers do table.insert(RetInfo,tostring(followers[idx].followerID));end;" +
                "return unpack(RetInfo)";
            List<string> followerId = Lua.GetReturnValues(lua);
            return followerId;
        }

        public static int GetNumFollowersOnMission(int missionId)
        {
            String lua = String.Format("return C_Garrison.GetNumFollowersOnMission(\"{0}\");", missionId);
            List<string> luaRet = Lua.GetReturnValues(lua);
            return Convert.ToInt32(luaRet[0]);
        }

        public static void AddFollowerToMission(int missionId, int followerId)
        {
            String lua = String.Format("C_Garrison.AddFollowerToMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }

        public static void RemoveFollowerFromMission(int missionId, int followerId)
        {
            String lua = String.Format("C_Garrison.RemoveFollowerFromMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }
    }

    public static class BuildingsLua
    {
        // Return the building corresponding to the id

        public static Building GetBuildingById(String buildingId)
        {
            GarrisonButler.GarrisonButler.Debug("GetBuildingById");
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
                    "Temp[14] = shipmentsReady;" +
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
            String id = building[0];
            String plotId = building[1];
            String buildingLevel = building[2];
            String name = building[3];
            String rank = building[4];
            String isBuilding = building[5];
            String timeStart = building[6];
            String buildTime = building[7];
            String canActivate = building[8];
            String canUpgrade = building[9];
            String isPrebuilt = building[11];
            String nameShipment = building[12];
            String shipmentCapacity = building[13];
            String shipmentsReady = building[14];
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
            GarrisonButler.GarrisonButler.Debug("GetListBuildingsId");
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
    }

    public static class InterfaceLua
    {
        public static bool IsGarrisonMissionFrameOpen()
        {
            const string lua =
                "if not GarrisonMissionFrame then return false; else return tostring(GarrisonMissionFrame:IsVisible());end;";
            string t = Lua.GetReturnValues(lua)[0];
            return t.ToBoolean();
        }

        public static bool IsGarrisonMissionTabVisible()
        {
            const string lua =
                "if not GarrisonMissionFrame or not GarrisonMissionFrame.MissionTab then return false; else return tostring(GarrisonMissionFrame.MissionTab:IsVisible()); end;";
            string t = Lua.GetReturnValues(lua)[0];
            return t.ToBoolean();
        }

        public static bool IsGarrisonMissionVisible()
        {
            const string lua =
                "if not GarrisonMissionFrame or not GarrisonMissionFrame.MissionTab or not GarrisonMissionFrame.MissionTab.MissionPage then return false;end;" +
                "return tostring(GarrisonMissionFrame.MissionTab.MissionPage:IsShown())";
            string t = Lua.GetReturnValues(lua)[0];
            return t.ToBoolean();
        }

        public static bool IsGarrisonMissionVisibleAndValid(string missionId)
        {
            string lua =
                String.Format(
                    "if not GarrisonMissionFrame.MissionTab.MissionPage or not GarrisonMissionFrame.MissionTab.MissionPage.missionInfo or not GarrisonMissionFrame.MissionTab.MissionPage:IsShown() then return false;end;" +
                    "return tostring(GarrisonMissionFrame.MissionTab.MissionPage.missionInfo.missionID == {0} )",
                    missionId);
            string t = Lua.GetReturnValues(lua)[0];
            return t.ToBoolean();
        }

        public static void ClickTabMission()
        {
            Lua.DoString("GarrisonMissionFrameTab1:Click();");
        }

        public static void OpenMission(Mission mission)
        {
            GarrisonButler.GarrisonButler.Debug("OpenMission - id: " + mission.MissionId);
            //Scroll until we see mission first
            String lua =
                "local mission; local am = {}; C_Garrison.GetAvailableMissions(am);" +
                String.Format(
                    "for idx = 1, #am do " +
                    "if am[idx].missionID == {0} then " +
                    "mission = am[idx];" +
                    "end;" +
                    "end;" +
                    "GarrisonMissionList_Update();" +
                    "GarrisonMissionFrame.MissionTab.MissionList:Hide();" +
                    "GarrisonMissionFrame.MissionTab.MissionPage:Show();" +
                    "GarrisonMissionPage_ShowMission(mission);" +
                    "GarrisonMissionFrame.followerCounters = C_Garrison.GetBuffedFollowersForMission(\"{0}\");" +
                    "GarrisonMissionFrame.followerTraits = C_Garrison.GetFollowersTraitsForMission(\"{0}\");" +
                    "GarrisonFollowerList_UpdateFollowers(GarrisonMissionFrame.FollowerList);"
                    , mission.MissionId);

            Lua.DoString(lua);
        }

        public static void ClickCloseMission()
        {
            //String lua = "GarrisonMissionFrame.MissionTab.MissionPage.CloseButton:Click();";

            String lua =
                "GarrisonMissionFrame.MissionTab.MissionPage:Hide();" +
                "GarrisonMissionFrame.MissionTab.MissionList:Show();" +
                "GarrisonMissionPage_ClearParty();" +
                "GarrisonMissionFrame.followerCounters = nil;" +
                "GarrisonMissionFrame.MissionTab.MissionPage.missionInfo = nil;";
            Lua.DoString(lua);
        }

        public static void AddFollowersToMissionOld(string missionId, List<string> followersId)
        {
            GarrisonButler.GarrisonButler.Debug("Cleaning mission Followers");
            String luaClear = String.Format(
                "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
                "for idx = 1, #MissionPageFollowers do " +
                "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
                "end;");
            Lua.DoString(luaClear);

            GarrisonButler.GarrisonButler.Debug("Adding mission Followers: " + followersId.Count);
            foreach (string t in followersId)
            {
                GarrisonButler.GarrisonButler.Debug("Adding mission Followers ID: " + t);
            }
            //    var
            //        luaAdd =
            //            "local fols = {};" +
            //            String.Format("fols[1]=\"{0}\";fols[2]=\"{1}\";fols[3]=\"{2}\";",
            //                followersId[0], followersId.ElementAtOrDefault(1),
            //                followersId.ElementAtOrDefault(2)) +
            //            "print(\"fols:\",fols[1],fols[2],fols[3]);" +
            //            "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
            //            "local am = {}; C_Garrison.GetAvailableMissions(am);" +
            //            "local missionID;" +
            //            "for idx = 1, #am do " +
            //string.Format("if am[idx].missionID == {0} then print(1000000); missionID = am[idx].missionID;" +
            //              "end;", missionId) +
            //            "end;" +
            //            "for idx = 1, #MissionPageFollowers do " +
            //                "local follower = C_Garrison.GetFollowerInfo(fols[idx]);" +
            //                "if follower then " +
            //                    "print(\"followerID:\",follower.followerID);" +

            //                    "print(\"missionID:\",missionID);" +
            //                    "C_Garrison.AddFollowerToMission(missionID, follower.followerID);" +
            //                "end;" +
            //            "end;";
            string
                luaAdd =
                    "local fols = {};" +
                    String.Format("fols[1]=\"{0}\";fols[2]=\"{1}\";fols[3]=\"{2}\";",
                        followersId[0], followersId.ElementAtOrDefault(1),
                        followersId.ElementAtOrDefault(2)) +
                    "print(\"fols:\",fols[1],fols[2],fols[3]);" +
                    "local am = {}; C_Garrison.GetAvailableMissions(am);" +
                    "local missionID;" +
                    "for idx = 1, #am do " +
                    String.Format("if am[idx].missionID == {0} then missionID = am[idx].missionID;" +
                                  "end;", missionId) +
                    "end;" +
                    "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
                    "for idx = 1, #MissionPageFollowers do " +
                    "local follower = C_Garrison.GetFollowerInfo(fols[idx]);" +
                    "local followerFrame = MissionPageFollowers[idx];" +
                    "if follower then " +
                    "print(\"followerID:\",follower.followerID);" +
                    "print(\"missionID:\",missionID);" +
                    "GarrisonMissionPage_SetFollower(followerFrame, follower);" +
                    "end;" +
                    "end;";
            //String
            //    luaAdd = String.Format("C_Garrison.AddFollowerToMission(\"{0}\",\"{1}\");", missionId, followersId[i]);
            //String
            //      luaAdd = String.Format("GarrisonMissionPage_AddFollower(\"{1}\");", missionId, followersId[i]);

            Lua.DoString(luaAdd);
        }

        public static void AddFollowersToMission(string missionId, List<string> followersId)
        {
            //GarrisonButler.Debug("Cleaning mission Followers");
            //String luaClear = String.Format(
            //    "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
            //    "for idx = 1, #MissionPageFollowers do " +
            //        "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
            //    "end;");
            //Lua.DoString(luaClear);

            //GarrisonButler.Debug("Adding mission Followers: " + followersId.Count);
            //    foreach (var t in followersId)
            //    {
            //        GarrisonButler.Debug("Adding mission Followers ID: " + t);
            //    }
            //    var
            //        luaAdd =
            //            "local fols = {};" +
            //            String.Format("fols[1]=\"{0}\";fols[2]=\"{1}\";fols[3]=\"{2}\";",
            //                followersId[0], followersId.ElementAtOrDefault(1),
            //                followersId.ElementAtOrDefault(2)) +
            //            "print(\"fols:\",fols[1],fols[2],fols[3]);" +
            //            "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
            //            "local am = {}; C_Garrison.GetAvailableMissions(am);" +
            //            "local missionID;" +
            //            "for idx = 1, #am do " +
            //string.Format("if am[idx].missionID == {0} then print(1000000); missionID = am[idx].missionID;" +
            //              "end;", missionId) +
            //            "end;" +
            //            "for idx = 1, #MissionPageFollowers do " +
            //                "local follower = C_Garrison.GetFollowerInfo(fols[idx]);" +
            //                "if follower then " +
            //                    "print(\"followerID:\",follower.followerID);" +

            //                    "print(\"missionID:\",missionID);" +
            //                    "C_Garrison.AddFollowerToMission(missionID, follower.followerID);" +
            //                "end;" +
            //            "end;";
            //    var
            //        luaAdd =
            //            "local fols = {};" +
            //            String.Format("fols[1]=\"{0}\";fols[2]=\"{1}\";fols[3]=\"{2}\";",
            //                followersId[0], followersId.ElementAtOrDefault(1),
            //                followersId.ElementAtOrDefault(2)) +
            //            "print(\"fols:\",fols[1],fols[2],fols[3]);" +
            //            "local am = {}; C_Garrison.GetAvailableMissions(am);" +
            //            "local missionID;" +
            //            "for idx = 1, #am do " +
            //string.Format("if am[idx].missionID == {0} then print(1000000); missionID = am[idx].missionID;" +
            //              "end;", missionId) +
            //            "end;" +
            //            "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
            //            "for idx = 1, #MissionPageFollowers do " +
            //                "local follower = C_Garrison.GetFollowerInfo(fols[idx]);" +
            //                "local followerFrame = MissionPageFollowers[idx];" +
            //                "if follower then " +
            //                    "print(\"followerID:\",follower.followerID);" +
            //                    "print(\"missionID:\",missionID);" +
            //                    "GarrisonMissionPage_SetFollower(followerFrame, follower);" +
            //                "end;" +
            //            "end;";
            //String
            //    luaAdd = String.Format("C_Garrison.AddFollowerToMission(\"{0}\",\"{1}\");", missionId, followersId[i]);
            //String luaAdd = String.Format("print(tonumber({0}));" +
            //                              "GarrisonMissionPage_AddFollower(tonumber({0}));", followersId.FirstOrDefault());
            foreach (string followerId in followersId)
            {
                String luaAdd = String.Format(
                    //Check if in current lsit
                    ///run print(GarrisonMissionFrameFollowersListScrollFrame.buttons[1].info.followerID);
                    "local button;" +
                    "local buttons = GarrisonMissionFrameFollowersListScrollFrame.buttons;" +
                    "local min, max = GarrisonMissionFrameFollowersListScrollFrame.scrollBar:GetMinMaxValues();" +
                    "GarrisonMissionFrameFollowersListScrollFrame.scrollBar:SetValue(min);" +
                    "for val=min,max,(max-min)/100 do " +
                    "for idx = 1, #buttons do " +
                    "local v = buttons[idx].info;" +
                    "local followerID = (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID;" +
                    "if(followerID == {0} ) then " +
                    "button = buttons[idx];" +
                    "break;" +
                    "end;" +
                    "end;" +
                    "if (not button) then GarrisonMissionFrameFollowersListScrollFrame.scrollBar:SetValue(val);" +
                    "else break; end;" +
                    "end;" +
                    "button:Click();" +
                    "button:Click('RightButton');", followerId);

                Lua.DoString(luaAdd);
                luaAdd = "DropDownList1:Click();";
                Lua.DoString(luaAdd);
                luaAdd = "DropDownList1Button1:Click();";
                Lua.DoString(luaAdd);
                //"local v = button.info;" +
                //"GarrisonFollowerOptionDropDown.followerID = (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID" +
                //"ToggleDropDownMenu(1, nil, GarrisonFollowerOptionDropDown, \"cursor\", 0, 0);"
            }
        }

        public static void ClickStartMission()
        {
            String lua = "GarrisonMissionFrame.MissionTab.MissionPage.StartMissionButton:Click();";
            Lua.DoString(lua);
        }

        public static void StartMission(string missionId)
        {
            GarrisonButler.GarrisonButler.Debug("StartMission");
            String lua = String.Format("C_Garrison.StartMission(\"{0}\");", missionId);
            Lua.DoString(lua);
        }
    }
}
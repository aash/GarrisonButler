#region

using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.API
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
            return
                ObjectManager.ObjectList.GetEmptyIfNull()
                    .FirstOrDefault(o => CommandTables.GetEmptyIfNull().Contains(o.Entry));
        }

        // Click on View Accepted Mission button
        public static void ViewCompletedMission()
        {
            Lua.DoString("GarrisonMissionFrameMissions.CompleteDialog.BorderFrame.ViewButton:Click()");
        }

        public static List<string> GetListMissionsId()
        {
            global::GarrisonButler.GarrisonButler.Diagnostic("GetListMissionsId LUA");

            String lua =
                "local available_missions = {}; local RetInfo = {}; C_Garrison.GetAvailableMissions(available_missions);" +
                "for idx = 1, #available_missions do table.insert(RetInfo,tostring(available_missions[idx].missionID));end;" +
                "return unpack(RetInfo)";

            List<string> missionsId = Lua.GetReturnValues(lua);

            global::GarrisonButler.GarrisonButler.Diagnostic("GetListMissionsId LUA");
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
            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetNumberAvailableMissions()
        {
            String lua = "local am = {}; C_Garrison.GetAvailableMissions(am); return tostring(#am);";
            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static List<Mission> GetAllCompletedMissions()
        {
            return
                GetListCompletedMissionsId()
                    .GetEmptyIfNull()
                    .Select(GetCompletedMissionById)
                    .SkipWhile(m => m == null)
                    .ToList();
        }

        // Return list of all available missions
        public static List<Mission> GetAllAvailableMissions()
        {
            return GetListMissionsId().GetEmptyIfNull().Select(GetMissionById).SkipWhile(m => m == null).ToList();
        }

        public static List<Mission> GetAllAvailableMissionsReport()
        {
            return GetListMissionsId().GetEmptyIfNull().Select(GetMissionById).SkipWhile(m => m == null).ToList();
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


        public static Mission GetMissionReportById(String missionId)
        {
            String lua =
                "local b = {}; local am = GarrisonLandingPageReport.List.AvailableItems; local RetInfo = {}; local cpt = 0;" +
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

            if (mission.IsNullOrEmpty())
                return null;

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

            if (mission.IsNullOrEmpty())
                return null;

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
                              "b[14] = am[idx].numFollowers;" +
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

            if (mission.IsNullOrEmpty())
                return null;

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


        public static void TurnInAllCompletedMissions()
        {
            foreach (Mission completedMission in GetAllCompletedMissions())
            {
                //Mark as complete and call for bonus rolls
                Lua.DoString(
                    "local cm = C_Garrison.GetCompleteMissions();" +
                    "for idx = 1, #cm do " +
                    " if cm[idx] and cm[idx].state and (cm[idx].state < 0) then " +
                    "C_Garrison.MarkMissionComplete(cm[idx].missionID)" +
                    " elseif cm[idx] and cm[idx].state then " +
                    "C_Garrison.MissionBonusRoll(cm[idx].missionID)" +
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
                        "C_Garrison.MarkMissionComplete(cm[idx].missionID)" +
                        " elseif cm[idx] and cm[idx].state then " +
                        "C_Garrison.MissionBonusRoll(cm[idx].missionID)" +
                        " end;" +
                        " end;end;", completedMission.MissionId));
            }
        }
    }
}
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
            GarrisonButler.Diagnostic("GetListMissionsId LUA");

            const string lua =
                "local available_missions = {}; local RetInfo = {}; C_Garrison.GetAvailableMissions(available_missions);" +
                "for idx = 1, #available_missions do table.insert(RetInfo,tostring(available_missions[idx].missionID));end;" +
                "return unpack(RetInfo)";

            var missionsId = Lua.GetReturnValues(lua);

            GarrisonButler.Diagnostic("GetListMissionsId LUA");
            return missionsId;
        }

        public static List<string> GetListCompletedMissionsId()
        {
            const string lua = "local complete_missions = C_Garrison.GetCompleteMissions(); local RetInfo = {};" +
                               "for idx = 1, #complete_missions do table.insert(RetInfo,tostring(complete_missions[idx].missionID));end;" +
                               "return unpack(RetInfo)";

            var missionsId = Lua.GetReturnValues(lua);
            return missionsId;
        }

        public static int GetNumberCompletedMissions()
        {
            const string lua = "return tostring(#(C_Garrison.GetCompleteMissions()))";
            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static int GetNumberAvailableMissions()
        {
            const string lua = "local am = {}; C_Garrison.GetAvailableMissions(am); return tostring(#am);";
            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        public static List<Mission> GetAllCompletedMissions()
        {
            return API.ButlerLua.GetAllFromLua<Mission>(GetListCompletedMissionsId, GetCompletedMissionById);
        }

        // Return list of all available missions
        public static List<Mission> GetAllAvailableMissions()
        {
            return API.ButlerLua.GetAllFromLua<Mission>(GetListMissionsId, GetMissionById);
        }

        private static List<string> GetEnemies(String missionId)
        {
            var lua =
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

            var enemies = Lua.GetReturnValues(lua);
            return enemies ?? new List<string>();
        }


        public static Mission GetMissionReportById(String missionIdArg)
        {
            var lua =
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
                    , missionIdArg) +
                "for j_=0,cpt do table.insert(RetInfo,tostring(b[j_]));end; " +
                "return unpack(RetInfo)";
            var mission = Lua.GetReturnValues(lua);

            if (mission.IsNullOrEmpty())
                return null;

            var enemies = GetEnemies(missionIdArg);
            var description = mission[0];
            var cost = mission[1].ToInt32();
            //mission[2] = this.duration;
            var durationSeconds = mission[3].ToInt32();
            var level = mission[4].ToInt32();
            var type = mission[5];
            //mission[6] = this.locPrefix; 
            var state = mission[7].ToInt32();
            var ilevel = mission[8].ToInt32();
            var name = mission[9];
            var location = mission[10];
            var isRare = mission[11].ToBoolean();
            //mission[12] = this.typeAtlas; 
            var missionId = mission[13];
            var numFollowers = mission[14].ToInt32();
            var xp = mission[15];
            var numRewards = mission[16].ToInt32();
            var environment = mission[17];

            return new Mission(cost, description,
                durationSeconds, enemies, level, ilevel,
                isRare, location, missionId,
                name, numFollowers, numRewards,
                state, type, xp, environment);
        }

        public static Mission GetMissionById(String missionIdArg)
        {
            var lua =
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
                    , missionIdArg) +
                "for j_=0,cpt do table.insert(RetInfo,tostring(b[j_]));end; " +
                "return unpack(RetInfo)";
            var mission = Lua.GetReturnValues(lua);

            if (mission.IsNullOrEmpty())
                return null;

            var enemies = GetEnemies(missionIdArg);
            var description = mission[0];
            var cost = mission[1].ToInt32();
            //mission[2] = this.duration;
            var durationSeconds = mission[3].ToInt32();
            var level = mission[4].ToInt32();
            var type = mission[5];
            //mission[6] = this.locPrefix; 
            var state = mission[7].ToInt32();
            var ilevel = mission[8].ToInt32();
            var name = mission[9];
            var location = mission[10];
            var isRare = mission[11].ToBoolean();
            //mission[12] = this.typeAtlas; 
            var missionId = mission[13];
            var numFollowers = mission[14].ToInt32();
            var xp = mission[15];
            var numRewards = mission[16].ToInt32();
            var environment = mission[17];

            return new Mission(cost, description,
                durationSeconds, enemies, level, ilevel,
                isRare, location, missionId,
                name, numFollowers, numRewards,
                state, type, xp, environment);
        }

        public static Mission GetCompletedMissionById(String missionIdArg)
        {
            var lua =
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
                              "end;", missionIdArg) +
                "for j_=0,20 do table.insert(RetInfo,tostring(b[j_]));end; " +
                "return unpack(RetInfo)";
            var mission = Lua.GetReturnValues(lua);

            if (mission.IsNullOrEmpty())
                return null;

            var enemies = GetEnemies(missionIdArg);
            var description = mission[0];
            var cost = mission[1].ToInt32();
            //mission[2] = this.duration;
            var durationSeconds = mission[3].ToInt32();
            var level = mission[4].ToInt32();
            var type = mission[5];
            //mission[6] = this.locPrefix; 
            var state = mission[7].ToInt32();
            var ilevel = mission[8].ToInt32();
            var name = mission[9];
            var location = mission[10];
            var isRare = mission[11].ToBoolean();
            //mission[12] = this.typeAtlas; 
            var missionId = mission[13];
            var numFollowers = mission[14].ToInt32();
            var numRewards = mission[15].ToInt32();
            var xp = mission[16].ToInt32();
            var material = mission[17].ToInt32();
            var successChance = mission[18];
            var xpBonus = mission[19].ToInt32();
            var success = mission[20].ToBoolean();

            return new Mission(cost, description,
                durationSeconds, enemies, level, ilevel,
                isRare, location, missionId,
                name, numFollowers, numRewards,
                state, type, xp, material, successChance, xpBonus, success);
        }


        public static void TurnInAllCompletedMissions()
        {
            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                // ReSharper disable once UnusedVariable
                foreach (var completedMission in GetAllCompletedMissions())
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
        }

        public static void TurnInCompletedMissions(List<Mission> missions)
        {
            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                foreach (var completedMission in missions)
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
}
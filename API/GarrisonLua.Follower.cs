#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Documents;
using GarrisonButler.Libraries;
using Styx.Helpers;
using Styx.WoWInternals;
using Facet.Combinatorics;
using GarrisonButler.Config;
using GarrisonButler.Libraries.JSON;
using GarrisonButler.Libraries.Wowhead;
using GarrisonButler.Objects;
using Styx;
using Styx.Common;

#endregion

namespace GarrisonButler.API
{
    public static class FollowersLua
    {
        // Return list of all available followers

        public static List<Follower> GetAllFollowers()
        {
            var followers = API.ButlerLua.GetAllFromLua<Follower>(GetListFollowersId, GetFollowerById);
            //var newMissionStubCodeResult = NewMissionStubCode(followers);
            return followers;
        }

        public static string GetFollowerName(string follower)
        {
            var lua = String.Format("return C_Garrison.GetFollowerName(\"{0}\");", follower);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerNameById(string followerId)
        {
            var lua = String.Format("return C_Garrison.GetFollowerNameByID(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerClassSpecName(string follower)
        {
            var lua = String.Format("return C_Garrison.GetFollowerClassSpecName(\"{0}\");", follower);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }


        public static string GetFollowerDisplayIdbyId(string followerId)
        {
            var lua = String.Format("return C_Garrison.GetFollowerDisplayIDByID(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        public static string GetFollowerStatus(string followerId)
        {
            var lua =
                "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format("for i,v in ipairs(followers) do " +
                              "local followerID = (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID;" +
                              "if (followerID == {0}) then table.insert(RetInfo,tostring(v.status)); end;" +
                              "end;" +
                              "return unpack(RetInfo)", followerId);

            return Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault() == "nil"
                ? "None"
                : (Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault() == default(string)
                    ? "None"
                    : Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault());
        }

        public static string GetFollowerInfo(string followerId)
        {
            var lua = String.Format("return {0} and C_Garrison.GetFollowerInfo(\"{0}\");", followerId);
            List<string> ret = Lua.GetReturnValues(lua);
            return ret.GetEmptyIfNull().FirstOrDefault();
        }

        // Return the follower corresponding to the id

        public static Follower GetFollowerById(String followerId)
        {
            var lua =
                "local RetInfo = {}; local Temp = {}; local followers = C_Garrison.GetFollowers();" +
                String.Format(
                    "for i,f in ipairs(followers) do " +
                    "local followerID = (f.garrFollowerID) and tonumber(f.garrFollowerID) or f.followerID;" +
                    "if (followerID == {0}) then " +
                    "Temp[0] = followerID;" +
                    "Temp[1] = f.name;" +
                    //"Temp[2] = f.status;" +
                    "if f.status == GARRISON_FOLLOWER_IN_PARTY then Temp[2] = 1;" +
                    "elseif f.status == GARRISON_FOLLOWER_WORKING then Temp[2] = 2;" +
                    "elseif f.status == GARRISON_FOLLOWER_ON_MISSION then Temp[2] = 3;" +
                    "elseif f.status == GARRISON_FOLLOWER_EXHAUSTED then Temp[2] = 4;" +
                    "elseif f.status == GARRISON_FOLLOWER_INACTIVE then Temp[2] = 5;" +
                    "else Temp[2] = 0;" +
                    "end;" + 
                    "Temp[3] = f.ClassSpecName;" +
                    "Temp[4] = f.quality;" +
                    "Temp[5] = f.level;" +
                    "Temp[6] = f.isCollected ;" +
                    "Temp[7] = f.iLevel;" +
                    "Temp[8] = f.levelXP;" +
                    "Temp[9] = f.xp;" +
                    "Temp[10] = f.followerID;" +
                    "end;" +
                    "end;" +
                    "for j_=0,10 do table.insert(RetInfo,tostring(Temp[j_]));end; " +
                    "return unpack(RetInfo)", followerId);
            var follower = Lua.GetReturnValues(lua);

            if (follower.IsNullOrEmpty())
                return null;

            var name = follower[1];
            var status = follower[2];
            var classSpecName = follower[3];
            var quality = follower[4];
            var level = follower[5].ToInt32();
            var isCollected = follower[6].ToBoolean();
            var iLevel = follower[7].ToInt32();
            var xp = follower[8].ToInt32();
            var levelXp = follower[9].ToInt32();
            var uniqueId = follower[10];

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", uniqueId != "nil" ? uniqueId : followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  "for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(counterID));" +
                  "end;" +
                  "end;" +
                  "return unpack(RetInfo)";

            var counters = Lua.GetReturnValues(lua);

            lua = String.Format("local abilities = C_Garrison.GetFollowerAbilities(\"{0}\");", uniqueId != "nil" ? uniqueId : followerId) +
                  "local RetInfo = {};" +
                  "for a = 1, #abilities do " +
                  "local ability= abilities[a];" +
                  //"for counterID, counterInfo in pairs(ability.counters) do " +
                  "table.insert(RetInfo,tostring(ability.id));" +
                  //"end;" +
                  "end;" +
                  "return unpack(RetInfo)";

            var abil_string = Lua.GetReturnValues(lua);
            var abilities = abil_string.Select(a => a.ToInt32()).ToList();

            return new Follower(followerId, uniqueId, name, status, classSpecName, quality, level, isCollected, iLevel, xp,
                levelXp, counters, abilities);
        }

        // Return list of all available missions

        public static List<string> GetListFollowersId()
        {
            const string lua = "local RetInfo = {}; local followers = C_Garrison.GetFollowers();" +
                               "for i,v in ipairs(followers) do " +
                               "table.insert(RetInfo,tostring( (v.garrFollowerID) and tonumber(v.garrFollowerID) or v.followerID));" +
                               "end;" +
                               "return unpack(RetInfo)";

            var followerId = Lua.GetReturnValues(lua);
            return followerId;
        }

        public static int GetNumFollowersOnMission(int missionId)
        {
            var lua = String.Format("return C_Garrison.GetNumFollowersOnMission(\"{0}\");", missionId);
            var luaRet = Lua.GetReturnValues(lua);
            return Convert.ToInt32(luaRet.GetEmptyIfNull().FirstOrDefault());
        }

        public static void AddFollowerToMission(int missionId, int followerId)
        {
            var lua = String.Format("C_Garrison.AddFollowerToMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }

        public static void RemoveFollowerFromMission(int missionId, int followerId)
        {
            var lua = String.Format("C_Garrison.RemoveFollowerFromMission(\"{0}\",\"{1}\");", missionId, followerId);
            Lua.DoString(lua);
        }
    }
}
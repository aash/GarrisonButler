using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonBuddy;
using Styx.Helpers;
using Styx.WoWInternals;

namespace GarrisonLua
{
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
}
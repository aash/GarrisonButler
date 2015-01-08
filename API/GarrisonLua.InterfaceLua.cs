#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler;
using GarrisonButler.Libraries;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.WoWInternals;

#endregion

namespace GarrisonLua
{
    public static class InterfaceLua
    {
        public static bool IsGarrisonMissionFrameOpen()
        {
            const string lua =
                "if not GarrisonMissionFrame then return false; else return tostring(GarrisonMissionFrame:IsVisible());end;";

            string t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        public static bool IsGarrisonCapacitiveDisplayFrame()
        {
            const string lua =
                "if not GarrisonCapacitiveDisplayFrame then return false; else return tostring(GarrisonCapacitiveDisplayFrame:IsVisible());end;";

            string t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        public static void ClickStartOrderButton()
        {
            Lua.DoString("GarrisonCapacitiveDisplayFrame.StartWorkOrderButton:Click()");
        }

        public static void ClickSendMail()
        {
            Lua.DoString("SendMailFrame_SendMail();");
        }

        public static void ClickCloseMailButton()
        {
            Lua.DoString("/click MailFrameCloseButton()");
        }

        public static bool IsGarrisonMissionTabVisible()
        {
            const string lua =
                "if not GarrisonMissionFrame or not GarrisonMissionFrame.MissionTab then return false; else return tostring(GarrisonMissionFrame.MissionTab:IsVisible()); end;";

            string t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        public static bool IsGarrisonMissionVisible()
        {
            const string lua =
                "if not GarrisonMissionFrame or not GarrisonMissionFrame.MissionTab or not GarrisonMissionFrame.MissionTab.MissionPage then return false;end;" +
                "return tostring(GarrisonMissionFrame.MissionTab.MissionPage:IsShown())";

            string t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        public static bool IsGarrisonMissionVisibleAndValid(string missionId)
        {
            string lua =
                String.Format(
                    "if not GarrisonMissionFrame.MissionTab.MissionPage or not GarrisonMissionFrame.MissionTab.MissionPage.missionInfo or not GarrisonMissionFrame.MissionTab.MissionPage:IsShown() then return false;end;" +
                    "return tostring(GarrisonMissionFrame.MissionTab.MissionPage.missionInfo.missionID == {0} )",
                    missionId);

            string t = Lua.GetReturnValues(lua).GetEmptyIfNull().FirstOrDefault();
            return t.ToBoolean();
        }

        public static void ClickTabMission()
        {
            Lua.DoString("GarrisonMissionFrameTab1:Click();");
        }

        public static void OpenMission(Mission mission)
        {
            GarrisonButler.GarrisonButler.Diagnostic("OpenMission - id: " + mission.MissionId);
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
            GarrisonButler.GarrisonButler.Diagnostic("Cleaning mission Followers");

            String luaClear = String.Format(
                "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
                "for idx = 1, #MissionPageFollowers do " +
                "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
                "end;");

            Lua.DoString(luaClear);

            GarrisonButler.GarrisonButler.Diagnostic("Adding mission Followers: " + followersId.Count);

            foreach (string t in followersId)
            {
                GarrisonButler.GarrisonButler.Diagnostic("Adding mission Followers ID: " + t);
            }
            string
                luaAdd =
                    "local fols = {};" +
                    String.Format("fols[1]=\"{0}\";fols[2]=\"{1}\";fols[3]=\"{2}\";",
                        followersId[0], followersId.ElementAtOrDefault(1),
                        followersId.ElementAtOrDefault(2)) +
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
                    "GarrisonMissionPage_SetFollower(followerFrame, follower);" +
                    "end;" +
                    "end;";

            Lua.DoString(luaAdd);
        }

        public static async Task AddFollowersToMission(string missionId, List<string> followersId)
        {
            foreach (string followerId in followersId)
            {
                String luaAdd = String.Format(
                    //Check if in current list
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
            }

            await CommonCoroutines.SleepForRandomReactionTime();
        }

        public static void ClickStartMission()
        {
            String lua = "GarrisonMissionFrame.MissionTab.MissionPage.StartMissionButton:Click();";

            Lua.DoString(lua);
        }

        public static void StartMission(Mission mission)
        {
            GarrisonButler.GarrisonButler.Diagnostic("StartMission: ");
            GarrisonButler.GarrisonButler.Diagnostic(mission.ToString());

            String lua = String.Format("C_Garrison.StartMission(\"{0}\");", mission.MissionId);

            Lua.DoString(lua);
        }
    }
}
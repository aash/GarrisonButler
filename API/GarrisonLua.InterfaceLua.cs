using System;
using System.Collections.Generic;
using System.Linq;
using GarrisonBuddy;
using Styx.Helpers;
using Styx.WoWInternals;

namespace GarrisonLua
{
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
            GarrisonBuddy.GarrisonBuddy.Diagnostic("OpenMission - id: " + mission.MissionId);
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
            GarrisonBuddy.GarrisonBuddy.Diagnostic("Cleaning mission Followers");
            String luaClear = String.Format(
                "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
                "for idx = 1, #MissionPageFollowers do " +
                "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
                "end;");
            Lua.DoString(luaClear);

            GarrisonBuddy.GarrisonBuddy.Diagnostic("Adding mission Followers: " + followersId.Count);
            foreach (string t in followersId)
            {
                GarrisonBuddy.GarrisonBuddy.Diagnostic("Adding mission Followers ID: " + t);
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
            //GarrisonBuddy.Diagnostic("Cleaning mission Followers");
            //String luaClear = String.Format(
            //    "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
            //    "for idx = 1, #MissionPageFollowers do " +
            //        "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
            //    "end;");
            //Lua.DoString(luaClear);

            //GarrisonBuddy.Diagnostic("Adding mission Followers: " + followersId.Count);
            //    foreach (var t in followersId)
            //    {
            //        GarrisonBuddy.Diagnostic("Adding mission Followers ID: " + t);
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
            GarrisonBuddy.GarrisonBuddy.Diagnostic("StartMission");
            String lua = String.Format("C_Garrison.StartMission(\"{0}\");", missionId);
            Lua.DoString(lua);
        }
    }
}
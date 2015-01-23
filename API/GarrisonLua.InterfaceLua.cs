#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Libraries;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.WoWInternals;

#endregion

namespace GarrisonButler.API
{
    public static class InterfaceLua
    {
        public static bool IsGarrisonMissionFrameOpen()
        {
            const string lua =
                "if not GarrisonMissionFrame then return false; else return tostring(GarrisonMissionFrame:IsVisible());end;";

            var results = Lua.GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        public static bool IsSplashFrame()
        {
            const string lua =
                @"if not SplashFrame then 
                      return tostring(false);
                  else 
                      if SplashFrame:IsVisible() == true then
                          return tostring(true);
                      end;
                  end;
                  return tostring(false);";
            var results = Lua.GetReturnValues(lua);

            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

//        public static bool IsGarrisonCapacitiveDisplayFrame()
//        {
//            const string lua =
//                @"if not GarrisonCapacitiveDisplayFrame then 
//                      return tostring(false);
//                  else 
//                      if GarrisonCapacitiveDisplayFrame:IsVisible() == true then
//                          return tostring(true);
//                      end;
//                  end;
//                  return tostring(false);";
//            var results = Lua.GetReturnValues(lua);

//            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
//        }
        
        public static void ToggleLandingPage()
        {
            Lua.DoString("GarrisonLandingPage_Toggle()");
        }

        public static void CloseLandingPage()
        {
            Lua.DoString("HideUIPanel(GarrisonLandingPage);");
        }

        public static void CloseSplashFrame()
        {
            Lua.DoString("HideUIPanel(SplashFrame);");
        }

        public static void MarkMailAsRead(int index)
        {
            Lua.DoString("GetInboxText(" + index + ")");
        }

        /// <summary>
        /// Must be called with Mail Frame open
        /// </summary>
        /// <returns>Number of mails shown in the player's mail inbox.  Max of 50 allowed at 1 time.</returns>
        public static int GetInboxMailCountInPlayerInbox()
        {
            const string lua =
                @"local numItems, totalItems = GetInboxNumItems();
                  if (not numItems) then
                     return tostring(0);
                  else
                     return tostring(numItems);
                  end;";

            var results = Lua.GetReturnValues(lua);

            return results.GetEmptyIfNull().FirstOrDefault().ToInt32();
        }

        /// <summary>
        /// Must be called with Mail Frame open
        /// </summary>
        /// <returns>Number of mails currently on blizzard server</returns>
        public static int GetInboxMailCountOnServer()
        {
            const string lua =
                @"local numItems, totalItems = GetInboxNumItems();
                  if (not totalItems) then
                     return tostring(0);
                  else
                     return tostring(totalItems);
                  end;";

            var results = Lua.GetReturnValues(lua);

            return results.GetEmptyIfNull().FirstOrDefault().ToInt32();
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

            var results = Lua.GetReturnValues(lua);

            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        public static bool IsGarrisonMissionVisible()
        {
            const string lua =
                "if not GarrisonMissionFrame or not GarrisonMissionFrame.MissionTab or not GarrisonMissionFrame.MissionTab.MissionPage then return false;end;" +
                "return tostring(GarrisonMissionFrame.MissionTab.MissionPage:IsShown())";

            var results = Lua.GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        public static bool IsGarrisonMissionVisibleAndValid(string missionId)
        {
            var lua =
                String.Format(
                    "if not GarrisonMissionFrame.MissionTab.MissionPage or not GarrisonMissionFrame.MissionTab.MissionPage.missionInfo or not GarrisonMissionFrame.MissionTab.MissionPage:IsShown() then return false;end;" +
                    "return tostring(GarrisonMissionFrame.MissionTab.MissionPage.missionInfo.missionID == {0} )",
                    missionId);

            var results = Lua.GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }

        public static void ClickTabMission()
        {
            Lua.DoString("GarrisonMissionFrameTab1:Click();");
        }

        public static void OpenMission(Mission mission)
        {
            GarrisonButler.Diagnostic("OpenMission - id: " + mission.MissionId);
            //Scroll until we see mission first
            var lua =
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
            const string lua = "GarrisonMissionFrame.MissionTab.MissionPage:Hide();" +
                               "GarrisonMissionFrame.MissionTab.MissionList:Show();" +
                               "GarrisonMissionPage_ClearParty();" +
                               "GarrisonMissionFrame.followerCounters = nil;" +
                               "GarrisonMissionFrame.MissionTab.MissionPage.missionInfo = nil;";

            Lua.DoString(lua);
        }

        public static void AddFollowersToMissionOld(string missionId, List<string> followersId)
        {
            GarrisonButler.Diagnostic("Cleaning mission Followers");

            var luaClear = String.Format(
                "local MissionPageFollowers = GarrisonMissionFrame.MissionTab.MissionPage.Followers;" +
                "for idx = 1, #MissionPageFollowers do " +
                "GarrisonMissionPage_ClearFollower(MissionPageFollowers[idx]);" +
                "end;");

            Lua.DoString(luaClear);

            GarrisonButler.Diagnostic("Adding mission Followers: " + followersId.Count);

            foreach (var t in followersId)
            {
                GarrisonButler.Diagnostic("Adding mission Followers ID: " + t);
            }
            var
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
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var followerId in followersId)
            {
                var luaAdd = String.Format(
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
            const string lua = "GarrisonMissionFrame.MissionTab.MissionPage.StartMissionButton:Click();";

            Lua.DoString(lua);
        }

        public static void StartMission(Mission mission)
        {
            GarrisonButler.Diagnostic("StartMission: ");
            GarrisonButler.Diagnostic(mission.ToString());

            var lua = String.Format("C_Garrison.StartMission(\"{0}\");", mission.MissionId);

            Lua.DoString(lua);
        }
    }
}
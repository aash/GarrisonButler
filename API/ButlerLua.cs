using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Coroutines;
using GarrisonButler.Libraries;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.WoWInternals;

namespace GarrisonButler.API
{
    public static class ButlerLua
    {
        private const int TimeOut = 10000;

        /// <summary>
        ///     Timer to avoid spamming too many LUA calls.
        /// </summary>
        private static WaitTimer _antiLuaSpamTimer;

        /// <summary>
        ///     Actual time waited between calls in ms.
        /// </summary>
        private static int _actualWaitTime = 10;

        /// <summary>
        ///     Return the current instance if exist or a new if not.
        /// </summary>
        //private static ButlerLua _instance;
        static ButlerLua()
        {
            _antiLuaSpamTimer = new WaitTimer(TimeSpan.FromMilliseconds(_actualWaitTime));
        }

        //public static ButlerLua Instance
        //{
        //    get
        //    {
        //        // if first run, init
        //        if (_instance == null)
        //            Instance = new ButlerLua();
        //        return _instance;
        //    }
        //    set { _instance = value; }
        //}

        private static void RefreshWaitTime()
        {
            var fps = StyxWoW.WoWClient.Fps;
            if (fps > 0)
            {
                _actualWaitTime = 1000/(int) fps;
            }
            if (fps == 0)
            {
                _actualWaitTime = TimeOut/2;
            }
            else
                _actualWaitTime = 10;
            _antiLuaSpamTimer.WaitTime = TimeSpan.FromMilliseconds(_actualWaitTime);
            _antiLuaSpamTimer.Reset();
        }

        /// <summary>
        ///     Anti Spam protection, wait for timeout or antiSpam finished.
        ///     If TimeOut comes first, an error is thrown.
        /// </summary>
        /// <returns></returns>
        private static async Task AntiSpamProtection()
        {
            var start = DateTime.Now;
            var now = DateTime.Now;
            while (!_antiLuaSpamTimer.IsFinished)
            {
                if ((now - start).TotalMilliseconds >= TimeOut)
                    throw new TimeoutException(
                        String.Format("Lua AntiSpamProtection timed out before end of wait time." +
                                      " (Started:{0},Now:{1},Timeout:{2},CurrentWait:{3}",
                            start, now, TimeOut, _actualWaitTime));

                await Buddy.Coroutines.Coroutine.Sleep(_antiLuaSpamTimer.TimeLeft);
                now = DateTime.Now;
            }
            RefreshWaitTime();
        }

        /// <summary>
        ///     Execute an Lua string after passing through the anti spam.
        /// </summary>
        /// <param name="luaString"></param>
        private static async Task<ActionResult> DoString(string luaString)
        {
            await AntiSpamProtection();
            Lua.DoString(luaString);
            return ActionResult.Done;
        }
        private static async Task<ActionResult> DoStringWhile(string luaString, Func<Task<bool>> condition, int maxTime)
        {
            var waitTimer = new WaitTimer(TimeSpan.FromMilliseconds(maxTime));
            var done = await condition();
            while (!done)
            {
                if (waitTimer.IsFinished)
                {
                    GarrisonButler.Diagnostic("[ButlerLua] Lua call condition termination timed out.");
                    return ActionResult.Failed;
                }
                await AntiSpamProtection();
                Lua.DoString(luaString);
                await Buddy.Coroutines.Coroutine.Yield();
                done = await condition();
            }
            return ActionResult.Done;
        }

        private static async Task<List<string>> GetReturnValues(string luaString)
        {
            await AntiSpamProtection();
            var results = Lua.GetReturnValues(luaString);
            return results;
        }

        /// <summary>
        ///     Split an item from a bag/slot to a free bag/slot with desired stack size.
        ///     Object manager should be updated once getting out.
        /// </summary>
        /// <param name="fromBag"></param>
        /// <param name="fromSlot"></param>
        /// <param name="amount"></param>
        /// <param name="toBag"></param>
        /// <param name="toSlot"></param>
        public static async Task<ActionResult> SplitItem(int fromBag, int fromSlot, int amount, int toBag, int toSlot)
        {
            var fromBagWoW = fromBag + 1;
            var toBagWoW = toBag + 1;
            var fromSlotWoW = fromSlot + 1;
            var toSlotWoW = toSlot + 1;
            GarrisonButler.Diagnostic(
                "[ButlerLua] SplitItem fromBag:{0}, fromSlot:{1}, amount:{2}, tobag:{3}, toSlot:{4}.",
                fromBagWoW, fromSlotWoW, amount, toBagWoW, toSlotWoW);

            var luaString = String.Format(@"
                ClearCursor(); 
                SplitContainerItem({0},{1},{2});
                if CursorHasItem() then 
                    PickupContainerItem({3},{4});
                    ClearCursor();
                end;",
                fromBagWoW, fromSlotWoW, amount, toBagWoW, toSlotWoW);

            var result = await DoString(luaString);
            // Refeshing object manager since we did changes on the bag items.
            ObjectManager.Update();
            return result;
        }

        public static async Task<ActionResult> CloseLandingPage()
        {
            const string luaString = "HideUIPanel(GarrisonLandingPage);";
            return await DoStringWhile(luaString, async () => !await IsLandingPageOpen(), 3000);
        }
        public static async Task<ActionResult> OpenLandingPageOld()
        {
            const string luaString = "GarrisonLandingPage_Toggle()";
            return await DoStringWhile(luaString, async () => await IsLandingPageOpen(), 3000);
        }
        public static async Task<ActionResult> OpenLandingPage()
        {
            const string luaString = "C_Garrison.RequestLandingPageShipmentInfo();";
            return await DoString(luaString);
        }


        public static async Task<bool> IsLandingPageOpen()
        {
            const string lua =
               @"if not GarrisonLandingPage then 
                      return tostring(false);
                  else 
                      if GarrisonLandingPage:IsVisible() == true then
                          return tostring(true);
                      end;
                  end;
                  return tostring(false);";
            var results = await GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }
        public static async Task<bool> IsTradeSkillFrameOpen()
        {
            const string lua =
               @"if not TradeSkillFrame then 
                      return tostring(false);
                  else 
                      if TradeSkillFrame:IsVisible() == true then
                          return tostring(true);
                      end;
                  end;
                  return tostring(false);";
            var results = await GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToBoolean();
        }
        
        public static async Task<Tuple<int, int>> GetServerTime()
        {
            const string lua = @"  
                    local hour,minute = GetGameTime();
                    local RetInfo = {};
                    table.insert(RetInfo,tostring(hour));
                    table.insert(RetInfo,tostring(minute));
                    return unpack(RetInfo);";
            var results = await GetReturnValues(lua);
            var hour = results.GetEmptyIfNull().ElementAt(0).ToInt32();
            var minutes = results.GetEmptyIfNull().ElementAt(1).ToInt32();
            return new Tuple<int, int>(hour, minutes);
        }
        public static async Task<int> GetTimeBeforeResetInSec()
        {
            const string lua = @"  
                    local timeInSeconds = GetQuestResetTime();
                    return tostring(timeInSeconds);";
            var results = await GetReturnValues(lua);
            return results.GetEmptyIfNull().FirstOrDefault().ToInt32();
        }
        public static async Task<DateTime> GetServerDate()
        {
            string lua = "local todayDate = date(\"*t\");" +
                         "local ret = {};" +
                         "for k,v in pairs(todayDate) do table.insert(ret,tostring(v)); end;" +
                                       "return unpack(ret);";
            var results = await GetReturnValues(lua);
            var dateServer = results.GetEmptyIfNull().ToArray();
            if (dateServer.Count() < 6)
            {
                GarrisonButler.Diagnostic("Error while loading lua date. size={0}", dateServer.Count());
                ObjectDumper.WriteToHb(dateServer, 3);
                return DateTime.Now;
            }
            var hour = dateServer[0].ToInt32();
            var min = dateServer[1].ToInt32();
            var sec = dateServer[2].ToInt32();
            var day = dateServer[3].ToInt32();
            var month = dateServer[4].ToInt32();
            var year = dateServer[5].ToInt32();
            DateTime date = new DateTime(year,month,day,hour,min,sec);
            return date;
        }
        
        public async static Task<Tuple<int,int>> GetShipmentReagentInfo()
        {
            const string lua =
                @"local name, texture, quality, needed, quantity, itemID = C_Garrison.GetShipmentReagentInfo(1);
                    local RetInfo = {};
                    table.insert(RetInfo,tostring(itemID));
                    table.insert(RetInfo,tostring(needed));
                    return unpack(RetInfo);";
            var results = (await GetReturnValues(lua)).GetEmptyIfNull().ToArray();
            if (results.Count() < 2)
            {
                GarrisonButler.Diagnostic("Error retrieving ShipmentReagentInfo.");
                return new Tuple<int, int>(-1, -1);
            }
            return new Tuple<int, int>(results[0].ToInt32(), results[1].ToInt32());
        }

        public static async Task<bool> CraftDraenicMortar()
        {
            const string lua =
                " for i=1,GetNumTradeSkills()do " +
                "   local na,_,n=GetTradeSkillInfo(i)" +
                "   if na==\"Draenic Mortar\" then " +
                "       DoTradeSkill(i,1);" +
                "       return tostring(true);" +
                "   end;" +
                " end;" +
                " return tostring(false);";

            var results = (await GetReturnValues(lua)).GetEmptyIfNull().ToArray();
            if (!results.Any())
            {
                GarrisonButler.Diagnostic("Error retrieving CraftDraenicMortar lua answer.");
                return false;
            }
            return results[0].ToBoolean();
        }

        public static async Task<bool> GetAutoLootValue()
        {
            const string lua =
                " local loot = GetCVar(\"autoLootDefault\");" +
                " return tostring(loot);";

            var results = (await GetReturnValues(lua)).GetEmptyIfNull().ToArray();
            if (!results.Any())
            {
                GarrisonButler.Diagnostic("Error retrieving GetAutoLootValue lua answer.");
                return false;
            }
            return results[0].ToInt32() != 0;
        }

        // NOT AWAITED 
        public static void SetAutoLootValue(bool value)
        {
            string lua = string.Format("SetCVar(\"autoLootDefault\",{0});", value ? 1 : 0);
            Lua.DoString(lua);
        }

    }
}
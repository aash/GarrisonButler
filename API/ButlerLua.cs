using System;
using System.Threading.Tasks;
using GarrisonButler.Coroutines;
using Styx;
using Styx.Common.Helpers;
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
            var luaString = String.Format(@"
                ClearCursor(); 
                SplitContainerItem({0},{1},{2});
                if CursorHasItem() then 
                    PickupContainerItem({3},{4});
                    ClearCursor();
                end;",
                fromBag+1, fromSlot+1, amount, toBag+1, toSlot);

            var result = await DoString(luaString);
            // Refeshing object manager since we did changes on the bag items.
            ObjectManager.Update();
            return result;
        }
    }
}
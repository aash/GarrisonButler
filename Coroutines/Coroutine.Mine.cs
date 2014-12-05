using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
using Styx.Common.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static readonly List<uint> mineItems = new List<uint>
        {
            232541, // Mine cart
            232542, // Blackrock Deposit 
            232543, // Rich Blackrock Deposit 
            232544, // True iron deposit
            232545 // Rich True iron deposit
        };

        internal static readonly List<uint> MinesId = new List<uint>
        {
            7324, //ally 1
            7325, // ally 2
            7326, // ally 3
            7327, // horde 1
            7328, // horde 2
            7329, // horde 3
        };

        private static int PreserverdMiningPickItemId = 118903;
        private static int PreserverdMiningPickAura = 176061;

        private static int MinersCofeeItemId = 118897;
        private static int MinersCofeeAura = 176049;

        private static bool IsToDoMine()
        {
            return _mineWaitTimer == null || _mineWaitTimer.IsFinished || mineRunning;

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
                return false;

            // Is there something to mine? 
            return ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o => mineItems.Contains(o.Entry));
        }

        private static WaitTimer _mineWaitTimer;
        public static async Task<bool> CleanMine()
        {
            if (_mineWaitTimer != null && !_mineWaitTimer.IsFinished && !mineRunning)
                return false;
            if (_mineWaitTimer == null)
                _mineWaitTimer = new WaitTimer(TimeSpan.FromMinutes(1));
            _mineWaitTimer.Reset();

            if (!GaBSettings.Mono.HarvestMine)
                return false;

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
                return false;
            // Is there something to mine? 
            List<WoWGameObject> ores =
                ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => mineItems.Contains(o.Entry)).ToList();
            if (!ores.Any())
            {
                mineRunning = false; 
                return false;
            }
            mineRunning = true;

            WoWGameObject itemToCollect = ores.OrderBy(i => i.Location.X).First();
            GarrisonBuddy.Diagnostic("Found ore to gather at:" + itemToCollect.Location);

            if (MinesId.Contains(Me.SubZoneId))
            {
                itemToCollect = ores.OrderBy(i => i.Distance).First();
                // Do I have a mining pick to use
                WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
                if (miningPick != null && miningPick.Usable
                    && !Me.HasAura(PreserverdMiningPickAura)
                    && miningPick.CooldownTimeLeft.TotalSeconds < 0.1)
                {
                    GarrisonBuddy.Diagnostic("Using Mining pick");
                    miningPick.Use();
                    ObjectManager.Update();
                }
                if (miningPick != null && miningPick.StackCount >= 4)
                {
                    GarrisonBuddy.Log("Mining picks full: deleting");
                    Lua.DoString("ClearCursor()");
                    miningPick.PickUp();
                    Lua.DoString("DeleteCursorItem()");
                }

                // Do I have a cofee to use
                WoWItem coffee = Me.BagItems.Where(o => o.Entry == MinersCofeeItemId).ToList().FirstOrDefault();
                if (coffee != null && coffee.Usable &&
                    (!Me.HasAura(MinersCofeeAura) ||
                     Me.Auras.FirstOrDefault(a => a.Value.SpellId == MinersCofeeAura).Value.StackCount < 2)
                    && coffee.CooldownTimeLeft.TotalSeconds == 0)
                {
                    GarrisonBuddy.Diagnostic("Using coffee");
                    coffee.Use();
                }
                if (coffee != null && coffee.StackCount >= 4)
                {
                    GarrisonBuddy.Log("Miner's Coffee full: deleting");
                    Lua.DoString("ClearCursor()");
                    coffee.PickUp();
                    Lua.DoString("DeleteCursorItem()");
                }


                if (await MoveTo(itemToCollect.Location))
                    return true;

                itemToCollect.Interact();
                SetLootPoi(itemToCollect);
                await Buddy.Coroutines.Coroutine.Sleep(3500);
                return true;
            }

            return await MoveTo(itemToCollect.Location);
        }

        public static bool mineRunning { get; set; }
    }
}
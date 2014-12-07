﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using GarrisonBuddy.Config;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
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

        private static bool CanRunMine()
        {
            // Settings
            if (!GaBSettings.Mono.HarvestMine)
                return false;

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
                return false;

            // Is there something to mine? 
            return ObjectManager.GetObjectsOfType<WoWGameObject>().Any(o => mineItems.Contains(o.Entry));
        }

        //public static async Task<bool> CleanMine()
        //{
        //    if (!CanRunMine())
        //        return false;

        //    var node = BotPoi.Current.AsObject as WoWGameObject;
        //    if (node == null || !node.IsValid)
        //    {
        //        BotPoi.Clear();
        //    }

        //    WoWGameObject itemToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => mineItems.Contains(o.Entry)).OrderBy(n=> n.Location.Z).First();
        //    GarrisonBuddy.Log("Found ore to gather, moving to ore at:" + itemToCollect.Location);

        //    if (MinesId.Contains(Me.SubZoneId))
        //    {
        //        // Do I have a mining pick to use
        //        WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
        //        if (miningPick != null && miningPick.Usable
        //            && !Me.HasAura(PreserverdMiningPickAura)
        //            && miningPick.CooldownTimeLeft.TotalSeconds < 0.1)
        //        {
        //            GarrisonBuddy.Log("Doh! Found a mining pick in my bag, let's get geared up.");
        //            miningPick.Use();
        //            ObjectManager.Update();
        //        }
        //        if (miningPick != null && miningPick.StackCount >= 4)
        //        {
        //            GarrisonBuddy.Log("Mining picks full: deleting");
        //            Lua.DoString("ClearCursor()");
        //            miningPick.PickUp();
        //            Lua.DoString("DeleteCursorItem()");
        //        }

        //        // Do I have a cofee to use
        //        WoWItem coffee = Me.BagItems.Where(o => o.Entry == MinersCofeeItemId).ToList().FirstOrDefault();
        //        if (coffee != null && coffee.Usable &&
        //            (!Me.HasAura(MinersCofeeAura) ||
        //             Me.Auras.FirstOrDefault(a => a.Value.SpellId == MinersCofeeAura).Value.StackCount < 2)
        //            && coffee.CooldownTimeLeft.TotalSeconds == 0)
        //        {
        //            GarrisonBuddy.Log("Found coffee in my bag, let's drink it.");
        //            coffee.Use();
        //        }
        //        if (coffee != null && coffee.StackCount >= 4)
        //        {
        //            GarrisonBuddy.Log("Miner's Coffee full: deleting");
        //            Lua.DoString("ClearCursor()");
        //            coffee.PickUp();
        //            Lua.DoString("DeleteCursorItem()");
        //        }


        //        if (await MoveTo(itemToCollect.Location))
        //            return true;


        //        if (!await Buddy.Coroutines.Coroutine.Wait(500, () => !Me.IsMoving))
        //        {
        //            WoWMovement.MoveStop();
        //        }
        //        await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();

        //        if(!Me.IsMoving && !Me.IsCasting && BotPoi.Current.AsObject != node)
        //            itemToCollect.Interact();

        //        BotPoi.Current = new BotPoi(itemToCollect,PoiType.Harvest);
        //        //GarrisonBuddy.Diagnostic("Interact");
        //        //await Buddy.Coroutines.Coroutine.Wait(3875, () => !Me.IsCasting && GarrisonBuddy.LootIsOpen);
        //        //await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();
        //        //await CheckLootFrame();
        //        //await Buddy.Coroutines.Coroutine.Wait(5000, () => !itemToCollect.IsValid);
        //        return true;
        //    }

        //    return await MoveTo(itemToCollect.Location);
        //}


        public static async Task<bool> CleanMine()
        {
            if (!CanRunMine())
                return false;

            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }

            WoWGameObject itemToCollect = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => mineItems.Contains(o.Entry)).OrderBy(n => n.Location.Z).First();
            GarrisonBuddy.Log("Found ore to gather, moving to ore at:" + itemToCollect.Location);

            if (MinesId.Contains(Me.SubZoneId))
            {
                // Do I have a mining pick to use
                WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
                if (miningPick != null && miningPick.Usable
                    && !Me.HasAura(PreserverdMiningPickAura)
                    && miningPick.CooldownTimeLeft.TotalSeconds < 0.1)
                {
                    GarrisonBuddy.Log("Doh! Found a mining pick in my bag, let's get geared up.");
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
                    GarrisonBuddy.Log("Found coffee in my bag, let's drink it.");
                    coffee.Use();
                }
                if (coffee != null && coffee.StackCount >= 4)
                {
                    GarrisonBuddy.Log("Miner's Coffee full: deleting");
                    Lua.DoString("ClearCursor()");
                    coffee.PickUp();
                    Lua.DoString("DeleteCursorItem()");
                }
                return await HarvestWoWGameOject(itemToCollect);
            }

            return await MoveTo(itemToCollect.Location);
        }

        private async static Task<bool> HarvestWoWGameOject(WoWGameObject toHarvest)
        {
            var node = BotPoi.Current.AsObject as WoWGameObject;
            if (node == null || !node.IsValid)
            {
                BotPoi.Clear();
            }
            if (await MoveTo(toHarvest.Location))
                return true;

            if (!await Buddy.Coroutines.Coroutine.Wait(500, () => !Me.IsMoving))
            {
                WoWMovement.MoveStop();
            }
            await Styx.CommonBot.Coroutines.CommonCoroutines.SleepForLagDuration();

            //if (!Me.IsMoving && !Me.IsCasting && BotPoi.Current.AsObject != toHarvest)
            //    toHarvest.Interact();

            BotPoi.Current = new BotPoi(toHarvest, PoiType.Harvest);
            return true;
        }
        public static bool mineRunning { get; set; }
    }
}
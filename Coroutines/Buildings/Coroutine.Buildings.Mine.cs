﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using GarrisonButler.Config;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonButler
{
    partial class Coroutine
    {
        private static readonly List<uint> MineItems = new List<uint>
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

        private const int PreserverdMiningPickItemId = 118903;
        private const int PreserverdMiningPickAura = 176061;

        private const int MinersCofeeItemId = 118897;
        private const int MinersCofeeAura = 176049;


        private static bool ShouldRunMine()
        {
            return CanRunMine().Item1;
        }

        private static Tuple<bool, WoWGameObject> CanRunMine()
        {
            // Settings
            if (!GaBSettings.Get().HarvestMine)
            {
                GarrisonButler.Diagnostic("[Mine] Deactivated in user settings.");
                return new Tuple<bool,WoWGameObject>(false,null);
            }

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
            {
                GarrisonButler.Diagnostic("[Mine] Building not detected in Garrison's Buildings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Is there something to mine? 
            WoWGameObject node = ObjectManager.GetObjectsOfTypeFast<WoWGameObject>().Where(o => MineItems.Contains(o.Entry)).OrderBy(o=> o.Distance).FirstOrDefault();
            if (node == default(WoWGameObject))
            {
                GarrisonButler.Diagnostic("[Mine] No ore found to harvest.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            GarrisonButler.Diagnostic("[Mine] Found ore to gather at:" + node.Location);
            return new Tuple<bool, WoWGameObject>(true, node);
        }

        private static bool MeIsInMine()
        {
            return MinesId.Contains(Me.SubZoneId);
        }

        //public static async Task<bool> CollectOreInMine(WoWGameObject nodeToCollect)
        //{
        //    if (MinesId.Contains(Me.SubZoneId))
        //    {
        //        if (await UseItemInbags(MinersCofeeItemId, MinersCofeeAura, 2))
        //            return true;

        //        if (await UseItemInbags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1))
        //            return true;

        //        GarrisonButler.Log("[Mine] In mine, moving to harvest ore at: " + nodeToCollect.Location);
        //        return await HarvestWoWGameOject(nodeToCollect);
        //    }

        //    GarrisonButler.Log("[Mine] Not in mine yet, moving to harvest ore at: " + nodeToCollect.Location);
        //    return await MoveTo(nodeToCollect.Location);
        //}

        public static Func<Tuple<bool, WoWItem>> CanUseItemInBags(uint entry, uint auraId = 0, int maxStack = 0)
        {
            return new Func<Tuple<bool, WoWItem>>(() =>
            {
                WoWItem item = Me.BagItems.FirstOrDefault(o => o.Entry == entry);
                if (item == null || !item.IsValid || !item.Usable)
                    return new Tuple<bool,
                        WoWItem>(false,
                            null);
                var auras = Me.Auras.Where(a => a.Value.SpellId == auraId);
                if (auraId != 0 && maxStack != 0 && auras.Any())
                {
                    var Aura = auras.First().Value;
                    if (Aura == null)
                    {
                        GarrisonButler.Diagnostic("[Item] Aura null skipping.");
                        return new Tuple<bool,
                            WoWItem>(false,
                                null);
                    }
                    if (Aura.StackCount >= maxStack)
                    {
                        GarrisonButler.Diagnostic("[Item] Number of stacks: {0} - too high to use item {1}",
                            Aura.StackCount,
                            Aura.Name);
                        return new Tuple<bool, WoWItem>(false, null);
                    }
                    GarrisonButler.Diagnostic("[Item] AuraCheck: {0} - current stack {1}", Aura.Name, Aura.StackCount);
                }

                if (item.CooldownTimeLeft.TotalSeconds > 0)
                    return new Tuple<bool, WoWItem>(false, null);
                return new Tuple<bool, WoWItem>(true, item);
            });
        }

        public static async Task<bool> UseItemInbags(WoWItem item)
        {
            item.Use();
            GarrisonButler.Log("[Item] Using: {0}", item.Name);
            await CommonCoroutines.SleepForLagDuration();
            await Buddy.Coroutines.Coroutine.Wait(20000, () => !Me.IsCasting);
            return true;
        }
    }
}
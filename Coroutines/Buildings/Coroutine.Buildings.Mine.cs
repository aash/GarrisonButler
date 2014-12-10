using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bots.DungeonBuddy.Helpers;
using GarrisonBuddy.Config;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles.Quest.Order;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
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
            WoWGameObject node = null;
            return CanRunMine(out node);
        }

        private static bool CanRunMine(out WoWGameObject node)
        {
            node = null;
            // Settings
            if (!GaBSettings.Mono.HarvestMine)
            {
                GarrisonBuddy.Diagnostic("[Mine] Deactivated in user settings.");
                return false;
            }

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
            {
                GarrisonBuddy.Diagnostic("[Mine] Building not detected in Garrison's Buildings.");
                return false;
            }

            // Is there something to mine? 
            node = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => MineItems.Contains(o.Entry)).OrderBy(o=> o.DistanceSqr).FirstOrDefault();
            if (node == null)
            {
                GarrisonBuddy.Diagnostic("[Mine] No ore found to harvest.");
                return false;
            }

            GarrisonBuddy.Diagnostic("[Mine] Found ore to gather at:" + node.Location);
            return true;
        }

        public static async Task<bool> CleanMine()
        {
            WoWGameObject nodeToCollect = null;
            if (!CanRunMine(out nodeToCollect))
                return false;

            if (MinesId.Contains(Me.SubZoneId))
            {
                if (await UseItemInbags(MinersCofeeItemId, MinersCofeeAura, 2))
                    return true;

                if (await UseItemInbags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1))
                    return true;

                GarrisonBuddy.Log("[Mine] In mine, moving to harvest ore at: " + nodeToCollect.Location);
                return await HarvestWoWGameOject(nodeToCollect);
            }

            GarrisonBuddy.Log("[Mine] Not in mine yet, moving to harvest ore at: " + nodeToCollect.Location);
            return await MoveTo(nodeToCollect.Location);
        }


        public static async Task<bool> UseItemInbags(uint entry, uint auraId = 0, int maxStack = 0)
        {
            WoWItem item = Me.BagItems.FirstOrDefault(o => o.Entry == entry);
            if (item == null || !item.IsValid || !item.Usable)
                return false;

            var auras = Me.Auras.Where(a => a.Value.SpellId == auraId);
            if (auraId != 0 && maxStack != 0 && auras.Any())
            {
                var Aura = auras.First().Value;
                if (Aura == null)
                {
                    GarrisonBuddy.Diagnostic("[Item] Aura null skipping.");
                    return false;
                }
                if (Aura.StackCount >= maxStack)
                {
                    GarrisonBuddy.Diagnostic("[Item] Number of stacks: {0} - too high to use item {1}", Aura.StackCount, Aura.Name); 
                    return false;
                }
                GarrisonBuddy.Diagnostic("[Item] AuraCheck: {0} - current stack {1}", Aura.Name, Aura.StackCount); 
            }

            if (item.CooldownTimeLeft.TotalSeconds > 0)
                return false;

            item.Use();
            GarrisonBuddy.Log("[Item] Using: {0}", item.Name); 
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }
    }
}
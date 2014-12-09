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
                return false;

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
                return false;

            // Is there something to mine? 
            node = ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => MineItems.Contains(o.Entry)).OrderBy(o=> o.DistanceSqr).FirstOrDefault();
            return node != null;
        }

        public static async Task<bool> CleanMine()
        {
            WoWGameObject nodeToCollect = null;
            if (!CanRunMine(out nodeToCollect))
                return false;

            GarrisonBuddy.Log("Found ore to gather, moving to ore at:" + nodeToCollect.Location);

            if (MinesId.Contains(Me.SubZoneId))
            {
                if (await UseItemInbags(MinersCofeeItemId, MinersCofeeAura, 2))
                    return true;

                if (await UseItemInbags(PreserverdMiningPickItemId, PreserverdMiningPickAura, 1))
                    return true;

                return await HarvestWoWGameOject(nodeToCollect);
            }

            return await MoveTo(nodeToCollect.Location);
        }


        public static async Task<bool> UseItemInbags(uint entry, uint auraId = 0, int maxStack = 0)
        {
            WoWItem item = Me.BagItems.FirstOrDefault(o => o.Entry == entry);
            if (item == null || !item.IsValid || !item.Usable)
                return false;
            if (auraId != 0 && maxStack != 0 && Me.Auras.FirstOrDefault(a => a.Value.SpellId == auraId).Value != null)
            {
                uint stackAura = Me.Auras.FirstOrDefault(a => a.Value.SpellId == auraId).Value.StackCount;
                if (stackAura >= maxStack)
                    return false;
            }
            if (item.CooldownTimeLeft.TotalSeconds > 0)
                return false;

            item.Use();
            await CommonCoroutines.SleepForLagDuration();
            return true;
        }
    }
}
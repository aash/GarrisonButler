#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.Config;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        private const int PreserverdMiningPickItemId = 118903;
        private const int PreserverdMiningPickAura = 176061;

        private const int MinersCofeeItemId = 118897;
        private const int MinersCofeeAura = 176049;

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
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].buildingIds.Contains(b.id)))
            {
                GarrisonButler.Diagnostic("[Mine] Building not detected in Garrison's Buildings.");
                return new Tuple<bool, WoWGameObject>(false, null);
            }

            // Is there something to mine? 
            WoWGameObject node =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .Where(o => MineItems.Contains(o.Entry))
                    .OrderBy(o => o.Distance)
                    .FirstOrDefault();
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

        public static Func<Tuple<bool, WoWItem>> CanUseItemInBags(uint entry, uint auraId = 0, int maxStack = 0)
        {
            return () =>
            {
                WoWItem item = Me.BagItems.FirstOrDefault(o => o.Entry == entry);
                if (item == null || !item.IsValid || !item.Usable)
                    return new Tuple<bool,
                        WoWItem>(false,
                            null);
                IEnumerable<KeyValuePair<string, WoWAura>> auras = Me.Auras.Where(a => a.Value.SpellId == auraId);
                if (auraId != 0 && maxStack != 0 && auras.Any())
                {
                    WoWAura Aura = auras.First().Value;
                    if (Aura == null)
                    {
                        GarrisonButler.Diagnostic("[Item] Aura null skipping.");
                        return new Tuple<bool,
                            WoWItem>(false,
                                null);
                    }
                    if (Aura.StackCount >= maxStack || maxStack == 1)
                    {
                        GarrisonButler.Diagnostic("[Item] Number of stacks/Max: {0}/{1} - too high to use item {2}",
                            Aura.StackCount,
                            maxStack,
                            Aura.Name);
                        return new Tuple<bool, WoWItem>(false, null);
                    }
                    GarrisonButler.Diagnostic("[Item] AuraCheck: {0} - current stack {1}", Aura.Name, Aura.StackCount);
                }

                if (item.CooldownTimeLeft.TotalSeconds > 0)
                    return new Tuple<bool, WoWItem>(false, null);
                return new Tuple<bool, WoWItem>(true, item);
            };
        }

        public static async Task<bool> UseItemInbags(WoWItem item)
        {
            if (item == null)
                return true;

            if (!item.IsValid)
                return true;

            item.Use();
            GarrisonButler.Log("[Item] Using: {0}", item.Name);
            await CommonCoroutines.SleepForLagDuration();
            await Buddy.Coroutines.Coroutine.Wait(20000, () => !Me.IsCasting);

            return true;
        }

        public static async Task<bool> DeleteItemInbags(WoWItem item)
        {
            GarrisonButler.Log("[Item] Deleting: {0}", item.Name);
            Lua.DoString("ClearCursor()");
            item.PickUp();
            Lua.DoString("DeleteCursorItem()");
            return true;
        }


        public static Func<Tuple<bool, WoWItem>> TooManyItemInBags(uint entry, int max = 0)
        {
            return () =>
            {
                WoWItem item = Me.BagItems.FirstOrDefault(o => o.Entry == entry);
                if (item == null || !item.IsValid || !item.Usable)
                    return new Tuple<bool,
                        WoWItem>(false,
                            null);

                if (item.StackCount >= max)
                    return new Tuple<bool, WoWItem>(true, item);

                return new Tuple<bool, WoWItem>(false, item);
            };
        }
    }
}
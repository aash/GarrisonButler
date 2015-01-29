#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonButler.API;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
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

        private static readonly List<uint> OresMine = new List<uint>
        {
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
            7329 // horde 3
        };


        private static async Task<Result> CanRunMine()
        {
            // Settings
            if (!GaBSettings.Get().HarvestMine)
            {
                GarrisonButler.Diagnostic("[Mine] Deactivated in user settings.");
                return new Result(ActionResult.Failed);
            }

            // Do i have a mine?
            if (!_buildings.Any(b => ShipmentsMap[0].BuildingIds.Contains(b.Id)))
            {
                GarrisonButler.Diagnostic("[Mine] Building not detected in Garrison's Buildings.");
                return new Result(ActionResult.Failed);
            }

            //// Is there something to mine?
            //WoWGameObject node =
            //    ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
            //        .GetEmptyIfNull()
            //        .Where(o => MineItems.Contains(o.Entry))
            //        .OrderBy(o => o.Distance)
            //        .FirstOrDefault();

            // Is there something to mine?
            var nodes =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => OresMine.Contains(o.Entry)).GetEmptyIfNull();

            var gameObjects = nodes as WoWGameObject[] ?? nodes.ToArray();
            if (gameObjects.IsNullOrEmpty())
            {
                GarrisonButler.Diagnostic("[Mine] No ore found to harvest.");
                return new Result(ActionResult.Failed);
            }
            var allObjects =
                ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                    .GetEmptyIfNull()
                    .Where(o => MineItems.Contains(o.Entry)).GetEmptyIfNull();
            var objects = allObjects as WoWGameObject[] ?? allObjects.ToArray();
            var closest = Dijkstra.GetClosestObjectSalesman(Me.Location, objects.ToArray());

            GarrisonButler.Diagnostic("[Mine] Found {0} to gather at {1}.", closest.Name, closest.Location);
            return new Result(ActionResult.Running, closest);
        }

        private static bool MeIsInMine()
        {
            return MinesId.Contains(Me.SubZoneId);
        }

        public static Func<Tuple<bool, WoWItem>> CanUseItemInBags(uint entry, uint auraId = 0, int maxStack = 0)
        {
            return () =>
            {
                var item = HbApi.GetItemInBags(entry).FirstOrDefault();
                if (item == null || !item.IsValid || !item.Usable)
                    return new Tuple<bool, WoWItem>(false,
                        null);
                var auras = Me.Auras.Where(a => a.Value.SpellId == auraId);
                var pairs = auras as KeyValuePair<string, WoWAura>[] ?? auras.ToArray();
                if (auraId != 0 && maxStack != 0 && pairs.Any())
                {
                    var aura = pairs.First().Value;
                    // ReSharper disable once InvertIf
                    if (aura == null)
                    {
                        GarrisonButler.Diagnostic("[Item] Aura null skipping.");
                        return new Tuple<bool, WoWItem>(false,
                            null);
                    }
                    // ReSharper disable once InvertIf
                    if (aura.StackCount >= maxStack || maxStack == 1)
                    {
                        GarrisonButler.Diagnostic("[Item] Number of stacks/Max: {0}/{1} - too high to use item {2}",
                            aura.StackCount,
                            maxStack,
                            aura.Name);
                        return new Tuple<bool, WoWItem>(false, null);
                    }
                    GarrisonButler.Diagnostic("[Item] AuraCheck: {0} - current stack {1}", aura.Name, aura.StackCount);
                }

                return item.CooldownTimeLeft.TotalSeconds > 0
                    ? new Tuple<bool, WoWItem>(false, null)
                    : new Tuple<bool, WoWItem>(true, item);
            };
        }

        public static async Task<Result> UseItemInbags(object obj)
        {
            var item = obj as WoWItem;
            if (item == null)
                return new Result(ActionResult.Failed);

            if (!item.IsValid)
                return new Result(ActionResult.Failed);

            item.Use();
            GarrisonButler.Log("[Item] Using: {0}", item.Name);
            await CommonCoroutines.SleepForLagDuration();
            await Buddy.Coroutines.Coroutine.Wait(20000, () => !Me.IsCasting);

            return new Result(ActionResult.Done);
        }

        /// <summary>
        /// Will use item and if used will wait for condition to turn true for max time of waiTimeCondition
        /// </summary>
        /// <param name="waitTimeCondition"></param>
        /// <param name="conditionExit"></param>
        /// <returns></returns>
        public static Func<object, Task<Result>> UseItemInbagsWithTimer(int waitTimeCondition = 0,
            Func<bool> conditionExit = null)
        {
            return (async delegate(object item)
            {
                var wowItem = item as WoWItem;
                if (wowItem == null)
                    return new Result(ActionResult.Failed);

                var res = await UseItemInbags(wowItem);
                if (res.Status == ActionResult.Done && conditionExit != null)
                    await Buddy.Coroutines.Coroutine.Wait(waitTimeCondition, conditionExit);

                return res;
            }
                );
        }


        public static async Task<Result> DeleteItemInbags(object obj)
        {
            var item = obj as WoWItem;
            if (item == null)
                return new Result(ActionResult.Failed);

            GarrisonButler.Log("[Item] Deleting one of: {0}", item.Name);
            Lua.DoString(
                String.Format(
                    "local amount = {0}; ", 1) +
                String.Format(
                    "local item = {0}; ", item.Entry) +
                "local ItemBagNr = 0; " +
                "local ItemSlotNr = 1; " +
                "for b=0,4 do " +
                "for s=1,GetContainerNumSlots(b) do " +
                "if ((GetContainerItemID(b,s) == item)) " + /*"and (select(3, GetContainerItemInfo(b,s)) == nil)) */
                "then " +
                "ItemBagNr = b; " +
                "ItemSlotNr = s; " +
                "end; " +
                "end; " +
                "end; " +
                "ClearCursor(); " +
                "SplitContainerItem(ItemBagNr,ItemSlotNr,amount); " +
                "if CursorHasItem() then " +
                "DeleteCursorItem(); " +
                "end;"
                );
            await CommonCoroutines.SleepForLagDuration();
            // To stop the task from continually running forever
            return new Result(ActionResult.Done);
        }


        public static Func<Tuple<bool, WoWItem>> TooManyItemInBags(uint entry, int max = 0)
        {
            return () =>
            {
                var item = HbApi.GetItemInBags(entry).FirstOrDefault();
                if (item == null || !item.IsValid || !item.Usable)
                    return new Tuple<bool, WoWItem>(false,
                        null);

                return item.StackCount >= max
                    ? new Tuple<bool, WoWItem>(true, item)
                    : new Tuple<bool, WoWItem>(false, item);
            };
        }
    }
}
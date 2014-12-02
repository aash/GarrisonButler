using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonBuddy.Config;
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

        internal static readonly List<uint> minesId = new List<uint>
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

        public static async Task<bool> CleanMine()
        {
            if (!GaBSettings.Mono.HarvestMine)
                return false;

            // Do i have a mine?
            if (!_buildings.Any(b => MinesId.Contains(b.id)))
                return false;

            // Is there something to mine? 
            List<WoWGameObject> ores =
                ObjectManager.GetObjectsOfType<WoWGameObject>().Where(o => mineItems.Contains(o.Entry)).ToList();
            if (!ores.Any())
                return false;
            
            WoWGameObject itemToCollect = ores.OrderBy(i => i.Distance).First();

            GarrisonBuddy.Diagnostic("Found ore to gather at:" + itemToCollect.Location );

            if (minesId.Contains(Me.SubZoneId))
            {
                // Do I have a mining pick to use
                WoWItem miningPick = Me.BagItems.FirstOrDefault(o => o.Entry == PreserverdMiningPickItemId);
                if (miningPick != null && miningPick.Usable
                    && !Me.HasAura(PreserverdMiningPickAura)
                    && miningPick.CooldownTimeLeft.TotalSeconds == 0)
                {
                    GarrisonBuddy.Diagnostic("Using Mining pick");
                    miningPick.Use();
                }
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

            if (await MoveTo(itemToCollect.Location))
                return true;

            await Buddy.Coroutines.Coroutine.Sleep(300);
            itemToCollect.Interact();
            SetLootPoi(itemToCollect);
            await Buddy.Coroutines.Coroutine.Sleep(3500);
            return true;
        }
    }
}
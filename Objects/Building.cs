using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace GarrisonBuddy
{
    public class Building
    {
        public delegate bool CanCompleteOrderD();

        public delegate Task<bool> PrepOrderD();

        private readonly String _buildTime;
        private readonly String buildingLevel;
        private readonly String canActivate;
        private readonly String canUpgrade;
        private readonly String creationTime;
        private readonly String duration;
        public readonly int id;
        private readonly String isBuilding;
        private readonly String isPrebuilt;
        private readonly String itemID;
        private readonly String itemName;
        private readonly String itemQuality;
        public readonly String name;
        private readonly String nameShipment;
        private readonly String plotId;
        public readonly int rank;
        public readonly int shipmentCapacity;
        public readonly int shipmentsReady;
        public readonly int shipmentsTotal;
        private readonly String timeStart;
        private List<uint> MillableFrom = new List<uint>();
        public int NumberReagent;
        public WoWPoint Pnj;
        public int PnjId;
        public PrepOrderD PrepOrder = () => new Task<bool>(() => false);
        public int ReagentId;
        public CanCompleteOrderD canCompleteOrder = () => false;
        private int currencyId;
        public int millItemPnj;

        public Building(bool MeIsAlliance, int id, string plotId, string buildingLevel, string name, int rank,
            string isBuilding,
            string timeStart, string buildTime, string canActivate, string canUpgrade, string isPrebuilt,
            string nameShipment, int shipmentCapacity, int shipmentsReady, int shipmentsTotal,
            string creationTime, string duration, string itemName, string itemQuality, string itemId)
        {
            this.id = id;
            this.plotId = plotId;
            this.buildingLevel = buildingLevel;
            this.name = name;
            this.rank = rank;
            this.isBuilding = isBuilding;
            this.timeStart = timeStart;
            _buildTime = buildTime;
            this.canActivate = canActivate;
            this.canUpgrade = canUpgrade;
            this.isPrebuilt = isPrebuilt;
            this.nameShipment = nameShipment;
            this.shipmentCapacity = shipmentCapacity;
            this.shipmentsReady = shipmentsReady;
            this.shipmentsTotal = shipmentsTotal;
            this.creationTime = creationTime;
            this.duration = duration;
            this.itemName = itemName;
            this.itemQuality = itemQuality;
            itemID = itemId ?? "None";
            GetOrderInfo(MeIsAlliance);

            GarrisonBuddy.Diagnostic(ToString());
        }

        public override string ToString()
        {
            return
                string.Format(
                    "Id: {0}, PlotId: {1}, BuildingLevel: {2}, Name: {3}, Rank: {4}, IsBuilding: {5}, TimeStart: {6}, BuildTime: {7}, CanActivate: {8}, CanUpgrade: {9}, IsPrebuilt: {10}, NameShipment: {11}, ShipmentCapacity: {12}, ShipmentsReady: {13}, ShipmentsTotal: {14}, CreationTime: {15}, Duration: {16}, ItemName: {17}, ItemQuality: {18}, ItemId: {19}",
                    id, plotId, buildingLevel, name, rank, isBuilding, timeStart, _buildTime, canActivate, canUpgrade,
                    isPrebuilt, nameShipment, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration,
                    itemName, itemQuality, itemID);
        }

        private bool canCompleteOrderItem()
        {
            long count = 0;
            WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInBags != null)
                count += itemInBags.StackCount;

            WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInReagentBank != null)
                count += itemInReagentBank.StackCount;
            return count >= NumberReagent;
        }

        private bool CanCompleteOrderCurrency()
        {
            return BuildingsLua.GetGarrisonRessources(id) > NumberReagent;
        }

        private bool CanCompleteOrderMillable()
        {
            long count = 0;
            WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInBags != null)
                count += itemInBags.StackCount;

            WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInReagentBank != null)
                count += itemInReagentBank.StackCount;

            if (count >= NumberReagent) return true;


            IEnumerable<WoWItem> millableInBags =
                StyxWoW.Me.BagItems.Where(i => MillableFrom.Contains(i.Entry) && i.StackCount > 5);
            if (millableInBags.Any())
            {
                if (MillOne(millableInBags.ToList()))
                    return false;
                count = 0;
                itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == ReagentId);
                if (itemInBags != null)
                    count += itemInBags.StackCount;

                if (itemInReagentBank != null)
                    count += itemInReagentBank.StackCount;
            }
            return count >= NumberReagent;
        }

        private bool MillOne(List<WoWItem> millable)
        {
            return false;
        }

        //private async Task<bool> MillForOrder(int countmMax)
        //{
        //    var millableInBags = StyxWoW.Me.BagItems.Where(i => MillableFrom.Contains(i.Entry) && i.StackCount > 5);
        //    long count = 0;

        //    WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == ReagentId);
        //    if (itemInBags != null)
        //        count += itemInBags.StackCount;

        //    WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == ReagentId);
        //    if (itemInReagentBank != null)
        //        count += itemInReagentBank.StackCount;

        //  if(StyxWoW.Me.KnowsSpell(51005))
        //        SpellManager.Spells.First(s=>s.Value.Id == 51005).Value.Cast();


        //    else if(StyxWoW.Me.BagItems.FirstOrDefault(i=> i.Entry == 109118).StackCount +
        //        StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == 109118).StackCount >= 5)
        //    {
        //        if (!StyxWoW.Me.BagItems.Any(i => i.Entry == 114942))
        //        {
        //            var pnj = ObjectManager.GetObjectsOfType<WoWUnit>().FirstOrDefault(u => u.Entry == millItemPnj);
        //            if (pnj == null) return false;
        //            if(await Coroutine.MoveTo(Coroutine.Dijkstra.ClosestToNodes(pnj.Location)))
        //                return false;
        //            pnj.Interact();
        //            TrainerFrame.Instance.SetServiceFilter(TrainerServiceFilter.Available, true);
        //            TrainerFrame.Instance.Buy(1);
        //        }
        //        var itemToMill = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == 114942);
        //        itemToMill.Use();
        //    }
        //    millable.First().UseContainerItem();
        //    Buddy.Coroutines.Coroutine.Wait(1000, () => !StyxWoW.Me.IsCasting);
        //    return true;
        //}

        private void GetOrderInfo(bool Alliance)
        {
            PnjId = 0;
            ReagentId = 0;
            NumberReagent = 0;
            currencyId = 0;
            Pnj = new WoWPoint();
            switch (id)
            {
                case (int) buildings.AlchemyLabLvl1:
                case (int) buildings.AlchemyLabLvl2:
                case (int) buildings.AlchemyLabLvl3:
                    break;

                case (int) buildings.BarnLvl1:
                case (int) buildings.BarnLvl2:
                case (int) buildings.BarnLvl3:
                    break;

                case (int) buildings.BarracksLvl1:
                case (int) buildings.BarracksLvl2:
                case (int) buildings.BarracksLvl3:
                    break;

                    //<Vendor Name="Dalana Clarke" Entry="89065" Type="Repair" X="1924.622" Y="225.1501" Z="76.96214" />
                case (int) buildings.DwarvenBunkerLvl1:
                case (int) buildings.DwarvenBunkerLvl2:
                case (int) buildings.DwarvenBunkerLvl3:
                    if (Alliance) PnjId = 89065;
                    else throw new NotImplementedException();
                    currencyId = 824;
                    NumberReagent = 20;
                    Pnj = new WoWPoint(1924.622, 225.1501, 76.96214);
                    canCompleteOrder = CanCompleteOrderCurrency;
                    break;

                case (int) buildings.EnchanterStudyLvl1:
                case (int) buildings.EnchanterStudyLvl2:
                case (int) buildings.EnchanterStudyLvl3:
                    break;

                case (int) buildings.EngineeringWorksLvl1:
                case (int) buildings.EngineeringWorksLvl2:
                case (int) buildings.EngineeringWorksLvl3:
                    break;
                    //<Vendor Name="Olly Nimkip" Entry="85514" Type="Repair" X="1862.214" Y="140" Z="78.29137" />
                case (int) buildings.GardenLvl1:
                case (int) buildings.GardenLvl2:
                case (int) buildings.GardenLvl3:
                    if (Alliance) PnjId = 85514;
                    else throw new NotImplementedException();
                    ReagentId = 116053;
                    NumberReagent = 5;
                    Pnj = new WoWPoint(1862.214, 140, 78.29137);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                case (int) buildings.GemBoutiqueLvl1:
                case (int) buildings.GemBoutiqueLvl2:
                case (int) buildings.GemBoutiqueLvl3:
                    break;

                case (int) buildings.GladiatorSanctumLvl1:
                case (int) buildings.GladiatorSanctumLvl2:
                case (int) buildings.GladiatorSanctumLvl3:
                    break;

                case (int) buildings.GnomishGearworksLvl1:
                case (int) buildings.GnomishGearworksLvl2:
                case (int) buildings.GnomishGearworksLvl3:
                    break;

                case (int) buildings.LumberMillLvl1:
                case (int) buildings.LumberMillLvl2:
                case (int) buildings.LumberMillLvl3:
                    break;

                case (int) buildings.MageTowerLvl1:
                case (int) buildings.MageTowerLvl2:
                case (int) buildings.MageTowerLvl3:
                    break;

                case (int) buildings.MineLvl1:
                case (int) buildings.MineLvl2:
                case (int) buildings.MineLvl3:
                    if (Alliance) PnjId = 77730;
                    else throw new NotImplementedException();
                    ReagentId = 115508;
                    NumberReagent = 5;
                    Pnj = new WoWPoint(1899.896, 101.2778, 83.52704);
                    canCompleteOrder = canCompleteOrderItem;
                    break;
                    // <Vendor Name="Timothy Leens" Entry="77730" Type="Repair" X="1899.896" Y="101.2778" Z="83.52704" />

                case (int) buildings.SalvageYardLvl1:
                case (int) buildings.SalvageYardLvl2:
                case (int) buildings.SalvageYardLvl3:
                    break;

                    // <Vendor Name="Kurt Broadoak" Entry="77777" Type="Repair" X="1817.415" Y="232.1284" Z="72.94653" />
                case (int) buildings.ScribeQuartersLvl1:
                case (int) buildings.ScribeQuartersLvl2:
                case (int) buildings.ScribeQuartersLvl3:
                    if (Alliance) PnjId = 77777;
                    else throw new NotImplementedException();
                    ReagentId = 114931;
                    NumberReagent = 2;
                    Pnj = new WoWPoint(1817.415, 232.1284, 72.94653);
                    canCompleteOrder = CanCompleteOrderMillable;
                    MillableFrom = Coroutine.GardenItems;
                    // PrepOrder = 
                    // <Vendor Name="Eric Broadoak" Entry="77372" Type="Repair" X="1817.415" Y="232.1284" Z="72.94568" />
                    millItemPnj = 77372;
                    break;

                case (int) buildings.StablesLvl1:
                case (int) buildings.StablesLvl2:
                case (int) buildings.StablesLvl3:
                    break;

                case (int) buildings.StorehouseLvl1:
                case (int) buildings.StorehouseLvl2:
                case (int) buildings.StorehouseLvl3:
                    break;

                case (int) buildings.TailoringEmporiumLvl1:
                case (int) buildings.TailoringEmporiumLvl2:
                case (int) buildings.TailoringEmporiumLvl3:
                    break;

                case (int) buildings.TheForgeLvl1:
                case (int) buildings.TheForgeLvl2:
                case (int) buildings.TheForgeLvl3:
                    break;

                case (int) buildings.TheTanneryLvl1:
                case (int) buildings.TheTanneryLvl2:
                case (int) buildings.TheTanneryLvl3:
                    break;


                case (int) buildings.TradingPostLvl1:
                case (int) buildings.TradingPostLvl2:
                case (int) buildings.TradingPostLvl3:
                    break;
            }
        }

        /*
         * Mine
         * alliance: 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         * 
         */

        private enum buildings
        {
            MineLvl1 = 61,
            MineLvl2 = 62,
            MineLvl3 = 63,

            GardenLvl1 = 29,
            GardenLvl2 = 136,
            GardenLvl3 = 137,


            BarracksLvl1 = 26,
            BarracksLvl2 = 27,
            BarracksLvl3 = 28,


            DwarvenBunkerLvl1 = 8,
            DwarvenBunkerLvl2 = 9,
            DwarvenBunkerLvl3 = 10,


            GnomishGearworksLvl1 = 162,
            GnomishGearworksLvl2 = 163,
            GnomishGearworksLvl3 = 164,


            MageTowerLvl1 = 37,
            MageTowerLvl2 = 38,
            MageTowerLvl3 = 39,


            StablesLvl1 = 65,
            StablesLvl2 = 66,
            StablesLvl3 = 67,


            BarnLvl1 = 24,
            BarnLvl2 = 25,
            BarnLvl3 = 133,


            GladiatorSanctumLvl1 = 159,
            GladiatorSanctumLvl2 = 160,
            GladiatorSanctumLvl3 = 161,


            LumberMillLvl1 = 40,
            LumberMillLvl2 = 41,
            LumberMillLvl3 = 138,


            TradingPostLvl1 = 111,
            TradingPostLvl2 = 144,
            TradingPostLvl3 = 145,


            AlchemyLabLvl1 = 76,
            AlchemyLabLvl2 = 119,
            AlchemyLabLvl3 = 120,


            EnchanterStudyLvl1 = 93,
            EnchanterStudyLvl2 = 125,
            EnchanterStudyLvl3 = 126,


            GemBoutiqueLvl1 = 96,
            GemBoutiqueLvl2 = 131,
            GemBoutiqueLvl3 = 132,


            SalvageYardLvl1 = 52,
            SalvageYardLvl2 = 140,
            SalvageYardLvl3 = 141,


            ScribeQuartersLvl1 = 95,
            ScribeQuartersLvl2 = 129,
            ScribeQuartersLvl3 = 130,


            StorehouseLvl1 = 51,
            StorehouseLvl2 = 142,
            StorehouseLvl3 = 143,


            TailoringEmporiumLvl1 = 94,
            TailoringEmporiumLvl2 = 127,
            TailoringEmporiumLvl3 = 128,


            TheForgeLvl1 = 60,
            TheForgeLvl2 = 117,
            TheForgeLvl3 = 118,


            TheTanneryLvl1 = 90,
            TheTanneryLvl2 = 121,
            TheTanneryLvl3 = 122,


            EngineeringWorksLvl1 = 91,
            EngineeringWorksLvl2 = 123,
            EngineeringWorksLvl3 = 124,
        }
    }
}
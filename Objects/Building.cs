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
        public List<int> ReagentIds;
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
        private bool canCompleteOrderItems()
        {
            foreach (var reagentId in ReagentIds)
            {
                long count = 0;
                WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == reagentId);
                if (itemInBags != null)
                    count += itemInBags.StackCount;

                WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == reagentId);
                if (itemInReagentBank != null)
                    count += itemInReagentBank.StackCount;
                if (count >= NumberReagent)
                        return true;
            }
            return false;
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

        private void GetOrderInfo(bool alliance)
        {
            PnjId = 0;
            ReagentId = 0;
            NumberReagent = 0;
            currencyId = 0;
            Pnj = new WoWPoint();
            switch (id)
            {
                //<Vendor Name="Keyana Tone" Entry="79814" Type="Repair" X="5662.159" Y="4551.546" Z="119.9567" />
                //Name="Peter Kearie" Entry="77791" X="1829.603" Y="201.3922" Z="72.73963" 
                case (int) buildings.AlchemyLabLvl1:
                case (int) buildings.AlchemyLabLvl2:
                case (int) buildings.AlchemyLabLvl3:
                    PnjId = alliance ? 77791 : 79814;
                    ReagentId = 108996;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;
                // horde   <Vendor Name="Farmer Lok'lub" Entry="85048" Type="Repair" X="5541.159" Y="4516.299" Z="131.7173" />
                case (int) buildings.BarnLvl1:
                case (int) buildings.BarnLvl2:
                case (int) buildings.BarnLvl3:
                    PnjId = alliance ? 84524 : 85048;
                    ReagentIds = new List<int>() { 119810, 119813, 119814 };
                    NumberReagent = 1;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItems;
                    break;

                case (int) buildings.BarracksLvl1:
                case (int) buildings.BarracksLvl2:
                case (int) buildings.BarracksLvl3:
                    break;


                    //horde 2: <Vendor Name="Magrish" Entry="89066" Type="Repair" X="5569.239" Y="4462.448" Z="132.5624" />
                    //ally 2: <Vendor Name="Dalana Clarke" Entry="89065" Type="Repair" X="1924.622" Y="225.1501" Z="76.96214" />
                case (int) buildings.DwarvenBunkerLvl1:
                case (int) buildings.DwarvenBunkerLvl2:
                case (int) buildings.DwarvenBunkerLvl3:
                    PnjId = alliance ? 89065 : 89066;
                    currencyId = 824;
                    NumberReagent = 20;
                    Pnj = alliance
                        ? new WoWPoint(1924.622, 225.1501, 76.96214)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = CanCompleteOrderCurrency;
                    break;


                    // horde 1 <Vendor Name="Yukla Greenshadow" Entry="79821" Type="Repair" X="5642.186" Y="4511.771" Z="120.1076" />
                    // ally 2 <Vendor Name="Garm" Entry="77781" Type="Repair" X="1806.123" Y="188.0837" Z="70.84762" />
                case (int) buildings.EnchanterStudyLvl1:
                case (int) buildings.EnchanterStudyLvl2:
                case (int) buildings.EnchanterStudyLvl3:
                    PnjId = alliance ? 77781 : 79821;
                    ReagentId = 109693;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;


                //Name="Helayn Whent" Entry="77831" X="1828.034" Y="198.3424" Z="72.75751"
                //horde 1 <Vendor Name="Garbra Fizzwonk" Entry="86696" Type="Repair" X="5669.706" Y="4550.133" Z="120.1031" />
                case (int) buildings.EngineeringWorksLvl1:
                case (int) buildings.EngineeringWorksLvl2:
                case (int)buildings.EngineeringWorksLvl3:
                    PnjId = alliance ? 77831 : 86696;
                    ReagentId = 111366;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                //ally lvl 2 : <Vendor Name="Olly Nimkip" Entry="85514" Type="Repair" X="1862.214" Y="140" Z="78.29137" />
                //horde lvl 2 : <Vendor Name="Nali Softsoil" Entry="85783" Type="Repair" X="5410.738" Y="4568.479" Z="138.3254" />
                case (int) buildings.GardenLvl1:
                case (int) buildings.GardenLvl2:
                case (int) buildings.GardenLvl3:
                    PnjId = alliance ? 85514 : 85783;
                    ReagentId = 116053;
                    NumberReagent = 5;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    canCompleteOrder = canCompleteOrderItem;
                    break;
                 
                //<Name="Kaya Solasen" Entry="77775" X="1825.785" Y="196.1163" Z="72.75745" /-->
                // horde 1 <Vendor Name="Elrondir Surrion" Entry="79830" Type="Repair" X="5649.468" Y="4509.388" Z="120.1563" />
                case (int) buildings.GemBoutiqueLvl1:
                case (int) buildings.GemBoutiqueLvl2:
                case (int)buildings.GemBoutiqueLvl3:
                    PnjId = alliance ? 77775 : 79830;
                    ReagentId = 115524;
                    NumberReagent = 5;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                //ally 2 <WoWUnit Name="Altar of Bones" Entry="86639" X="1865.334" Y="313.169" Z="83.95637" />
                case (int) buildings.GladiatorSanctumLvl1:
                case (int) buildings.GladiatorSanctumLvl2:
                case (int)buildings.GladiatorSanctumLvl3:
                    PnjId = alliance ? 86639 : 0;
                    ReagentId = 118043;
                    NumberReagent = 10;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                case (int) buildings.GnomishGearworksLvl1:
                case (int) buildings.GnomishGearworksLvl2:
                case (int) buildings.GnomishGearworksLvl3:
                    break;


                    // Horde default location: 5574.952" Y="4508.236" Z="129.8942
                    //Horde lvl 2 <Vendor Name="Lumber Lord Oktron" Entry="84247" Type="Repair" X="5697.096" Y="4475.479" Z="131.5005" />
                    // ally 2 : <Vendor Name="Justin Timberlord" Entry="84248" Type="Repair" X="1872.647" Y="310.0204" Z="82.61102" />
                case (int) buildings.LumberMillLvl1:
                case (int) buildings.LumberMillLvl2:
                case (int) buildings.LumberMillLvl3:
                    PnjId = alliance ? 84248 : 84247;
                    ReagentId = 114781; // Wood
                    NumberReagent = 10;
                    Pnj = alliance
                        ? new WoWPoint(1872.647, 310.0204, 82.61102)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                case (int) buildings.MageTowerLvl1:
                case (int) buildings.MageTowerLvl2:
                case (int) buildings.MageTowerLvl3:
                    break;

                    //Ally 3 : <Vendor Name="Timothy Leens" Entry="77730" Type="Repair" X="1899.896" Y="101.2778" Z="83.52704" />
                    //horde 3 : <Vendor Name="Gorsol" Entry="81688" Type="Repair" X="5467.965" Y="4449.892" Z="144.6722" />
                case (int) buildings.MineLvl1:
                case (int) buildings.MineLvl2:
                case (int) buildings.MineLvl3:
                    PnjId = alliance ? 77730 : 81688;
                    ReagentId = 115508;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1899.896, 101.2778, 83.52704)
                        : new WoWPoint(5467.965, 4449.892, 144.6722);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                    //ally <Vendor Name="Hennick Helmsley" Entry="77378" Type="Repair" X="1830.828" Y="199.172" Z="72.71624" />
                case (int) buildings.SalvageYardLvl1:
                case (int) buildings.SalvageYardLvl2:
                case (int) buildings.SalvageYardLvl3:
                    PnjId = alliance ? 77378 : 79857;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    break;

                    // Ally lvl 2 <Vendor Name="Kurt Broadoak" Entry="77777" Type="Repair" X="1817.415" Y="232.1284" Z="72.94653" />
                    // Horde lvl 2 <Vendor Name="Y'rogg" Entry="79831" Type="Repair" X="5666.928" Y="4545.664" Z="120.0819" />
                case (int) buildings.ScribeQuartersLvl1:
                case (int) buildings.ScribeQuartersLvl2:
                case (int) buildings.ScribeQuartersLvl3:
                    PnjId = alliance ? 77777 : 79831;
                    ReagentId = 114931; // Cerulean Pigment
                    NumberReagent = 2;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
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
                    
                // Horde : <Vendor Name="Turga" Entry="79863" Type="Repair" X="5643.418" Y="4507.895" Z="119.9948" />
                case (int) buildings.TailoringEmporiumLvl1:
                case (int) buildings.TailoringEmporiumLvl2:
                case (int)buildings.TailoringEmporiumLvl3:
                    if (alliance) PnjId = 77778;
                    else PnjId = 79863;
                    ReagentId = 111556; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;
                    // <Vendor Name="Kinja" Entry="79817" Type="Repair" X="5641.551" Y="4508.724" Z="119.9587" />
                case (int) buildings.TheForgeLvl1:
                case (int) buildings.TheForgeLvl2:
                case (int) buildings.TheForgeLvl3:
                    if (alliance) PnjId = 77792;
                    PnjId = 79817;
                    ReagentId = 109119; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;

                case (int) buildings.TheTanneryLvl1:
                case (int) buildings.TheTanneryLvl2:
                case (int)buildings.TheTanneryLvl3:
                    if (alliance) PnjId = 78207;
                    PnjId = 0;
                    ReagentId = 110611; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    break;


                // <Vendor Name="Trader Joseph" Entry="87208" Type="Repair" X="1892.497" Y="183.4631" Z="79.72182" />
                case (int) buildings.TradingPostLvl1:
                case (int) buildings.TradingPostLvl2:
                case (int)buildings.TradingPostLvl3:
                    break; // This one changes everyday... 
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
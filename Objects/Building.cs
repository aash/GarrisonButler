#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GarrisonLua;
using Styx;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonBuddy
{
    public class Building
    {
        public delegate bool CanCompleteOrderD();

        public delegate Task<bool> PrepOrderD();

        private static List<uint> ListDisplayIds;

        private static List<Tuple<int, WoWPoint>> plotIdToPosition = new List<Tuple<int, WoWPoint>>
        {
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)), // mine
            new Tuple<int, WoWPoint>(63, new WoWPoint(1854.596, 146.9596, 78.29183)), //garden lvl 3
            new Tuple<int, WoWPoint>(59, new WoWPoint(2010.991, 167.3314, 83.60134)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
            new Tuple<int, WoWPoint>(59, new WoWPoint(1906.121, 92.27457, 83.52486)),
        };

        private readonly String itemID;
        public List<uint> Displayids;
        private List<uint> MillableFrom = new List<uint>();
        public int NumberReagent;
        public WoWPoint Pnj;
        public int PnjId;
        public PrepOrderD PrepOrder = () => new Task<bool>(() => false);
        public int ReagentId;
        public List<int> ReagentIds;

        private String timeStart;
        private String nameShipment;
        private String plotId;
        private String _buildTime;
        private String buildingLevel;
        internal bool canActivate;
        private String canUpgrade;
        private int currencyId;
        private String duration;
        public int id;
        internal bool isBuilding;
        private String isPrebuilt;
        private String itemName;
        private String itemQuality;
        public int millItemPnj;
        public String name;
        public int rank;
        public int shipmentCapacity;
        public int shipmentsReady;

        public int shipmentsTotal;
        public String creationTime;
        public CanCompleteOrderD canCompleteOrder = () => false;
        // Settings


        public Building(bool MeIsAlliance, int id, string plotId, string buildingLevel, string name, int rank,
            bool isBuilding,
            string timeStart, string buildTime, bool canActivate, string canUpgrade, string isPrebuilt,
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

        public int NumberShipmentLeftToStart()
        {
            return shipmentCapacity - shipmentsTotal;
            // return BuildingsLua.GetNumberShipmentLeftToStart(id);
        }

        public void Refresh()
        {
            Building b = BuildingsLua.GetBuildingById(id.ToString());
            id = b.id;
            plotId = b.plotId;
            buildingLevel = b.buildingLevel;
            name = b.name;
            rank = b.rank;
            isBuilding = b.isBuilding;
            timeStart = b.timeStart;
            _buildTime = b._buildTime;
            canActivate = b.canActivate;
            canUpgrade = b.canUpgrade;
            isPrebuilt = b.isPrebuilt;
            nameShipment = b.nameShipment;
            shipmentCapacity = b.shipmentCapacity;
            shipmentsReady = b.shipmentsReady;
            shipmentsTotal = b.shipmentsTotal;
            creationTime = b.creationTime;
            duration = b.duration;
            itemName = b.itemName;
            itemQuality = b.itemQuality;
        }


        private bool canCompleteOrderItem()
        {
            long count = 0;
            WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInBags != null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] In Bags {0} - #{1}", itemInBags.Name, itemInBags.StackCount);
                count += itemInBags.StackCount;
            }

            WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == ReagentId);
            if (itemInReagentBank != null)
            {
                GarrisonBuddy.Diagnostic("[ShipmentStart] In Bank {0} - #{1}", itemInReagentBank.Name,
                    itemInReagentBank.StackCount);
                count += itemInReagentBank.StackCount;
            }

            GarrisonBuddy.Diagnostic("[ShipmentStart] Total found {0} - #{1} - needed #{2} - {3} ", ReagentId, count,
                NumberReagent, count >= NumberReagent);
            return count >= NumberReagent;
        }

        private bool canCompleteOrderItems()
        {
            foreach (int reagentId in ReagentIds)
            {
                long count = 0;
                WoWItem itemInBags = StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == reagentId);
                if (itemInBags != null)
                {
                    GarrisonBuddy.Diagnostic("[ShipmentStart] In Bags {0} - #{1}", itemInBags.Name,
                        itemInBags.StackCount);
                    count += itemInBags.StackCount;
                }

                WoWItem itemInReagentBank = StyxWoW.Me.ReagentBankItems.FirstOrDefault(i => i.Entry == reagentId);
                if (itemInReagentBank != null)
                {
                    GarrisonBuddy.Diagnostic("[ShipmentStart] In Bank {0} - #{1}", itemInReagentBank.Name,
                        itemInReagentBank.StackCount);
                    count += itemInReagentBank.StackCount;
                }

                GarrisonBuddy.Diagnostic("[ShipmentStart] Total found {0} - #{1} - needed #{2}", ReagentId, count,
                    NumberReagent);
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

            if (count >= NumberReagent)
                return true;
            return false;

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
        //            var pnj = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().FirstOrDefault(u => u.Entry == millItemPnj);
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
            Displayids = new List<uint>();
            switch (id)
            {
                    //<Vendor Name="Keyana Tone" Entry="79814" Type="Repair" X="5662.159" Y="4551.546" Z="119.9567" />
                    //Name="Peter Kearie" Entry="77791" X="1829.603" Y="201.3922" Z="72.73963" 
                case (int) buildings.AlchemyLabLvl1:
                case (int) buildings.AlchemyLabLvl2:
                case (int) buildings.AlchemyLabLvl3:
                    PnjId = alliance ? 77791 : 79814;
                    ReagentId = 109124;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15377, // Garrison Building Alchemy Level 1
                        15378, // Garrison Building Alchemy Level 2
                        15149, // Garrison Building Alchemy Level 3
                        22950, // Garrison Building Horde Alchemy V1
                        22951, // Garrison Building Horde Alchemy V2
                        22952, // Garrison Building Horde Alchemy V3
                    };
                    break;
                    // horde   <Vendor Name="Farmer Lok'lub" Entry="85048" Type="Repair" X="5541.159" Y="4516.299" Z="131.7173" />
                case (int) buildings.BarnLvl1:
                case (int) buildings.BarnLvl2:
                case (int) buildings.BarnLvl3:
                    PnjId = alliance ? 84524 : 85048;
                    ReagentIds = new List<int> {119810, 119813, 119814};
                    NumberReagent = 1;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItems;
                    Displayids = new List<uint>
                    {
                        14609, // Garrison Building Barn V1
                        14523, // Garrison Building  Level 2
                        18234, // Garrison Building  Level 3
                        18556, // Garrison Building Horde Barn V1
                        18557, // Garrison Building Horde Barn V2
                        18573, // Garrison Building Horde Barn V3
                    };
                    break;

                case (int) buildings.BarracksLvl1:
                case (int) buildings.BarracksLvl2:
                case (int) buildings.BarracksLvl3:
                    Displayids = new List<uint>
                    {
                        14398, // Garrison Building Barracks V1
                        14399, // Garrison Building Barracks V2
                        14400, // Garrison Building Barracks V3
                        18558, // Garrison Building Horde Barracks V1
                        18559, // Garrison Building Horde Barracks V2
                        18560, // Garrison Building Horde Barracks V3
                    };
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
                    Displayids = new List<uint>
                    {
                        14474, // Garrison Building Armory V1
                        14516, // Garrison Building Armory V2
                        14517, // Garrison Building Armory V3
                        18553, // Garrison Building Horde Armory V1
                        18554, // Garrison Building Horde Armory V2
                        18555, // Garrison Building Horde Armory V3
                    };
                    break;


                    // horde 1 <Vendor Name="Yukla Greenshadow" Entry="79821" Type="Repair" X="5642.186" Y="4511.771" Z="120.1076" />
                    // ally 2 <Vendor Name="Garm" Entry="77781" Type="Repair" X="1806.123" Y="188.0837" Z="70.84762" />
                case (int) buildings.EnchanterStudyLvl1:
                case (int) buildings.EnchanterStudyLvl2:
                case (int) buildings.EnchanterStudyLvl3:
                    PnjId = alliance ? 77781 : 79820;
                    ReagentId = 109693;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5645.052, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15384, // Garrison Building Enchanting Level 1
                        15385, // Garrison Building Enchanting Level 2
                        15143, // Garrison Building Enchanting Level 3
                        22966, // Garrison Building Horde Enchanting V1
                        22967, // Garrison Building Horde Enchanting V2
                        22968, // Garrison Building Horde Enchanting V3
                    };
                    break;


                    //Name="Helayn Whent" Entry="77831" X="1828.034" Y="198.3424" Z="72.75751"
                    //horde 1 <Vendor Name="Garbra Fizzwonk" Entry="86696" Type="Repair" X="5669.706" Y="4550.133" Z="120.1031" />
                case (int) buildings.EngineeringWorksLvl1:
                case (int) buildings.EngineeringWorksLvl2:
                case (int) buildings.EngineeringWorksLvl3:
                    PnjId = alliance ? 77831 : 86696;
                    ReagentId = 111366;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15142, // Garrison Building Engineering Level 3
                        15382, // Garrison Building Engineering Level 2
                        15381, // Garrison Building Engineering Level 1
                        22969, // Garrison Building Horde Engineering V1
                        22970, // Garrison Building Horde Engineering V2
                        22971, // Garrison Building Horde Engineering V3
                    };
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
                    Displayids = new List<uint>
                    {
                        20785, // Garrison Building Farm V3
                        20784, // Garrison Building Farm V2
                        20783, // Garrison Building Farm V1
                        21880, // Garrison Building Farm V3 H
                        21879, // Garrison Building Farm V2H
                        21878, // Garrison Building Farm V1H
                    };
                    break;

                    //<Name="Kaya Solasen" Entry="77775" X="1825.785" Y="196.1163" Z="72.75745" /-->
                    // horde 1 <Vendor Name="Elrondir Surrion" Entry="79830" Type="Repair" X="5649.468" Y="4509.388" Z="120.1563" />
                case (int) buildings.GemBoutiqueLvl1:
                case (int) buildings.GemBoutiqueLvl2:
                case (int) buildings.GemBoutiqueLvl3:
                    PnjId = alliance ? 77775 : 79830;
                    ReagentId = 115524;
                    NumberReagent = 5;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15390, // Garrison Building  Jewelcrafting V1
                        15391, // Garrison Building  Jewelcrafting V2
                        15145, // Garrison Building  Jewelcrafting V3
                        22975, // Garrison Building Horde Jewelcrafting V1
                        22976, // Garrison Building Horde Jewelcrafting V2
                        22977, // Garrison Building Horde Jewelcrafting V3
                    };
                    break;

                    //ally 2 <WoWUnit Name="Altar of Bones" Entry="86639" X="1865.334" Y="313.169" Z="83.95637" />
                case (int) buildings.GladiatorSanctumLvl1:
                case (int) buildings.GladiatorSanctumLvl2:
                case (int) buildings.GladiatorSanctumLvl3:
                    PnjId = alliance ? 86639 : 86639;
                    ReagentId = 118043;
                    NumberReagent = 10;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        14597, // Garrison Building Alliance Sparring Arena V1
                        14623, // Garrison Building Alliance Sparring Arena V2
                        19148, // Garrison Building Alliance Sparring Arena V3
                        18577, // Garrison Building Horde Sparring Arena V1
                        18578, // Garrison Building Horde Sparring Arena V2
                        18579, // Garrison Building Horde Sparring Arena V3
                    };
                    break;

                case (int) buildings.GnomishGearworksLvl1:
                case (int) buildings.GnomishGearworksLvl2:
                case (int) buildings.GnomishGearworksLvl3:
                    Displayids = new List<uint>
                    {
                        19149, // Garrison Building Horde Workshop V1
                        19150, // Garrison Building Horde Workshop V2
                        18580, // Garrison Building Horde Workshop V3
                        16044, // Garrison Building  Workshop V1
                        16045, // Garrison Building  Workshop V2
                        16046, // Garrison Building  Workshop V3
                    };
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
                    Displayids = new List<uint>
                    {
                        14620, // Garrison Building  Mill V1
                        14621, // Garrison Building  Mill V2
                        19145, // Garrison Building  Mill V3
                        20111, // Garrison Building Horde Mill V1
                        20112, // Garrison Building Horde Mill V2
                        20113, // Garrison Building Horde Mill V3
                    };
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
                    Displayids = new List<uint>
                    {
                        14622, // Garrison Building  Mine V1
                        14647, // Garrison Building  Mine V2
                        14648, // Garrison Building  Mine V3
                        18567, // Garrison Building Horde Mine V1
                        18568, // Garrison Building Horde Mine V2
                        18569, // Garrison Building Horde Mine V3
                    };
                    break;

                    //ally <Vendor Name="Hennick Helmsley" Entry="77378" Type="Repair" X="1830.828" Y="199.172" Z="72.71624" />
                case (int) buildings.SalvageYardLvl1:
                case (int) buildings.SalvageYardLvl2:
                case (int) buildings.SalvageYardLvl3:
                    PnjId = alliance ? 77378 : 79857;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    Displayids = new List<uint>
                    {
                        22908, // Garrison Building Salvage Tent
                        22902, // Garrison Building Alliance Salvage Tent V2
                        15363, // Garrison Building Salvage Yard V3
                        22981, // Garrison Building Horde Salvage Yard V1
                        22982, // Garrison Building Horde Salvage Yard V2
                        22983, // Garrison Building Horde Salvage Yard V3
                    };
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
                    Displayids = new List<uint>
                    {
                        15388, // Garrison Building  Inscription V1
                        15389, // Garrison Building  Inscription V2
                        15144, // Garrison Building  Inscription V3
                        22972, // Garrison Building Horde Inscription V1
                        22973, // Garrison Building Horde Inscription V2
                        22974, // Garrison Building Horde Inscription V3
                    };
                    break;

                case (int) buildings.StablesLvl1:
                case (int) buildings.StablesLvl2:
                case (int) buildings.StablesLvl3:
                    Displayids = new List<uint>
                    {
                        14625, // Garrison Building  Stable V1
                        14652, // Garrison Building  Stable V2
                        14653, // Garrison Building  Stable V3
                        18570, // Garrison Building Horde Stable V1
                        18571, // Garrison Building Horde Stable V2
                        18572, // Garrison Building Horde Stable V3
                    };
                    break;

                case (int) buildings.StorehouseLvl1:
                case (int) buildings.StorehouseLvl2:
                case (int) buildings.StorehouseLvl3:
                    break;

                    // Horde : <Vendor Name="Turga" Entry="79863" Type="Repair" X="5643.418" Y="4507.895" Z="119.9948" />
                case (int) buildings.TailoringEmporiumLvl1:
                case (int) buildings.TailoringEmporiumLvl2:
                case (int) buildings.TailoringEmporiumLvl3:
                    PnjId = alliance ? 77778 : 79863;
                    ReagentId = 111556; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15386, // Garrison Building  Tailoring V1
                        15387, // Garrison Building  Tailoring V2
                        15195, // Garrison Building  Tailoring V3
                        22987, // Garrison Building Horde Tailoring V1
                        22988, // Garrison Building Horde Tailoring V2
                        22989, // Garrison Building Horde Tailoring V3
                    };
                    break;
                    // <Vendor Name="Kinja" Entry="79817" Type="Repair" X="5641.551" Y="4508.724" Z="119.9587" />
                case (int) buildings.TheForgeLvl1:
                case (int) buildings.TheForgeLvl2:
                case (int) buildings.TheForgeLvl3:
                    PnjId = alliance ? 77792 : 79817;
                    ReagentId = 109119; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15375, // Garrison Building Blacksmith Level 1
                        15376, // Garrison Building Blacksmith Level 2
                        15194, // Garrison Building Blacksmith Level 3
                        22953, // Garrison Building Horde Blacksmith V1
                        22954, // Garrison Building Horde Blacksmith V2
                        22955, // Garrison Building Horde Blacksmith V3
                    };
                    break;

                case (int) buildings.TheTanneryLvl1:
                case (int) buildings.TheTanneryLvl2:
                case (int) buildings.TheTanneryLvl3:
                    PnjId = alliance ? 78207 : 79833;
                    ReagentId = 110611;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1816.578, 225.9814, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    canCompleteOrder = canCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15379, // Garrison Building  Leatherworking V1
                        15380, // Garrison Building  Leatherworking V2
                        15140, // Garrison Building  Leatherworking V3
                        22978, // Garrison Building Horde Leatherworking V1
                        22979, // Garrison Building Horde Leatherworking V2
                        22980, // Garrison Building Horde Leatherworking V3
                    };
                    break;


                    // <Vendor Name="Trader Joseph" Entry="87208" Type="Repair" X="1892.497" Y="183.4631" Z="79.72182" />
                case (int) buildings.TradingPostLvl1:
                case (int) buildings.TradingPostLvl2:
                case (int) buildings.TradingPostLvl3:
                    Displayids = new List<uint>
                    {
                        18574, // Garrison Building  Trading Post V1
                        18575, // Garrison Building  Trading Post V2
                        18576, // Garrison Building  Trading Post V3
                        15403, // Garrison Building Horde Trading Post V1
                        15404, // Garrison Building Horde Trading Post V2
                        20150, // Garrison Building Horde Trading Post V3
                    };
                    break; // This one changes everyday... 
            }
        }
    }

    public enum buildings
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

        FishingShackLvl1 = 64,
        FishingShackLvl2 = 134,
        FishingShackLvl3 = 135,

        EngineeringWorksLvl1 = 91,
        EngineeringWorksLvl2 = 123,
        EngineeringWorksLvl3 = 124,
    }
}
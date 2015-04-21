#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Buddy.Coroutines;
using GarrisonButler.API;
using GarrisonButler.ButlerCoroutines;
using GarrisonButler.ButlerCoroutines.AtomsLibrary;
using GarrisonButler.ButlerCoroutines.AtomsLibrary.Garrison;
using GarrisonButler.Config;
using GarrisonButler.Libraries;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

#endregion

namespace GarrisonButler
{
    public class Building
    {
        public delegate Task<Result> CanCompleteOrderD();

        public delegate Task<Result> PrepOrderD(int numberToStart);

        private readonly String _itemId;
        public List<uint> Displayids;
        public int NumberReagent;
        public WoWPoint Pnj;
        public int PnjId;
        public List<uint> PnjIds;
        public PrepOrderD PrepOrder;
        public Atom PrepOrderAtom;
        public uint ReagentId;
        public List<uint> ReagentIds;
        private String _buildTime;
        private String _buildingLevel;
        internal bool CanActivate;

        public CanCompleteOrderD CanCompleteOrder =
            () => { return new Task<Result>(() => new Result(ActionResult.Done, 0)); };

        public Func<int> maxCanComplete = () => { return 0; }; 

        private String _canUpgrade;
        public String CreationTime;
        private int _currencyId;
        private String _duration;
        public int Id;
        internal bool IsBuilding;
        private String _isPreBuilt;
        private String _itemName;
        private String _itemQuality;
        public int MillItemPnj;
        public String Name;
        private String _nameShipment;
        public int PlotId;
        public int Rank;
        public int ShipmentCapacity;
        public int ShipmentsReady;

        public int ShipmentsTotal;
        private String _timeStart;

        public int WorkFrameWorkAroundTries = 0;
        public int GossipFrameTries = 0;
        public int StartWorkOrderTries = 0;
        public const int GossipFrameMaxTries = 15;
        public const int WorkFrameWorkAroundMaxTriesUntilBlacklist = 10;
        public const int StartWorkOrderMaxTries = 5;
        // Settings

        public List<uint> buildingIDs { get; set; }

        public Building(bool meIsAlliance, int id, int plotId, string buildingLevel, string name, int rank,
            bool isBuilding,
            string timeStart, string buildTime, bool canActivate, string canUpgrade, string isPreBuilt,
            string nameShipment, int shipmentCapacity, int shipmentsReady, int shipmentsTotal,
            string creationTime, string duration, string itemName, string itemQuality, string itemId)
        {
            Id = id;
            PlotId = plotId;
            _buildingLevel = buildingLevel;
            Name = name;
            Rank = rank;
            IsBuilding = isBuilding;
            _timeStart = timeStart;
            _buildTime = buildTime;
            CanActivate = canActivate;
            _canUpgrade = canUpgrade;
            _isPreBuilt = isPreBuilt;
            _nameShipment = nameShipment;
            ShipmentCapacity = shipmentCapacity;
            ShipmentsReady = shipmentsReady;
            ShipmentsTotal = shipmentsTotal;
            CreationTime = creationTime;
            _duration = duration;
            _itemName = itemName;
            _itemQuality = itemQuality;
            _itemId = itemId ?? "None";
            GetOrderInfo(meIsAlliance);
            NumberShipmentLeftToStart = ShipmentCapacity - ShipmentsTotal;

            //GarrisonButler.Diagnostic(ToString());
        }

        public int workFrameWorkAroundTries { get; set; }

        public int NumberShipmentLeftToStart { get; private set; }

        public override string ToString()
        {
            return
                string.Format(
                    "Id: {0}, PlotId: {1}, BuildingLevel: {2}, Name: {3}, Rank: {4}, IsBuilding: {5}, TimeStart: {6}, BuildTime: {7}, CanActivate: {8}, CanUpgrade: {9}, IsPrebuilt: {10}, NameShipment: {11}, ShipmentCapacity: {12}, ShipmentsReady: {13}, ShipmentsTotal: {14}, CreationTime: {15}, Duration: {16}, ItemName: {17}, ItemQuality: {18}, ItemId: {19}",
                    Id, PlotId, _buildingLevel, Name, Rank, IsBuilding, _timeStart, _buildTime, CanActivate, _canUpgrade,
                    _isPreBuilt, _nameShipment, ShipmentCapacity, ShipmentsReady, ShipmentsTotal, CreationTime,
                    _duration,
                    _itemName, _itemQuality, _itemId);
        }

        public void RefreshOrders(int running, int capacity)
        {
            GarrisonButler.Diagnostic("[Building] Refreshing {0}, running={1}, total={2}", Name, capacity - running,
                running);
            NumberShipmentLeftToStart = capacity - running;
            ShipmentsTotal = running;
        }

        public void Refresh()
        {
            GarrisonButler.Diagnostic("Refreshing " + Name);

            var b = BuildingsLua.GetBuildingById(Id.ToString(CultureInfo.CurrentCulture));
            Id = b.Id;
            PlotId = b.PlotId;
            _buildingLevel = b._buildingLevel;
            Name = b.Name;
            Rank = b.Rank;
            IsBuilding = b.IsBuilding;
            _timeStart = b._timeStart;
            _buildTime = b._buildTime;
            CanActivate = b.CanActivate;
            _canUpgrade = b._canUpgrade;
            _isPreBuilt = b._isPreBuilt;
            _nameShipment = b._nameShipment;
            ShipmentCapacity = b.ShipmentCapacity;
            ShipmentsReady = b.ShipmentsReady;
            ShipmentsTotal = b.ShipmentsTotal;
            CreationTime = b.CreationTime;
            _duration = b._duration;
            _itemName = b._itemName;
            _itemQuality = b._itemQuality;

            NumberShipmentLeftToStart = ShipmentCapacity - ShipmentsTotal;
        }

        public bool IsActionForRefreshNeeded()
        {
            switch (Id)
            {
                case (int) Buildings.TradingPostLvl1:
                case (int) Buildings.TradingPostLvl2:
                case (int) Buildings.TradingPostLvl3:
                    // Before being able to calculate it, we need to know what's today's reagent.
                    // It can be saved in settings with the date.
                    var serverTimeLua = ButlerLua.GetServerCurrentDate();
                    var secBeforeReset = TimeSpan.FromSeconds(ButlerLua.GetTimeBeforeServerResetInSec());
                    var nextReset = serverTimeLua + secBeforeReset;
                    var lastReset = nextReset - TimeSpan.FromHours(24);

                    if (GaBSettings.Get().LastCheckTradingPost == default(DateTime)
                        || GaBSettings.Get().LastCheckTradingPost < lastReset)
                    {
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
        
        private async Task<Result> CanCompleteOrderItem()
        {
            // add number in bags
            var count = HbApi.GetNumberItemInBags(ReagentId);
            // add number in reagent banks
            count += HbApi.GetNumberItemInReagentBank(ReagentId);

            //GarrisonButler.Diagnostic(
            //    "[CanCompleteOrderItem,{4}] Total found {0} - #{1} - needed #{2} - ID: {3} - max: {4}", ReagentId,
            //    count, NumberReagent, count >= NumberReagent, Id, (int)(count / NumberReagent));

            return new Result(ActionResult.Done, (int)(count / NumberReagent));
        }

        private int MaxCanCompleteItem()
        {
            // add number in bags
            var count = HbApi.GetNumberItemInBags(ReagentId);
            // add number in reagent banks
            count += HbApi.GetNumberItemInReagentBank(ReagentId);

            //GarrisonButler.Diagnostic(
            //    "[CanCompleteOrderItem,{4}] Total found {0} - #{1} - needed #{2} - ID: {3} - max: {4}", ReagentId,
            //    count, NumberReagent, count >= NumberReagent, Id, (int)(count / NumberReagent));

            return (int)(count / NumberReagent);
        }

        private int MaxCanCompleteItems()
        {
            var maxCanStart = 0;

            foreach (var reagentId in ReagentIds)
            {
                // add number in bags
                var count = HbApi.GetNumberItemInBags(reagentId);
                // add number in reagent banks
                count += HbApi.GetNumberItemInReagentBank(reagentId);

                //GarrisonButler.Diagnostic("[CanCompleteOrderItems,{3}] Total found {0} - #{1} - needed #{2}", reagentId,
                //    count,
                //    NumberReagent, Id);
                if (count < NumberReagent)
                {
                    maxCanStart = 0;
                    break;
                }
                if (maxCanStart == 0)
                    maxCanStart = (int)count / NumberReagent;
                else if (count >= NumberReagent)
                    maxCanStart = Math.Min((int)count / NumberReagent, maxCanStart);
            }

            //GarrisonButler.Diagnostic("[CanCompleteOrderItems] The max that can be started is {0}: {1}", maxCanStart,
            //    Name);
            return maxCanStart;
        }

        private int MaxCanCompleteOneOfItems()
        {
            var maxCanStart = 0;
            foreach (var reagentId in ReagentIds)
            {
                // add number in bags
                var count = HbApi.GetNumberItemInBags(reagentId);
                // add number in reagent banks
                count += HbApi.GetNumberItemInReagentBank(reagentId);

                //GarrisonButler.Diagnostic("[CanCompleteOrderOneOfItems] Total found {0} - #{1} - needed #{2}", reagentId,
                //    count,
                //    NumberReagent);
                if (count >= NumberReagent)
                    maxCanStart = Math.Max((int)count / NumberReagent, maxCanStart);
            }
            return maxCanStart;
        }

        private int MaxCanCompleteCurrency()
        {
            return BuildingsLua.GetGarrisonRessources() / NumberReagent;
        }

        private int MaxCanCompleteMillable()
        {
            var inscription = StyxWoW.Me.GetSkill(SkillLine.Inscription);
            var mortar = HbApi.GetItemInBags(114942).FirstOrDefault();
            var oreInBags = HbApi.GetNumberItemInBags(109118);
            var oreInBank = HbApi.GetNumberItemInReagentBank(109118);
            if ((inscription == null || inscription.CurrentValue <= 0)
                && (mortar == default(WoWItem) || mortar.StackCount <= 0)
                && (oreInBags + oreInBank < 5))
                return MaxCanCompleteItem();

            // get number in bags
            var count = HbApi.GetNumberItemInBags(ReagentId);

            // add number in reagent banks
            count += HbApi.GetNumberItemInReagentBank(ReagentId);

            GarrisonButler.Diagnostic(
                "[CanCompleteOrderMillable] Sub Total without preProcessing found {0} - #{1} - needed #{2} - {3} ",
                ReagentId,
                count,
                NumberReagent, count >= NumberReagent);

            count += HbApi.GetNumberItemByMillingBags(ReagentId, GaBSettings.Get().Pigments.GetEmptyIfNull().ToList());

            GarrisonButler.Diagnostic(
                "[CanCompleteOrderMillable] Total found with milling {0} - #{1} - needed #{2} - {3} ",
                ReagentId, count,
                NumberReagent, count >= NumberReagent);

            return (int)count / NumberReagent;
        }

        private async Task<Result> CanCompleteOrderItems()
        {
            var maxCanStart = 0;

            foreach (var reagentId in ReagentIds)
            {
                // add number in bags
                var count = HbApi.GetNumberItemInBags(reagentId);
                // add number in reagent banks
                count += HbApi.GetNumberItemInReagentBank(reagentId);

                //GarrisonButler.Diagnostic("[CanCompleteOrderItems,{3}] Total found {0} - #{1} - needed #{2}", reagentId,
                    //count,
                    //NumberReagent, Id);
                if (count < NumberReagent)
                {
                    maxCanStart = 0;
                    break;
                }
                if (maxCanStart == 0)
                    maxCanStart = (int) count/NumberReagent;
                else if (count >= NumberReagent)
                    maxCanStart = Math.Min((int) count/NumberReagent, maxCanStart);
            }

            //GarrisonButler.Diagnostic("[CanCompleteOrderItems] The max that can be started is {0}: {1}", maxCanStart,
            //    Name);
            return new Result(ActionResult.Done, maxCanStart);
        }

        private async Task<Result> CanCompleteOrderOneOfItems()
        {
            var maxCanStart = 0;
            foreach (var reagentId in ReagentIds)
            {
                // add number in bags
                var count = HbApi.GetNumberItemInBags(reagentId);
                // add number in reagent banks
                count += HbApi.GetNumberItemInReagentBank(reagentId);

                //GarrisonButler.Diagnostic("[CanCompleteOrderOneOfItems] Total found {0} - #{1} - needed #{2}", reagentId,
                    //count,
                    //NumberReagent);
                if (count >= NumberReagent)
                    maxCanStart = Math.Max((int) count/NumberReagent, maxCanStart);
            }
            return new Result(ActionResult.Done, maxCanStart);
        }

        private async Task<Result> CanCompleteOrderCurrency()
        {
            return new Result(ActionResult.Done, BuildingsLua.GetGarrisonRessources()/NumberReagent);
        }

        private async Task<Result> CanCompleteOrderMillable()
        {
            var inscription = StyxWoW.Me.GetSkill(SkillLine.Inscription);
            var mortar = HbApi.GetItemInBags(114942).FirstOrDefault();
            var oreInBags = HbApi.GetNumberItemInBags(109118);
            var oreInBank = HbApi.GetNumberItemInReagentBank(109118);
            if ((inscription == null || inscription.CurrentValue <= 0)
                && (mortar == default(WoWItem) || mortar.StackCount <= 0)
                && (oreInBags + oreInBank < 5))
                return await CanCompleteOrderItem();

            // get number in bags
            var count = HbApi.GetNumberItemInBags(ReagentId);

            // add number in reagent banks
            count += HbApi.GetNumberItemInReagentBank(ReagentId);

            //GarrisonButler.Diagnostic(
            //    "[CanCompleteOrderMillable] Sub Total without preProcessing found {0} - #{1} - needed #{2} - {3} ",
            //    ReagentId,
            //    count,
            //    NumberReagent, count >= NumberReagent);

            count += HbApi.GetNumberItemByMillingBags(ReagentId, GaBSettings.Get().Pigments.GetEmptyIfNull().ToList());

            //GarrisonButler.Diagnostic(
            //    "[CanCompleteOrderMillable] Total found with milling {0} - #{1} - needed #{2} - {3} ",
            //    ReagentId, count,
            //    NumberReagent, count >= NumberReagent);

            return new Result(ActionResult.Done, (int) count/NumberReagent);
        }


        private async Task<Result> MillBeforeOrder(int numberToStart)
        {
            // Do we need to mill
            // get number in bags
            var count = HbApi.GetNumberItemInBags(ReagentId);
            // add number in reagent banks
            count += HbApi.GetNumberItemInReagentBank(ReagentId);

            if (count/NumberReagent >= numberToStart)
                return new Result(ActionResult.Done);

            // We need to mill
            // If we don't have inscription check if we have mortar in bags
            var inscription = StyxWoW.Me.GetSkill(SkillLine.Inscription);
            if (inscription == null || inscription.CurrentValue <= 0)
            {
                // We need mortar
                var millingItem = HbApi.GetItemInBags(114942).FirstOrDefault();
                if (millingItem == default(WoWItem) || millingItem.StackCount <= 0)
                {
                    // We don't have a mortar in bag, let's craft one
                    // We need 5 blackrock ore
                    var oreInBags = HbApi.GetNumberItemInBags(109118);
                    var oreInBank = HbApi.GetNumberItemInReagentBank(109118);
                    if (oreInBags + oreInBank < 5)
                    {
                        GarrisonButler.Diagnostic(
                            "[MillBeforeOrder] Inscription profession not found and not enough Blackrock ore to craft a mortar. InBags={0}, InReagentBank={1}.",
                            oreInBags, oreInBank);
                        return new Result(ActionResult.Failed);
                    }

                    // Enough ore, moving to vendor and crafting

                    // Moving to vendor
                    // Alliance - Eric Broadoak, ID: 77372
                    // Horde - ID: http://www.wowhead.com/npc=79829/urgra

                    var unit = ObjectManager.GetObjectsOfTypeFast<WoWUnit>().GetEmptyIfNull()
                        .FirstOrDefault(u => u.Entry == (StyxWoW.Me.IsAlliance ? 77372 : 79829));

                    if (unit == null)
                    {
                        await
                            ButlerCoroutine.MoveTo(Pnj,
                                String.Format(
                                    "[MillBeforeOrder,{0}] Could not find unit ({1}), moving to default location.",
                                    Id, PnjId));
                        return new Result(ActionResult.Running);
                    }

                    if ((await ButlerCoroutine.MoveToInteract(unit)).State == ActionResult.Running)
                        return new Result(ActionResult.Running);

                    unit.Interact();

                    // Crafting
                    if (!(await ButlerLua.IsTradeSkillFrameOpen()))
                    {
                        GarrisonButler.Diagnostic("[MillBeforeOrder] TradeSkillFrame not open.");
                        return new Result(ActionResult.Running);
                    }

                    if (!await ButlerLua.CraftDraenicMortar())
                    {
                        GarrisonButler.Diagnostic("[MillBeforeOrder] CraftDraenicMortar returned false.");
                        return new Result(ActionResult.Running);
                    }

                    // wait for cast
                    await Coroutine.Wait(10000, () =>
                    {
                        ObjectManager.Update();
                        return HbApi.GetNumberItemInBags(114942) > 0;
                    });
                    await CommonCoroutines.SleepForLagDuration();

                    // Final check for item in bags
                    millingItem = HbApi.GetItemInBags(114942).FirstOrDefault();
                    if (millingItem == default(WoWItem) || millingItem.StackCount <= 0)
                    {
                        GarrisonButler.Diagnostic("[MillBeforeOrder] No Draenic mortar in bags after crafting it.");
                        return new Result(ActionResult.Failed);
                    }
                    return new Result(ActionResult.Running);
                }
            }


            // Mill until we don't need anymore
            var millable = HbApi.GetAllItemsToMillFrom(ReagentId, GaBSettings.Get().Pigments).ToArray();
            if (millable.Any() && count/NumberReagent < numberToStart)
            {
                await millable.First().Mill();
                return new Result(ActionResult.Running);
            }

            if (count/NumberReagent >= numberToStart)
                return new Result(ActionResult.Done);

            return new Result(ActionResult.Failed);
        }

        private void GetOrderInfo(bool alliance)
        {
            PnjId = 0;
            ReagentId = 0;
            NumberReagent = 0;
            _currencyId = 0;
            Pnj = new WoWPoint();
            Displayids = new List<uint>();
            buildingIDs = new List<uint>();
            switch (Id)
            {
                //<Vendor Name="Keyana Tone" Entry="79814" Type="Repair" X="5662.159" Y="4551.546" Z="119.9567" />
                //Name="Peter Kearie" Entry="77791" X="1829.603" Y="201.3922" Z="72.73963" 
                case (int) Buildings.AlchemyLabLvl1:
                case (int) Buildings.AlchemyLabLvl2:
                case (int) Buildings.AlchemyLabLvl3:
                    PnjId = alliance ? 77791 : 79814;
                    ReagentId = 109124;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15377, // Garrison Building Alchemy Level 1
                        15378, // Garrison Building Alchemy Level 2
                        15149, // Garrison Building Alchemy Level 3
                        22950, // Garrison Building Horde Alchemy V1
                        22951, // Garrison Building Horde Alchemy V2
                        22952 // Garrison Building Horde Alchemy V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230443,
                        227179,
                        230444,
                        227590,
                        230445,
                        227591
                    };
                    break;

                // horde   <Vendor Name="Farmer Lok'lub" Entry="85048" Type="Repair" X="5541.159" Y="4516.299" Z="131.7173" />
                case (int) Buildings.BarnLvl1:
                case (int) Buildings.BarnLvl2:
                case (int) Buildings.BarnLvl3:
                    PnjId = alliance ? 84524 : 85048;
                    ReagentIds = new List<uint> {119810, 119813, 119814, 119815, 119817, 119819};
                    NumberReagent = 1;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteOneOfItems;
                    CanCompleteOrder = CanCompleteOrderOneOfItems;
                    Displayids = new List<uint>
                    {
                        14609, // Garrison Building Barn V1
                        14523, // Garrison Building  Level 2
                        18234, // Garrison Building  Level 3
                        18556, // Garrison Building Horde Barn V1
                        18557, // Garrison Building Horde Barn V2
                        18573 // Garrison Building Horde Barn V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230410,
                        224795,
                        230411,
                        224796,
                        233188,
                        233186
                    };
                    break;

                case (int) Buildings.BarracksLvl1:
                case (int) Buildings.BarracksLvl2:
                case (int) Buildings.BarracksLvl3:
                    Displayids = new List<uint>
                    {
                        14398, // Garrison Building Barracks V1
                        14399, // Garrison Building Barracks V2
                        14400, // Garrison Building Barracks V3
                        18558, // Garrison Building Horde Barracks V1
                        18559, // Garrison Building Horde Barracks V2
                        18560 // Garrison Building Horde Barracks V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230412,
                        224797,
                        230413,
                        224798,
                        230414,
                        224799
                    };
                    break;


                //horde 2: <Vendor Name="Magrish" Entry="89066" Type="Repair" X="5569.239" Y="4462.448" Z="132.5624" />
                //ally 2: <Vendor Name="Dalana Clarke" Entry="89065" Type="Repair" X="1924.622" Y="225.1501" Z="76.96214" />
                case (int) Buildings.DwarvenBunkerLvl1:
                case (int) Buildings.DwarvenBunkerLvl2:
                case (int) Buildings.DwarvenBunkerLvl3:
                    PnjId = alliance ? 89065 : 89066;
                    _currencyId = 824;
                    NumberReagent = 20;
                    Pnj = alliance
                        ? new WoWPoint(1924.622, 225.1501, 76.96214)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteCurrency;
                    CanCompleteOrder = CanCompleteOrderCurrency;
                    Displayids = new List<uint>
                    {
                        14474, // Garrison Building Armory V1
                        14516, // Garrison Building Armory V2
                        14517, // Garrison Building Armory V3
                        18553, // Garrison Building Horde Armory V1
                        18554, // Garrison Building Horde Armory V2
                        18555 // Garrison Building Horde Armory V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230406,
                        224548,
                        230407,
                        224549,
                        230409,
                        224550
                    };
                    break;

                case (int) Buildings.InnTavernLvl1:
                case (int) Buildings.InnTavernLvl2:
                case (int) Buildings.InnTavernLvl3:
                    //TODO - Need display ids for tavern for alliance / horde
                    //Displayids = new List<uint>
                    //{
                    //    14474, // Garrison Building Armory V1
                    //    14516, // Garrison Building Armory V2
                    //    14517, // Garrison Building Armory V3
                    //    18553, // Garrison Building Horde Armory V1
                    //    18554, // Garrison Building Horde Armory V2
                    //    18555, // Garrison Building Horde Armory V3
                    //};
                    buildingIDs = new List<uint>
                    {
                        230416,
                        224805,
                        230417,
                        224806,
                        230418,
                        224807
                    };
                    break;


                // horde 1 <Vendor Name="Yukla Greenshadow" Entry="79821" Type="Repair" X="5642.186" Y="4511.771" Z="120.1076" />
                // ally 2 <Vendor Name="Garm" Entry="77781" Type="Repair" X="1806.123" Y="188.0837" Z="70.84762" />
                case (int) Buildings.EnchanterStudyLvl1:
                case (int) Buildings.EnchanterStudyLvl2:
                case (int) Buildings.EnchanterStudyLvl3:
                    PnjId = alliance ? 77781 : 79820;
                    ReagentId = 109693;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5645.052, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15384, // Garrison Building Enchanting Level 1
                        15385, // Garrison Building Enchanting Level 2
                        15143, // Garrison Building Enchanting Level 3
                        22966, // Garrison Building Horde Enchanting V1
                        22967, // Garrison Building Horde Enchanting V2
                        22968 // Garrison Building Horde Enchanting V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230451,
                        227073,
                        230452,
                        227596,
                        230453,
                        227597
                    };
                    break;


                //Name="Helayn Whent" Entry="77831" X="1828.034" Y="198.3424" Z="72.75751"
                //horde 1 <Vendor Name="Garbra Fizzwonk" Entry="86696" Type="Repair" X="5669.706" Y="4550.133" Z="120.1031" />
                case (int) Buildings.EngineeringWorksLvl1:
                case (int) Buildings.EngineeringWorksLvl2:
                case (int) Buildings.EngineeringWorksLvl3:
                    PnjId = alliance ? 77831 : 86696;
                    ReagentIds = new List<uint>
                    {
                        109118, // Blackrock Ore
                        109119 // True Iron Ore
                    };
                    NumberReagent = 2;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItems;
                    CanCompleteOrder = CanCompleteOrderItems;
                    Displayids = new List<uint>
                    {
                        15142, // Garrison Building Engineering Level 3
                        15382, // Garrison Building Engineering Level 2
                        15381, // Garrison Building Engineering Level 1
                        22969, // Garrison Building Horde Engineering V1
                        22970, // Garrison Building Horde Engineering V2
                        22971 // Garrison Building Horde Engineering V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230454,
                        227072,
                        230455,
                        227594,
                        230456,
                        227595
                    };
                    break;

                //ally lvl 2 : <Vendor Name="Olly Nimkip" Entry="85514" Type="Repair" X="1862.214" Y="140" Z="78.29137" />
                //horde lvl 2 : <Vendor Name="Nali Softsoil" Entry="85783" Type="Repair" X="5410.738" Y="4568.479" Z="138.3254" />
                case (int) Buildings.GardenLvl1:
                case (int) Buildings.GardenLvl2:
                case (int) Buildings.GardenLvl3:
                    PnjId = alliance ? 85514 : 85783;
                    ReagentId = 116053;
                    NumberReagent = 5;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        20785, // Garrison Building Farm V3
                        20784, // Garrison Building Farm V2
                        20783, // Garrison Building Farm V1
                        21880, // Garrison Building Farm V3 H
                        21879, // Garrison Building Farm V2H
                        21878 // Garrison Building Farm V1H
                    };
                    buildingIDs = new List<uint>
                    {
                        230415,
                        224800,
                        236448,
                        235990,
                        236449,
                        235991
                    };
                    break;

                //<Name="Kaya Solasen" Entry="77775" X="1825.785" Y="196.1163" Z="72.75745" /-->
                // horde 1 <Vendor Name="Elrondir Surrion" Entry="79830" Type="Repair" X="5649.468" Y="4509.388" Z="120.1563" />
                case (int) Buildings.GemBoutiqueLvl1:
                case (int) Buildings.GemBoutiqueLvl2:
                case (int) Buildings.GemBoutiqueLvl3:
                    PnjId = alliance ? 77775 : 79830;
                    ReagentId = 109118;
                    NumberReagent = 5;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5630.081, 4526.252, 119.2066);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15390, // Garrison Building  Jewelcrafting V1
                        15391, // Garrison Building  Jewelcrafting V2
                        15145, // Garrison Building  Jewelcrafting V3
                        22975, // Garrison Building Horde Jewelcrafting V1
                        22976, // Garrison Building Horde Jewelcrafting V2
                        22977 // Garrison Building Horde Jewelcrafting V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230460,
                        227075,
                        230461,
                        227602,
                        230462,
                        227603
                    };
                    break;

                //ally 2 <WoWUnit Name="Altar of Bones" Entry="86639" X="1865.334" Y="313.169" Z="83.95637" />
                case (int) Buildings.GladiatorSanctumLvl1:
                case (int) Buildings.GladiatorSanctumLvl2:
                case (int) Buildings.GladiatorSanctumLvl3:
                    PnjId = 86639;
                    ReagentId = 118043;
                    NumberReagent = 10;
                    Pnj = alliance ? new WoWPoint(1862.214, 140, 78.29137) : new WoWPoint(5410.738, 4568.479, 138.3254);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        14597, // Garrison Building Alliance Sparring Arena V1
                        14623, // Garrison Building Alliance Sparring Arena V2
                        19148, // Garrison Building Alliance Sparring Arena V3
                        18577, // Garrison Building Horde Sparring Arena V1
                        18578, // Garrison Building Horde Sparring Arena V2
                        18579 // Garrison Building Horde Sparring Arena V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230477,
                        230480,
                        230478,
                        230486,
                        230479,
                        230487
                    };
                    break;

                case (int) Buildings.GnomishGearworksLvl1:
                case (int) Buildings.GnomishGearworksLvl2:
                case (int) Buildings.GnomishGearworksLvl3:
                    Displayids = new List<uint>
                    {
                        19149, // Garrison Building Horde Workshop V1
                        19150, // Garrison Building Horde Workshop V2
                        18580, // Garrison Building Horde Workshop V3
                        16044, // Garrison Building  Workshop V1
                        16045, // Garrison Building  Workshop V2
                        16046 // Garrison Building  Workshop V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230489,
                        230492,
                        230490,
                        230493,
                        230491,
                        230494
                    };
                    break;


                // Horde default location: 5574.952" Y="4508.236" Z="129.8942
                //Horde lvl 2 <Vendor Name="Lumber Lord Oktron" Entry="84247" Type="Repair" X="5697.096" Y="4475.479" Z="131.5005" />
                // ally 2 : <Vendor Name="Justin Timberlord" Entry="84248" Type="Repair" X="1872.647" Y="310.0204" Z="82.61102" />
                case (int) Buildings.LumberMillLvl1:
                case (int) Buildings.LumberMillLvl2:
                case (int) Buildings.LumberMillLvl3:
                    PnjId = alliance ? 84248 : 84247;
                    ReagentId = 114781; // Wood
                    NumberReagent = 10;
                    Pnj = alliance
                        ? new WoWPoint(1872.647, 310.0204, 82.61102)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        14620, // Garrison Building  Mill V1
                        14621, // Garrison Building  Mill V2
                        19145, // Garrison Building  Mill V3
                        20111, // Garrison Building Horde Mill V1
                        20112, // Garrison Building Horde Mill V2
                        20113 // Garrison Building Horde Mill V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230422,
                        224811,
                        230423,
                        224812,
                        233266,
                        233267
                    };
                    break;

                case (int) Buildings.MageTowerLvl1:
                case (int) Buildings.MageTowerLvl2:
                case (int) Buildings.MageTowerLvl3:
                    buildingIDs = new List<uint>
                    {
                        230419,
                        224808,
                        230420,
                        224809,
                        230421,
                        224810
                    };
                    break;

                //Ally 3 : <Vendor Name="Timothy Leens" Entry="77730" Type="Repair" X="1899.896" Y="101.2778" Z="83.52704" />
                //horde 3 : <Vendor Name="Gorsol" Entry="81688" Type="Repair" X="5467.965" Y="4449.892" Z="144.6722" />
                case (int) Buildings.MineLvl1:
                case (int) Buildings.MineLvl2:
                case (int) Buildings.MineLvl3:
                    PnjId = alliance ? 77730 : 81688;
                    ReagentId = 115508;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1899.896, 101.2778, 83.52704)
                        : new WoWPoint(5467.965, 4449.892, 144.6722);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        14622, // Garrison Building  Mine V1
                        14647, // Garrison Building  Mine V2
                        14648, // Garrison Building  Mine V3
                        18567, // Garrison Building Horde Mine V1
                        18568, // Garrison Building Horde Mine V2
                        18569 // Garrison Building Horde Mine V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230466,
                        225538,
                        230467,
                        225539,
                        230468,
                        225540
                    };
                    break;

                //ally <Vendor Name="Hennick Helmsley" Entry="77378" Type="Repair" X="1830.828" Y="199.172" Z="72.71624" />
                case (int) Buildings.SalvageYardLvl1:
                case (int) Buildings.SalvageYardLvl2:
                case (int) Buildings.SalvageYardLvl3:
                    PnjId = alliance ? 77378 : 79857;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5630.081, 4526.252, 119.2066);
                    Displayids = new List<uint>
                    {
                        22908, // Garrison Building Salvage Tent
                        22902, // Garrison Building Alliance Salvage Tent V2
                        15363, // Garrison Building Salvage Yard V3
                        22981, // Garrison Building Horde Salvage Yard V1
                        22982, // Garrison Building Horde Salvage Yard V2
                        22983 // Garrison Building Horde Salvage Yard V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230440,
                        224853,
                        230441,
                        230475,
                        230442,
                        230476
                    };
                    break;

                // Ally lvl 2 <Vendor Name="Kurt Broadoak" Entry="77777" Type="Repair" X="1817.415" Y="232.1284" Z="72.94653" />
                // Horde lvl 2 <Vendor Name="Y'rogg" Entry="79831" Type="Repair" X="5666.928" Y="4545.664" Z="120.0819" />
                case (int) Buildings.ScribeQuartersLvl1:
                case (int) Buildings.ScribeQuartersLvl2:
                case (int) Buildings.ScribeQuartersLvl3:
                    PnjId = alliance ? 77777 : 79831;
                    ReagentId = 114931; // Cerulean Pigment
                    NumberReagent = 2;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    if (GarrisonButler.IsIceVersion())
                    {
                        maxCanComplete = MaxCanCompleteMillable;
                        CanCompleteOrder = CanCompleteOrderMillable;
                        PrepOrder = MillBeforeOrder;
                        //PrepOrderAtom = new MillBeforeOrder(this, () =>
                        //{
                        //    var maxSettings = GaBSettings.Get().GetBuildingSettings(Id).MaxCanStartOrder;
                        //    var maxInProgress = maxSettings == 0
                        //        ? ShipmentCapacity
                        //        : Math.Min(ShipmentCapacity,
                        //            maxSettings);
                        //    var maxToStart = maxInProgress - ShipmentsTotal;
                        //    var maxCanCompletee = maxCanComplete();
                        //    maxToStart = Math.Min(maxCanCompletee, maxToStart);
                        //    return maxToStart;
                        //});
                            MillItemPnj = 77372;
                    }
                    else
                    {
                        maxCanComplete = MaxCanCompleteItem;
                        CanCompleteOrder = CanCompleteOrderItem;
                    }
                    // <Vendor Name="Eric Broadoak" Entry="77372" Type="Repair" X="1817.415" Y="232.1284" Z="72.94568" />
                    Displayids = new List<uint>
                    {
                        15388, // Garrison Building  Inscription V1
                        15389, // Garrison Building  Inscription V2
                        15144, // Garrison Building  Inscription V3
                        22972, // Garrison Building Horde Inscription V1
                        22973, // Garrison Building Horde Inscription V2
                        22974 // Garrison Building Horde Inscription V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230427,
                        227074,
                        230430,
                        227600,
                        230432,
                        227601
                    };
                    break;

                case (int) Buildings.StablesLvl1:
                case (int) Buildings.StablesLvl2:
                case (int) Buildings.StablesLvl3:
                    Displayids = new List<uint>
                    {
                        14625, // Garrison Building  Stable V1
                        14652, // Garrison Building  Stable V2
                        14653, // Garrison Building  Stable V3
                        18570, // Garrison Building Horde Stable V1
                        18571, // Garrison Building Horde Stable V2
                        18572 // Garrison Building Horde Stable V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230469,
                        225577,
                        230470,
                        225578,
                        230471,
                        225579
                    };
                    break;

                case (int) Buildings.StorehouseLvl1:
                case (int) Buildings.StorehouseLvl2:
                case (int) Buildings.StorehouseLvl3:
                    buildingIDs = new List<uint>
                    {
                        230437,
                        224854,
                        230438,
                        234678,
                        230439,
                        234679
                    };
                    break;

                // Horde : <Vendor Name="Turga" Entry="79863" Type="Repair" X="5643.418" Y="4507.895" Z="119.9948" />
                case (int) Buildings.TailoringEmporiumLvl1:
                case (int) Buildings.TailoringEmporiumLvl2:
                case (int) Buildings.TailoringEmporiumLvl3:
                    PnjId = alliance ? 77778 : 79863;
                    ReagentId = 111557; // Sumptuous fur
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15386, // Garrison Building  Tailoring V1
                        15387, // Garrison Building  Tailoring V2
                        15195, // Garrison Building  Tailoring V3
                        22987, // Garrison Building Horde Tailoring V1
                        22988, // Garrison Building Horde Tailoring V2
                        22989 // Garrison Building Horde Tailoring V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230463,
                        227180,
                        230464,
                        227598,
                        230465,
                        227599
                    };
                    break;
                // <Vendor Name="Kinja" Entry="79817" Type="Repair" X="5641.551" Y="4508.724" Z="119.9587" />
                case (int) Buildings.TheForgeLvl1:
                case (int) Buildings.TheForgeLvl2:
                case (int) Buildings.TheForgeLvl3:
                    PnjId = alliance ? 77792 : 79817;
                    ReagentId = 109119; // True iron ore
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1830.828, 199.172, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15375, // Garrison Building Blacksmith Level 1
                        15376, // Garrison Building Blacksmith Level 2
                        15194, // Garrison Building Blacksmith Level 3
                        22953, // Garrison Building Horde Blacksmith V1
                        22954, // Garrison Building Horde Blacksmith V2
                        22955 // Garrison Building Horde Blacksmith V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230448,
                        225537,
                        230449,
                        227588,
                        230450,
                        227589
                    };
                    break;

                case (int) Buildings.TheTanneryLvl1:
                case (int) Buildings.TheTanneryLvl2:
                case (int) Buildings.TheTanneryLvl3:
                    PnjId = alliance ? 78207 : 79833;
                    ReagentId = 110609;
                    NumberReagent = 5;
                    Pnj = alliance
                        ? new WoWPoint(1816.578, 225.9814, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteItem;
                    CanCompleteOrder = CanCompleteOrderItem;
                    Displayids = new List<uint>
                    {
                        15379, // Garrison Building  Leatherworking V1
                        15380, // Garrison Building  Leatherworking V2
                        15140, // Garrison Building  Leatherworking V3
                        22978, // Garrison Building Horde Leatherworking V1
                        22979, // Garrison Building Horde Leatherworking V2
                        22980 // Garrison Building Horde Leatherworking V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230457,
                        227070,
                        230458,
                        227592,
                        230459,
                        227593
                    };
                    break;


                // <Vendor Name="Trader Joseph" Entry="87208" Type="Repair" X="1892.497" Y="183.4631" Z="79.72182" />
                case (int) Buildings.TradingPostLvl1:
                case (int) Buildings.TradingPostLvl2:
                case (int) Buildings.TradingPostLvl3:
                    Pnj = alliance
                        ? new WoWPoint(1816.578, 225.9814, 72.71624)
                        : new WoWPoint(5574.952, 4508.236, 129.8942);
                    maxCanComplete = MaxCanCompleteTradingPost;
                    CanCompleteOrder = CanCompleteOrderTradingPost;
                    PnjIds = alliance
                        ? new List<uint>
                        {
                            87207,
                            87209,
                            87211,
                            87208,
                            87213,
                            87214,
                            87217,
                            87215,
                            87212,
                            87216,
                            87210,
                            91071
                        }
                        : new List<uint>
                        {
                            86803,
                            87112,
                            87113,
                            87114,
                            87115,
                            87116,
                            87117,
                            87118,
                            87119,
                            87120,
                            87121,
                            91070
                        };
                    Displayids = new List<uint>
                    {
                        18574, // Garrison Building  Trading Post V1
                        18575, // Garrison Building  Trading Post V2
                        18576, // Garrison Building  Trading Post V3
                        15403, // Garrison Building Horde Trading Post V1
                        15404, // Garrison Building Horde Trading Post V2
                        20150 // Garrison Building Horde Trading Post V3
                    };
                    buildingIDs = new List<uint>
                    {
                        230472,
                        227673,
                        230473,
                        227674,
                        230474,
                        233189
                    };
                    break;
            }
        }


        private int MaxCanCompleteTradingPost()
        {
            ReagentId = GaBSettings.Get().ItemIdTradingPost;
            NumberReagent = GaBSettings.Get().NumberReagentTradingPost;

            var rea =
                GaBSettings.Get().TradingPostReagentsSettings.FirstOrDefault(i => i.Activated && i.ItemId == ReagentId);
            if (rea == null)
            {
                GarrisonButler.Diagnostic(
                    "[TradingPost] Couldn't find matching reagent activated in settings, reagentId={0}, #={1}",
                    ReagentId, NumberReagent);
                //ObjectDumper.WriteToHb(GaBSettings.Get().TradingPostReagentsSettings, 3);
                return 0;
            }
            // Done with the check of reagent, so we switch to simple routine.
            GarrisonButler.Diagnostic("[TradingPost] Calling CanCompleteOrder with reagentId={0}, #={1}", ReagentId,
                NumberReagent);
            return MaxCanCompleteItem();
        }

        /// <summary>
        /// Needs to have the capacitive frame opened
        /// </summary>
        internal void RefreshOrderTradingPost()
        {
            var serverTimeLua = ButlerLua.GetServerCurrentDate();

            // Check reagent
            var reagent = ButlerLua.GetShipmentReagent();
            if (reagent.Item1 == -1)
            {
                GarrisonButler.Warning("[TradingPost] Failed to find reagent id");
                return;
            }

            GarrisonButler.Log("[TradingPost] Found reagentId for trading post :{0}, #={1}, time={2}", reagent.Item1,
                reagent.Item2, serverTimeLua);
            // Override value
            GaBSettings.Get().ItemIdTradingPost = (uint)reagent.Item1;
            GaBSettings.Get().NumberReagentTradingPost = reagent.Item2;
            GaBSettings.Get().LastCheckTradingPost = serverTimeLua;
            GaBSettings.Save();
        }

        private async Task<Result> CanCompleteOrderTradingPost()
        {
            // Before being able to calculate it, we need to know what's today's reagent.
            // It can be saved in settings with the date.
            var serverTimeLua = await ButlerLua.GetServerDate();
            var secBeforeReset = TimeSpan.FromSeconds(await ButlerLua.GetTimeBeforeResetInSec());
            var nextReset = serverTimeLua + secBeforeReset;
            var lastReset = nextReset - TimeSpan.FromHours(24);

            if (GaBSettings.Get().LastCheckTradingPost == default(DateTime)
                || GaBSettings.Get().LastCheckTradingPost < lastReset)
            {
                // moving to pnj
                var moveResult = (await ButlerCoroutine.MoveToAndOpenCapacitiveFrame(this)).State;
                if (moveResult == ActionResult.Running)
                {
                    return new Result(ActionResult.Running);
                }

                // Check reagent
                var reagent = await ButlerLua.GetShipmentReagentInfo();
                if (reagent.Item1 == -1)
                {
                    GarrisonButler.Diagnostic("[TradingPost] Failed to find reagent id");
                    return new Result(ActionResult.Failed);
                }

                GarrisonButler.Log("[TradingPost] Found reagentId for trading post :{0}, #={1}, time={2}", reagent.Item1,
                    reagent.Item2, serverTimeLua);
                // Override value
                GaBSettings.Get().ItemIdTradingPost = (uint) reagent.Item1;
                GaBSettings.Get().NumberReagentTradingPost = reagent.Item2;
                GaBSettings.Get().LastCheckTradingPost = serverTimeLua;
                GaBSettings.Save();
            }
            ReagentId = GaBSettings.Get().ItemIdTradingPost;
            NumberReagent = GaBSettings.Get().NumberReagentTradingPost;

            CanCompleteOrder = CanCompleteOrderTradingPostSimple;
            return await CanCompleteOrderTradingPostSimple();
        }

        private async Task<Result> CanCompleteOrderTradingPostSimple()
        {
            var rea =
                GaBSettings.Get().TradingPostReagentsSettings.FirstOrDefault(i => i.Activated && i.ItemId == ReagentId);
            if (rea == null)
            {
                GarrisonButler.Diagnostic(
                    "[TradingPost] Couldn't find matching reagent activated in settings, reagentId={0}, #={1}",
                    ReagentId, NumberReagent);
                ObjectDumper.WriteToHb(GaBSettings.Get().TradingPostReagentsSettings, 3);
                return new Result(ActionResult.Failed);
            }
            // Done with the check of reagent, so we switch to simple routine.
            GarrisonButler.Diagnostic("[TradingPost] Calling CanCompleteOrder with reagentId={0}, #={1}", ReagentId,
                NumberReagent);
            return await CanCompleteOrderItem();
        }

        public static bool HasOrder(Buildings b)
        {
            switch (b)
            {
                case Buildings.AlchemyLabLvl1:
                case Buildings.AlchemyLabLvl2:
                case Buildings.AlchemyLabLvl3:
                case Buildings.BarnLvl1:
                case Buildings.BarnLvl2:
                case Buildings.BarnLvl3:
                case Buildings.DwarvenBunkerLvl1:
                case Buildings.DwarvenBunkerLvl2:
                case Buildings.DwarvenBunkerLvl3:
                case Buildings.EnchanterStudyLvl1:
                case Buildings.EnchanterStudyLvl2:
                case Buildings.EnchanterStudyLvl3:
                case Buildings.EngineeringWorksLvl1:
                case Buildings.EngineeringWorksLvl2:
                case Buildings.EngineeringWorksLvl3:
                case Buildings.GardenLvl1:
                case Buildings.GardenLvl2:
                case Buildings.GardenLvl3:
                case Buildings.GemBoutiqueLvl1:
                case Buildings.GemBoutiqueLvl2:
                case Buildings.GemBoutiqueLvl3:
                case Buildings.GladiatorSanctumLvl1:
                case Buildings.GladiatorSanctumLvl2:
                case Buildings.GladiatorSanctumLvl3:
                case Buildings.TailoringEmporiumLvl1:
                case Buildings.TailoringEmporiumLvl2:
                case Buildings.TailoringEmporiumLvl3:
                case Buildings.TheForgeLvl1:
                case Buildings.TheForgeLvl2:
                case Buildings.TheForgeLvl3:
                case Buildings.TheTanneryLvl1:
                case Buildings.TheTanneryLvl2:
                case Buildings.TheTanneryLvl3:
                case Buildings.LumberMillLvl1:
                case Buildings.LumberMillLvl2:
                case Buildings.LumberMillLvl3:
                case Buildings.MineLvl1:
                case Buildings.MineLvl2:
                case Buildings.MineLvl3:
                case Buildings.ScribeQuartersLvl1:
                case Buildings.ScribeQuartersLvl2:
                case Buildings.ScribeQuartersLvl3:
                case Buildings.TradingPostLvl1:
                case Buildings.TradingPostLvl2:
                case Buildings.TradingPostLvl3: // This one changes everyday... 
                    return true;
                case Buildings.GnomishGearworksLvl1:
                case Buildings.GnomishGearworksLvl2:
                case Buildings.GnomishGearworksLvl3:
                case Buildings.MageTowerLvl1:
                case Buildings.MageTowerLvl2:
                case Buildings.MageTowerLvl3:
                case Buildings.BarracksLvl1:
                case Buildings.BarracksLvl2:
                case Buildings.BarracksLvl3:
                case Buildings.SalvageYardLvl1:
                case Buildings.SalvageYardLvl2:
                case Buildings.SalvageYardLvl3:
                case Buildings.StablesLvl1:
                case Buildings.StablesLvl2:
                case Buildings.StablesLvl3:
                case Buildings.StorehouseLvl1:
                case Buildings.StorehouseLvl2:
                case Buildings.StorehouseLvl3:
                case Buildings.InnTavernLvl1:
                case Buildings.InnTavernLvl2:
                case Buildings.InnTavernLvl3:
                case Buildings.FishingShackLvl1:
                case Buildings.FishingShackLvl2:
                case Buildings.FishingShackLvl3:
                    break;
                default:
                    GarrisonButler.Warning("HasOrder hit default case with b=" + b);
                    break;
            }
            return false;
        }
    }

    public enum TradingPostReagents
    {
        [Description("Draenic Dust")] DraenicDust = 109693,
        [Description("Sumptuous Fur")] SumptuousFur = 111557,
        [Description("Raw Beast Hide")] RawBeastHide = 110609,

        // Herb
        [Description("Talador Orchid")] TaladorOrchid = 109129,
        [Description("Nagrand Arrow Bloom")] NagrandArrowbloom = 109128,
        [Description("Starflower")] Starflower = 109127,
        [Description("Gorgrond FlyTrap")] GorgrondFlytrap = 109126,
        [Description("Fireweed")] Fireweed = 109125,
        [Description("Frostweed")] Frostweed = 109124,

        // Ore
        [Description("True Iron Ore")] TrueIronOre = 109119,
        [Description("Blackrock Ore")] BlackrockOre = 109118,

        // Meat
        [Description("Raw Clefthoof Meat")] RawClefthoofMeat = 109131,
        [Description("Raw Talbuk Meat")] RawTalbukMeat = 109132,
        [Description("Rylak Egg")] RylakEgg = 109133,
        [Description("Raw Elekk Meat")] RawElekkMeat = 109134,
        [Description("Raw Riverbeast Meat")] RawRiverbeastMeat = 109135,
        [Description("Raw Boar Meat")] RawBoarMeat = 109136,

        // Fish
        [Description("Crescent Saberfish Flesh")] CrescentSaberfishFlesh = 109137,
        [Description("Jawless Skulker Flesh")] JawlessSkulkerFlesh = 109138,
        [Description("Fat Sleeper Flesh")] FatSleeperFlesh = 109139,
        [Description("Blind Lake Sturgeon Flesh")] BlindLakeSturgeonFlesh = 109140,
        [Description("Fire Ammonite Tentacle")] FireAmmoniteTentacle = 109141,
        [Description("Sea Scorpion Segment")] SeaScorpionSegment = 109142,
        [Description("Abyssal Gulper Eel Flesh")] AbyssalGulperEelFlesh = 109143
    }

    public enum Buildings
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

        InnTavernLvl1 = 34,
        InnTavernLvl2 = 35,
        InnTavernLvl3 = 36
    }
}
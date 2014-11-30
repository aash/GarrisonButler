using System;

namespace GarrisonBuddy
{
    public class Building
    {
        private readonly String buildTime;
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
        private readonly String name;
        private readonly String nameShipment;
        private readonly String plotId;
        public readonly int rank;
        private readonly String shipmentCapacity;
        public readonly int shipmentsReady;
        private readonly String shipmentsTotal;
        private readonly String timeStart;

        public Building(int id, string plotId, string buildingLevel, string name, int rank, string isBuilding,
            string timeStart, string buildTime, string canActivate, string canUpgrade, string isPrebuilt,
            string nameShipment, string shipmentCapacity, int shipmentsReady, string shipmentsTotal,
            string creationTime, string duration, string itemName, string itemQuality, string itemId)
        {
            this.id = id;
            this.plotId = plotId;
            this.buildingLevel = buildingLevel;
            this.name = name;
            this.rank = rank;
            this.isBuilding = isBuilding;
            this.timeStart = timeStart;
            this.buildTime = buildTime;
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

            //GarrisonBuddy.Diagnostic(ToString());
        }

        public override string ToString()
        {
            return
                string.Format(
                    "Id: {0}, PlotId: {1}, BuildingLevel: {2}, Name: {3}, Rank: {4}, IsBuilding: {5}, TimeStart: {6}, BuildTime: {7}, CanActivate: {8}, CanUpgrade: {9}, IsPrebuilt: {10}, NameShipment: {11}, ShipmentCapacity: {12}, ShipmentsReady: {13}, ShipmentsTotal: {14}, CreationTime: {15}, Duration: {16}, ItemName: {17}, ItemQuality: {18}, ItemId: {19}",
                    id, plotId, buildingLevel, name, rank, isBuilding, timeStart, buildTime, canActivate, canUpgrade,
                    isPrebuilt, nameShipment, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration,
                    itemName, itemQuality, itemID);
        }
    }
}
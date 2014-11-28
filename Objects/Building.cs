using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GarrisonBuddy
{

    public class Building
    {

        String id;
        String plotId;
        String buildingLevel;
        String name;
        String rank;
        String isBuilding;
        String timeStart;
        String buildTime;
        String canActivate;
        String canUpgrade;
        String isPrebuilt;
        String nameShipment;
        String shipmentCapacity;
        String shipmentsReady;
        String shipmentsTotal;
        String creationTime;
        String duration;
        String itemName;
        String itemQuality;
        String itemID;

        public override string ToString()
        {
            return string.Format("Id: {0}, PlotId: {1}, BuildingLevel: {2}, Name: {3}, Rank: {4}, IsBuilding: {5}, TimeStart: {6}, BuildTime: {7}, CanActivate: {8}, CanUpgrade: {9}, IsPrebuilt: {10}, NameShipment: {11}, ShipmentCapacity: {12}, ShipmentsReady: {13}, ShipmentsTotal: {14}, CreationTime: {15}, Duration: {16}, ItemName: {17}, ItemQuality: {18}, ItemId: {19}", id, plotId, buildingLevel, name, rank, isBuilding, timeStart, buildTime, canActivate, canUpgrade, isPrebuilt, nameShipment, shipmentCapacity, shipmentsReady, shipmentsTotal, creationTime, duration, itemName, itemQuality, itemID);
        }

        public Building(string id, string plotId, string buildingLevel, string name, string rank, string isBuilding, string timeStart, string buildTime, string canActivate, string canUpgrade, string isPrebuilt, string nameShipment, string shipmentCapacity, string shipmentsReady, string shipmentsTotal, string creationTime, string duration, string itemName, string itemQuality, string itemId)
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
            itemID = itemId;

            GarrisonButler.Debug(ToString());
        }
    }
}

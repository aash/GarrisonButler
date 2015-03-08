/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.work-order-tab', ['ngMaterial', 'ngAria','smart-table', 'ui.slider'])

    .controller('buildingsListController', function ($scope) {


        // Load buildings in js value from c# code
        $scope.loadBuildings = function () {
            try{
                var res = window.external.getBuildingsJs();
            }
            catch(e)
            {
                $scope.Diagnostic(e);
            }
            return res;
        };
        // Represents a building
        $scope.Building = function Building(id, name, displayicon, canStartOrder, maxCanStartOrder, canCollectOrder, available) {
            this.id = id;
            this.name = name;
            this.displayicon = displayicon;
            this.canStartOrder = canStartOrder;
            this.maxCanStartOrder = maxCanStartOrder;
            this.canCollectOrder = canCollectOrder;
            this.available = available;
        };

        try
        {
            var buildings = JSON.parse($scope.loadBuildings());
            $scope.Diagnostic("Received: " + buildings);
            $scope.butlerSettings.Buildings = [];
            for (var i = 0; i < buildings.length; i++)
            {
                var buildingId = buildings[i];
                $scope.Diagnostic("Request for building: " + buildingId);
                var building = JSON.parse($scope.loadBuildingById(buildingId));
                $scope.Diagnostic("Parsed: " + building);
                $scope.butlerSettings.Buildings[i] = new $scope.Building(buildingId, building[0], "", Boolean(building[1]), parseInt(building[2]), Boolean(building[3]), Boolean(building[4]));
            }
            $scope.butlerSettings.Buildings = $scope.butlerSettings.Buildings.sort(function(a, b) { return a.name.localeCompare(b.name); });
        }
        catch(e)
        {
            $scope.Diagnostic("Request for buildings error: " + e);
            $scope.butlerSettings.Buildings = [
                // Mine / Garden
                new $scope.Building("Building id", "Mine", "http://wow.zamimg.com/images/wow/icons/medium/trade_mining.jpg", "CanStartOrder", 10, "CanCollectOrder", false),
                new $scope.Building("Building id", "Garden", "http://wow.zamimg.com/images/wow/icons/medium/inv_misc_herb_sansamroot.jpg", "CanStartOrder", 23, "CanCollectOrder", "Available"),
                // Small Buildings
                new $scope.Building("Building id", "Alchemy Lab", "http://wow.zamimg.com/images/wow/icons/medium/trade_alchemy.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Enchanter's Study", "http://wow.zamimg.com/images/wow/icons/medium/trade_engraving.jpg", "CanStartOrder", 30, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Engineering Works", "http://wow.zamimg.com/images/wow/icons/medium/trade_engineering.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Gem Boutique", "http://wow.zamimg.com/images/wow/icons/medium/inv_misc_gem_01.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Salvage Yard", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_salvageyard.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Scribe's Quarter", "http://wow.zamimg.com/images/wow/icons/medium/inv_inscription_tradeskill01.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Storehouse", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_storehouse.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Tailoring Emporium", "http://wow.zamimg.com/images/wow/icons/medium/trade_tailoring.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                // Medium Buildings
                new $scope.Building("Building id", "Barn", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_barn.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Gladiator's Sanctum", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_sparringarena.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Lumber Mill", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_lumbermill.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                new $scope.Building("Building id", "Trading Post", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_tradingpost.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available"),
                // Large Buildings
                new $scope.Building("Building id", "Dwarven Bunker / War Mill", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_armory.jpg", "CanStartOrder", 0, "CanCollectOrder", "Available")
            ];
            $scope.butlerSettings.Buildings = $scope.butlerSettings.Buildings.sort(function(a, b) { return a.name.localeCompare(b.name); });
        }




        if (!Array.prototype.filter)
        {
            Array.prototype.filter = function(fun /*, thisp*/)
            {
                var len = this.length;
                if (typeof fun != "function")
                    throw new TypeError();

                var res = new Array();
                var thisp = arguments[1];
                for (var i = 0; i < len; i++)
                {
                    if (i in this)
                    {
                        var val = this[i]; // in case fun mutates this
                        if (fun.call(thisp, val, i, this))
                            res.push(val);
                    }
                }

                return res;
            };
        }

        $scope.isAvailable = function isAvailable(element, index, array) {
            return (element.available);
        };
        $scope.isNotAvailable = function isNotAvailable(element, index, array) {
            return (!element.available);
        };

        $scope.itemsAvailable = $scope.butlerSettings.Buildings.filter($scope.isAvailable);
        $scope.itemsNotAvailable = $scope.butlerSettings.Buildings.filter($scope.isNotAvailable);

        $scope.startAll = function(value)
        {
            for (index = 0; index < $scope.butlerSettings.Buildings.length; ++index) {
                $scope.butlerSettings.Buildings[index].canStartOrder = value;
            }
        };
        $scope.collectAll = function(value)
        {
            for (index = 0; index < $scope.butlerSettings.Buildings.length; ++index) {
                $scope.butlerSettings.Buildings[index].canCollectOrder = value;
            }
        };
    })

    .controller('buildingController', function ($scope) {
        $scope.init = function (item) {
            $scope.building = item;
            $scope.nameNoSpaces = item.name.replace(/\s+/g, '');

            $scope.$watch(
                'building.canCollectOrder',
                function (newValue, oldValue)
                {
                    try{
                        $scope.saveBuildingCanCollect($scope.building.id, newValue);
                    }
                    catch(e)
                    {
                        $scope.Diagnostic(e);
                    }
                }
            );

            $scope.$watch(
                'building.canStartOrder',
                function (newValue, oldValue)
                {
                    try {
                        $scope.saveBuildingCanStart($scope.building.id, newValue);
                    }
                    catch(e)
                    {
                        $scope.Diagnostic(e);
                    }
                }
            );

            $scope.$watch(
                'building.maxCanStartOrder',
                function (newValue, oldValue)
                {
                    try {
                        $scope.saveBuildingMaxStart($scope.building.id, newValue);
                    }
                    catch(e)
                    {
                        $scope.Diagnostic(e);
                    }
                }
            );
        };
    });



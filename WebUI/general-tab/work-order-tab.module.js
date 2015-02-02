/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.work-order-tab', ['mobile-angular-ui', 'ngMaterial', 'ngAria','smart-table', 'ngTouch'])

    .controller('buildingsListController', function ($scope) {

        // Represents a building
        $scope.Building = function Building(id, name, displayicon, canStartOrder, maxCanStartOrder, canCollectOrder) {
            this.id = id;
            this.name = name;
            this.displayicon = displayicon;
            this.canStartOrder = canStartOrder;
            this.maxCanStartOrder = maxCanStartOrder;
            this.canCollectOrder = canCollectOrder;
        };

        $scope.butlerSettings.Buildings = [
            // Mine / Garden
            new $scope.Building("Building id", "Mine", "http://wow.zamimg.com/images/wow/icons/medium/trade_mining.jpg", "CanStartOrder", 10, "CanCollectOrder"),
            new $scope.Building("Building id", "Garden", "http://wow.zamimg.com/images/wow/icons/medium/inv_misc_herb_sansamroot.jpg", "CanStartOrder", 23, "CanCollectOrder"),
            // Small Buildings
            new $scope.Building("Building id", "Alchemy Lab", "http://wow.zamimg.com/images/wow/icons/medium/trade_alchemy.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Enchanter's Study", "http://wow.zamimg.com/images/wow/icons/medium/trade_engraving.jpg", "CanStartOrder", 30, "CanCollectOrder"),
            new $scope.Building("Building id", "Engineering Works", "http://wow.zamimg.com/images/wow/icons/medium/trade_engineering.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Gem Boutique", "http://wow.zamimg.com/images/wow/icons/medium/inv_misc_gem_01.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Salvage Yard", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_salvageyard.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Scribe's Quarter", "http://wow.zamimg.com/images/wow/icons/medium/inv_inscription_tradeskill01.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Storehouse", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_storehouse.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Tailoring Emporium", "http://wow.zamimg.com/images/wow/icons/medium/trade_tailoring.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            // Medium Buildings
            new $scope.Building("Building id", "Barn", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_barn.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Gladiator's Sanctum", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_sparringarena.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Lumber Mill", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_lumbermill.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            new $scope.Building("Building id", "Trading Post", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_tradingpost.jpg", "CanStartOrder", 0, "CanCollectOrder"),
            // Large Buildings
            new $scope.Building("Building id", "Dwarven Bunker / War Mill", "http://wow.zamimg.com/images/wow/icons/medium/garrison_building_armory.jpg", "CanStartOrder", 0, "CanCollectOrder")
        ];

        $scope.items = $scope.butlerSettings.Buildings;
    })

    .controller('buildingController', function ($scope) {
        $scope.init = function (item) {
            $scope.name = item.name;
            $scope.nameNoSpaces = item.name.replace(/\s+/g, '');
            $scope.displayicon = item.displayicon;
            $scope.CanStartOrder = item.CanStartOrder;
            $scope.CanCollectOrder = item.CanCollectOrder;
            $scope.maxCanStartOrder = item.maxCanStartOrder;
        };

        $scope.save = function () {
            $scope.saveCSharpBool($scope.propertyName, $scope.value);
        };
    });

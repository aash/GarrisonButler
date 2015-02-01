var app = angular.module('GarrisonButlerApp', ['mobile-angular-ui', 'ngMaterial', 'ui.grid', 'ui.grid.edit', 'ui.grid.cellNav', 'ngTouch']);

app.controller('mainController', function($scope) {
    // Save boolean from js value in c# code
    $scope.saveCSharpBool = function (propertyName, value) {
        window.external.UpdateBooleanValue(propertyName, value);
    };

    // Load boolean in js value from c# code
    $scope.loadCSharpBool = function (propertyName) {
        var res = window.external.GetBooleanValue(propertyName);
        return res;
    };

    // Represents a variable name, a title and a description
    $scope.Entry = function Entry(variableName, label, description) {
        this.variableName = variableName;
        this.label = label;
        this.description = description;
    };


    $scope.butlerSettings = {};
    $scope.butlerSettings.general = [ // variable name in c#, Title, Description

        new $scope.Entry(
        'UseGarrisonHearthstone',
        'Garrison Hearthstone',
        'Enable to use the garrison hearthstone if the toon is outside.'),

        new $scope.Entry(
        'ForceJunkSell',
        'Sell Junk',
        'Enable to sell grey items in bags to the vendor.'),

        new $scope.Entry(
        'GarrisonCache',
        'Garrison Cache',
        'Enable to harvest the garrison cache.'),

        new $scope.Entry(
        'HbRelogMode',
        'HB Relog compatibility',
        'This will auto skip to the next task at the end of the run and close honorbuddy. ' +
        'Can be used without HBRelog to close honorbuddy at the end of the run.'),

        new $scope.Entry(
        'HarvestGarden',
        'Garden',
        'Enable to clean your garden.'),

        new $scope.Entry(
        'HarvestMine',
        'Mine',
        'Enable to clean your mine.'),

        new $scope.Entry(
        'UseCoffee',
        'Miner\'s coffe',
        'Enable to use as a speed boost when in the mine.'),

        new $scope.Entry(
        'DeleteCoffee',
        'Miner\'s coffe',
        'Enable to delete a unit of coffee if at max stack.'),

        new $scope.Entry(
        'UseMiningPick',
        'Mining Pick',
        'Enable to use as a speed boost when harvesting in the mine.'),

        new $scope.Entry(
        'DeleteMiningPick',
        'Mining Pick',
        'Enable to delete a unit of mining pick if at max stack.'),

        new $scope.Entry(
        'ActivateBuildings',
        'Activate Buildings',
        'Enable to activate finished buildings.'),

        new $scope.Entry(
        'SalvageCrates',
        'Salvage',
        'Enable to salvage crates.'),

        new $scope.Entry(
        'StartMissions',
        'Missions',
        'Enable to start missions.'),

        new $scope.Entry(
        'CompletedMissions',
        'Missions',
        'Enable to pick up rewards from missions.'),

        new $scope.Entry(
        'DisableLastRoundCheck',
        'Disable Last Round',
        'Check to disable the last round check.')
    ];


    // Represents a building
    $scope.Building = function Building(id, name, buildingIcon, canStartOrder, maxCanStartOrder, canCollectOrder) {
        this.id = id;
        this.name = name;
        this.buildingIcon = buildingIcon;
        this.canStartOrder = canStartOrder;
        this.maxCanStartOrder = maxCanStartOrder;
        this.canCollectOrder = canCollectOrder;
    };

    $scope.butlerSettings.Buildings = [
        // Mine / Garden
        new $scope.Building("Building id", "Mine", "http://wow.zamimg.com/images/wow/icons/medium/trade_mining.jpg", "CanStartOrder", 0, "CanCollectOrder"),
        new $scope.Building("Building id", "Garden", "http://wow.zamimg.com/images/wow/icons/medium/inv_misc_herb_sansamroot.jpg", "CanStartOrder", 0, "CanCollectOrder"),
        // Small Buildings
        new $scope.Building("Building id", "Alchemy Lab", "http://wow.zamimg.com/images/wow/icons/medium/trade_alchemy.jpg", "CanStartOrder", 0, "CanCollectOrder"),
        new $scope.Building("Building id", "Enchanter's Study", "http://wow.zamimg.com/images/wow/icons/medium/trade_engraving.jpg", "CanStartOrder", 0, "CanCollectOrder"),
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

    // Represents a dailyCD
    $scope.Daily = function Daily(itemId, spellId, name, profession, activated) {
        this.itemId = itemId;
        this.spellId = spellId;
        this.name = name;
        this.profession = profession;
        this.activated = activated;
    };

    $scope.butlerSettings.Professions = [
        new $scope.Daily(108996, 156587, "Alchemical Catalyst", "Alchemy", 0),
        new $scope.Daily(118700, 175880, "Secrets of Draenor Alchemy", "Alchemy", 0)
    ];


    


});


// General Tab

// List of switch controller
app.controller('switchListController', function ($scope) {
    $scope.items = $scope.butlerSettings.general;

});

// Boolean switch controllers
app.controller('switchController', function ($scope) {
    $scope.init = function (item) {
        $scope.propertyName = item.variableName;
        $scope.propertyTitle = item.label;
        $scope.propertyDescription = item.description;
        try {
            $scope.value = $scope.loadCSharpBool($scope.propertyName);
        } catch (e) {

        } 
    };

    $scope.save = function () {
        $scope.saveCSharpBool($scope.propertyName, $scope.value);
    };
});

// Buildings Tab

// List of switch controller
app.controller('buildingsListController', function ($scope) {
    $scope.items = $scope.butlerSettings.Buildings;

});

// Boolean switch controllers
app.controller('buildingController', function ($scope) {
    $scope.init = function (item) {
        $scope.name = item.name;
        $scope.nameNoSpaces = item.name.replace(/\s+/g, '');
        $scope.buildingIcon = item.buildingIcon;
        $scope.CanStartOrder = item.CanStartOrder;
        $scope.CanCollectOrder = item.CanCollectOrder;
        $scope.maxCanStartOrder = item.maxCanStartOrder;
    };

    $scope.save = function () {
        $scope.saveCSharpBool($scope.propertyName, $scope.value);
    };
});


// Professions Tab

app.controller('professionsListController', function ($scope) {
    $scope.items = $scope.butlerSettings.Professions;
    
    $scope.gridOptions = {
        data: $scope.items,
        enableSorting: true,
        enableCellEditOnFocus: true
    };
    $scope.gridOptions.onRegisterApi = function(gridApi){
        $scope.gridApi = gridApi;
    };
});

// Boolean switch controllers
app.controller('dailyController', function ($scope) {
    $scope.init = function (item) {
        $scope.itemId = item.itemId;
        $scope.profession = item.profession;
        $scope.name = item.name;
        $scope.activated = item.activated;
    };

    $scope.save = function () {
        $scope.saveCSharpBool($scope.propertyName, $scope.value);
    };
});



// Tabs

app.controller('AppCtrl', function ($scope) {
      $scope.data = {
          selectedIndex: 0,
          mailingLocked: true,
          secondLabel: "Item Two"
      };
      $scope.next = function () {
          $scope.data.selectedIndex = Math.min($scope.data.selectedIndex + 1, 3);
      };
      $scope.previous = function () {
          $scope.data.selectedIndex = Math.max($scope.data.selectedIndex - 1, 0);
      };
  });
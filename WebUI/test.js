var app = angular.module('GarrisonButlerApp', ['ngMaterial']);

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
    $scope.butlerSettings.general = [

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
    $scope.Building = function Building(id, name, canStartOrder, maxCanStartOrder, canCollectOrder) {
        this.id = id;
        this.name = name;
        this.canStartOrder = canStartOrder;
        this.maxCanStartOrder = maxCanStartOrder;
        this.canCollectOrder = canCollectOrder;
    };

    $scope.butlerSettings.Buildings = [
        new $scope.Building("Building id", "Test Name", "CanStartOrder", 0, "CanCollectOrder"),
        new $scope.Building("Building id", "Dwarven Bunker", "CanStartOrder", 10, "CanCollectOrder"),
        new $scope.Building("Building id", "Trading Post", "CanStartOrder", 23, "CanCollectOrder"),
        new $scope.Building("Building id", "Test Name2", "CanStartOrder", 0, "CanCollectOrder")
    ];

    // Represents a dailyCD
    $scope.Daily = function Daily(itemId, professionName, name, activated) {
        this.itemId = itemId;
        this.professionName = professionName;
        this.name = name;
        this.activated = activated;
    };

    $scope.butlerSettings.Professions = [
        new $scope.Daily("itemId", "Profession Name", "Daily Name", "Activated"),
        new $scope.Daily("itemId", "Profession Name", "Daily Name", "Activated")
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
        enableSorting: true
    };
});

// Boolean switch controllers
app.controller('dailyController', function ($scope) {
    $scope.init = function (item) {
        $scope.itemId = item.itemId;
        $scope.professionName = item.professionName;
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
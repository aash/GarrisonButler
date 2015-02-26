/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.milling-tab', ['ngMaterial', 'ngAria', 'smart-table'])

    .controller('millingListController', function ($scope) {

        console.log("test00");
        // Represents a Item
        $scope.MillingItem = function(itemId, name, activated) {
            this.itemId = itemId;
            this.itemName = name;
            this.activated = activated;
        };

        try
        {
            console.log("test0");

            var millingItems = JSON.parse($scope.loadMilling());
            $scope.MillingItems = [];
            for (var i = 0; i < millingItems.length; i++)
            {
                var itemId = millingItems[i];
                var millingItem = JSON.parse($scope.loadMillingById(itemId));
                $scope.GBDiagnostic(millingItem);
                $scope.MillingItems[i] = new $scope.MillingItem(itemId, millingItem[0], Boolean(millingItem[1]));
            }
            $scope.MillingItems = $scope.MillingItems.sort(function(a, b) { return a.itemName.localeCompare(b.itemName); });
        }
        catch(e) {
            try {
                $scope.GBDiagnostic(e);
            }
            catch(e2){}
            console.log("test1");
            $scope.MillingItems = [
                new $scope.MillingItem(0, "Fireweed", true),
                new $scope.MillingItem(0, "Frostweed", true),
                new $scope.MillingItem(0, "Gorgron Flytrap", false),
                new $scope.MillingItem(0, "Nagrand Arrowbloom", false),
                new $scope.MillingItem(0, "Starflower", true),
                new $scope.MillingItem(0, "Talador Orchid", false)
            ];
        }
        $scope.selectAll = function(val)
        {
            for (var i = 0; i < $scope.MillingItems.length; i++)
            {
                $scope.MillingItems[i].activated = val;
            }
        };
    })

    .controller('millingController', function ($scope) {
        $scope.init = function (item) {
            $scope.millingItem = item;
        };

        $scope.$watch(
            'millingItem.activated',
            function (newValue, oldValue)
            {
                $scope.saveMillingItem($scope.millingItem.itemId, newValue);
            }
        );
    });
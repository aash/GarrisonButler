/**
 * Created by Mickaël on 2/18/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.trading-post-tab', ['ngMaterial', 'ngAria', 'smart-table'])

    .controller('tradingPostListController', function ($scope) {
        // Represents a Item
        $scope.TPItem = function(itemId, name, activated) {
            this.itemId = itemId;
            this.itemName = name;
            this.activated = activated;
        };

        try
        {
            var tradingItems = JSON.parse($scope.loadTP());
            $scope.TradingReagents = [];
            for (var i = 0; i < tradingItems.length; i++)
            {
                var itemId = tradingItems[i];
                var tpItem = JSON.parse($scope.loadTPById(itemId));
                $scope.GBDiagnostic(tpItem);
                $scope.TradingReagents[i] = new $scope.TPItem(itemId, tpItem[0], Boolean(tpItem[1]));
            }
            $scope.TradingReagents = $scope.TradingReagents.sort(function(a, b) { return a.itemName.localeCompare(b.itemName); });
        }
        catch(e) {
            try {
                $scope.GBDiagnostic(e);
            }
            catch(e2){}
            $scope.TradingReagents = [
                new $scope.TPItem(0, "Fireweed", true),
                new $scope.TPItem(0, "Frostweed", true),
                new $scope.TPItem(0, "Gorgron Flytrap", false),
                new $scope.TPItem(0, "Nagrand Arrowbloom", false),
                new $scope.TPItem(0, "Starflower", true),
                new $scope.TPItem(0, "Talador Orchid", false)
            ];
        }
        $scope.selectAll = function(val)
        {
            for (var i = 0; i < $scope.TradingReagents.length; i++)
            {
                $scope.TradingReagents[i].activated = val;
            }
        };
    })

    .controller('tradingController', function ($scope) {
        $scope.init = function (item) {
            $scope.tradingItem = item;
        };

        $scope.$watch(
            'tradingItem.activated',
            function (newValue, oldValue)
            {
                $scope.saveTPItem($scope.tradingItem.itemId, newValue);
            }
        );
    });
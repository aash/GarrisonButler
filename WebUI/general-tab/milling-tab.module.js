/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.milling-tab', ['mobile-angular-ui', 'ngMaterial', 'ngAria', 'smart-table', 'ngTouch'])

    .controller('millingListController', function ($scope) {

        // Represents a Item
        $scope.Item = function Item(itemId, name, activated) {
            this.itemId = itemId;
            this.name = name;
            this.activated = activated;
        };

        $scope.butlerSettings.Milling = [
            new $scope.Item(0, "Fireweed", true),
            new $scope.Item(0, "Frostweed", true),
            new $scope.Item(0, "Gorgron Flytrap", false),
            new $scope.Item(0, "Nagrand Arrowbloom", false),
            new $scope.Item(0, "Starflower", true),
            new $scope.Item(0, "Talador Orchid", false)
        ];

        $scope.rowCollection = $scope.butlerSettings.Milling;
    });

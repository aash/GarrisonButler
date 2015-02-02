/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.mailing-tab', ['mobile-angular-ui', 'ngMaterial', 'ngAria', 'smart-table', 'ngTouch'])

    .controller('mailingListController', function ($scope) {

        // Represents a mail item
        $scope.MailItem = function MailItem(itemId, recipient, condition, value, comment) {
            this.itemId = itemId;
            this.recipient = recipient;
            this.condition = condition;
            this.value = value;
            this.comment = comment;
        };

        $scope.butlerSettings.mailItems = [
            new $scope.MailItem(109124, "superStar", "Keep in Bags at least", 50, "Herb-Frostweed"),
            new $scope.MailItem(109125, "Eranette", "if >= in bags", 110, "Herb-Fireweed"),
            new $scope.MailItem(109118, "Lilitur", "if > in bags", 666, "Ore1"),
            new $scope.MailItem(109119, "Lilitur", "if > in bags", 666, "Ore2")
        ];

        $scope.rowCollection = $scope.butlerSettings.mailItems;

        $scope.removeRow = function removeRow(row) {
            var index = $scope.rowCollection.indexOf(row);
            if (index !== -1) {
                $scope.rowCollection.splice(index, 1);
            }
        }
    }).directive("modal", function($rootScope, $timeout) {
        return {
            restrict: "E",
            templateUrl: "templates/modal.html",
            transclude: true
        };
    });

    //.controller('mailController', function ($scope) {
    //
    //    $scope.init = function (item) {
    //        $scope.itemId = item.itemId;
    //        $scope.recipient = item.recipient;
    //        $scope.condition = item.condition;
    //        $scope.value = item.value;
    //        $scope.comment = item.comment;
    //    };
    //
    //    $scope.save = function () {
    //        $scope.saveCSharpBool($scope.propertyName, $scope.value);
    //    };
    //})

    //.directive('csDelete', function () {
    //    return {
    //        require: '^stTable',
    //        template: '',
    //        scope: {
    //            row: '=csDelete'
    //        },
    //        link: function (scope, element, attr, ctrl) {
    //            scope.row.isSelected = scope.row.activated; // Switch the property "activated" of a daily to the value of the selection
    //
    //            scope.$watch('row.isSelected', function (newValue, oldValue) {
    //                scope.row.activated = newValue; // Switch the property "activated" of a daily to the value of the selection
    //            });
    //        }
    //    };
    //})


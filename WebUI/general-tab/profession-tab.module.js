/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.profession-tab', ['mobile-angular-ui', 'ngMaterial', 'ngAria', 'smart-table', 'ngTouch'])

    .controller('professionsListController', function ($scope) {

        // Represents a dailyCD
        $scope.Daily = function Daily(itemId, spellId, name, profession, activated) {
            this.itemId = itemId;
            this.spellId = spellId;
            this.name = name;
            this.profession = profession;
            this.activated = activated;
        };

        $scope.butlerSettings.Professions = [
            new $scope.Daily(108996, 156587, "Alchemical Catalyst", "Alchemy", true),
            new $scope.Daily(118700, 175880, "Secrets of Draenor Alchemy", "Alchemy", false),
            new $scope.Daily(108996, 156587, "B ","B", true),
            new $scope.Daily(118700, 175880, "C", "C", false)
        ];

        $scope.rowCollection = $scope.butlerSettings.Professions;
    })

    .controller('dailyController', function ($scope) {


        $scope.init = function (item) {
            $scope.itemId = item.itemId;
            $scope.profession = item.profession;
            $scope.name = item.name;
            $scope.activated = item.activated;
        };

        $scope.save = function () {
            $scope.saveCSharpBool($scope.propertyName, $scope.value);
        };
    })

    .directive('csSelect', function () {
        return {
            require: '^stTable',
            template: '',
            scope: {
                row: '=csSelect'
            },
            link: function (scope, element, attr, ctrl) {
                scope.row.isSelected = scope.row.activated; // Switch the property "activated" of a daily to the value of the selection

                scope.$watch('row.isSelected', function (newValue, oldValue) {
                    scope.row.activated = newValue; // Switch the property "activated" of a daily to the value of the selection
                });
            }
        };
    });

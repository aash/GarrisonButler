/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.profession-tab', ['ngMaterial', 'ngAria', 'smart-table'])

    .controller('professionsListController', function ($scope) {

        // Represents a dailyCD
        $scope.Daily = function Daily(itemId, name, profession, activated) {
            this.itemId = itemId;
            this.name = name;
            this.profession = profession;
            this.activated = activated;
        };
        try
        {
            var dailies = JSON.parse($scope.loadDailies());
            $scope.Professions = [];
            for (var i = 0; i < dailies.length; i++)
            {
                var dailyId = dailies[i];
                var dailyCd = JSON.parse($scope.loadDailyById(dailyId));
                $scope.GBDiagnostic(dailyCd);
                $scope.Professions[i] = new $scope.Daily(dailyId, dailyCd[0], dailyCd[1], Boolean(dailyCd[2]));
            }
            $scope.Professions = $scope.Professions.sort(function(a, b) { return a.name.localeCompare(b.name); });
        }
        catch(e) {
            try
            {
                $scope.GBDiagnostic(e);
            }
            catch(e2){}
            $scope.Professions = [
                new $scope.Daily(108996, "Alchemical Catalyst", "Alchemy", true),
                new $scope.Daily(118700, "Secrets of Draenor Alchemy", "Alchemy", false),
                new $scope.Daily(108996, "B ", "B", true),
                new $scope.Daily(118700, "C", "C", false)
            ];
        }

        $scope.selectAll = function(val)
        {
            for (var i = 0; i < $scope.Professions.length; i++)
            {
                $scope.Professions[i].activated = val;
            }
        };
    })

    .controller('dailyController', function ($scope) {


        $scope.init = function (item) {
            $scope.daily = item;
        };


        $scope.$watch(
            'daily.activated',
            function (newValue, oldValue)
            {
                $scope.saveDailyCd($scope.daily.itemId, newValue);
            }
        );
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

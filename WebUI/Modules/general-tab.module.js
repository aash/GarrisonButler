/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.general-tab', ['ngMaterial', 'uiSwitch'])

    .controller('GeneralOptionsController', function ($scope) {

        // Represents a variable name, a title and a description
        $scope.Entry = function Entry(variableName, label, description) {
            this.variableName = variableName;
            this.value = false;
            this.label = label;
            this.description = description;
        };

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
                'Use Miner\'s Coffe',
                'Enable to use as a speed boost when in the mine.'),

            new $scope.Entry(
                'UseMiningPick',
                'Use Preserved Mining Pick',
                'Enable to use as a speed boost when harvesting in the mine.'),

            new $scope.Entry(
                'ActivateBuildings',
                'Activate Buildings',
                'Enable to activate finished buildings.'),

            new $scope.Entry(
                'SalvageCrates',
                'Salvage',
                'Enable to salvage crates.'),

            new $scope.Entry(
                'DisableLastRoundCheck',
                'Disable Last Round',
                'Check to disable the last round check.')
        ];

        $scope.items = $scope.butlerSettings.general;



        $scope.itemsLite = [ // variable name in c#, Title, Description

            new $scope.Entry(
                'StartMissions',
                'Start Missions',
                'Enable to let the Butler start missions based on an all countered and 100% success method (optimized mission engine available in Ice Edition).'),

            new $scope.Entry(
                'CompletedMissions',
                'Collect Rewards',
                'Enable to let the Butler collect rewards from missions (optimized reward engine available in Ice Edition).'),
        ];


        $scope.selectAll = function(val)
        {
            for (var i = 0; i < $scope.items.length; i++)
            {
                $scope.items[i].value = val;
            }
        };
    })

    .controller('switchController', function ($scope) {
        $scope.init = function (item) {
            $scope.propertyName = item.variableName;
            $scope.propertyTitle = item.label;
            $scope.propertyDescription = item.description;

            $scope.$watch(
                'item.value',
                function (newValue, oldValue)
                {

                    try {
                        $scope.saveCSharpBool($scope.propertyName, newValue);
                    }
                    catch(e)
                    {
                        $scope.Diagnostic(e);
                    }
                }
            );

            try
            {
                item.value = $scope.loadCSharpBool($scope.propertyName);
            }
            catch (e)
            {
                $scope.Diagnostic(e);
            }
        };
    });

angular.module('uiSwitch', [])

    .directive('switch', function(){
        return {
            restrict: 'AE'
            , replace: true
            , transclude: true
            , template: function(element, attrs) {
                var html = '';
                html += '<span';
                html +=   ' class="switch' + (attrs.class ? ' ' + attrs.class : '') + '"';
                html +=   attrs.ngModel ? ' ng-click="' + attrs.ngModel + '=!' + attrs.ngModel + '"' : '';
                html +=   ' ng-class="{ checked:' + attrs.ngModel + ' }"';
                html +=   '>';
                html +=   '<small></small>';
                html +=   '<input type="checkbox"';
                html +=     attrs.id ? ' id="' + attrs.id + '"' : '';
                html +=     attrs.name ? ' name="' + attrs.name + '"' : '';
                html +=     attrs.ngModel ? ' ng-model="' + attrs.ngModel + '"' : '';
                html +=     attrs.ngChange ? ' ng-change="' + attrs.ngChange + '"' : '';
                html +=     ' style="display:none" />';
                html += '</span>';
                return html;
            }
        }
    });
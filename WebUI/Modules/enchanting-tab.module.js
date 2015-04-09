/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.enchanting-tab', ['ngMaterial', 'ngAria', 'smart-table', 'xeditable'])

    .controller('enchantingListController', function ($scope) {

        // Represents a variable name, a title and a description
        $scope.Entry = function Entry(variableName, label, description) {
            this.variableName = variableName;
            this.value = false;
            this.label = label;
            this.description = description;
        };

        $scope.items = [ // variable name in c#, Title, Description

            new $scope.Entry(
                'ShouldDisenchant',
                'Disenchanting',
                'Enable to disenchant items, if deactivated nothing will be disenchanted.'),

            new $scope.Entry(
                'ShouldDisenchantBoE',
                'Disenchant BoE',
                'Enable to disenchant Bind on Equipped (Disenchanting must be On).'),

            new $scope.Entry(
                'ShouldDisenchantBoP',
                'Disenchant BoP',
                'Enable to disenchant Bind on Pickup (Disenchanting must be On).')
        ];

        try {
            $scope.itemQualities = JSON.parse($scope.loadDisenchantQualities());
            $scope.itemQuality = $scope.loadDisenchantQuality();
        }
        catch(e)
        {
            $scope.Diagnostic(e);
        }

        $scope.$watch(
            'maxItemILVL',
            function (newValue, oldValue)
            {
                try {
                    $scope.saveCSharpInt("MaxDisenchantIlvl", newValue);
                }
                catch(e)
                {
                    $scope.Diagnostic(e);
                }
            });

        try {
            $scope.maxItemILVL = JSON.parse($scope.loadCSharpInt("MaxDisenchantIlvl"));
        }
        catch (e) {
            $scope.Diagnostic(e);
        }
        $scope.saveQuality = function()
        {
            $scope.saveDisenchantQuality($scope.itemQuality);
        };

        $scope.selectAll = function (val) {
            for (var i = 0; i < $scope.items.length; i++) {
                $scope.items[i].value = val;
            }
        };
    })

    .controller('enchantingOptionController', function ($scope) {
        $scope.init = function (item) {
            $scope.propertyName = item.variableName;
            $scope.propertyTitle = item.label;
            $scope.propertyDescription = item.description;

            $scope.$watch(
                'item.value',
                function (newValue, oldValue) {

                    try {
                        $scope.saveCSharpBool($scope.propertyName, newValue);
                    }
                    catch (e) {
                        $scope.Diagnostic(e);
                    }
                }
            );

            try {
                item.value = $scope.loadCSharpBool($scope.propertyName);
            }
            catch (e) {
                $scope.Diagnostic(e);
            }
        };
    });


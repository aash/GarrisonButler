/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.general-tab', ['mobile-angular-ui', 'ngMaterial', 'ngAria', 'ngTouch'])

    .controller('switchListController', function ($scope) {

        // Represents a variable name, a title and a description
        $scope.Entry = function Entry(variableName, label, description, displayicon) {
            this.variableName = variableName;
            this.label = label;
            this.description = description;
            this.displayicon = displayicon;
        };

        $scope.butlerSettings.general = [ // variable name in c#, Title, Description

            new $scope.Entry(
                'UseGarrisonHearthstone',
                'Garrison Hearthstone',
                'Enable to use the garrison hearthstone if the toon is outside.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_rune_01.jpg'),

            new $scope.Entry(
                'ForceJunkSell',
                'Sell Junk',
                'Enable to sell grey items in bags to the vendor.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_bag_10_red.jpg'),

            new $scope.Entry(
                'GarrisonCache',
                'Garrison Cache',
                'Enable to harvest the garrison cache.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_garrison_resource.jpg'),

            new $scope.Entry(
                'HbRelogMode',
                'HB Relog compatibility',
                'This will auto skip to the next task at the end of the run and close honorbuddy. ' +
                'Can be used without HBRelog to close honorbuddy at the end of the run.',
                'http://i.imgur.com/rgDmRIo.png'),

            new $scope.Entry(
                'HarvestGarden',
                'Garden',
                'Enable to clean your garden.',
                'http://wow.zamimg.com/images/wow/icons/medium/spell_nature_naturetouchgrow.jpg'),

            new $scope.Entry(
                'HarvestMine',
                'Mine',
                'Enable to clean your mine.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_pick_02.jpg'),

            new $scope.Entry(
                'UseCoffee',
                'Use Miner\'s Coffe',
                'Enable to use as a speed boost when in the mine.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_drink_15.jpg'),

            new $scope.Entry(
                'DeleteCoffee',
                'Delete Miner\'s Coffee',
                'Enable to delete a unit of coffee if at max stack.',
                'http://i.imgur.com/as5xNIF.png'),

            new $scope.Entry(
                'UseMiningPick',
                'Use Preserved Mining Pick',
                'Enable to use as a speed boost when harvesting in the mine.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_axe_1h_6miningpick.jpg'),

            new $scope.Entry(
                'DeleteMiningPick',
                'Delete Preserved Mining Pick',
                'Enable to delete a unit of mining pick if at max stack.',
                'http://i.imgur.com/j1eM0FF.png'),

            new $scope.Entry(
                'ActivateBuildings',
                'Activate Buildings',
                'Enable to activate finished buildings.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_wrench_01.jpg'),

            new $scope.Entry(
                'SalvageCrates',
                'Salvage',
                'Enable to salvage crates.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_crate_01.jpg'),

            new $scope.Entry(
                'StartMissions',
                'Start Missions',
                'Enable to start missions.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_map07.jpg'),

            new $scope.Entry(
                'CompletedMissions',
                'Complete Missions',
                'Enable to pick up rewards from missions.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_map_01.jpg'),

            new $scope.Entry(
                'DisableLastRoundCheck',
                'Disable Last Round',
                'Check to disable the last round check.',
                'http://wow.zamimg.com/images/wow/icons/medium/inv_misc_coin_17.jpg')
        ];


        $scope.items = $scope.butlerSettings.general;

    })

    .controller('switchController', function ($scope, $mdDialog) {
        $scope.init = function (item) {
            $scope.propertyName = item.variableName;
            $scope.propertyTitle = item.label;
            $scope.propertyDescription = item.description;
            try {
                $scope.value = $scope.loadCSharpBool($scope.propertyName);
            } catch (e) {

            }

            $scope.alert = '';

            $scope.visible = false;

            document.addEventListener("keyup", function(e) {
                if (e.keyCode === 27)
                    $scope.$apply(function() {
                        if ($scope.visible === true)
                        {
                            $scope.close();
                            $scope.visible = false;
                        }
                    });
            });
        };

        $scope.close = function() {
            $scope.visible = false;
            $scope.close();
        };

        $scope.save = function () {
            $scope.saveCSharpBool($scope.propertyName, $scope.value);
        };

        $scope.showAlert = function(ev) {
            $scope.visible = true;
            //$mdDialog.show(
            //    $mdDialog.alert()
            //        .title($scope.propertyTitle)
            //        .content($scope.propertyDescription)
            //        .ariaLabel('Password notification')
            //        .ok('Got it!')
            //        .targetEvent(ev)
            //);
        };
    });


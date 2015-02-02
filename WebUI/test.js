app = angular.module('GarrisonButlerApp',
    [
        'GarrisonButlerApp.general-tab',
        'GarrisonButlerApp.profession-tab',
        'GarrisonButlerApp.work-order-tab',
        'GarrisonButlerApp.mailing-tab',
        'GarrisonButlerApp.milling-tab'
    ])


    .controller('mainController', function($scope) {

        $scope.butlerSettings = {};

        // Save boolean from js value in c# code
        $scope.saveCSharpBool = function (propertyName, value) {
            window.external.UpdateBooleanValue(propertyName, value);
        };

        // Load boolean in js value from c# code
        $scope.loadCSharpBool = function (propertyName) {
            var res = window.external.GetBooleanValue(propertyName);
            return res;
        };
    })


    .controller('AppCtrl', function ($scope) {
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

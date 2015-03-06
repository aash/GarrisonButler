/**
 * Created by Mickaël on 3/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.missions-tab', ['ngMaterial', 'ngAria', 'smart-table'])

    .controller('missionsListController', function ($scope) {
        // Represents a missionSetting
        $scope.MissionSetting = function(id, name, category, successChance, level, disallowed, individualSuccessEnabled) {
            this.id = id;
            this.name = name;
            this.category = category;
            this.successChance = successChance;
            this.level = level;
            this.disallowed = disallowed;
            this.individualSuccessEnabled = individualSuccessEnabled;
        };


        try
        {
            var missions = JSON.parse($scope.loadMissions());
            $scope.GBDiagnostic("Received: " + missions);
            $scope.MissionRewards = [];
            for (var i = 0; i < missions.length; i++)
            {
                var missionId = missions[i];
                $scope.GBDiagnostic("Request for missions rewards: " + missionId);
                var mission = JSON.parse($scope.loadBuildingById(missionId));
                $scope.GBDiagnostic("Parsed: " + mission);
                $scope.MissionRewards[i] = new $scope.MissionSetting(missionId, mission[0], mission[1], parseInt(mission[2]), parseInt(mission[3]), Boolean(mission[4]), Boolean(mission[5]));
            }
            $scope.MissionRewards = $scope.MissionRewards.sort(function(a, b) { return a.name.localeCompare(b.name); });
        }
        catch(e) {
            try
            {
                $scope.GBDiagnostic("Request for missions rewards error: " + e);
            }
            catch(e)
            {
                $scope.MissionRewards = [
                    // Mine / Garden
                    new $scope.MissionSetting("setting mission id", "FollowerExperience", "FollowerExperience", 0, 0, false, false),
                    new $scope.MissionSetting("setting mission id", "Gold", "Gold", 0, 0, false, false)
                ];
            }
            $scope.MissionRewards = $scope.MissionRewards.sort(function (a, b) {
                return a.name.localeCompare(b.name);
            });
        }

    })
    .controller('RewardController', function ($scope) {
        $scope.init = function (item) {
            $scope.missionReward = item;
        };

        $scope.$watch(
            'missionReward.disallowed',
            function (newValue, oldValue)
            {
                $scope.saveRewardDisallowed($scope.missionReward.disallowed, newValue);
            }
        );
        $scope.$watch(
            'missionReward.individualSuccessEnabled',
            function (newValue, oldValue)
            {
                $scope.saveRewardDisallowed($scope.missionReward.individualSuccessEnabled, newValue);
            }
        );
    });
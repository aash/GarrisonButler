/**
 * Created by Mickaël on 3/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.missions-tab', ['ngMaterial', 'ui.sortable', 'ngAria', 'smart-table', 'xeditable'])

    .controller('missionsListController', function ($scope) {
        // Represents a missionSetting
        $scope.MissionSetting = function(id, name, category, disallowReward, individualSuccessEnabled, successChance, missionLevel, playerLevel, positionInList) {
            this.id = id;
            this.name = name;
            this.category = category;
            this.disallowReward = disallowReward;
            this.individualSuccessEnabled = individualSuccessEnabled;
            this.successChance = successChance;
            this.missionLevel = missionLevel;
            this.playerLevel = playerLevel;
        };

        $scope.checkEmpty = function(data) {
            if (!data || typeof data == 'undefined' || data == '' || data == "empty")
            {
                return "You must enter a value.";
            }
        };

        $scope.sortingLog = [];

        $scope.sortableOptions = {
            update: function(e, ui) {
                var logEntry = $scope.MissionRewards.map(function(i){
                    return i.id;
                }).join(', ');
                $scope.sortingLog.push('Update: ' + logEntry);
                console.debug("test update");
            },
            stop: function(e, ui) {
                // this callback has the changed model
                var logEntry = $scope.MissionRewards.map(function(i){
                    return i.id;
                }).join(', ');
                $scope.sortingLog.push('Stop: ' + logEntry);
                console.debug("test stop");
            }
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
                $scope.MissionRewards[i] = new $scope.MissionSetting(missionId, mission[0], mission[1], parseInt(mission[2]), parseInt(mission[3]), Boolean(mission[4]), Boolean(mission[5]), parseInt(mission[6]));
            }
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
                    new $scope.MissionSetting("1", "FollowerExperience", "FollowerExperience", false, true, 10, 15, 20, 0),
                    new $scope.MissionSetting("1", "adada", "FollowerExperience", false, true, 10, 15, 20, 10),
                    new $scope.MissionSetting("1", "fefefef", "fcesrf", false, true, 10, 15, 20, 1),
                    new $scope.MissionSetting("1", "gtghrthsht", "FollowerExperience", false, true, 10, 15, 20, 2),
                    new $scope.MissionSetting("2", "Gold", "Gold", false, false, 85, 90, 90, 3)
                ];
            }
        }
        $scope.MissionRewards = $scope.MissionRewards.sort(function(a, b) { return a.positionInList > b.positionInList; });

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
                $scope.saveRewardCustomChanceEnabled($scope.missionReward.individualSuccessEnabled, newValue);
            }
        );
    });
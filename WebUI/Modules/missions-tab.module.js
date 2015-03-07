///**
// * Created by Mickaël on 3/1/2015.
// */
//
// General Tab
angular.module('GarrisonButlerApp.missions-tab', ['ngMaterial', 'ui.sortable', 'ngAria', 'smart-table', 'xeditable'])

    .controller('missionsListController', function ($scope) {
        // Represents a missionSetting
        $scope.MissionSetting = function(rewardId, rewardName, rewardCategory, disallowReward, individualSuccessEnabled, successChance, missionLevel, playerLevel) {
            this.rewardId = rewardId;
            this.rewardName = rewardName;
            this.rewardCategory = rewardCategory;
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


        $scope.updateReward = function(reward) {
            $scope.updateRewardById(reward.rewardId, reward);
        };

        $scope.sortingLog = [];

        $scope.sortableOptions = {
            update: function(e, ui) {
                var logEntry = $scope.MissionRewards.map(function(i){
                    return i.rewardId;
                }).join(', ');
                $scope.sortingLog.push('Update: ' + logEntry);
                console.debug("test update");
            },
            stop: function(e, ui) {
                // this callback has the changed model
                var logEntry = $scope.MissionRewards.map(function(i){
                    return i.rewardId;
                });
                $scope.sortingLog.push('Stop: ' + logEntry);
                console.debug("test stop");
                $scope.updateRewardsOrder(logEntry);
            }
        };

        try
        {
            var rewards = JSON.parse($scope.loadRewards());
            $scope.GBDiagnostic("Received: " + rewards);
            $scope.MissionRewards = [];
            for (var i = 0; i < rewards.length; i++)
            {
                var rewardId = rewards[i];
                $scope.GBDiagnostic("Request for reward: " + rewardId);
                var reward = JSON.parse($scope.loadRewardById(rewardId));
                $scope.GBDiagnostic("Parsed: " + reward);
                $scope.MissionRewards[i] = new $scope.MissionSetting(rewardId, reward[0], reward[1], Boolean(reward[2]), Boolean(reward[3]), parseInt(reward[4]), parseInt(reward[5]), parseInt(reward[6]));
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
                    new $scope.MissionSetting("1", "FollowerExperience", "FollowerExperience", false, true, 10, 15, 20),
                    new $scope.MissionSetting("1", "adada", "FollowerExperience", false, true, 10, 15, 20, 10),
                    new $scope.MissionSetting("1", "fefefef", "fcesrf", false, true, 10, 15, 20, 1),
                    new $scope.MissionSetting("1", "gtghrthsht", "FollowerExperience", false, true, 10, 15, 20, 2),
                    new $scope.MissionSetting("2", "Gold", "Gold", false, false, 85, 90, 90, 3)
                ];
            }
        }

    })
    .controller('RewardController', function ($scope) {
        $scope.init = function (item) {
            $scope.missionReward = item;
        };


        $scope.$watch(
            'missionReward',
            function (newValue, oldValue)
            {
                $scope.updateReward(newValue);
            },
            true
        );
    });
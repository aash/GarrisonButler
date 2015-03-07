///**
// * Created by Mickaël on 3/1/2015.
// */
//
// General Tab
angular.module('GarrisonButlerApp.missions-tab', ['ngMaterial', 'ngAria', 'smart-table', 'xeditable'])

    .controller('missionsListController', function ($scope) {
        // Represents a missionSetting
        $scope.MissionSetting = function(rewardId, rewardName, rewardCategory, disallowReward, individualSuccessEnabled, successChance, missionLevel, playerLevel, isCategory, priorityList) {
            this.rewardId = rewardId;
            this.rewardName = rewardName;
            this.rewardCategory = rewardCategory;
            this.disallowReward = disallowReward;
            this.individualSuccessEnabled = individualSuccessEnabled;
            this.successChance = successChance;
            this.missionLevel = missionLevel;
            this.playerLevel = playerLevel;
            this.isCategory = isCategory;
            this.priorityList = priorityList;
        };

        $scope.checkEmpty = function(data) {
            if (!data || typeof data == 'undefined' || data == '' || data == "empty")
            {
                return "You must enter a value.";
            }
        };

        $scope.updateList = function() {
            $scope.MissionRewards = $scope.MissionRewards.sort(function(a, b) { return a.priorityList > b.priorityList; });
        };


        $scope.canWowhead = function(reward)
        {
            if(reward.isCategory)
            {
                return false;
            }

            var cat = reward.rewardCategory;
            if(
                cat === "PlayerGear" ||
                cat === "FollowerContract" ||
                cat === "FollowerGear" ||
                cat === "FollowerItem" ||
                cat === "ReputationToken" ||
                cat === "VanityItem" ||
                cat === "Profession" ||
                cat === "MiscItem")
            {
                return true;
            }
            return false;
        };

        $scope.MissionRewards = [];

        try
        {
            $scope.updateReward = function(reward) {
                $scope.updateRewardById(reward.rewardId, reward);
            };
            var rewards = JSON.parse($scope.loadRewards());
            $scope.GBDiagnostic("Received: " + rewards);
            for (var i = 0; i < rewards.length; i++)
            {
                var rewardId = rewards[i];
                $scope.GBDiagnostic("Request for reward: " + rewardId);
                var reward = JSON.parse($scope.loadRewardById(rewardId));
                $scope.GBDiagnostic("Parsed: " + reward);
                $scope.MissionRewards[i] = new $scope.MissionSetting(rewardId, reward[0], reward[1], Boolean(reward[2]), Boolean(reward[3]), parseInt(reward[4]), parseInt(reward[5]), parseInt(reward[6]), Boolean(reward[7]), i);
            }
        }
        catch(e) {
            try
            {
                $scope.GBDiagnostic("Request for missions rewards error: " + e);
                $scope.MissionRewards = [
                    // Mine / Garden
                    new $scope.MissionSetting("1", "FollowerExperience", "FollowerExperience", false, true, 10, 15, 20, false, 1),
                    new $scope.MissionSetting("1", "adada", "FollowerExperience", false, true, 10, 15, 20, false, 2),
                    new $scope.MissionSetting("1", "fefefef", "fcesrf", false, true, 10, 15, 20, false, 3),
                    new $scope.MissionSetting("1", "gtghrthsht", "FollowerExperience", false, true, 10, 15, 20, false, 4),
                    new $scope.MissionSetting("2", "Gold", "Gold", false, false, 85, 90, 90, false, 5)
                ];
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





        $scope.$watch(
            'startMissions',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("StartMissions", newValue);
            }
        );

        $scope.$watch(
            'completedMissions',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("CompletedMissions", newValue);
            }
        );

        $scope.$watch(
            'AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers", newValue);
            }
        );

        $scope.$watch(
            'DefaultMissionSuccessChance',
            function (newValue, oldValue)
            {
                $scope.saveCSharpInt("DefaultMissionSuccessChance", newValue);
            }
        );

        $scope.$watch(
            'UseEpicMaxLevelFollowersToBoostLowerFollowers',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("UseEpicMaxLevelFollowersToBoostLowerFollowers", newValue);
            }
        );

        $scope.$watch(
            'MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting',
            function (newValue, oldValue)
            {
                $scope.saveCSharpInt("MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting", newValue);
            }
        );

        $scope.$watch(
            'MinimumMissionLevel',
            function (newValue, oldValue)
            {
                $scope.saveCSharpInt("MinimumMissionLevel", newValue);
            }
        );
        $scope.$watch(
            'MinimumGarrisonResourcesToStartMissions',
            function (newValue, oldValue)
            {
                $scope.saveCSharpInt("MinimumGarrisonResourcesToStartMissions", newValue);
            }
        );
        $scope.$watch(
            'PreferFollowersWithScavengerForGarrisonResourcesReward',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("PreferFollowersWithScavengerForGarrisonResourcesReward", newValue);
            }
        );
        $scope.$watch(
            'PreferFollowersWithTreasureHunterForGoldReward',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("PreferFollowersWithTreasureHunterForGoldReward", newValue);
            }
        );
        $scope.$watch(
            'DisallowScavengerOnNonGarrisonResourcesMissions',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("DisallowScavengerOnNonGarrisonResourcesMissions", newValue);
            }
        );
        $scope.$watch(
            'DisallowTreasureHunterOnNonGoldMissions',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("DisallowTreasureHunterOnNonGoldMissions", newValue);
            }
        );
        $scope.$watch(
            'DisallowRushOrderRewardIfBuildingDoesntExist',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("DisallowRushOrderRewardIfBuildingDoesntExist", newValue);
            }
        );

        try
        {
            $scope.startMissions = $scope.loadCSharpBool("StartMissions");
            $scope.completedMissions = $scope.loadCSharpBool("CompletedMissions");
            $scope.AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers = $scope.loadCSharpBool("AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers");
            $scope.DefaultMissionSuccessChance = $scope.loadCSharpInt("DefaultMissionSuccessChance");
            $scope.UseEpicMaxLevelFollowersToBoostLowerFollowers = $scope.loadCSharpBool("UseEpicMaxLevelFollowersToBoostLowerFollowers");
            $scope.MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting = $scope.loadCSharpInt("MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting");
            $scope.MinimumMissionLevel = $scope.loadCSharpInt("MinimumMissionLevel");
            $scope.MinimumGarrisonResourcesToStartMissions = $scope.loadCSharpInt("MinimumGarrisonResourcesToStartMissions");
            $scope.PreferFollowersWithScavengerForGarrisonResourcesReward = $scope.loadCSharpBool("PreferFollowersWithScavengerForGarrisonResourcesReward");
            $scope.PreferFollowersWithTreasureHunterForGoldReward = $scope.loadCSharpBool("PreferFollowersWithTreasureHunterForGoldReward");
            $scope.DisallowScavengerOnNonGarrisonResourcesMissions = $scope.loadCSharpBool("DisallowScavengerOnNonGarrisonResourcesMissions");
            $scope.DisallowTreasureHunterOnNonGoldMissions = $scope.loadCSharpBool("DisallowTreasureHunterOnNonGoldMissions");
            $scope.DisallowRushOrderRewardIfBuildingDoesntExist = $scope.loadCSharpBool("DisallowRushOrderRewardIfBuildingDoesntExist");

        }
        catch (e)
        {
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
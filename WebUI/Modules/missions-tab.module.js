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

        $scope.MissionEntry = function(variableName, variableType, label, description) {
            this.variableName = variableName;
            this.label = label;
            this.description = description;
            this.variableType = variableType;
        };



        $scope.checkEmpty = function(data) {
            if (!data || typeof data == 'undefined' || data == '' || data == "empty")
            {
                return "You must enter a value.";
            }
        };

        $scope.updateList = function () {
            $scope.GBDiagnostic("updateList");
            $scope.MissionRewards = $scope.MissionRewards.sort(function(a, b) { return a.priorityList >= b.priorityList; });

            for (var i = 0; i < $scope.MissionRewards.length; i++)
            {
                $scope.MissionRewards[i].priorityList = i;
            }
        };

        $scope.canWowhead = function (reward)
        {
            return $scope.canWowheadItem(reward) || $scope.canWowheadCurrency(reward);
        };

        $scope.canWowheadCurrency = function (reward)
        {
            if (reward.isCategory)
            {
                return false;
            }

            var cat = reward.rewardCategory;
            if (cat === "Currency")
            {
                return true;
            }

            return false;
        };


        $scope.canWowheadItem = function(reward)
        {
            if(reward.isCategory)
            {
                return false;
            }

            var cat = reward.rewardCategory;
            if(
                cat === "PlayerGear" ||
                cat === "PlayerExperience" ||
                cat === "LegendaryQuestItem" ||
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
        $scope.MissionGeneralOptions = [];

        $scope.MissionGeneralOptions = [
            new $scope.MissionEntry(
                'StartMissions',
                'bool',
                'Start Missions',
                'Activate to let the Butler start missions based on the rest of the settings.'),

            new $scope.MissionEntry(
                'CompletedMissions',
                'bool',
                'Collect Rewards',
                'Activate to let the Butler collect rewards from completed missions.'),

            new $scope.MissionEntry(
                'AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers',
                'bool',
                'Use Epic Followers to fill slots',
                "On a mission that rewards FollowerXP, this will allow GB to fill all slots with Epic Max Level followers for that mission.  This is normally NOT ideal because Epic Max Level followers don't benefit from XP."),

            new $scope.MissionEntry(
                'UseEpicMaxLevelFollowersToBoostLowerFollowers',
                'bool',
                'Boost XP with epic followers',
                "When enabled, this specifies the maximum number of Epic Max Level Followers that are allowed to be slotted into a mission."),

            new $scope.MissionEntry(
                'PreferFollowersWithScavengerForGarrisonResourcesReward',
                'bool',
                'Scavenger priority for Garrison resources',
                "This will consider followers with the Scavenger trait as highest priority on missions that reward Garrison Resources."),

            new $scope.MissionEntry(
                'PreferFollowersWithTreasureHunterForGoldReward',
                'bool',
                'Treasure Hunter priority',
                "This will consider followers with the Treasure Hunter trait as highest priority on missions that reward Gold."),

            new $scope.MissionEntry(
                'DisallowScavengerOnNonGarrisonResourcesMissions',
                'bool',
                'Keep Scavenger for Garrison resources',
                "This will PREVENT assigning followers with the Scavenger trait on ANY mission that DOESN'T have Garrison Resources as a reward."),

            new $scope.MissionEntry(
                'DisallowTreasureHunterOnNonGoldMissions',
                'bool',
                'Keep Treasure Hunter for Gold missions',
                "This will PREVENT assigning followers with the Treasure Hunter trait on ANY mission that DOESN'T have Gold as a reward."),

            new $scope.MissionEntry(
                'DisallowRushOrderRewardIfBuildingDoesntExist',
                'bool',
                'Disallow Rush Order based on buildings',
                "This will prevent starting missions that reward a Rush Order for a building that you DON'T HAVE."),

            new $scope.MissionEntry(
                'DefaultMissionSuccessChance',
                'int',
                'Minimum Success Chance',
                'This value is the one used for rewards where the custom success chances are not activated.'),
            new $scope.MissionEntry(
                'MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting',
                'int',
                'Max Number of Epic followers for Boost',
                "Specifies the maximum number of Epic Max Level Followers that are allowed to be slotted into a mission."),
            new $scope.MissionEntry(
                'MinimumMissionLevel',
                'int',
                'Minimum Mission Level',
                "Minimum required mission level to start a mission"),
            new $scope.MissionEntry(
                'MinimumGarrisonResourcesToStartMissions',
                'int',
                'Minimum Garrison Resources',
                'Minimum required Garrison Resources to start a mission.')
        ];

        try
        {



            for (var i = 0; i < $scope.MissionGeneralOptions.length; i++)
            {
                var option = $scope.MissionGeneralOptions[i];

                if(option.variableType === "bool")
                {
                    try{
                        option.val = $scope.loadCSharpBool(option.variableName);
                    }
                    catch(e)
                    {
                        option.val = false;
                    }
                }
                else if(option.variableType === "int")
                {
                    try{
                        option.val = $scope.loadCSharpInt(option.variableName);
                    }
                    catch(e)
                    {
                        option.val = 0;
                    }
                }
            }


            $scope.updateReward = function (reward) {
                $scope.GBDiagnostic("updateReward: " + reward.rewardId);
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
                    new $scope.MissionSetting("1", "FollowerExperience", "FollowerExperience", false, true, 10, 15, 20, true, 1),
                    new $scope.MissionSetting("1", "adada", "FollowerExperience", false, true, 10, 15, 20, false, 2),
                    new $scope.MissionSetting("1", "fefefef", "fcesrf", false, true, 10, 15, 20, false, 3),
                    new $scope.MissionSetting("1", "gtghrthsht", "FollowerExperience", false, true, 10, 15, 20, false, 4),
                    new $scope.MissionSetting("2", "Gold", "Gold", false, false, 85, 90, 90, true, 5)
                ];
            }
            catch(e)
            {
                $scope.MissionRewards = [
                    // Mine / Garden
                    new $scope.MissionSetting("1", "FollowerExperience", "FollowerExperience", false, true, 10, 15, 20, true, 1),
                    new $scope.MissionSetting("1", "adada", "FollowerExperience", false, true, 10, 15, 20, false, 2),
                    new $scope.MissionSetting("1", "fefefef", "fcesrf", false, true, 10, 15, 20, false, 3),
                    new $scope.MissionSetting("1", "gtghrthsht", "FollowerExperience", false, true, 10, 15, 20, false, 4),
                    new $scope.MissionSetting("2", "Gold", "Gold", false, false, 85, 90, 90, true, 5)
                ];
            }
        }


    })
    .controller('MissionOptionController', function ($scope) {
        $scope.init = function (item) {
            $scope.option = item;
        };

        $scope.$watch(
            'option',
            function (newValue, oldValue)
            {
                if(newValue.variableType === "bool")
                    $scope.saveCSharpBool(newValue.variableName, newValue.val);
                else if(newValue.variableType === "int")
                    $scope.saveCSharpInt(newValue.variableName, newValue.val);
            },
            true
        );
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






//
//
//$scope.$watch(
//    'startMissions.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("StartMissions", newValue);
//    }
//);
//
//$scope.$watch(
//    'completedMissions.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("CompletedMissions", newValue);
//    }
//);
//
//$scope.$watch(
//    'AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("AllowFollowerXPMissionsToFillAllSlotsWithEpicMaxLevelFollowers", newValue);
//    }
//);
//
//$scope.$watch(
//    'DefaultMissionSuccessChance.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpInt("DefaultMissionSuccessChance", newValue);
//    }
//);
//
//$scope.$watch(
//    'UseEpicMaxLevelFollowersToBoostLowerFollowers.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("UseEpicMaxLevelFollowersToBoostLowerFollowers", newValue);
//    }
//);
//
//$scope.$watch(
//    'MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpInt("MaxNumberOfEpicMaxLevelFollowersToUseWhenBoosting", newValue);
//    }
//);
//
//$scope.$watch(
//    'MinimumMissionLevel.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpInt("MinimumMissionLevel", newValue);
//    }
//);
//$scope.$watch(
//    'MinimumGarrisonResourcesToStartMissions.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpInt("MinimumGarrisonResourcesToStartMissions", newValue);
//    }
//);
//$scope.$watch(
//    'PreferFollowersWithScavengerForGarrisonResourcesReward.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("PreferFollowersWithScavengerForGarrisonResourcesReward", newValue);
//    }
//);
//$scope.$watch(
//    'PreferFollowersWithTreasureHunterForGoldReward.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("PreferFollowersWithTreasureHunterForGoldReward", newValue);
//    }
//);
//$scope.$watch(
//    'DisallowScavengerOnNonGarrisonResourcesMissions.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("DisallowScavengerOnNonGarrisonResourcesMissions", newValue);
//    }
//);
//$scope.$watch(
//    'DisallowTreasureHunterOnNonGoldMissions.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("DisallowTreasureHunterOnNonGoldMissions", newValue);
//    }
//);
//$scope.$watch(
//    'DisallowRushOrderRewardIfBuildingDoesntExist.variable',
//    function (newValue, oldValue)
//    {
//        $scope.saveCSharpBool("DisallowRushOrderRewardIfBuildingDoesntExist", newValue);
//    }
//);
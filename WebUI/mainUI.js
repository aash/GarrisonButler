angular.module('GarrisonButlerApp',
    [
        'ui.bootstrap',
        'GarrisonButlerApp.general-tab',
        'GarrisonButlerApp.work-order-tab',
        'GarrisonButlerApp.profession-tab',
        'GarrisonButlerApp.milling-tab',
        'GarrisonButlerApp.mailing-tab',
        'GarrisonButlerApp.trading-post-tab',
        'GarrisonButlerApp.missions-tab'
    ]);

angular.module('GarrisonButlerApp').controller('MainController', function ($scope, $window) {

    $scope.lastErrorArray = [];
    $scope.lastErrorArray.push("Start debug");
    $scope.lastError = "";
    $scope.Diagnostic = function (msg) {
        try {
            $scope.lastErrorArray.push(msg.toLocaleString());
            $scope.lastError = $scope.lastErrorArray.join("\n");

            window.external.diagnosticJs(msg.toLocaleString());
        }
        catch(e) {
            console.log(msg);
        }
    };

    try {
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

        // Save boolean from js value in c# code
        $scope.saveCSharpInt = function (propertyName, value) {
            window.external.UpdateIntValue(propertyName, value);
        };

        // Load boolean in js value from c# code
        $scope.loadCSharpInt = function (propertyName) {
            var res = window.external.GetIntValue(propertyName);
            return res;
        };


        // Load buildings id in js value from c# code
        $scope.loadBuildings = function () {
            var res = window.external.getBuildingsJs();
            return res;
        };
        // Load building in js value from c# code
        $scope.loadBuildingById = function (buildingId) {
            var res = window.external.getBuildingById(buildingId);
            return res;
        };
        // Save building to c#
        $scope.saveBuildingCanCollect = function (buildingId, value) {
            window.external.saveBuildingCanCollectOrder(buildingId, value);
        };
        $scope.saveBuildingCanStart = function (buildingId, value) {
            window.external.saveBuildingCanStartOrder(buildingId, value);
        };
        $scope.saveBuildingMaxStart = function (buildingId, value) {
            window.external.saveBuildingMaxCanStart(buildingId, value);
        };


        // Load dailies id in js value from c# code
        $scope.loadDailies = function () {
            var res = window.external.getDailyCdJs();
            return res;
        };

        // Load daily by id in js value from c# code
        $scope.loadDailyById = function (Id) {
            var res = window.external.getDailyCdById(Id);
            return res;
        };
        $scope.saveDailyCd = function (itemId, activated) {
            window.external.saveDailyCd(itemId, activated);
        };


        // Load dailies id in js value from c# code
        $scope.loadMilling = function () {
            var res = window.external.getMillingJs();
            return res;
        };

        // Load daily by id in js value from c# code
        $scope.loadMillingById = function (Id) {
            var res = window.external.getMillingById(Id);
            return res;
        };
        $scope.saveMillingItem = function (itemId, activated) {
            window.external.saveMillingItem(itemId, activated);
        };


        $scope.loadTP = function () {
            var res = window.external.getTPJs();
            return res;
        };

        // Load daily by id in js value from c# code
        $scope.loadTPById = function (Id) {
            var res = window.external.getTPById(Id);
            return res;
        };
        $scope.saveTPItem = function (itemId, activated) {
            window.external.saveTPItem(itemId, activated);
        };


        $scope.loadMailConditions = function () {
            var res = window.external.getMailConditions();
            return res;
        };

        $scope.loadMails = function () {
            var res = window.external.getMailsJs();
            return res;
        };
        $scope.loadMailById = function (itemId) {
            var res = window.external.getMailById(itemId);
            return res;
        };

        $scope.deleteMail = function (itemId) {
            window.external.deleteMailById(itemId);
        };

        $scope.saveMail = function (mailItem) {
            var mailToJson = [];
            mailToJson[0] = mailItem.itemId;
            mailToJson[1] = mailItem.recipient;
            mailToJson[2] = mailItem.condition;
            mailToJson[3] = mailItem.value;
            mailToJson[4] = mailItem.comment;
            var json = JSON.stringify(mailToJson);
            window.external.saveMail(json);
        };


        $scope.loadRewards = function () {
            var res = window.external.getRewardsJs();
            return res;
        };

        // Load daily by id in js value from c# code
        $scope.loadRewardById = function (Id) {
            var res = window.external.getRewardsById(Id);
            return res;
        };

        $scope.updateReward = function (reward) {
            try {
                var rewardJson = [];
                rewardJson[0] = reward.disallowReward;
                rewardJson[1] = reward.individualSuccessEnabled;
                rewardJson[2] = reward.successChance;
                rewardJson[3] = reward.missionLevel;
                rewardJson[4] = reward.playerLevel;
                rewardJson[5] = reward.rewardId;
                var json = JSON.stringify(rewardJson);
                window.external.updateRewardById(json);
            }
            catch(e)
            {
                $scope.Diagnostic(e);
            }
        };
        $scope.updateRewardsOrder = function (listIds) {
            var json = JSON.stringify(listIds);
            window.external.updateRewardsOrder(json);
        };

        $scope.VersionNumber = function () {
            return window.external.GetVersionNumber();
        };


        $scope.isIceVersion = function () {
            return window.external.IsIceVersion();
        };
    }
    catch(e)
    {
        $scope.Diagnostic(e);
    }
})

// This CSS class-based directive controls the pre-bootstrap loading screen. By
// default, it is visible on the screen; but, once the application loads, we'll
// fade it out and remove it from the DOM.
// --
// NOTE: Normally, I would probably just jQuery to fade-out the container; but,
// I thought this would be a nice moment to learn a bit more about AngularJS
// animation. As such, I'm using the ng-leave workflow to learn more about the
// ngAnimate module.
.directive(
    "mAppLoading",
    function( $animate ) {

        // Return the directive configuration.
        return({
            link: link,
            restrict: "C"
        });


        // I bind the JavaScript events to the scope.
        function link( scope, element, attributes ) {

            // Due to the way AngularJS prevents animation during the bootstrap
            // of the application, we can't animate the top-level container; but,
            // since we added "ngAnimateChildren", we can animated the inner
            // container during this phase.
            // --
            // NOTE: Am using .eq(1) so that we don't animate the Style block.
            $animate.leave( element.children().eq( 1 ) ).then(
                function cleanupAfterAnimation() {

                    // Remove the root directive element.
                    element.remove();

                    // Clear the closed-over variable references.
                    scope = element = attributes = null;

                }
            );

        }

    }
);
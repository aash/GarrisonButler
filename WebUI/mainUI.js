angular.module('GarrisonButlerApp',
    [
        'ui.bootstrap',
        'GarrisonButlerApp.general-tab',
        'GarrisonButlerApp.work-order-tab',
        'GarrisonButlerApp.profession-tab',
        'GarrisonButlerApp.milling-tab',
        'GarrisonButlerApp.mailing-tab',
        'GarrisonButlerApp.trading-post-tab'
    ]);

angular.module('GarrisonButlerApp').controller('MainController', function ($scope, $window) {
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


    $scope.GBDiagnostic = function (msg) {
        window.external.diagnosticJs(msg);
    };
    $scope.VersionNumber = function() {
        return window.external.GetVersionNumber();
    };


    $scope.isIceVersion = function() {
        return window.external.IsIceVersion();
    };

});
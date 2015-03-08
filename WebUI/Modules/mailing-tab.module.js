/**
 * Created by Mickaël on 2/1/2015.
 */

// General Tab
angular.module('GarrisonButlerApp.mailing-tab', ['ngMaterial', 'ngAria', 'smart-table', 'xeditable'])

    .controller('mailingListController', function ($scope) {

        // Represents a mail item
        $scope.MailItem = function(itemId, recipient, condition, value, comment) {
            this.itemId = itemId;
            this.recipient = recipient;
            this.condition = condition;
            this.value = value;
            this.comment = comment;
        };

        $scope.mailItems = [];

        $scope.removeRow = function(row) {
            var index = $scope.mailItems.indexOf(row);
            if (index !== -1) {
                $scope.deleteMail(row.itemId);
                $scope.mailItems.splice(index, 1);

            }
        };

        $scope.addRow = function() {
            $scope.inserted = new $scope.MailItem(0,"","",0,"");
            $scope.mailItems.push($scope.inserted);
        };

        $scope.checkItemId = function(data, row) {
            if (!data)
            {
                return "You must enter a value.";
            }

            for (var i = 0; i < $scope.mailItems.length; i++)
            {
                if ($scope.mailItems[i] != row && $scope.mailItems[i].itemId == data) {
                    return "A rule with the same Item ID already exists.";
                }
            }
        };

        $scope.checkEmpty = function(data) {
            if (!data || typeof data == 'undefined' || data == '' || data == "empty")
            {
                return "You must enter a value.";
            }
        };

        $scope.checkEmptyInt = function(data) {
            if (!data || typeof data == 'undefined' || data == '' || data == "empty")
            {
                data = 0;
            }
        };


        $scope.saveMailToCSharp = function(data) {
            console.log("Current data:" + data);
            try{
                $scope.saveMail(data);
            }
            catch(e)
            {

            }
        };



        try
        {
            $scope.conditions = JSON.parse($scope.loadMailConditions());
            var itemsIds = JSON.parse($scope.loadMails());

            // In case one element only, cast as an array
            if( typeof itemsIds === 'string' ) {
                itemsIds = [ itemsIds ];
            }
            for (var i = 0; i < itemsIds.length; i++)
            {
                var itemId = itemsIds[i];
                var mailItem = JSON.parse($scope.loadMailById(itemId));
                $scope.mailItems[i] = new $scope.MailItem(itemId, mailItem[0], mailItem[1], parseInt(mailItem[2]), mailItem[3]);
            }


        }
        catch(e) {
            try {
            }
            catch(e2){}
            $scope.mailItems = [
                new $scope.MailItem(109124, "superStar", "Keep in Bags at least", 50, "Herb-Frostweed"),
                new $scope.MailItem(109125, "Eranette", "if >= in bags", 110, "Herb-Fireweed"),
                new $scope.MailItem(109118, "Lilitur", "if > in bags", 666, "Ore1"),
                new $scope.MailItem(109119, "Lilitur", "if > in bags", 666, "Ore2")
            ];
        }


        $scope.$watch(
            'retrieveMail',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("RetrieveMail", newValue);
            }
        );

        $scope.$watch(
            'sendMail',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("SendMail", newValue);
            }
        );

        $scope.$watch(
            'sendGreen',
            function (newValue, oldValue)
            {
                $scope.saveCSharpBool("SendDisenchantableGreens", newValue);
            }
        );

        $scope.$watch(
            'recipientGreen',
            function (newValue, oldValue)
            {
                window.external.UpdateGreenToCharRecipient(newValue);
            }
        );

        try
        {
            $scope.retrieveMail = $scope.loadCSharpBool("RetrieveMail");
            $scope.sendMail = $scope.loadCSharpBool("SendMail");
            $scope.sendGreen = $scope.loadCSharpBool("SendDisenchantableGreens")
            $scope.recipientGreen = window.external.GetGreenToCharRecipient();
        }
        catch (e)
        {
        }

    });


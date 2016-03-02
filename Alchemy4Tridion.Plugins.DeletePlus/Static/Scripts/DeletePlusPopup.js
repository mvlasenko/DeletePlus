/**
 * Handles all functionality of my popup window
 */
!(function () {
    // Alchemy comes with jQuery and several other libraries already pre-installed so assigning
    // it a variable here eliminates the redundancy of loading my own copy, and avoids any conflicts over
    // the $ character
    $j = Alchemy.library("jQuery");

    // Grabs the URL of the popup and gets the TCM of the item selected in Tridion from the querystring
    var url = location.href;
    var tcm = url.substring(url.indexOf("uri=tcm%3A") + 10, url.indexOf("#"));

    // On page load I display the items not in use within the folder defined by tcm.
    loadItemsToDelete(tcm);

    /**
     * Takes a TCM ID for a Tridion item and retrieves the a list of the dependent items
     */
    function loadItemsToDelete(tcmInput) {

        //disable buttons
        $j("#delete_item").removeClass("enabled");
        $j("#delete_item").addClass("disabled");

        $j("#refresh_items").removeClass("enabled");
        $j("#refresh_items").addClass("disabled");

        //enable progress bar
        $j("#progBar").show();

        // This is the call to my controller where the core service code is used get the list of items
        Alchemy.Plugins["${PluginName}"].Api.DeletePlusService.getItemsToDelete(tcmInput).success(function (items) {

            //disable progress bar
            $j("#progBar").hide();

            $j(".tab-body.active").empty();
            $j(".tab-body.active").append(items);

            //change buttons visibility

            $j("#delete_item").removeClass("disabled");
            $j("#delete_item").addClass("enabled");

            $j("#refresh_items").removeClass("disabled");
            $j("#refresh_items").addClass("enabled");

            //register button handlers

            $j("#delete_item.enabled").click(function () {
                forceDelete(tcm);
            });

            $j("#refresh_items.enabled").click(function () {
                loadItemsToDelete(tcm);
            });

        })
        .error(function (type, error) {
            // First arg is a string that shows the type of error i.e. (500 Internal), 2nd arg is object representing
            // the error.  For BadRequests and Exceptions, the error message will be in the error.message property.
            console.log("There was an error", error.message);
        })
        .complete(function () {
            // this is called regardless of success or failure.
        });
    }

    function forceDelete(tcmInput) {
        
        //disable buttons
        $j("#delete_item").removeClass("enabled");
        $j("#delete_item").addClass("disabled");

        $j("#refresh_items").removeClass("enabled");
        $j("#refresh_items").addClass("disabled");

        //enable progress bar
        $j("#progBar").show();

        // This is the call to my controller where the core service code is used get the list of items
        Alchemy.Plugins["${PluginName}"].Api.DeletePlusService.getDeletedItems(tcmInput).success(function (items) {

            //disable progress bar
            $j("#progBar").hide();

            $j(".tab-body.active").empty();
            $j(".tab-body.active").append(items);

        })
        .error(function (type, error) {
            // First arg is a string that shows the type of error i.e. (500 Internal), 2nd arg is object representing
            // the error.  For BadRequests and Exceptions, the error message will be in the error.message property.
            console.log("There was an error", error.message);
        })
        .complete(function () {
            // this is called regardless of success or failure.
        });
    }

})();
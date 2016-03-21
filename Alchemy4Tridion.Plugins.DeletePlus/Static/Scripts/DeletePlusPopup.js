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
    var tcm = location.href.substring(url.indexOf("uri=tcm%3A") + 10, url.indexOf("&"));
    // From the page url we want to get the title so we can update our first tab with the title
    var title = location.href.substring(url.indexOf("title=") + 6, url.indexOf("#")).replace(/\%20/g, ' ');

    // On page load I display the items not in use within the folder defined by tcm.
    loadItemsToDelete(tcm, title);

    /**
     * Takes a TCM ID for a Tridion item and retrieves the a list of the dependent items
     */
    function loadItemsToDelete(tcmInput, title) {

        //disable buttons
        $j("#delete_item").removeClass("enabled");
        $j("#delete_item").addClass("disabled");

        $j("#go_to_item_location").removeClass("enabled");
        $j("#go_to_item_location").addClass("disabled");

        $j("#refresh_items").removeClass("enabled");
        $j("#refresh_items").addClass("disabled");

        $j("#close_window").removeClass("disabled");
        $j("#close_window").addClass("enabled");

        //enable progress bar
        $j("#progBar").show();

        // This is the call to my controller where the core service code is used get the list of items
        Alchemy.Plugins["${PluginName}"].Api.DeletePlusService.getItemsToDelete(tcmInput).success(function (items) {

            //disable progress bar
            $j("#progBar").hide();

            // Update the tab title
            $j(".tabs>div.active").empty();
            $j(".tabs>div.active").append(title);

            //show list of items
            $j(".tab-body.active").empty();
            $j(".tab-body.active").append(items);

            //detect if error happened
            var bSuccess = (items.lastIndexOf("error.png") == -1) && (items.lastIndexOf("warning.png") == -1) && (items.lastIndexOf("Look event log for details") == -1);

            //change buttons visibility

            if (bSuccess) {
                $j("#delete_item").removeClass("disabled");
                $j("#delete_item").addClass("enabled");
            }

            $j("#refresh_items").removeClass("disabled");
            $j("#refresh_items").addClass("enabled");

            $j("#close_window").removeClass("disabled");
            $j("#close_window").addClass("enabled");

            //register button handlers

            if (bSuccess) {
                $j("#delete_item.enabled").click(function () {
                    forceDelete(tcm);
                });
            }

            $j("#refresh_items.enabled").click(function () {
                loadItemsToDelete(tcm, title);
            });

            $j("#close_window.enabled").click(function () {
                closeWindow();
            });

            setupForItemClicked();

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

        $j("#go_to_item_location").removeClass("enabled");
        $j("#go_to_item_location").addClass("disabled");

        $j("#refresh_items").removeClass("enabled");
        $j("#refresh_items").addClass("disabled");

        $j("#close_window").removeClass("enabled");
        $j("#close_window").addClass("disabled");

        //enable progress bar
        $j("#progBar").show();

        // This is the call to my controller where the core service code is used get the list of items
        Alchemy.Plugins["${PluginName}"].Api.DeletePlusService.getDeletedItems(tcmInput).success(function (items) {

            //disable progress bar
            $j("#progBar").hide();

            $j(".tab-body.active").empty();
            $j(".tab-body.active").append(items);

            //change buttons visibility

            $j("#close_window").removeClass("disabled");
            $j("#close_window").addClass("enabled");

            //register button handlers

            $j("#close_window.enabled").click(function () {
                closeWindow();
            });

            setupForItemClicked();

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

    function closeWindow() {
        window.close();
    }

    function setupForItemClicked() {

        // We want to have an action when we click anywhere on the tab body
        // that isn't a used or using item
        $j(".tab-body").mouseup(function (e) {
            // To do this we first find the results item containing the not used items
            var results = $j(".results");
            if (!results.is(e.target) // if the target of the click isn't the results...
            && results.has(e.target).length === 0) // ... nor a descendant of the results
            {
                // deselect the current item
                $j(".item.selected").removeClass("selected");

                // disable go to location button
                $j("#go_to_item_location").removeClass("enabled");
                $j("#go_to_item_location").addClass("disabled");
            }
        });

        // An item is a Tridion item that is not being used by the current item (folder).
        // This is the click function for the items.
        $j(".item").click(function () {
            // When you click on an item we deselect any currently selected item
            $j(".item.selected").removeClass("selected");
            // And select the item you clicked on
            $j(this).addClass("selected");

            // Gets the selected item TCM
            var selectedItemId = $j(".item.selected").attr("id");

            //if id is set
            if (selectedItemId) {

                // enable go to location button
                $j("#go_to_item_location").removeClass("disabled");
                $j("#go_to_item_location").addClass("enabled");

                // These are all the click functions for the buttons at the bottom of the plugin.
                // They get set when we click on an item because we only want them to happen when
                // the buttons are enabled and the buttons only get enabled when an item is selected.

                $j("#go_to_item_location.enabled").click(function () {
                    // Runs the Tridion command to go to the location of the selected item in the original CM window
                    // Note that because this uses a $ rather than the $j assigned to JQuery this is actually
                    // using the Sizzler library from the Tridion CME
                    $cme.executeCommand("Goto", new Tridion.Cme.Selection(new Tridion.Core.Selection([selectedItemId])));

                    // deselect the current item
                    $j(".item.selected").removeClass("selected");
                });

            } else {
                // disable go to location button
                $j("#go_to_item_location").removeClass("enabled");
                $j("#go_to_item_location").addClass("disabled");

                // deselect the current item
                $j(".item.selected").removeClass("selected");
            }

        });
    }

})();
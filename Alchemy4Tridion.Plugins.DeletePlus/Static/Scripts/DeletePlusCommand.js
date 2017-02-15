/**
 * Creates an anguilla command using a wrapper shorthand.
 *
 * Note the ${PluginName} will get replaced by the actual plugin name.
 */
Alchemy.command("${PluginName}", "DeletePlus", {

    /**
     * If an init function is created, this will be called from the command's constructor when a command instance
     * is created.
     */
    init: function () {
        console.log("Init DeletePlus");
    },

    /**
     * Whether or not the command is enabled for the user (will usually have extensions displayed but disabled).
     * @returns {boolean}
     */
    isEnabled: function (selection) {

        var count = selection.getCount();
        if (!count)
            return false;

        for (var i = 0; i < count; i++) {

            var itemUri = selection.getItem(i);
            if (!itemUri)
                continue;

            var item = $models.getItem(itemUri);

            var containerUri = item.getOrganizationalItemId();
            if (containerUri.startsWith("tcm:0-")) // exclude top folder and SG
                return false;

            var itemType = $models.getItemType(itemUri);

            if (itemType === $const.ItemType.COMPONENT
                || itemType === $const.ItemType.FOLDER
                || itemType === $const.ItemType.PAGE
                || itemType === $const.ItemType.STRUCTURE_GROUP
                || itemType === $const.ItemType.KEYWORD
                || itemType === $const.ItemType.CATEGORY
                || itemType === $const.ItemType.SCHEMA
                || itemType === $const.ItemType.COMPONENT_TEMPLATE
                || itemType === $const.ItemType.PAGE_TEMPLATE
                || itemType === $const.ItemType.TEMPLATE_BUILDING_BLOCK) { }
            else {
                return false;
            }
        }

        return true;
    },

    /**
     * Whether or not the command is available to the user.
     * @returns {boolean}
     */
    isAvailable: function (selection) {

        var count = selection.getCount();
        if (!count)
            return false;

        for (var i = 0; i < count; i++) {

            var itemUri = selection.getItem(i);
            if (!itemUri)
                continue;

            var itemType = $models.getItemType(itemUri);

            if (itemType === $const.ItemType.COMPONENT
                || itemType === $const.ItemType.FOLDER
                || itemType === $const.ItemType.PAGE
                || itemType === $const.ItemType.STRUCTURE_GROUP
                || itemType === $const.ItemType.KEYWORD
                || itemType === $const.ItemType.CATEGORY
                || itemType === $const.ItemType.SCHEMA
                || itemType === $const.ItemType.COMPONENT_TEMPLATE
                || itemType === $const.ItemType.PAGE_TEMPLATE
                || itemType === $const.ItemType.TEMPLATE_BUILDING_BLOCK) { }
            else {
                return false;
            }
        }

        return true;
    },

    /**
     * Executes your command. You can use _execute or execute as the property name.
     */
    execute: function (selection) {

        var uri = "";
        var title = "";

        // Gets the item id and its title
        var count = selection.getCount();
        for (var i = 0; i < count; i++) {

            var itemUri = selection.getItem(i);
            uri += itemUri.replace("tcm:", "");
            title += $models.getItem(itemUri).getStaticTitle();

            if (i < count - 1) {
                uri += "|";
                title += " | ";
            }
        }

        // Sets the url of a popup window, passing through params for the ID of the selected folder/item
        var url = "${ViewsUrl}DeletePlusPopup.aspx?uri=" + uri + "&title=" + title;
        // Creates a popup with the above URL
        var popup = $popup.create(url, "menubar=no,location=no,resizable=no,scrollbars=no,status=no,width=800,height=450,top=10,left=10", null);
        popup.open();
    }

});
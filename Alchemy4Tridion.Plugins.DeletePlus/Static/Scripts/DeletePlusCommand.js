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

        if (selection.getCount() === 0)
            return false;

        var itemUri = selection.getItem(0);
        if (itemUri) {
            var itemType = $models.getItemType(itemUri);
            return itemType === $const.ItemType.COMPONENT
                || itemType === $const.ItemType.FOLDER
                || itemType === $const.ItemType.PAGE
                || itemType === $const.ItemType.STRUCTURE_GROUP
                || itemType === $const.ItemType.KEYWORD
                || itemType === $const.ItemType.CATEGORY
                || itemType === $const.ItemType.SCHEMA
                || itemType === $const.ItemType.COMPONENT_TEMPLATE
                || itemType === $const.ItemType.PAGE_TEMPLATE
                || itemType === $const.ItemType.TEMPLATE_BUILDING_BLOCK
            ;
        }

        return false;
    },

    /**
     * Whether or not the command is available to the user.
     * @returns {boolean}
     */
    isAvailable: function (selection) {

        if (selection.getCount() === 0)
            return false;

        var itemUri = selection.getItem(0);
        if (itemUri) {
            var itemType = $models.getItemType(itemUri);
            return itemType === $const.ItemType.FOLDER
                || itemType === $const.ItemType.STRUCTURE_GROUP
                || itemType === $const.ItemType.KEYWORD
                || itemType === $const.ItemType.CATEGORY
            ;
        }

        return false;
    },

    /**
     * Executes your command. You can use _execute or execute as the property name.
     */
    execute: function (selection) {

        // Gets the item id and its title
        var itemId = selection.getItem(0);
        var title = $models.getItem(itemId).getStaticTitle();
        // Sets the url of a popup window, passing through params for the ID of the selected folder/item
        var url = "${ViewsUrl}DeletePlusPopup.aspx?uri=" + itemId + "&title=" + title;
        // Creates a popup with the above URL
        var popup = $popup.create(url, "menubar=no,location=no,resizable=no,scrollbars=no,status=no,width=800,height=450,top=10,left=10", null);
        popup.open();
    }

});
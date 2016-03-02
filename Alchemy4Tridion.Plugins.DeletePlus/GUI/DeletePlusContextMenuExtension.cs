using Alchemy4Tridion.Plugins.GUI.Configuration;

namespace Alchemy4Tridion.Plugins.DeletePlus.GUI
{
    public class DeletePlusContextMenuExtension : ContextMenuExtension
    {
        public DeletePlusContextMenuExtension()
        {
            AssignId = "DeletePlus";

            // The name of the extension menu
            Name = "DeletePlusMenu";

            // Use this property to specify where in the context menu your items will go
            InsertBefore = Constants.ContextMenuIds.MainContextMenu.Delete;

            // Use AddItem() or AddSubMenu() to add items for this context menu

            //       element id      title        command name
            AddItem("delete_plus_cm", "Delete Plus...", "DeletePlus");

            // We need to add our resource group as a dependency to this extension
            Dependencies.Add<DeletePlusResourceGroup>();

            // apply the extension to a specific view.
            Apply.ToView(Constants.Views.DashboardView);
        }
    }
}

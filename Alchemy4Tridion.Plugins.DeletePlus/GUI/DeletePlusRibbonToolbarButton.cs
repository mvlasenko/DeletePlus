using Alchemy4Tridion.Plugins.GUI.Configuration;

namespace Alchemy4Tridion.Plugins.DeletePlus.GUI
{
    public class DeletePlusRibbonToolbarButton : RibbonToolbarExtension
    {
        public DeletePlusRibbonToolbarButton()
        {
            // The unique identifier used for the html element created.
            AssignId = "DeletePlusButton";

            // Using command instead of .ascx so we can position it correctly
            Command = "DeletePlus";

            // The label of the button.
            Name = "Delete Plus";

            // The page tab to assign this extension to. See Constants.PageIds.
            PageId = Constants.PageIds.HomePage;

            // Option GroupId, put this into an existing group (not capable if using a .ascx Control)
            GroupId = Constants.GroupIds.HomePage.ManageGroup;
            InsertBefore = "DeleteBtn";

            // The tooltip label that will get applied.
            Title = "Delete Plus";

            // We need to add our resource group as a dependency to this extension
            Dependencies.Add<DeletePlusResourceGroup>();

            // apply the extension to a specific view.
            Apply.ToView(Constants.Views.DashboardView, "DashboardToolbar");
        }
    }
}

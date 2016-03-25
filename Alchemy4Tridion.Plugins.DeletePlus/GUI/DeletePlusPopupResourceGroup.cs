using Alchemy4Tridion.Plugins.GUI.Configuration;

namespace Alchemy4Tridion.Plugins.DeletePlus.GUI
{
    public class DeletePlusPopupResourceGroup : ResourceGroup
    {
        public DeletePlusPopupResourceGroup()
        {
            // only the filename of our JS files are needed
            AddFile("DeletePlusPopup.js");
            // only the filename of our CSS files are needed
            AddFile("DeletePlusPopup.css");

            // Since Alchemy comes with several libraries I can reference JQuery this way and avoid having
            // to add it myself
            Dependencies.AddLibraryJQuery();
            Dependencies.Add("Tridion.Web.UI.Editors.CME");
            Dependencies.Add("Tridion.Web.UI.Editors.CME.commands");

            // If you want this resource group to contain the js proxies to call your webservice, call AddWebApiProxy()
            AddWebApiProxy();

            // Let's add our resources to the DeletePlusPopup.aspx page.  This will inject the resources without us having to manually edit it.
            AttachToView("DeletePlusPopup.aspx");
        }
    }
}

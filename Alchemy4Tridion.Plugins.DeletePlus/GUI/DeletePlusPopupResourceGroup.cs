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

            AddFile("favicon.png");

            // Since Alchemy comes with several libraries I can reference JQuery this way and avoid having
            // to add it myself
            Dependencies.AddLibraryJQuery();

            // If you want this resource group to contain the js proxies to call your webservice, call AddWebApiProxy()
            AddWebApiProxy();
        }
    }
}

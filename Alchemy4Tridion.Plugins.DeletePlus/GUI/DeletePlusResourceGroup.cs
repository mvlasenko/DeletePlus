using Alchemy4Tridion.Plugins.GUI.Configuration;

namespace Alchemy4Tridion.Plugins.DeletePlus.GUI
{
    public class DeletePlusResourceGroup : ResourceGroup
    {
        public DeletePlusResourceGroup()
        {
            // only the filename of our JS files are needed
            AddFile("DeletePlusCommand.js");
            // only the filename of our CSS files are needed
            AddFile("Styles.css");
            // add genertic type param to reference our command set
            AddFile<DeletePlusCommandSet>();

            // If you want this resource group to contain the js proxies to call your webservice, call AddWebApiProxy()
            AddWebApiProxy();
        }
    }
}

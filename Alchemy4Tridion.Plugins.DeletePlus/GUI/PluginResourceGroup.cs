using Alchemy4Tridion.Plugins.GUI.Configuration;
using Alchemy4Tridion.Plugins.GUI.Configuration.Elements;

namespace Alchemy4Tridion.Plugins.DeletePlus.GUI
{
    public class PluginResourceGroup : Alchemy4Tridion.Plugins.GUI.Configuration.ResourceGroup
    {
        public PluginResourceGroup()
        {
            // only the filename of our JS files are needed
            AddFile("SayHelloCommand.js");
            // only the filename of our CSS files are needed
            AddFile("Styles.css");
            // add genertic type param to reference our command set
            AddFile<PluginCommandSet>();

            // Adds the web api proxy JS to this resource group... this allows us to call
            // our webapi service without any 3rd party libs.
            AddWebApiProxy();

            // AddWebApiProxy() includes Alchemy.Core as a dependency already... if not using
            // the proxy you can remove the comment from below.

            // Dependencies.AddAlchemyCore();
        }
    }
}

using Alchemy4Tridion.Plugins;
using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace Alchemy4Tridion.Plugins.DeletePlus.Controllers
{
    /// <summary>
    /// An ApiController to create web services that your plugin can interact with.
    /// </summary>
    /// <remarks>
    /// The AlchemyRoutePrefix accepts a Service Name as its first parameter.  This will be used by both
    /// the generated Url's as well as the generated JS proxy.
    /// <c>/Alchemy/Plugins/{YourPluginName}/api/{ServiceName}/{action}</c>
    /// <c>Alchemy.Plugins.YourPluginName.Api.Service.action()</c>
    /// 
    /// The attribute is optional and if you exclude it, url's and methods will be attached to "api" instead.
    /// <c>/Alchemy/Plugins/{YourPluginName}/api/{action}</c>
    /// <c>Alchemy.Plugins.YourPluginName.Api.action()</c>
    /// </remarks>
    [AlchemyRoutePrefix("DeletePlusService")]
    public class DeletePlusController : AlchemyApiController
    {
        /// // GET /Alchemy/Plugins/{YourPluginName}/api/{YourServiceName}/YourRoute
        /// <summary>
        /// Just a simple example..
        /// </summary>
        /// <returns>A string "Your Response" as the response message.</returns>
        /// </summary>
        /// <remarks>
        /// All of your action methods must have both a verb attribute as well as the RouteAttribute in
        /// order for the js proxy to work correctly.
        /// </remarks>
        [HttpGet]
        [Route("Hello")]
        public string DeletePlus()
        {
            return String.Format("Hello Alchemist {0}!", User.GetName());
        }
    }
}

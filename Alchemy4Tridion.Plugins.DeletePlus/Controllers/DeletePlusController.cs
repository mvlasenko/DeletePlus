using System;
using System.Web.Http;
using System.Xml.Linq;
using Tridion.ContentManager.CoreService.Client;

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
        [HttpGet]
        [Route("ItemsToDelete/{tcmItem}")]
        public string GetItemsToDelete(string tcmItem)
        {
            try
            {

                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<div class=\"usingItems results\">";
                html += CreateItemsHeading();

                var filterData = new OrganizationalItemItemsFilterData();
                filterData.ItemTypes = new[]{ItemType.Schema,
                                             ItemType.Component,
                                             ItemType.TemplateBuildingBlock,
                                             ItemType.ComponentTemplate,
                                             ItemType.PageTemplate};
                // When using OrganizationalItemItemsFilterData, we need to explicitly set a flag to include paths in resultXml.
                filterData.IncludePathColumn = true;
                filterData.Recursive = true;

                // Use the filter to get the list of ALL items contained in the folder represented by tcmItem.
                // We have to add "tcm:" here because we can't pass a full tcm id (with colon) via a URL.
                XElement resultXml = this.Client.GetListXml("tcm:" + tcmItem, filterData);

                // Iterate over all items returned by the above filtered list returned.
                foreach (XElement currentItem in resultXml.Nodes())
                {
                    html += CreateItem(currentItem) + Environment.NewLine;
                }

                // Close the div we opened above
                html += "</div>";

                // Explicitly abort to ensure there are no memory leaks.
                this.Client.Abort();

                // Return the html we've built.
                return html;
            }
            catch (Exception ex)
            {
                // Proper way of ensuring that the client gets closed... we close it in our try block above,
                // then in a catch block if an exception is thrown we abort it.
                if (this.Client != null)
                {
                    this.Client.Abort();
                }

                // We are rethrowing the original exception and just letting webapi handle it.
                throw ex;
            }
        }

        [HttpGet]
        [Route("DeleteItems/{tcmItem}")]
        public string GetDeletedItems(string tcmItem)
        {
            try
            {

                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<div class=\"usingItems results disabled\">";
                html += CreateItemsHeading();

                var filterData = new OrganizationalItemItemsFilterData();
                filterData.ItemTypes = new[]{ItemType.Schema,
                                             ItemType.Component,
                                             ItemType.TemplateBuildingBlock,
                                             ItemType.ComponentTemplate,
                                             ItemType.PageTemplate};
                // When using OrganizationalItemItemsFilterData, we need to explicitly set a flag to include paths in resultXml.
                filterData.IncludePathColumn = true;
                filterData.Recursive = true;

                // Use the filter to get the list of ALL items contained in the folder represented by tcmItem.
                // We have to add "tcm:" here because we can't pass a full tcm id (with colon) via a URL.
                XElement resultXml = this.Client.GetListXml("tcm:" + tcmItem, filterData);

                // Iterate over all items returned by the above filtered list returned.
                foreach (XElement currentItem in resultXml.Nodes())
                {
                    html += CreateItem(currentItem) + Environment.NewLine;
                }

                // Close the div we opened above
                html += "</div>";

                // Explicitly abort to ensure there are no memory leaks.
                this.Client.Abort();

                // Return the html we've built.
                return html;
            }
            catch (Exception ex)
            {
                // Proper way of ensuring that the client gets closed... we close it in our try block above,
                // then in a catch block if an exception is thrown we abort it.
                if (this.Client != null)
                {
                    this.Client.Abort();
                }

                // We are rethrowing the original exception and just letting webapi handle it.
                throw ex;
            }
        }

        private string CreateItemsHeading()
        {
            string html = "<div class=\"headings\">";
            html += "<div class=\"icon\">&nbsp</div>";
            html += "<div class=\"name\">Name</div>";
            html += "<div class=\"path\">Path</div>";
            html += "<div class=\"id\">ID</div></div>";

            return html;
        }

        private string CreateItem(XElement item)
        {
            string html = "<div class=\"item\">";
            html += "<div class=\"icon\" style=\"background-image: url(/WebUI/Editors/CME/Themes/Carbon2/icon_v7.1.0.66.627_.png?name=" + item.Attribute("Icon").Value + "&size=16)\"></div>";
            html += "<div class=\"name\">" + item.Attribute("Title").Value + "</div>";
            html += "<div class=\"path\">" + item.Attribute("Path").Value + "</div>";
            html += "<div class=\"id\">" + item.Attribute("ID").Value + "</div>";
            html += "</div>";
            return html;
        }

    }
}

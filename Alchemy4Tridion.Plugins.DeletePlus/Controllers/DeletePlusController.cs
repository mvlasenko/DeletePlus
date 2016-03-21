using System;
using System.Collections.Generic;
using System.Web.Http;
using Alchemy4Tridion.Plugins.DeletePlus.Helpers;
using Alchemy4Tridion.Plugins.DeletePlus.Models;

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

                List<ResultInfo> results  = new List<ResultInfo>();
                MainHelper.Delete(this.Client, "tcm:" + tcmItem, false, results);

                // Iterate over all items returned by the above filtered list returned.
                foreach (ResultInfo result in results)
                {
                    html += CreateItem(result) + Environment.NewLine;
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
                //throw ex;

                return ErrorMessage();
            }
        }

        [HttpGet]
        [Route("DeleteItems/{tcmItem}")]
        public string GetDeletedItems(string tcmItem)
        {
            try
            {

                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<div class=\"usingItems results\">";
                html += CreateItemsHeading();

                List<ResultInfo> results = new List<ResultInfo>();
                MainHelper.Delete(this.Client, "tcm:" + tcmItem, true, results);

                // Iterate over all items returned by the above filtered list returned.
                foreach (ResultInfo result in results)
                {
                    html += CreateItem(result) + Environment.NewLine;
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
                //throw ex;

                return ErrorMessage();
            }
        }

        private string CreateItemsHeading()
        {
            string html = "<div class=\"headings\">";
            html += "<div class=\"icon\">&nbsp</div>";
            html += "<div class=\"name\">Name</div>";
            html += "<div class=\"path\">Path</div>";
            html += "<div class=\"operation\">Operation</div>";
            html += "</div>";

            return html;
        }

        private string CreateItem(ResultInfo result)
        {
            string html = "";
            if (result.Status == Status.Deleted)
            {
                html += "<div class=\"item disabled\">";
            }
            else
            {
                html += string.Format("<div class=\"item\" id=\"{0}\">", result.TcmId);
            }

            html += string.Format("<div class=\"icon\" style=\"background-image: url(/WebUI/Editors/CME/Themes/Carbon2/icon_v7.1.0.66.627_.png?name={0}&size=16)\"></div>", result.Icon);
            html += string.Format("<div class=\"name\" title=\"{0} ({1})\">{0}</div>", result.Title, result.TcmId);
            html += string.Format("<div class=\"path\">{0}</div>", result.Path);
            html += string.Format("<div class=\"operation\"><img src=\"/Alchemy/Plugins/Delete_Plus/assets/img/{0}\" title=\"{1}\"/></div>", result.StatusIcon, result.Message.Replace("\"", "'"));
            html += "</div>";
            return html;
        }

        private string ErrorMessage()
        {
            string html = "<h1>Error</h1>";
            html += "<p>Look event log for details</p>";
            return html;
        }

    }
}
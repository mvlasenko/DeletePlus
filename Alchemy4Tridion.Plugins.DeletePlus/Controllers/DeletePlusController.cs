using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        [Route("ItemsToDelete/{tcmItem}/{unlink}")]
        public string GetItemsToDelete(string tcmItem, bool unlink)
        {
            try
            {
                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<table class=\"usingItems results\">";

                List<ResultInfo> results  = new List<ResultInfo>();
                MainHelper.Delete(this.Client, "tcm:" + tcmItem, false, false, unlink, results);

                SetTreeIcons(results);

                html += CreateItemsHeading();

                // Iterate over all items returned by the above filtered list returned.
                foreach (ResultInfo result in results)
                {
                    html += CreateItem(result, false) + Environment.NewLine;
                }

                // Close the div we opened above
                html += "</table>";

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

                // Write to Application Event Log
                //EventLog.WriteEntry("Alchemy4Tridion.Plugins.DeletePlus", ex.Message + "\n\nTrace:\n" + ex.StackTrace, EventLogEntryType.Error);

                throw ex;
            }
        }

        [HttpGet]
        [Route("DeleteItems/{tcmItem}/{unlink}")]
        public string GetDeletedItems(string tcmItem, bool unlink)
        {
            try
            {

                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<table class=\"usingItems results\">";

                List<ResultInfo> results = new List<ResultInfo>();
                MainHelper.Delete(this.Client, "tcm:" + tcmItem, true, false, unlink, results);

                SetTreeIcons(results);

                html += CreateItemsHeading();

                // Iterate over all items returned by the above filtered list returned.
                foreach (ResultInfo result in results)
                {
                    html += CreateItem(result, true) + Environment.NewLine;
                }

                // Close the div we opened above
                html += "</table>";

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

                // Write to Application Event Log
                //EventLog.WriteEntry("Alchemy4Tridion.Plugins.DeletePlus", ex.Message + "\n\nTrace:\n" + ex.StackTrace, EventLogEntryType.Error);

                throw ex;
            }
        }

        [HttpGet]
        [Route("UnpublishingItems/{tcmItem}/{unlink}")]
        public string GetUnpublishingItems(string tcmItem, bool unlink)
        {
            try
            {

                // Start building up a string of html to return, including headings for the table that the html will represent.
                string html = "<table class=\"usingItems results\">";
                
                List<ResultInfo> results = new List<ResultInfo>();
                MainHelper.Delete(this.Client, "tcm:" + tcmItem, true, true, unlink, results);

                SetTreeIcons(results);

                html += CreateItemsHeading();

                // Iterate over all items returned by the above filtered list returned.
                foreach (ResultInfo result in results)
                {
                    html += CreateItem(result, true) + Environment.NewLine;
                }

                // Close the div we opened above
                html += "</table>";

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

                // Write to Application Event Log
                //EventLog.WriteEntry("Alchemy4Tridion.Plugins.DeletePlus", ex.Message + "\n\nTrace:\n" + ex.StackTrace, EventLogEntryType.Error);

                throw ex;
            }
        }

        private string CreateItemsHeading()
        {
            string html = "<tr class=\"headings\">";
            html += "<th class=\"name\" style='padding-left: 18px !important;'>Name</th>";
            html += "<th class=\"path\">Path</th>";
            html += "<th class=\"operation\">Operation</th>";
            html += "</tr>";

            return html;
        }

        private string CreateItem(ResultInfo result, bool disabled)
        {
            string html = "";
            if (disabled)
            {
                html += "<tr class=\"item disabled\">";
            }
            else
            {
                html += string.Format("<tr class=\"item\" id=\"{0}\">", result.TcmId);
            }

            string treeIcons = "";

            for (int i = 0; i < result.TreeIconLevel; i++)
            {
                treeIcons += "<img src=\"/Alchemy/Plugins/Delete_Plus/assets/img/tree_vertical.png\" />";
            }

            if (!string.IsNullOrEmpty(result.TreeIcon))
            {
                treeIcons += string.Format("<img src=\"/Alchemy/Plugins/Delete_Plus/assets/img/{0}\" />", result.TreeIcon);
            }

            html += string.Format("<td class=\"name\" title=\"{0} ({1})\"><div class=\"treeicon\" style=\"width: {3}px; text-align: right;\">{4}</div><div class=\"icon\" style=\"background-image: url(/WebUI/Editors/CME/Themes/Carbon2/icon_v7.1.0.66.627_.png?name={2}&size=16)\"></div><div class=\"title\">{0}</div></td>", result.Title, result.TcmId, result.Icon, result.Level * 16, treeIcons);
            html += string.Format("<td class=\"path\">{0}</td>", result.Path);
            html += string.Format("<td class=\"operation\"><img src=\"/Alchemy/Plugins/Delete_Plus/assets/img/{0}\" title=\"{1}\"/></td>", result.StatusIcon, result.Message.Replace("\"", "'"));
            html += "</tr>";
            return html;
        }

        private void SetTreeIcons(List<ResultInfo> results)
        {
            int i = 0;
            foreach (ResultInfo result in results)
            {
                List<ResultInfo> childResults = results.Where(x => x.DependentItemTcmId == result.TcmId).ToList();
                int j = 0;
                foreach (ResultInfo childResult in childResults)
                {
                    childResult.TreeIcon = j == 0 ? "tree_top.png" : "tree_middle.png";
                    j++;
                }

                for (int j1 = 0; j1 < i; j1++)
                {
                    if (results[j1].Level < result.Level)
                    {
                        result.TreeIconLevel = 1;
                    }
                }

                i++;
            }
        }

    }
}
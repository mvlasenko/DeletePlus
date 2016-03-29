using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel;
using System.Xml;
using Alchemy4Tridion.Plugins.DeletePlus.Helpers;
using Alchemy4Tridion.Plugins.DeletePlus.Models;
using Tridion.ContentManager.CoreService.Client;

namespace DeletePlusTest
{
    class Program
    {
        static void Main(string[] args)
        {
            SessionAwareCoreServiceClient client = GetTcpClient("localhost", "admin", "123", "2013");
            string tcmItem = "tcm:6068-38337";

            //var list = MainHelper.GetItemsByParentContainer(client, "tcm:5061-11404-2");

            //var list = MainHelper.GetPublications(client);

            List<ResultInfo> results = new List<ResultInfo>();
            MainHelper.Delete(client, tcmItem, true, results);

            string html = "";

            foreach (ResultInfo result in results)
            {
                html += CreateItem(result) + Environment.NewLine;
            }
        }

        private static string CreateItem(ResultInfo result)
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


        public static SessionAwareCoreServiceClient GetTcpClient(string host, string username, string password, string clientVersion)
        {
            if (string.IsNullOrEmpty(host))
                host = "localhost";

            host = GetDomainName(host);

            var binding = GetBinding();

            var endpoint = new EndpointAddress(string.Format("net.tcp://{0}:2660/CoreService/{1}/netTcp", host, clientVersion));

            var client = new SessionAwareCoreServiceClient(binding, endpoint);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.ChannelFactory.Credentials.Windows.ClientCredential = new NetworkCredential(username, password);
            }

            return client;
        }

        private static NetTcpBinding GetBinding()
        {
            var binding = new NetTcpBinding
            {
                MaxReceivedMessageSize = 2147483647,
                ReaderQuotas = new XmlDictionaryReaderQuotas
                {
                    MaxStringContentLength = 2097152,
                    MaxArrayLength = 819200,
                    MaxBytesPerRead = 5120,
                    MaxDepth = 32,
                    MaxNameTableCharCount = 81920
                },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10),
                TransactionFlow = true,
                TransactionProtocol = TransactionProtocol.WSAtomicTransaction11
            };
            return binding;
        }

        public static string GetDomainName(string url)
        {
            if (!url.Contains(Uri.SchemeDelimiter))
            {
                url = string.Concat(Uri.UriSchemeHttp, Uri.SchemeDelimiter, url);
            }
            Uri uri = new Uri(url);
            return uri.Host;
        }
    }
}

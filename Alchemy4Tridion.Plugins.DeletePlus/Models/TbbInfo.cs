using System.Xml.Linq;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class TbbInfo
    {
        public string TcmId
        {
            get; set;
        }

        public string Title
        {
            get; set;
        }

        public XElement TemplateParameters
        {
            get; set;
        }
    }
}
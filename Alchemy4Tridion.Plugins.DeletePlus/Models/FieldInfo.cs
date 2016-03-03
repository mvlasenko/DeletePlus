using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class FieldInfo
    {
        public ItemFieldDefinitionData Field { get; set; }
        public bool IsMeta { get; set; }
        public FieldInfo Parent { get; set; }
        public int Level { get; set; }
        public string RootElementName { get; set; }
    }
}

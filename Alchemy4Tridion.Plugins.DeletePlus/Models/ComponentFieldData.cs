using Alchemy4Tridion.Plugins.DeletePlus.Helpers;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class ComponentFieldData
    {
        public object Value { get; set; }
        
        public ItemFieldDefinitionData SchemaField { get; set; }

        public bool IsMultiValue
        {
            get
            {
                return SchemaField.IsMultiValue();
            }
        }

        public bool IsMandatory
        {
            get
            {
                return SchemaField.IsMandatory();
            }
        }
    }
}
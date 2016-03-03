using System;
using System.Collections.Generic;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class HistoryItemMappingInfo
    {
        public string TcmId { get; set; }
        public int Version { get; set; }
        public DateTime Modified { get; set; }
        public bool Current { get; set; }
        public List<FieldMappingInfo> Mapping { get; set; }
    }
}

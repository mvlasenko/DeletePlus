using System;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class HistoryItemInfo
    {
        public string TcmId { get; set; }

        public string Title { get; set; }

        public int Version { get; set; }

        public DateTime Modified { get; set; }

        public bool Current { get; set; }

        public ItemType ItemType { get; set; }

    }
}
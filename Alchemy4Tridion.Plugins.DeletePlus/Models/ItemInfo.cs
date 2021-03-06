﻿using Alchemy4Tridion.Plugins.DeletePlus.Helpers;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class ItemInfo
    {
        public string TcmId { get; set; }

        public string Title { get; set; }

        public string Icon { get; set; }

        public string Path { get; set; }

        public ItemType ItemType { get; set; }

        public SchemaType SchemaType { get; set; }

        public string MimeType { get; set; }

        public string FromPub { get; set; }

        public bool IsPublished { get; set; }

        public string WebDav
        {
            get
            {
                if (string.IsNullOrEmpty(this.Path))
                    return this.TcmId;
                return this.Path.Trim('\\').Replace('\\', '/') + "/" + this.Title;
            }
        }

        public bool IsLocalized
        {
            get
            {
                return this.FromPub == "(Local copy)";
            }
        }
        
        public bool IsLocal
        {
            get
            {
                return string.IsNullOrEmpty(this.FromPub);
            }
        }

        public bool IsShared
        {
            get
            {
                return !string.IsNullOrEmpty(this.FromPub) && this.FromPub != "(Local copy)";
            }
        }

    }
}
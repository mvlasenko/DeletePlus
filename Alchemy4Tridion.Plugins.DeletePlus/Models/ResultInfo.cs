using Alchemy4Tridion.Plugins.DeletePlus.Helpers;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Models
{
    public class ResultInfo
    {
        public ItemInfo Item { get; set; }

        public string Message { get; set; }

        public Status Status { get; set; }

        public string StackTrace { get; set; }

        public string TcmId
        {
            get
            {
                if (this.Item == null)
                    return string.Empty;
                return this.Item.TcmId;
            }
        }

        public ItemType ItemType
        {
            get
            {
                if (this.Item == null)
                    return ItemType.None;
                return this.Item.ItemType;
            }
        }

        public string Title
        {
            get
            {
                if (this.Item == null)
                    return string.Empty;
                return this.Item.Title;
            }
        }

        public string Icon
        {
            get
            {
                if (this.Item == null)
                    return string.Empty;
                return this.Item.Icon;
            }
        }

        public string StatusIcon
        {
            get
            {
                if (this.Status == Status.Success)
                    return "success.png";
                if (this.Status == Status.Warning)
                    return "warning.png";
                if (this.Status == Status.Error)
                    return "error.png";

                return "info.png";
            }
        }

        public string Path
        {
            get
            {
                if (this.Item == null)
                    return string.Empty;
                return this.Item.Path;
            }
        }

        public string WebDav
        {
            get
            {
                if (this.Item == null)
                    return this.TcmId;
                return this.Item.WebDav;
            }
        }

    }
}
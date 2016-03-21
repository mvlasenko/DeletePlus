﻿namespace Alchemy4Tridion.Plugins.DeletePlus.Helpers
{
    public enum Status
    {
        Info,
        Success,
        Delete,
        Deleted,
        Warning,
        Error,
        None
    }

    public enum SchemaType
    {
        Any,
        Component,
        Metadata,
        Embedded,
        Multimedia,
        Parameters,
        Bundle,
        None
    }

    public enum LinkStatus
    {
        Found,
        NotFound,
        Mandatory,
        Error
    }
}
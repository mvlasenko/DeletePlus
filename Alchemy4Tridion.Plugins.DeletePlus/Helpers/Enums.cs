namespace Alchemy4Tridion.Plugins.DeletePlus.Helpers
{
    public enum Status
    {
        Info,
        Success,
        Warning,
        Error,
        None
    }

    public enum FieldType
    {
        SingleLineText,
        MultiLineText,
        Xhtml,
        Date,
        Number,
        Keyword,
        Multimedia,
        ExternalLink,
        ComponentLink,
        EmbeddedSchema,
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

    public enum ObjectType
    {
        Any,
        Component,
        Folder,
        ComponentOrFolder,
        Page,
        StructureGroup,
        PageOrStructureGroup
    }

    public enum LinkStatus
    {
        Found,
        NotFound,
        Mandatory,
        Error
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Alchemy4Tridion.Plugins.DeletePlus.Models;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Helpers
{
    public static class MainHelper
    {
        #region Tridion items access

        public static bool ExistsItem(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return ReadItem(client, tcmItem) != null;
        }

        public static IdentifiableObjectData ReadItem(SessionAwareCoreServiceClient client, string id)
        {
            try
            {
                return client.Read(id, null);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Tridion hierarchy

        public static List<ItemInfo> GetItemsByParentContainer(SessionAwareCoreServiceClient client, string tcmContainer)
        {
            return client.GetListXml(tcmContainer, new OrganizationalItemItemsFilterData()).ToList();
        }

        public static List<ItemInfo> GetItemsByParentContainer(SessionAwareCoreServiceClient client, string tcmContainer, bool recursive)
        {
            return client.GetListXml(tcmContainer, new OrganizationalItemItemsFilterData { Recursive = recursive }).ToList();
        }

        public static List<ItemInfo> GetContainersByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Folder, ItemType.StructureGroup } }).ToList();
        }

        public static List<ItemInfo> GetCategoriesByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Category } }).ToList();
        }

        public static List<ItemInfo> GetProcessDefinitionsByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetSystemWideListXml(new ProcessDefinitionsFilterData { ContextRepository = new LinkToRepositoryData { IdRef = tcmPublication } }).ToList(ItemType.ProcessDefinition);
        }

        public static List<ItemInfo> GetItemsByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            List<ItemInfo> list = new List<ItemInfo>();

            list.AddRange(GetContainersByPublication(client, tcmPublication));

            if (GetCategoriesByPublication(client, tcmPublication).Any())
                list.Add(new ItemInfo { Title = "Categories and Keywords", TcmId = "catman-" + tcmPublication, ItemType = ItemType.Folder });

            if (GetProcessDefinitionsByPublication(client, tcmPublication).Any())
                list.Add(new ItemInfo { Title = "Process Definitions", TcmId = "proc-" + tcmPublication, ItemType = ItemType.Folder });

            return list;
        }

        public static List<ItemInfo> GetItemsByPublication(SessionAwareCoreServiceClient client, string tcmPublication, bool recursive)
        {
            if (!recursive)
                return GetItemsByPublication(client, tcmPublication);

            List<ItemInfo> list = new List<ItemInfo>();

            foreach (ItemInfo container in GetContainersByPublication(client, tcmPublication))
            {
                list.Add(container);
                list.AddRange(GetItemsByParentContainer(client, container.TcmId, true));
            }

            List<ItemInfo> categories = GetCategoriesByPublication(client, tcmPublication);
            if (categories.Any())
            {
                list.Add(new ItemInfo { Title = "Categories and Keywords", TcmId = "catman-" + tcmPublication, ItemType = ItemType.Folder });
                list.AddRange(categories);
            }

            List<ItemInfo> processDefinitions = GetProcessDefinitionsByPublication(client, tcmPublication);
            if (processDefinitions.Any())
            {
                list.Add(new ItemInfo { Title = "Process Definitions", TcmId = "proc-" + tcmPublication, ItemType = ItemType.Folder });
                list.AddRange(processDefinitions);
            }

            return list;
        }

        public static string GetWebDav(this RepositoryLocalObjectData item)
        {
            if(item.LocationInfo == null || string.IsNullOrEmpty(item.LocationInfo.WebDavUrl))
                return string.Empty;

            string webDav = HttpUtility.UrlDecode(item.LocationInfo.WebDavUrl.Replace("/webdav/", string.Empty));
            if (string.IsNullOrEmpty(webDav))
                return string.Empty;

            int dotIndex = webDav.LastIndexOf(".", StringComparison.Ordinal);
            int slashIndex = webDav.LastIndexOf("/", StringComparison.Ordinal);

            return dotIndex >= 0 && dotIndex > slashIndex ? webDav.Substring(0, dotIndex) : webDav;
        }

        public static List<string> GetUsingItems(SessionAwareCoreServiceClient client, string tcmItem, bool current = false, ItemType[] itemTypes = null)
        {
            UsingItemsFilterData filter = new UsingItemsFilterData();
            filter.IncludedVersions = current ? VersionCondition.OnlyLatestVersions : VersionCondition.AllVersions;
            filter.BaseColumns = ListBaseColumns.Id;
            if (itemTypes != null)
                filter.ItemTypes = itemTypes;

            List<string> items = client.GetListXml(tcmItem, filter).ToList().Select(x => x.TcmId).ToList();
            return items;
        }

        private static List<string> GetUsingCurrentItems(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return GetUsingItems(client, tcmItem, true);
        }

        public static List<string> GetUsedItems(SessionAwareCoreServiceClient client, string tcmItem, ItemType[] itemTypes = null)
        {
            UsedItemsFilterData filter = new UsedItemsFilterData();
            filter.BaseColumns = ListBaseColumns.Id;
            if (itemTypes != null)
                filter.ItemTypes = itemTypes;

            List<string> items = client.GetListXml(tcmItem, filter).ToList().Select(x => x.TcmId).ToList();
            return items;
        }

        public static List<HistoryItemInfo> GetItemHistory(SessionAwareCoreServiceClient client, string tcmItem)
        {
            VersionsFilterData versionsFilter = new VersionsFilterData();
            XElement listOfVersions = client.GetListXml(tcmItem, versionsFilter);

            List<HistoryItemInfo> res = new List<HistoryItemInfo>();

            if (listOfVersions != null && listOfVersions.HasElements)
            {
                foreach (XElement element in listOfVersions.Descendants())
                {
                    HistoryItemInfo item = new HistoryItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = element.Attributes().Any(x => x.Name == "Type") ? (ItemType)int.Parse(element.Attribute("Type").Value) : GetItemType(item.TcmId);
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.Version = int.Parse(element.Attribute("Version").Value.Replace("v", ""));
                    item.Modified = DateTime.Parse(element.Attribute("Modified").Value);

                    res.Add(item);
                }
            }

            res.Last().Current = true;

            return res;
        }

        #endregion

        #region Tridion schemas

        public static List<ItemFieldDefinitionData> GetSchemaFields(SessionAwareCoreServiceClient client, string tcmSchema)
        {
            SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            if (schemaFieldsData == null || schemaFieldsData.Fields == null)
                return null;

            return schemaFieldsData.Fields.ToList();
        }

        public static List<ItemFieldDefinitionData> GetSchemaMetadataFields(SessionAwareCoreServiceClient client, string tcmSchema)
        {
            var schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            if (schemaFieldsData == null || schemaFieldsData.MetadataFields == null)
                return null;

            return schemaFieldsData.MetadataFields.ToList();
        }

        #endregion

        #region Tridion components

        public static List<ItemInfo> GetComponents(SessionAwareCoreServiceClient client, string tcmSchema)
        {
            return client.GetListXml(tcmSchema, new UsingItemsFilterData { ItemTypes = new[] { ItemType.Component } }).ToList(ItemType.Component);
        }

        public static List<ItemInfo> GetComponents(SessionAwareCoreServiceClient client, string tcmFolder, bool recursive)
        {
            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Component }, Recursive = recursive }).ToList(ItemType.Component);
        }

        public static List<ItemInfo> GetComponents(SessionAwareCoreServiceClient client, string tcmFolder, string tcmSchema)
        {
            if (string.IsNullOrEmpty(tcmFolder) && string.IsNullOrEmpty(tcmFolder))
                return new List<ItemInfo>();

            if (string.IsNullOrEmpty(tcmFolder))
                return GetComponents(client, tcmSchema);

            if (string.IsNullOrEmpty(tcmSchema))
                return GetComponents(client, tcmFolder, true);

            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Component }, Recursive = true, BasedOnSchemas = new [] { new LinkToSchemaData { IdRef = tcmSchema} } }).ToList(ItemType.Component);
        }

        public static XElement GetComponentXml(XNamespace ns, string rootElementName, List<ComponentFieldData> componentFieldValues)
        {
            if (string.IsNullOrEmpty(rootElementName) || componentFieldValues.Count == 0)
                return null;
            
            XElement contentXml = new XElement(ns + rootElementName);

            foreach (ComponentFieldData fieldValue in componentFieldValues)
            {
                if (fieldValue.IsMultiValue)
                {
                    IList values = fieldValue.Value as IList;
                    if (values != null)
                    {
                        foreach (object value in values)
                        {
                            if (value is XElement)
                            {
                                contentXml.Add(((XElement)value).Clone(fieldValue.SchemaField.Name));
                            }
                            else
                            {
                                contentXml.Add(new XElement(ns + fieldValue.SchemaField.Name, value));
                            }
                        }
                    }
                }
                else
                {
                    if (fieldValue.Value is XElement)
                    {
                        contentXml.Add(((XElement)fieldValue.Value).Clone(fieldValue.SchemaField.Name));
                    }
                    else
                    {
                        contentXml.Add(new XElement(ns + fieldValue.SchemaField.Name, fieldValue.Value));
                    }
                }
            }

            return contentXml;
        }

        public static object GetComponentFieldData(this XElement element, ItemFieldDefinitionData schemaField)
        {
            if (element == null)
                return null;

            string value = element.GetInnerXml();

            if (schemaField.IsNumber())
                return string.IsNullOrEmpty(value) ? null : (double?)double.Parse(value);

            if (schemaField.IsDate())
                return string.IsNullOrEmpty(value) ? null : (DateTime?)DateTime.Parse(value);

            if (schemaField.IsText() || schemaField.IsRichText() || schemaField.IsTextSelect())
                return value;

            return element.Clone(schemaField.Name);
        }

        public static XElement Clone(this XElement node, string name)
        {
            XNamespace linkNs = "http://www.w3.org/1999/xlink";
            if (node.Attribute(linkNs + "href") != null)
                return GetComponentLink(node.Attribute(linkNs + "href").Value, node.Attribute(linkNs + "title") == null ? null : node.Attribute(linkNs + "title").Value, name);

            XNamespace ns = node.GetDefaultNamespace();
            if (node.Elements().Any())
                return new XElement(ns + name, node.Attributes(), node.Elements());

            return new XElement(ns + name, node.Attributes(), node.Value);
        }

        public static XElement GetComponentLink(string id, string title, string fieldName)
        {
            XNamespace ns = "http://www.w3.org/1999/xlink";

            if (string.IsNullOrEmpty(title))
                return new XElement(fieldName,
                    new XAttribute(XNamespace.Xmlns + "xlink", ns),
                    new XAttribute(ns + "href", id));

            return new XElement(fieldName,
                new XAttribute(XNamespace.Xmlns + "xlink", ns),
                new XAttribute(ns + "href", id),
                new XAttribute(ns + "title", title));
        }

        public static List<ComponentFieldData> GetValues(XNamespace schemaNs, List<ItemFieldDefinitionData> schemaFields, XElement parent)
        {
            List<ComponentFieldData> res = new List<ComponentFieldData>();

            if (schemaFields == null)
                return res;
            
            foreach (ItemFieldDefinitionData field in schemaFields)
            {
                if (!string.IsNullOrEmpty(field.Name) && parent.Element(schemaNs + field.Name) != null)
                {
                    List<XElement> elements = parent.Elements(schemaNs + field.Name).ToList();

                    ComponentFieldData item = new ComponentFieldData();
                    item.SchemaField = field;

                    item.Value = !field.IsMultiValue() ? elements.FirstOrDefault().GetComponentFieldData(field) : elements.Select(x => x.GetComponentFieldData(field)).ToList();

                    res.Add(item);
                }
            }

            return res;
        }

        public static List<ComponentFieldData> GetValues(XNamespace schemaNs, List<ItemFieldDefinitionData> schemaFields, string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            if (doc.Root == null)
                return null;

            List<ComponentFieldData> res = new List<ComponentFieldData>();

            foreach (ItemFieldDefinitionData field in schemaFields)
            {
                if (!string.IsNullOrEmpty(field.Name) && doc.Root.Element(schemaNs + field.Name) != null)
                {
                    List<XElement> elements = doc.Root.Elements(schemaNs + field.Name).ToList();

                    ComponentFieldData item = new ComponentFieldData();
                    item.SchemaField = field;

                    item.Value = !field.IsMultiValue() ? elements.FirstOrDefault().GetComponentFieldData(field) : elements.Select(x => x.GetComponentFieldData(field)).ToList();

                    res.Add(item);
                }
            }

            return res;
        }

        #endregion

        #region Tridion delete

        public static void Delete(SessionAwareCoreServiceClient client, string tcmItem, bool delete, bool unpublish, bool unlink, List<ResultInfo> results)
        {
            ItemType itemType = GetItemType(tcmItem);

            if (itemType == ItemType.Publication)
            {
                DeletePublication(client, tcmItem, delete, unpublish, unlink, results);
            }
            else if (itemType == ItemType.Folder || itemType == ItemType.StructureGroup || itemType == ItemType.Category)
            {
                DeleteFolderOrStructureGroup(client, tcmItem, delete, unpublish, unlink, results);
            }
            else
            {
                DeleteTridionObject(client, tcmItem, delete, unpublish, unlink, results);
            }
        }

        private static LinkStatus RemoveDependency(SessionAwareCoreServiceClient client, string tcmItem, string tcmDependentItem, bool delete, List<ResultInfo> results, int level = 0)
        {
            if (results.Any(x => x.Status == Status.Error))
                return LinkStatus.Error;

            if (results.Count > 50)
            {
                results.Insert(0, new ResultInfo
                {
                    Message = "Delete stack exceeds 50 items. Please select other item",
                    Item = new ItemInfo { Title = "Delete stack exceeds 50 items" },
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return LinkStatus.Error;
            }

            ResultInfo result = new ResultInfo();
            result.Level = level;
            result.DependentItemTcmId = tcmDependentItem;

            ItemType itemType = GetItemType(tcmItem);
            ItemType dependentItemType = GetItemType(tcmDependentItem);
            LinkStatus status = LinkStatus.NotFound;
            string stackTraceMessage = "";

            RepositoryLocalObjectData dependentItemData = ReadItem(client, tcmDependentItem) as RepositoryLocalObjectData;

            //publication properies are not handled
            if (itemType == ItemType.Publication)
            {
                PublicationData publicationData = (PublicationData)ReadItem(client, tcmItem);
                result.Item = publicationData.ToItem();

                result.Status = Status.Error;
                result.Message = string.Format("Not able to unlink \"{1}\" from publication \"{0}\".", publicationData.Title, dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
                results.Add(result);

                return LinkStatus.Error;
            }

            RepositoryLocalObjectData itemData = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
            if (itemData == null)
                return LinkStatus.NotFound;

            if (itemData.BluePrintInfo.IsShared == true)
            {
                tcmItem = GetBluePrintTopTcmId(client, tcmItem);

                itemData = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
                if (itemData == null)
                    return LinkStatus.NotFound;
            }

            result.Item = itemData.ToItem();

            if (delete)
            {
                //remove linked schema
                if (itemType == ItemType.Folder && dependentItemType == ItemType.Schema)
                {
                    status = RemoveFolderLinkedSchema(client, tcmItem, out stackTraceMessage);
                }

                //remove metadata schema
                if (dependentItemType == ItemType.Schema)
                {
                    status = RemoveMetadataSchema(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove parameters schema
                if (dependentItemType == ItemType.Schema && itemData is TemplateData)
                {
                    status = RemoveParameterSchema(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove component template linked schema
                if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.Schema)
                {
                    status = RemoveCTLinkedSchema(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove CP
                if (itemType == ItemType.Page && (dependentItemType == ItemType.Component || dependentItemType == ItemType.ComponentTemplate))
                {
                    status = RemoveComponentPresentation(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove TBB from page template
                else if (itemType == ItemType.PageTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = RemoveTbbFromPageTemplate(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove TBB from component template
                else if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = RemoveTbbFromComponentTemplate(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove TBB from compound TBB
                else if (itemType == ItemType.TemplateBuildingBlock && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = RemoveTbbFromCompoundTbb(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //change schema keyword field to text field
                else if (itemType == ItemType.Schema && dependentItemType == ItemType.Category)
                {
                    status = RemoveKeywordField(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove component or keyword link from component
                else if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = CheckRemoveLinkFromComponent(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field change
                    if (status == LinkStatus.Mandatory)
                    {
                        RemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results, level);
                    }

                    status = RemoveLinkFromComponent(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove component or keyword link from metadata
                else if (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword)
                {
                    status = CheckRemoveLinkFromMetadata(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field change
                    if (status == LinkStatus.Mandatory)
                    {
                        RemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results, level);
                    }

                    status = RemoveLinkFromMetadata(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                if (status == LinkStatus.Found && itemData is VersionedItemData)
                    status = RemoveHistory(client, tcmItem, tcmDependentItem, out stackTraceMessage);

                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Success;
                    result.Message = string.Format("Item \"{1}\" was removed from \"{0}\".", itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
                }
            }
            else
            {
                //check if possible to remove linked schema
                if (itemType == ItemType.Folder && dependentItemType == ItemType.Schema)
                {
                    status = CheckRemoveFolderLinkedSchema(client, tcmItem);
                }

                //check if possible to remove metadata schema
                if (dependentItemType == ItemType.Schema)
                {
                    status = CheckRemoveMetadataSchema(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove parameters schema
                if (dependentItemType == ItemType.Schema && itemData is TemplateData)
                {
                    status = CheckRemoveParameterSchema(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove component template linked schema
                if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.Schema)
                {
                    status = CheckRemoveCTLinkedSchema(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove CP
                if (itemType == ItemType.Page && (dependentItemType == ItemType.Component || dependentItemType == ItemType.ComponentTemplate))
                {
                    status = CheckRemoveComponentPresentation(client, tcmItem, tcmDependentItem);
                }
                //check if possible to remove TBB from page template
                else if (itemType == ItemType.PageTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = CheckRemoveTbbFromPageTemplate(client, tcmItem, tcmDependentItem);
                }
                //check if possible to remove TBB from component template
                else if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = CheckRemoveTbbFromComponentTemplate(client, tcmItem, tcmDependentItem);
                }
                //check if possible to remove TBB from compound TBB
                else if (itemType == ItemType.TemplateBuildingBlock && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = CheckRemoveTbbFromCompoundTbb(client, tcmItem, tcmDependentItem);
                }
                //change schema keyword field to text field
                else if (itemType == ItemType.Schema && dependentItemType == ItemType.Category)
                {
                    status = CheckRemoveKeywordField(client, tcmItem, tcmDependentItem);
                }
                //check if possible to remove component or keyword link from component
                else if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = CheckRemoveLinkFromComponent(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field needs to be changed
                    if (status == LinkStatus.Mandatory)
                    {
                        CheckRemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results, level);
                        status = LinkStatus.Found;
                    }
                }
                //check if possible to remove component or keyword link from metadata
                else if (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword)
                {
                    status = CheckRemoveLinkFromMetadata(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field needs to be changed
                    if (status == LinkStatus.Mandatory)
                    {
                        CheckRemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results, level);
                        status = LinkStatus.Found;
                    }
                }

                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Unlink;
                    result.Message = string.Format("Remove item \"{1}\" from \"{0}\".", itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
                }
            }

            if (status == LinkStatus.Error)
            {
                result.Status = Status.Error;
                result.StackTrace = stackTraceMessage;
                result.Message = string.Format("Not able to unlink \"{1}\" from \"{0}\".", itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
            }

            if (status != LinkStatus.NotFound)
                results.Add(result);

            return status;
        }

        private static LinkStatus RemoveFolderLinkedSchema(SessionAwareCoreServiceClient client, string folderUri, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            FolderData innerFolderData = ReadItem(client, folderUri) as FolderData;
            if (innerFolderData == null || innerFolderData.LinkedSchema == null || string.IsNullOrEmpty(innerFolderData.LinkedSchema.IdRef) || innerFolderData.LinkedSchema.IdRef == "tcm:0-0-0")
                return LinkStatus.NotFound;

            try
            {
                //change schema id
                innerFolderData.LinkedSchema.IdRef = "tcm:0-0-0";

                //make non-mandatory to aviod conflicts with inner components
                innerFolderData.IsLinkedSchemaMandatory = false;

                client.Save(innerFolderData, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveFolderLinkedSchema(SessionAwareCoreServiceClient client, string folderUri)
        {
            FolderData innerFolderData = ReadItem(client, folderUri) as FolderData;
            if (innerFolderData == null || innerFolderData.LinkedSchema == null || string.IsNullOrEmpty(innerFolderData.LinkedSchema.IdRef) || innerFolderData.LinkedSchema.IdRef == "tcm:0-0-0")
                return LinkStatus.NotFound;

            return LinkStatus.Found;
        }

        private static LinkStatus RemoveMetadataSchema(SessionAwareCoreServiceClient client, string itemUri, string schemaUri, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            RepositoryLocalObjectData itemData = ReadItem(client, itemUri) as RepositoryLocalObjectData;
            if (itemData == null || itemData.MetadataSchema == null || string.IsNullOrEmpty(itemData.MetadataSchema.IdRef) || itemData.MetadataSchema.IdRef != schemaUri)
                return LinkStatus.NotFound;

            if (itemData is VersionedItemData)
            {
                VersionedItemData versionedItemData = (VersionedItemData) itemData;

                if (versionedItemData.BluePrintInfo.IsShared == true)
                {
                    itemUri = GetBluePrintTopTcmId(client, itemUri);

                    versionedItemData = ReadItem(client, itemUri) as VersionedItemData;
                    if (versionedItemData == null)
                        return LinkStatus.NotFound;
                }

                try
                {
                    versionedItemData = client.CheckOut(versionedItemData.Id, true, new ReadOptions());
                }
                catch (Exception ex)
                {

                }

                if (versionedItemData == null)
                    return LinkStatus.NotFound;

                //change schema id
                versionedItemData.MetadataSchema.IdRef = "tcm:0-0-0";

                try
                {
                    versionedItemData = (VersionedItemData)client.Update(versionedItemData, new ReadOptions());
                    client.CheckIn(versionedItemData.Id, new ReadOptions());
                    return LinkStatus.Found;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (versionedItemData == null)
                        return LinkStatus.Error;

                    client.UndoCheckOut(versionedItemData.Id, true, new ReadOptions());
                    return LinkStatus.Error;
                }
            }

            try
            {
                //change schema id
                itemData.MetadataSchema.IdRef = "tcm:0-0-0";

                client.Save(itemData, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveMetadataSchema(SessionAwareCoreServiceClient client, string itemUri, string schemaUri)
        {
            RepositoryLocalObjectData itemData = ReadItem(client, itemUri) as RepositoryLocalObjectData;
            if (itemData == null || itemData.MetadataSchema == null || string.IsNullOrEmpty(itemData.MetadataSchema.IdRef) || itemData.MetadataSchema.IdRef != schemaUri)
                return LinkStatus.NotFound;

            return LinkStatus.Found;
        }

        private static LinkStatus RemoveParameterSchema(SessionAwareCoreServiceClient client, string itemUri, string schemaUri, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            TemplateData templateData = ReadItem(client, itemUri) as TemplateData;
            if (templateData == null || templateData.ParameterSchema == null || string.IsNullOrEmpty(templateData.ParameterSchema.IdRef) || templateData.ParameterSchema.IdRef != schemaUri)
                return LinkStatus.NotFound;

            if (templateData.BluePrintInfo.IsShared == true)
            {
                itemUri = GetBluePrintTopTcmId(client, itemUri);

                templateData = ReadItem(client, itemUri) as TemplateData;
                if (templateData == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                templateData = client.CheckOut(templateData.Id, true, new ReadOptions()) as TemplateData;
            }
            catch (Exception ex)
            {

            }

            if (templateData == null)
                return LinkStatus.NotFound;

            //change schema id
            templateData.ParameterSchema.IdRef = "tcm:0-0-0";

            try
            {
                templateData = (TemplateData)client.Update(templateData, new ReadOptions());
                client.CheckIn(templateData.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (templateData == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveParameterSchema(SessionAwareCoreServiceClient client, string itemUri, string schemaUri)
        {
            TemplateData templateData = ReadItem(client, itemUri) as TemplateData;
            if (templateData == null || templateData.ParameterSchema == null || string.IsNullOrEmpty(templateData.ParameterSchema.IdRef) || templateData.ParameterSchema.IdRef != schemaUri)
                return LinkStatus.NotFound;

            return LinkStatus.Found;
        }

        private static LinkStatus RemoveCTLinkedSchema(SessionAwareCoreServiceClient client, string tcmComponentTemplate, string tcmSchema, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            ComponentTemplateData componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
            if (componentTemplate == null)
                return LinkStatus.NotFound;

            if(componentTemplate.RelatedSchemas.All(x => x.IdRef != tcmSchema))
                return LinkStatus.NotFound;

            if (componentTemplate.BluePrintInfo.IsShared == true)
            {
                tcmComponentTemplate = GetBluePrintTopTcmId(client, tcmComponentTemplate);

                componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
                if (componentTemplate == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                componentTemplate = client.CheckOut(componentTemplate.Id, true, new ReadOptions()) as ComponentTemplateData;
            }
            catch (Exception ex)
            {

            }

            if (componentTemplate == null)
                return LinkStatus.NotFound;

            componentTemplate.RelatedSchemas = componentTemplate.RelatedSchemas.Where(x => x.IdRef != tcmSchema).ToArray();

            try
            {
                componentTemplate = (ComponentTemplateData)client.Update(componentTemplate, new ReadOptions());
                client.CheckIn(componentTemplate.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (componentTemplate == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(componentTemplate.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveCTLinkedSchema(SessionAwareCoreServiceClient client, string tcmComponentTemplate, string tcmSchema)
        {
            ComponentTemplateData componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
            if (componentTemplate == null)
                return LinkStatus.NotFound;

            if (componentTemplate.RelatedSchemas.All(x => x.IdRef != tcmSchema))
                return LinkStatus.NotFound;

            return LinkStatus.Found;
        }

        private static LinkStatus RemoveComponentPresentation(SessionAwareCoreServiceClient client, string tcmPage, string tcmDependentItem, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            PageData page = ReadItem(client, tcmPage) as PageData;
            if (page == null)
                return LinkStatus.NotFound;

            ComponentPresentationData[] newComponentPresentations = page.ComponentPresentations.Where(x => x.Component.IdRef.GetId() != tcmDependentItem.GetId() && x.ComponentTemplate.IdRef.GetId() != tcmDependentItem.GetId()).ToArray();

            if (page.ComponentPresentations.Length == newComponentPresentations.Length)
                return LinkStatus.NotFound;

            if (page.BluePrintInfo.IsShared == true)
            {
                tcmPage = GetBluePrintTopTcmId(client, tcmPage);

                page = ReadItem(client, tcmPage) as PageData;
                if (page == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                page = client.CheckOut(page.Id, true, new ReadOptions()) as PageData;
            }
            catch (Exception ex)
            {

            }

            if (page == null)
                return LinkStatus.NotFound;

            page.ComponentPresentations = newComponentPresentations;

            try
            {
                page = (PageData)client.Update(page, new ReadOptions());
                client.CheckIn(page.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (page == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(page.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveComponentPresentation(SessionAwareCoreServiceClient client, string tcmPage, string tcmDependentItem)
        {
            PageData page = ReadItem(client, tcmPage) as PageData;
            if (page == null)
                return LinkStatus.NotFound;

            return page.ComponentPresentations.Any(x => x.Component.IdRef.GetId() == tcmDependentItem.GetId() || x.ComponentTemplate.IdRef.GetId() == tcmDependentItem.GetId()) ? LinkStatus.Found : LinkStatus.NotFound;
        }

        public static List<TbbInfo> GetTbbList(string templateContent)
        {
            List<TbbInfo> tbbList = new List<TbbInfo>();

            XNamespace ns = "http://www.tridion.com/ContentManager/5.3/CompoundTemplate";
            XNamespace linkNs = "http://www.w3.org/1999/xlink";

            XDocument xml = XDocument.Parse(templateContent);

            if (xml.Root == null)
                return tbbList;

            List<XElement> templateInvocations = xml.Root.Elements(ns + "TemplateInvocation").ToList();
            foreach (XElement invovation in templateInvocations)
            {
                TbbInfo tbbInfo = new TbbInfo();

                XElement template = invovation.Elements(ns + "Template").FirstOrDefault();
                if (template != null)
                {
                    tbbInfo.TcmId = template.Attribute(linkNs + "href").Value;
                    tbbInfo.Title = template.Attribute(linkNs + "title").Value;
                }

                XElement templateParameters = invovation.Elements(ns + "TemplateParameters").FirstOrDefault();
                if (templateParameters != null)
                {
                    tbbInfo.TemplateParameters = templateParameters;
                }

                tbbList.Add(tbbInfo);
            }

            return tbbList;
        }

        private static string GetTemplateContent(List<TbbInfo> tbbList)
        {
            XNamespace ns = "http://www.tridion.com/ContentManager/5.3/CompoundTemplate";

            XElement root = new XElement(ns + "CompoundTemplate");
            foreach (TbbInfo tbbInfo in tbbList)
            {
                XElement templateInvocation = new XElement(ns + "TemplateInvocation");
                
                XElement template = GetComponentLink(tbbInfo.TcmId, tbbInfo.Title, "Template");
                if(template != null)
                    templateInvocation.Add(template);

                if(tbbInfo.TemplateParameters != null)
                    templateInvocation.Add(tbbInfo.TemplateParameters);

                root.Add(templateInvocation);
            }

            return root.ToString().Replace(" xmlns=\"\"", "");
        }

        private static string RemoveTbbFromTemplate(string templateContent, string tcmTbb)
        {
            List<TbbInfo> tbbList = GetTbbList(templateContent).Where(x => x.TcmId.GetId() != tcmTbb.GetId()).ToList();
            return GetTemplateContent(tbbList);
        }

        private static LinkStatus RemoveTbbFromPageTemplate(SessionAwareCoreServiceClient client, string tcmPageTemplate, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            PageTemplateData pageTemplate = ReadItem(client, tcmPageTemplate) as PageTemplateData;
            if (pageTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(pageTemplate.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                if (tbbList.Count == 1)
                    return LinkStatus.Mandatory;
            }
            else
            {
                return LinkStatus.NotFound;
            }

            string newContent = RemoveTbbFromTemplate(pageTemplate.Content, tcmTbb);

            if (pageTemplate.BluePrintInfo.IsShared == true)
            {
                tcmPageTemplate = GetBluePrintTopTcmId(client, tcmPageTemplate);

                pageTemplate = ReadItem(client, tcmPageTemplate) as PageTemplateData;
                if (pageTemplate == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                pageTemplate = client.CheckOut(pageTemplate.Id, true, new ReadOptions()) as PageTemplateData;
            }
            catch (Exception ex)
            {

            }

            if (pageTemplate == null)
                return LinkStatus.NotFound;

            pageTemplate.Content = newContent;

            try
            {
                pageTemplate = (PageTemplateData)client.Update(pageTemplate, new ReadOptions());
                client.CheckIn(pageTemplate.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (pageTemplate == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(pageTemplate.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveTbbFromPageTemplate(SessionAwareCoreServiceClient client, string tcmPageTemplate, string tcmTbb)
        {
            PageTemplateData pageTemplate = ReadItem(client, tcmPageTemplate) as PageTemplateData;
            if (pageTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(pageTemplate.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                return tbbList.Count == 1 ? LinkStatus.Mandatory : LinkStatus.Found;
            }
            return LinkStatus.NotFound;
        }

        private static LinkStatus RemoveTbbFromComponentTemplate(SessionAwareCoreServiceClient client, string tcmComponentTemplate, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            ComponentTemplateData componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
            if (componentTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(componentTemplate.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                if (tbbList.Count == 1)
                    return LinkStatus.Mandatory;
            }
            else
            {
                return LinkStatus.NotFound;
            }

            string newContent = RemoveTbbFromTemplate(componentTemplate.Content, tcmTbb);

            if (componentTemplate.BluePrintInfo.IsShared == true)
            {
                tcmComponentTemplate = GetBluePrintTopTcmId(client, tcmComponentTemplate);

                componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
                if (componentTemplate == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                componentTemplate = client.CheckOut(componentTemplate.Id, true, new ReadOptions()) as ComponentTemplateData;
            }
            catch (Exception ex)
            {

            }

            if (componentTemplate == null)
                return LinkStatus.NotFound;

            componentTemplate.Content = newContent;

            try
            {
                componentTemplate = (ComponentTemplateData)client.Update(componentTemplate, new ReadOptions());
                client.CheckIn(componentTemplate.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (componentTemplate == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(componentTemplate.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveTbbFromComponentTemplate(SessionAwareCoreServiceClient client, string tcmComponentTemplate, string tcmTbb)
        {
            ComponentTemplateData componentTemplate = ReadItem(client, tcmComponentTemplate) as ComponentTemplateData;
            if (componentTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(componentTemplate.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                return tbbList.Count == 1 ? LinkStatus.Mandatory : LinkStatus.Found;
            }
            return LinkStatus.NotFound;
        }

        private static LinkStatus RemoveTbbFromCompoundTbb(SessionAwareCoreServiceClient client, string tcmCompoundTbb, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            TemplateBuildingBlockData compoundTbb = ReadItem(client, tcmCompoundTbb) as TemplateBuildingBlockData;
            if (compoundTbb == null || compoundTbb.TemplateType != "CompoundTemplate")
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(compoundTbb.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                if (tbbList.Count == 1)
                    return LinkStatus.Mandatory;
            }
            else
            {
                return LinkStatus.NotFound;
            }

            string newContent = RemoveTbbFromTemplate(compoundTbb.Content, tcmTbb);

            if (compoundTbb.BluePrintInfo.IsShared == true)
            {
                tcmCompoundTbb = GetBluePrintTopTcmId(client, tcmCompoundTbb);

                compoundTbb = ReadItem(client, tcmCompoundTbb) as TemplateBuildingBlockData;
                if (compoundTbb == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                compoundTbb = client.CheckOut(compoundTbb.Id, true, new ReadOptions()) as TemplateBuildingBlockData;
            }
            catch (Exception ex)
            {

            }

            if (compoundTbb == null)
                return LinkStatus.NotFound;

            compoundTbb.Content = newContent;

            try
            {
                compoundTbb = (TemplateBuildingBlockData)client.Update(compoundTbb, new ReadOptions());
                client.CheckIn(compoundTbb.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (compoundTbb == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(compoundTbb.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveTbbFromCompoundTbb(SessionAwareCoreServiceClient client, string tcmCompoundTbb, string tcmTbb)
        {
            TemplateBuildingBlockData compoundTbb = ReadItem(client, tcmCompoundTbb) as TemplateBuildingBlockData;
            if (compoundTbb == null || compoundTbb.TemplateType != "CompoundTemplate")
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(compoundTbb.Content);
            if (tbbList.Any(x => x.TcmId.GetId() == tcmTbb.GetId()))
            {
                return tbbList.Count == 1 ? LinkStatus.Mandatory : LinkStatus.Found;
            }
            return LinkStatus.NotFound;
        }

        private static List<ComponentFieldData> RemoveLinkFromValues(SessionAwareCoreServiceClient client, List<ComponentFieldData> values, XNamespace ns, string linkUri)
        {
            List<ComponentFieldData> newValues = new List<ComponentFieldData>();
            XNamespace linkNs = "http://www.w3.org/1999/xlink";

            foreach (ComponentFieldData value in values)
            {
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.GetId() == linkUri.GetId()))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.GetId() != linkUri.GetId()).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        return values;

                    value.Value = elements;
                    newValues.Add(value);
                }
                else if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.GetId() == linkUri.GetId())
                {
                    if (value.IsMandatory)
                        return values;
                }
                else if (value.SchemaField.IsEmbedded())
                {
                    if (value.Value is XElement)
                    {
                        value.Value = RemoveLinkFromXml(client, (XElement)value.Value, ns, (EmbeddedSchemaFieldDefinitionData)value.SchemaField, linkUri);
                        newValues.Add(value);
                    }
                    else if (value.Value is IList)
                    {
                        value.Value = ((IList)value.Value).Cast<XElement>().Select(x => RemoveLinkFromXml(client, x, ns, (EmbeddedSchemaFieldDefinitionData)value.SchemaField, linkUri)).ToList();
                        newValues.Add(value);
                    }
                }
                else
                {
                    newValues.Add(value);
                }
            }

            return newValues;
        }

        private static LinkStatus CheckRemoveLinkFromValues(SessionAwareCoreServiceClient client, XElement xml, XNamespace ns, List<ItemFieldDefinitionData> schemaFields, string linkUri)
        {
            XNamespace linkNs = "http://www.w3.org/1999/xlink";

            List<ComponentFieldData> values = GetValues(ns, schemaFields, xml);

            foreach (ComponentFieldData value in values)
            {
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.GetId() == linkUri.GetId()))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.GetId() != linkUri.GetId()).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        return LinkStatus.Mandatory;

                    return LinkStatus.Found;
                }
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.GetId() == linkUri.GetId())
                {
                    if (value.IsMandatory)
                        return LinkStatus.Mandatory;

                    return LinkStatus.Found;
                }
                if (value.SchemaField.IsEmbedded())
                {
                    List<ItemFieldDefinitionData> embeddedSchemaFields = GetSchemaFields(client, ((EmbeddedSchemaFieldDefinitionData)value.SchemaField).EmbeddedSchema.IdRef);
                    
                    if (value.Value is XElement)
                    {
                        LinkStatus status = CheckRemoveLinkFromValues(client, (XElement)value.Value, ns, embeddedSchemaFields, linkUri);
                        if (status != LinkStatus.NotFound)
                            return status;
                    }
                    else if (value.Value is IList)
                    {
                        foreach (XElement childValue in ((IList)value.Value).Cast<XElement>())
                        {
                            LinkStatus status = CheckRemoveLinkFromValues(client, childValue, ns, embeddedSchemaFields, linkUri);
                            if (status != LinkStatus.NotFound)
                                return status;
                        }
                    }
                }
            }

            return LinkStatus.NotFound;
        }

        //gets list of pairs schemaUri|field that are mandatory and contains specified link
        private static Dictionary<string, ItemFieldDefinitionData> GetMandatoryLinkFields(SessionAwareCoreServiceClient client, XElement xml, XNamespace ns, string schemaUri, string linkUri)
        {
            Dictionary<string, ItemFieldDefinitionData> res = new Dictionary<string, ItemFieldDefinitionData>();

            if(xml == null || string.IsNullOrEmpty(schemaUri) || string.IsNullOrEmpty(linkUri) || schemaUri == "tcm:0-0-0" || linkUri == "tcm:0-0-0")
                return res;

            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if (schema == null)
                return res;

            List<ItemFieldDefinitionData> schemaFields = schema.Purpose == SchemaPurpose.Metadata ? GetSchemaMetadataFields(client, schemaUri) : GetSchemaFields(client, schemaUri);

            List<ComponentFieldData> values = GetValues(ns, schemaFields, xml);

            XNamespace linkNs = "http://www.w3.org/1999/xlink";

            foreach (ComponentFieldData value in values)
            {
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.GetId() == linkUri.GetId()))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.GetId() != linkUri.GetId()).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        res.Add(schemaUri, value.SchemaField);
                }
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.GetId() == linkUri.GetId())
                {
                    if (value.IsMandatory)
                        res.Add(schemaUri, value.SchemaField);
                }
                if (value.SchemaField.IsEmbedded())
                {
                    if (value.Value is XElement)
                    {
                        Dictionary<string, ItemFieldDefinitionData> resEmbedded = GetMandatoryLinkFields(client, (XElement)value.Value, ns, ((EmbeddedSchemaFieldDefinitionData)value.SchemaField).EmbeddedSchema.IdRef, linkUri);
                        if (resEmbedded.Count > 0)
                        {
                            foreach (KeyValuePair<string, ItemFieldDefinitionData> pair in resEmbedded)
                            {
                                res.Add(pair.Key, pair.Value);
                            }
                        }
                    }
                    else if (value.Value is IList)
                    {
                        foreach (XElement childValue in ((IList)value.Value).Cast<XElement>())
                        {
                            Dictionary<string, ItemFieldDefinitionData> resEmbedded = GetMandatoryLinkFields(client, childValue, ns, ((EmbeddedSchemaFieldDefinitionData)value.SchemaField).EmbeddedSchema.IdRef, linkUri);
                            if (resEmbedded.Count > 0)
                            {
                                foreach (KeyValuePair<string, ItemFieldDefinitionData> pair in resEmbedded)
                                {
                                    res.Add(pair.Key, pair.Value);
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }

        private static string RemoveLinkFromXml(SessionAwareCoreServiceClient client, string schemaUri, string xmlContent, string linkUri)
        {
            if (string.IsNullOrEmpty(xmlContent))
                return xmlContent;

            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if(schema == null)
                return xmlContent;

            List<ItemFieldDefinitionData> schemaFields = schema.Purpose == SchemaPurpose.Metadata ? GetSchemaMetadataFields(client, schemaUri) : GetSchemaFields(client, schemaUri);

            List<ComponentFieldData> values = GetValues(schema.NamespaceUri, schemaFields, xmlContent);

            List<ComponentFieldData> newValues = RemoveLinkFromValues(client, values, schema.NamespaceUri, linkUri);

            XElement xml = GetComponentXml(schema.NamespaceUri, schema.RootElementName, newValues);

            return xml == null ? string.Empty : xml.ToString().PrettyXml();
        }

        private static XElement RemoveLinkFromXml(SessionAwareCoreServiceClient client, XElement parent, XNamespace ns, EmbeddedSchemaFieldDefinitionData embeddedSchemaField, string linkUri)
        {
            List<ItemFieldDefinitionData> schemaFields = GetSchemaFields(client, embeddedSchemaField.EmbeddedSchema.IdRef);

            List<ComponentFieldData> values = GetValues(ns, schemaFields, parent);

            List<ComponentFieldData> newValues = RemoveLinkFromValues(client, values, ns, linkUri);

            return GetComponentXml(ns, parent.Name.LocalName, newValues);
        }

        private static LinkStatus RemoveLinkFromComponent(SessionAwareCoreServiceClient client, string tcmComponent, string tcmLink, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            ComponentData component = ReadItem(client, tcmComponent) as ComponentData;
            if (component == null)
                return LinkStatus.NotFound;

            SchemaData schema = ReadItem(client, component.Schema.IdRef) as SchemaData;
            if(schema == null)
                return LinkStatus.NotFound;

            List<ItemFieldDefinitionData> schemaFields = GetSchemaFields(client, component.Schema.IdRef);

            LinkStatus status = CheckRemoveLinkFromValues(client, XElement.Parse(component.Content), schema.NamespaceUri, schemaFields, tcmLink);
            if (status != LinkStatus.Found)
                return status;

            string newContent = RemoveLinkFromXml(client, component.Schema.IdRef, component.Content, tcmLink);
            string newMetadata = RemoveLinkFromXml(client, component.MetadataSchema.IdRef, component.Metadata, tcmLink);

            if (component.BluePrintInfo.IsShared == true)
            {
                tcmComponent = GetBluePrintTopTcmId(client, tcmComponent);

                component = ReadItem(client, tcmComponent) as ComponentData;
                if (component == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                component = client.CheckOut(component.Id, true, new ReadOptions()) as ComponentData;
            }
            catch (Exception ex)
            {

            }

            if (component == null)
                return LinkStatus.NotFound;

            component.Content = newContent;
            component.Metadata = newMetadata;

            try
            {
                component = (ComponentData)client.Update(component, new ReadOptions());
                client.CheckIn(component.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;

                if (component == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(component.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveLinkFromComponent(SessionAwareCoreServiceClient client, string tcmComponent, string tcmLink)
        {
            ComponentData component = ReadItem(client, tcmComponent) as ComponentData;
            if (component == null)
                return LinkStatus.NotFound;

            SchemaData schema = ReadItem(client, component.Schema.IdRef) as SchemaData;
            if (schema == null)
                return LinkStatus.NotFound;

            List<ItemFieldDefinitionData> schemaFields = GetSchemaFields(client, component.Schema.IdRef);

            return CheckRemoveLinkFromValues(client, XElement.Parse(component.Content), schema.NamespaceUri, schemaFields, tcmLink);
        }

        private static LinkStatus RemoveLinkFromMetadata(SessionAwareCoreServiceClient client, string tcmItem, string tcmLink, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            RepositoryLocalObjectData item = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
            if (item == null)
                return LinkStatus.NotFound;

            SchemaData metadataSchema = ReadItem(client, item.MetadataSchema.IdRef) as SchemaData;
            if (metadataSchema == null)
                return LinkStatus.NotFound;

            List<ItemFieldDefinitionData> metadataSchemaFields = GetSchemaFields(client, item.MetadataSchema.IdRef);

            LinkStatus status = CheckRemoveLinkFromValues(client, XElement.Parse(item.Metadata), metadataSchema.NamespaceUri, metadataSchemaFields, tcmLink);
            if (status != LinkStatus.Found)
                return status;

            string newMetadata = RemoveLinkFromXml(client, item.MetadataSchema.IdRef, item.Metadata, tcmLink);

            if (item.BluePrintInfo.IsShared == true)
            {
                tcmItem = GetBluePrintTopTcmId(client, tcmItem);

                item = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
                if (item == null)
                    return LinkStatus.NotFound;
            }

            try
            {
                item = client.CheckOut(item.Id, true, new ReadOptions());
            }
            catch (Exception ex)
            {

            }

            if (item == null)
                return LinkStatus.NotFound;

            item.Metadata = newMetadata;

            try
            {
                item = (RepositoryLocalObjectData)client.Update(item, new ReadOptions());
                client.CheckIn(item.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                if (item == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(item.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveLinkFromMetadata(SessionAwareCoreServiceClient client, string tcmItem, string tcmLink)
        {
            RepositoryLocalObjectData item = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
            if(item == null || item.MetadataSchema == null || item.MetadataSchema.IdRef == "tcm:0-0-0")
                return LinkStatus.NotFound;

            SchemaData metadataSchema = ReadItem(client, item.MetadataSchema.IdRef) as SchemaData;
            if (metadataSchema == null)
                return LinkStatus.NotFound;

            List<ItemFieldDefinitionData> metadataSchemaFields = GetSchemaFields(client, item.MetadataSchema.IdRef);

            return CheckRemoveLinkFromValues(client, XElement.Parse(item.Metadata), metadataSchema.NamespaceUri, metadataSchemaFields, tcmLink);
        }

        private static void RemoveSchemaMandatoryLinkFields(SessionAwareCoreServiceClient client, RepositoryLocalObjectData itemData, string tcmDependentItem, List<ResultInfo> results, int level = 0)
        {
            string schemaUri = string.Empty;
            XElement xml = null;

            if (itemData is ComponentData)
            {
                ComponentData component = (ComponentData)itemData;
                schemaUri = component.Schema.IdRef;
                xml = XElement.Parse(component.Content);
            }
            else if(!string.IsNullOrEmpty(itemData.Metadata) && !string.IsNullOrEmpty(schemaUri) && schemaUri != "tcm:0-0-0")
            {
                schemaUri = itemData.MetadataSchema.IdRef;
                xml = XElement.Parse(itemData.Metadata);
            }

            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if (schema == null)
                return;

            Dictionary<string, ItemFieldDefinitionData> mandatoryLinkFields = GetMandatoryLinkFields(client, xml, schema.NamespaceUri, schemaUri, tcmDependentItem);

            foreach (string innerSchemaUri in mandatoryLinkFields.Keys)
            {
                ItemFieldDefinitionData field = mandatoryLinkFields[innerSchemaUri];

                SchemaData innerSchemaData = ReadItem(client, innerSchemaUri) as SchemaData;
                if (innerSchemaData == null)
                    continue;

                SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(innerSchemaUri, false, null);
                if (schemaFieldsData.Fields != null && schemaFieldsData.Fields.Any(x => x.Name == field.Name))
                {
                    schemaFieldsData.Fields.First(x => x.Name == field.Name).MinOccurs = 0;
                }
                if (schemaFieldsData.MetadataFields != null && schemaFieldsData.MetadataFields.Any(x => x.Name == field.Name))
                {
                    schemaFieldsData.MetadataFields.First(x => x.Name == field.Name).MinOccurs = 0;
                }

                try
                {
                    innerSchemaData = client.CheckOut(innerSchemaData.Id, true, new ReadOptions()) as SchemaData;
                }
                catch (Exception ex)
                {

                }

                if (innerSchemaData == null)
                    return;

                innerSchemaData.Xsd = client.ConvertSchemaFieldsToXsd(schemaFieldsData).ToString();

                try
                {
                    client.Save(innerSchemaData, new ReadOptions());
                    client.CheckIn(innerSchemaUri, new ReadOptions());

                    results.Add(new ResultInfo
                    {
                        Status = Status.Success,
                        Item = innerSchemaData.ToItem(),
                        Message = string.Format("Make non-mandatory field \"{0}\" in \"{1}\".", field.Name, innerSchemaData.GetWebDav()),
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing folder linked schema for \"{0}\". Error message \"{1}\"", itemData.GetWebDav(), ex.Message),
                        Item = itemData.ToItem(),
                        Status = Status.Error,
                        StackTrace = ex.StackTrace,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
            }
        }

        private static void CheckRemoveSchemaMandatoryLinkFields(SessionAwareCoreServiceClient client, RepositoryLocalObjectData itemData, string tcmDependentItem, List<ResultInfo> results, int level = 0)
        {
            string schemaUri = string.Empty;
            XElement xml = null;

            if (itemData is ComponentData)
            {
                ComponentData component = (ComponentData)itemData;
                schemaUri = component.Schema.IdRef;
                xml = XElement.Parse(component.Content);
            }
            else if (!string.IsNullOrEmpty(itemData.Metadata) && !string.IsNullOrEmpty(schemaUri) && schemaUri != "tcm:0-0-0")
            {
                schemaUri = itemData.MetadataSchema.IdRef;
                xml = XElement.Parse(itemData.Metadata);
            }

            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if (schema == null)
                return;

            Dictionary<string, ItemFieldDefinitionData> mandatoryLinkFields = GetMandatoryLinkFields(client, xml, schema.NamespaceUri, schemaUri, tcmDependentItem);

            foreach (string innerSchemaUri in mandatoryLinkFields.Keys)
            {
                ItemFieldDefinitionData field = mandatoryLinkFields[innerSchemaUri];

                SchemaData innerSchemaData = ReadItem(client, innerSchemaUri) as SchemaData;
                if(innerSchemaData == null)
                    continue;

                results.Add(new ResultInfo
                {
                    Status = Status.ChangeSchema,
                    Item = innerSchemaData.ToItem(),
                    Message = string.Format("Make non-mandatory field \"{0}\" in \"{1}\".", field.Name, innerSchemaData.GetWebDav()),
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });
            }
        }

        private static LinkStatus RemoveKeywordField(SessionAwareCoreServiceClient client, string schemaUri, string categoryUri, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if (schema == null)
                return LinkStatus.NotFound;

            if (schema.BluePrintInfo.IsShared == true)
            {
                schemaUri = GetBluePrintTopTcmId(client, schemaUri);

                schema = ReadItem(client, schemaUri) as SchemaData;
                if (schema == null)
                    return LinkStatus.NotFound;
            }

            SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(schemaUri, false, null);

            if (schema.Purpose != SchemaPurpose.Metadata && schemaFieldsData.Fields != null && schemaFieldsData.Fields.Any())
            {
                List<ItemFieldDefinitionData> schemaFields = schemaFieldsData.Fields.ToList();
                for (int index = 0; index < schemaFields.Count; index++)
                {
                    if (!(schemaFields[index] is KeywordFieldDefinitionData))
                        continue;

                    KeywordFieldDefinitionData keywordField = (KeywordFieldDefinitionData)schemaFields[index];
                    if (keywordField.Category.IdRef.GetId() != categoryUri.GetId())
                        continue;

                    schemaFieldsData.Fields[index] = new SingleLineTextFieldDefinitionData { Name = keywordField.Name, Description = keywordField.Description, DefaultValue = keywordField.DefaultValue?.Title, MinOccurs = keywordField.MinOccurs, MaxOccurs = keywordField.MaxOccurs };
                }
            }

            if (schemaFieldsData.MetadataFields != null && schemaFieldsData.MetadataFields.Any())
            {
                List<ItemFieldDefinitionData> metadataSchemaFields = schemaFieldsData.MetadataFields.ToList();
                for (int index = 0; index < metadataSchemaFields.Count; index++)
                {
                    if (!(metadataSchemaFields[index] is KeywordFieldDefinitionData))
                        continue;

                    KeywordFieldDefinitionData keywordField = (KeywordFieldDefinitionData)metadataSchemaFields[index];
                    if (keywordField.Category.IdRef.GetId() != categoryUri.GetId())
                        continue;

                    schemaFieldsData.MetadataFields[index] = new SingleLineTextFieldDefinitionData { Name = keywordField.Name, Description = keywordField.Description, DefaultValue = keywordField.DefaultValue?.Title, MinOccurs = keywordField.MinOccurs, MaxOccurs = keywordField.MaxOccurs };
                }
            }

            try
            {
                schema = client.CheckOut(schema.Id, true, new ReadOptions()) as SchemaData;
            }
            catch (Exception ex)
            {

            }

            if (schema == null)
                return LinkStatus.NotFound;

            schema.Xsd = client.ConvertSchemaFieldsToXsd(schemaFieldsData).ToString();

            try
            {
                schema = (SchemaData)client.Update(schema, new ReadOptions());
                client.CheckIn(schema.Id, new ReadOptions());
                return LinkStatus.Found;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                if (schema == null)
                    return LinkStatus.Error;

                client.UndoCheckOut(schema.Id, true, new ReadOptions());
                return LinkStatus.Error;
            }
        }

        private static LinkStatus CheckRemoveKeywordField(SessionAwareCoreServiceClient client, string schemaUri, string categoryUri)
        {
            SchemaData schema = ReadItem(client, schemaUri) as SchemaData;
            if (schema == null)
                return LinkStatus.NotFound;

            SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(schemaUri, false, null);

            if (schema.Purpose != SchemaPurpose.Metadata && schemaFieldsData.Fields != null && schemaFieldsData.Fields.Any())
            {
                List<ItemFieldDefinitionData> schemaFields = schemaFieldsData.Fields.ToList();
                if (schemaFields.OfType<KeywordFieldDefinitionData>().Any(keywordField => keywordField.Category.IdRef.GetId() == categoryUri.GetId()))
                    return LinkStatus.Found;
            }

            if (schemaFieldsData.MetadataFields != null && schemaFieldsData.MetadataFields.Any())
            {
                List<ItemFieldDefinitionData> metadataSchemaFields = schemaFieldsData.MetadataFields.ToList();
                if (metadataSchemaFields.OfType<KeywordFieldDefinitionData>().Any(keywordField => keywordField.Category.IdRef.GetId() == categoryUri.GetId()))
                    return LinkStatus.Found;
            }

            return LinkStatus.NotFound;
        }

        private static LinkStatus RemoveHistory(SessionAwareCoreServiceClient client, string tcmItem, string parentTcmId, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            List<HistoryItemInfo> history = GetItemHistory(client, tcmItem);

            if (history.Count <= 1)
                return LinkStatus.Mandatory;

            LinkStatus status = LinkStatus.NotFound;
            foreach (HistoryItemInfo historyItem in history)
            {
                if (historyItem.TcmId == history.Last().TcmId)
                    continue;

                List<string> historyItemUsedItems = GetUsedItems(client, historyItem.TcmId);
                if (historyItemUsedItems.Any(x => x.GetId() == parentTcmId.GetId()))
                {
                    try
                    {
                        client.Delete(historyItem.TcmId);
                        status = LinkStatus.Found;
                    }
                    catch (Exception ex)
                    {
                        stackTraceMessage = ex.Message;
                        return LinkStatus.Error;
                    }
                }
            }

            return status;
        }

        public static void DeleteTridionObject(SessionAwareCoreServiceClient client, string tcmItem, bool delete, bool unpublish, bool unlink, List<ResultInfo> results, string tcmDependentItem = "", bool currentVersion = true, int level = 0)
        {
            if (tcmItem.StartsWith("tcm:0-"))
                return;

            if (results.Any(x => x.Status == Status.Error))
                return;

            if (results.Count > 50)
            {
                results.Insert(0, new ResultInfo
                {
                    Message = "Delete stack exceeds 50 items. Please select other item",
                    Item = new ItemInfo { Title = "Delete stack exceeds 50 items" },
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            if (!ExistsItem(client, tcmItem))
                return;

            RepositoryLocalObjectData itemData = (RepositoryLocalObjectData)ReadItem(client, tcmItem);

            if (level > 3)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try delete item  manually \"{0}\"", itemData.GetWebDav()),
                    Item = itemData.ToItem(),
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            if (itemData.BluePrintInfo.IsShared == true)
            {
                tcmItem = GetBluePrintTopTcmId(client, tcmItem);

                itemData = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
                if (itemData == null)
                    return;
            }

            bool isAnyLocalized = IsAnyLocalized(client, tcmItem);

            List<string> usingItems = GetUsingItems(client, tcmItem);
            List<string> usingCurrentItems = GetUsingCurrentItems(client, tcmItem);

            if (currentVersion)
            {
                foreach (string usingItem in usingItems)
                {
                    //using category includes category into using list
                    if (usingItem.GetId() == tcmItem.GetId())
                        continue;

                    //try to unlink objects
                    if (unlink)
                    {
                        LinkStatus status = RemoveDependency(client, usingItem, tcmItem, delete, results, level + 1);
                        if (status == LinkStatus.Error)
                            return;

                        if (status == LinkStatus.Found)
                            continue;
                    }

                    //not wish or not able to unlink objects - delete whole parent object

                    ItemType usingItemType = GetItemType(usingItem);

                    if (usingItemType == ItemType.Folder || usingItemType == ItemType.StructureGroup || usingItemType == ItemType.Category)
                    {
                        DeleteFolderOrStructureGroup(client, usingItem, delete, unpublish, unlink, results, tcmItem, level + 1);
                    }
                    else
                    {
                        DeleteTridionObject(client, usingItem, delete, unpublish, unlink, results, tcmItem, usingCurrentItems.Any(x => x == usingItem), level + 1);
                    }
                }
            }

            if (unpublish)
            {
                //item is published - start unpublishing
                if (IsPublished(client, tcmItem))
                {
                    foreach (KeyValuePair<string, ItemInfo> publishedItem in GetPublishedItems(client, itemData.ToItem()))
                    {
                        //start unpublishing
                        UnPublish(client, new[] { publishedItem.Value.TcmId }, new[] { publishedItem.Key });

                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unpublishing item \"{0}\" from target \"{1}\"...", publishedItem.Value.Path, publishedItem.Key),
                            Item = publishedItem.Value,
                            Status = Status.Unpublish,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                }
            }
            else if (delete)
            {
                //unlocalize before delete
                if (isAnyLocalized)
                {
                    try
                    {
                        UnLocalizeAll(client, tcmItem);

                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unlocalized item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Success,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error unlocalizing item \"{0}\". Error message \"{1}\"", itemData.GetWebDav(), ex.Message),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = ex.StackTrace,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                }

                //undo checkout
                try
                {
                    client.UndoCheckOut(tcmItem, true, new ReadOptions());
                }
                catch (Exception ex)
                {

                }

                if (!currentVersion && itemData is VersionedItemData)
                {
                    //remove used versions
                    string stackTraceMessage;
                    LinkStatus status = RemoveHistory(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                    if (status == LinkStatus.Found)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Removed history for item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Success,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                    else
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error removing history from item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = stackTraceMessage,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                }
                else
                {
                    //delete used item
                    try
                    {
                        client.Delete(tcmItem);

                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Deleteed item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Delete,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error deleting item \"{0}\". Error message \"{1}\"", itemData.GetWebDav(), ex.Message),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = ex.StackTrace,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                }
            }
            else
            {
                //item is published - not possible to delete - unpublish first
                if (IsPublished(client, tcmItem))
                {
                    foreach (KeyValuePair<string, ItemInfo> publishedItem in GetPublishedItems(client, itemData.ToItem()))
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unpublish item \"{0}\" from target \"{1}\"", publishedItem.Value.Path, publishedItem.Key),
                            Item = publishedItem.Value,
                            Status = Status.Unpublish,
                            Level = level,
                            DependentItemTcmId = tcmDependentItem
                        });
                    }
                }

                if (isAnyLocalized)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Unlocalize item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Unlocalize,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }

                if (!currentVersion)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Remove old versions of item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.ChangeHistory,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
                else
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Delete item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Delete,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
            }
        }

        public static void DeleteFolderOrStructureGroup(SessionAwareCoreServiceClient client, string tcmFolder, bool delete, bool unpublish, bool unlink, List<ResultInfo> results, string tcmDependentItem = "", int level = 0)
        {
            if (results.Any(x => x.Status == Status.Error))
                return;

            if (results.Count > 50)
            {
                results.Insert(0, new ResultInfo
                {
                    Message = "Delete stack exceeds 50 items. Please select other item",
                    Item = new ItemInfo { Title = "Delete stack exceeds 50 items" },
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            if (level > 3)
            {
                RepositoryLocalObjectData itemData = (RepositoryLocalObjectData)ReadItem(client, tcmFolder);

                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try delete item  manually \"{0}\"", itemData.GetWebDav()),
                    Item = itemData.ToItem(),
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            List<BluePrintNodeData> bluePrintItems = GetBluePrintItems(client, tcmFolder);
            bluePrintItems.Reverse();

            foreach (BluePrintNodeData bluePrintItem in bluePrintItems)
            {
                List<ItemInfo> childItems = GetItemsByParentContainer(client, bluePrintItem.Item.Id).Where(x => x.IsLocal).ToList();
                
                //delete inner items
                foreach (ItemInfo childItem in childItems)
                {
                    if (childItem.ItemType == ItemType.Folder || childItem.ItemType == ItemType.StructureGroup)
                    {
                        DeleteFolderOrStructureGroup(client, childItem.TcmId, delete, unpublish, unlink, results, tcmFolder, level + 1);
                    }
                    else
                    {
                        if (ExistsItem(client, childItem.TcmId))
                            DeleteTridionObject(client, childItem.TcmId, delete, unpublish, unlink, results, tcmFolder, true, level + 1);
                    }

                    if (results.Any(x => x.Status == Status.Error))
                        return;
                }
            }

            DeleteTridionObject(client, tcmFolder, delete, unpublish, unlink, results, tcmDependentItem, true, level);
        }

        public static void DeletePublication(SessionAwareCoreServiceClient client, string tcmPublication, bool delete, bool unpublish, bool unlink, List<ResultInfo> results, string tcmDependentItem = "", int level = 0)
        {
            if (results.Any(x => x.Status == Status.Error))
                return;

            if (results.Count > 50)
            {
                results.Insert(0, new ResultInfo
                {
                    Message = "Delete stack exceeds 50 items. Please select other item",
                    Item = new ItemInfo { Title = "Delete stack exceeds 50 items" },
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            PublicationData publication = (PublicationData)ReadItem(client, tcmPublication);

            if (level > 3)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try delete publication  manually \"{0}\"", publication.Title),
                    Item = publication.ToItem(),
                    Status = Status.Error,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });

                return;
            }

            //delete dependent publications
            List<string> usingItems = GetUsingItems(client, tcmPublication);
            foreach (string usingItem in usingItems)
            {
                ItemType itemType = GetItemType(usingItem);
                if (itemType == ItemType.Publication)
                {
                    DeletePublication(client, usingItem, delete, unpublish, unlink, results, tcmPublication, level + 1);
                }
                else
                {
                    DeleteTridionObject(client, usingItem, delete, unpublish, unlink, results, tcmPublication, true, level + 1);
                }
            }

            //delete / inform published items
            List<ItemInfo> pulishedItems = GetItemsByPublication(client, tcmPublication, true).Where(x => x.IsPublished).ToList();
            foreach (ItemInfo publishedItem in pulishedItems)
            {
                DeleteTridionObject(client, publishedItem.TcmId, delete, unpublish, unlink, results, tcmPublication, true, level + 1);
            }

            try
            {
                if (delete)
                {
                    //delete publication as an object
                    client.Delete(tcmPublication);

                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Deleted publication \"{0}\"", publication.Title),
                        Item = publication.ToItem(),
                        Status = Status.Delete,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
                else
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Delete publication \"{0}\"", publication.Title),
                        Item = publication.ToItem(),
                        Status = Status.Delete,
                        Level = level,
                        DependentItemTcmId = tcmDependentItem
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Error deleting publication \"{0}\". Error message \"{1}\"", publication.Title, ex.Message),
                    Item = publication.ToItem(),
                    Status = Status.Error,
                    StackTrace = ex.StackTrace,
                    Level = level,
                    DependentItemTcmId = tcmDependentItem
                });
            }
        }

        #endregion

        #region Tridion publishing

        public static bool IsPublished(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return client.GetListPublishInfo(tcmItem).Any();
        }

        private static Dictionary<string, ItemInfo> GetPublishedItems(SessionAwareCoreServiceClient client, ItemInfo item)
        {
            return client.GetListPublishInfo(item.TcmId).ToDictionary(publishData => publishData.PublicationTarget.IdRef, publishData => new ItemInfo {TcmId = GetBluePrintItemTcmId(item.TcmId, publishData.Repository.IdRef), Title = item.Title, Icon = item.Icon, ItemType = item.ItemType, IsPublished = true, FromPub = publishData.Repository.Title, Path = string.IsNullOrEmpty(item.Path) ? string.Empty : item.Path.Replace(item.Path.Trim('\\').Split('\\')[0], publishData.Repository.Title)});
        }

        public static string[] Publish(SessionAwareCoreServiceClient client, string[] items, string[] targets)
        {
            RenderInstructionData renderInstruction = new RenderInstructionData();
            ResolveInstructionData resolveInstruction = new ResolveInstructionData
            {
                IncludeWorkflow = true
            };

            PublishInstructionData publishInstruction = new PublishInstructionData
            {
                DeployAt = DateTime.Now,
                MaximumNumberOfRenderFailures = 0,
                RenderInstruction = renderInstruction,
                ResolveInstruction = resolveInstruction,
                StartAt = DateTime.Now
            };

            var transactions = client.Publish(items, publishInstruction, targets, PublishPriority.Normal, null);
            if (transactions == null)
                return null;

            return transactions.Select(x => x.Id).ToArray();
        }

        public static string[] UnPublish(SessionAwareCoreServiceClient client, string[] items, string[] targets)
        {
            ResolveInstructionData resolveInstruction = new ResolveInstructionData
            {
                IncludeWorkflow = true
            };

            UnPublishInstructionData unPublishInstruction = new UnPublishInstructionData
            {
                ResolveInstruction = resolveInstruction,
                StartAt = DateTime.Now
            };

            var transactions = client.UnPublish(items, unPublishInstruction, targets, PublishPriority.Normal, null);
            if (transactions == null)
                return null;

            return transactions.Select(x => x.Id).ToArray();
        }

        #endregion

        #region Tridion Blueprint

        public static string GetBluePrintTopTcmId(SessionAwareCoreServiceClient client, string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            return list2.First().Item.Id;
        }

        public static List<BluePrintNodeData> GetBluePrintItems(SessionAwareCoreServiceClient client, string id)
        {
            if (id.StartsWith("tcm:0-"))
                return null;

            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return null;

            return list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();
        }

        public static bool IsAnyLocalized(SessionAwareCoreServiceClient client, string id)
        {
            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return false;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null && x.Id == x.Item.BluePrintInfo.OwningRepository.IdRef);

            return list2.Count() > 1;
        }

        public static void UnLocalizeAll(SessionAwareCoreServiceClient client, string id)
        {
            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null && x.Id == x.Item.BluePrintInfo.OwningRepository.IdRef).ToList();

            string topTcmId = list2.First().Item.Id;

            foreach (BluePrintNodeData item in list2)
            {
                UnLocalize(client, item.Item.ToItem(topTcmId));
            }
        }

        private static void UnLocalize(SessionAwareCoreServiceClient client, ItemInfo item)
        {
            if (item.IsLocalized)
                client.UnLocalize(item.TcmId, new ReadOptions());
        }

        #endregion

        #region Collection helpers

        public static List<ItemInfo> ToList(this XElement xml, ItemType itemType)
        {
            List<ItemInfo> res = new List<ItemInfo>();
            if (xml != null && xml.HasElements)
            {
                foreach (XElement element in xml.Nodes())
                {
                    ItemInfo item = new ItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = itemType;
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.Icon = element.Attributes().Any(x => x.Name == "Icon") ? element.Attribute("Icon").Value : "T16L0P0";
                    item.Path = element.Attributes().Any(x => x.Name == "Path") ? element.Attribute("Path").Value : string.Empty;

                    if (item.ItemType == ItemType.Schema)
                    {
                        if (element.Attributes().Any(x => x.Name == "Icon"))
                        {
                            string icon = element.Attribute("Icon").Value;
                            if (icon.EndsWith("S7"))
                            {
                                item.SchemaType = SchemaType.Bundle;
                            }
                            else if (icon.EndsWith("S6"))
                            {
                                item.SchemaType = SchemaType.Parameters;
                            }
                            else if (icon.EndsWith("S3"))
                            {
                                item.SchemaType = SchemaType.Metadata;
                            }
                            else if (icon.EndsWith("S2"))
                            {
                                item.SchemaType = SchemaType.Embedded;
                            }
                            else if (icon.EndsWith("S1"))
                            {
                                item.SchemaType = SchemaType.Multimedia;
                            }
                            else if (icon.EndsWith("S0"))
                            {
                                item.SchemaType = SchemaType.Component;
                            }
                            else
                            {
                                item.SchemaType = SchemaType.None;
                            }
                        }
                        else
                        {
                            item.SchemaType = SchemaType.None;
                        }
                    }
                    else
                    {
                        item.SchemaType = SchemaType.None;
                    }

                    if (item.ItemType == ItemType.Component)
                    {
                        item.MimeType = element.Attributes().Any(x => x.Name == "MIMEType") ? element.Attribute("MIMEType").Value : null;
                    }
                    
                    item.FromPub = element.Attributes().Any(x => x.Name == "FromPub") ? element.Attribute("FromPub").Value : null;
                    item.IsPublished = element.Attributes().Any(x => x.Name == "Icon") && element.Attribute("Icon").Value.EndsWith("P1");
                    
                    res.Add(item);
                }
            }
            return res;
        }

        public static List<ItemInfo> ToList(this XElement xml)
        {
            List<ItemInfo> res = new List<ItemInfo>();
            if (xml != null && xml.HasElements)
            {
                foreach (XElement element in xml.Nodes())
                {
                    ItemInfo item = new ItemInfo();
                    item.TcmId = element.Attribute("ID").Value;
                    item.ItemType = element.Attributes().Any(x => x.Name == "Type") ? (ItemType)int.Parse(element.Attribute("Type").Value) : GetItemType(item.TcmId);
                    item.Title = element.Attributes().Any(x => x.Name == "Title") ? element.Attribute("Title").Value : item.TcmId;
                    item.Icon = element.Attributes().Any(x => x.Name == "Icon") ? element.Attribute("Icon").Value : "T16L0P0";
                    item.Path = element.Attributes().Any(x => x.Name == "Path") ? element.Attribute("Path").Value : string.Empty;

                    if (item.ItemType == ItemType.Schema)
                    {
                        if (element.Attributes().Any(x => x.Name == "Icon"))
                        {
                            string icon = element.Attribute("Icon").Value;
                            if (icon.EndsWith("S7"))
                            {
                                item.SchemaType = SchemaType.Bundle;
                            }
                            else if (icon.EndsWith("S6"))
                            {
                                item.SchemaType = SchemaType.Parameters;
                            }
                            else if (icon.EndsWith("S3"))
                            {
                                item.SchemaType = SchemaType.Metadata;
                            }
                            else if (icon.EndsWith("S2"))
                            {
                                item.SchemaType = SchemaType.Embedded;
                            }
                            else if (icon.EndsWith("S1"))
                            {
                                item.SchemaType = SchemaType.Multimedia;
                            }
                            else if (icon.EndsWith("S0"))
                            {
                                item.SchemaType = SchemaType.Component;
                            }
                            else
                            {
                                item.SchemaType = SchemaType.None;
                            }
                        }
                        else
                        {
                            item.SchemaType = SchemaType.None;
                        }
                    }
                    else
                    {
                        item.SchemaType = SchemaType.None;
                    }

                    if (item.ItemType == ItemType.Component)
                    {
                        item.MimeType = element.Attributes().Any(x => x.Name == "MIMEType") ? element.Attribute("MIMEType").Value : null;    
                    }

                    item.FromPub = element.Attributes().Any(x => x.Name == "FromPub") ? element.Attribute("FromPub").Value : null;
                    item.IsPublished = element.Attributes().Any(x => x.Name == "Icon") && element.Attribute("Icon").Value.EndsWith("P1");
                    
                    res.Add(item);
                }
            }
            return res;
        }

        public static ItemInfo ToItem(this RepositoryLocalObjectData dataItem, string topTcmId = null)
        {
            ItemInfo item = new ItemInfo();
            item.TcmId = dataItem.Id;
            item.ItemType = GetItemType(dataItem.Id);
            item.Title = dataItem.Title;

            string webDav = dataItem.GetWebDav();
            item.Path = string.IsNullOrEmpty(webDav) ? string.Empty : Path.GetDirectoryName(webDav.Replace('/', '\\'));

            if (item.ItemType == ItemType.Schema)
            {
                SchemaData schemaDataItem = (SchemaData) dataItem;
                if (schemaDataItem.Purpose == SchemaPurpose.Bundle)
                {
                    item.SchemaType = SchemaType.Bundle;
                }
                else if (schemaDataItem.Purpose == SchemaPurpose.TemplateParameters)
                {
                    item.SchemaType = SchemaType.Parameters;
                }
                else if (schemaDataItem.Purpose == SchemaPurpose.Metadata)
                {
                    item.SchemaType = SchemaType.Metadata;
                }
                else if (schemaDataItem.Purpose == SchemaPurpose.Embedded)
                {
                    item.SchemaType = SchemaType.Embedded;
                }
                else if (schemaDataItem.Purpose == SchemaPurpose.Multimedia)
                {
                    item.SchemaType = SchemaType.Multimedia;
                }
                else if (schemaDataItem.Purpose == SchemaPurpose.Component)
                {
                    item.SchemaType = SchemaType.Component;
                }
                else
                {
                    item.SchemaType = SchemaType.None;
                }
            }

            if (GetPublicationTcmId(dataItem.Id) == dataItem.BluePrintInfo.OwningRepository.IdRef && dataItem.Id == topTcmId)
                item.FromPub = string.Empty;
            else if (GetPublicationTcmId(dataItem.Id) == dataItem.BluePrintInfo.OwningRepository.IdRef)
                item.FromPub = "(Local copy)"; 
            else
                item.FromPub = dataItem.BluePrintInfo.OwningRepository.Title;

            if (dataItem.IsPublishedInContext != null)
                item.IsPublished = dataItem.IsPublishedInContext.Value;

            item.Icon = "T" + (int)item.ItemType + "L0P" + (item.IsPublished ? "1" : "0");

            if (item.ItemType == ItemType.Component)
            {
                ComponentData componentDataItem = (ComponentData)dataItem;
                if (componentDataItem.ComponentType == ComponentType.Multimedia)
                {
                    item.MimeType = componentDataItem.BinaryContent.MimeType;

                    if (componentDataItem.BinaryContent.Filename != null)
                    {
                        string ext = Path.GetExtension(componentDataItem.BinaryContent.Filename);
                        item.Icon += "M" + ext.Trim('.');
                    }
                }
            }

            return item;
        }

        public static ItemInfo ToItem(this PublicationData publicationData)
        {
            ItemInfo item = new ItemInfo();
            item.TcmId = publicationData.Id;
            item.ItemType = ItemType.Publication;
            item.Title = publicationData.Title;
            item.Icon = "T1L0P0S0";

            return item;
        }

        public static ItemType GetItemType(string tcmItem)
        {
            if (string.IsNullOrEmpty(tcmItem))
                return ItemType.None;

            string[] arr = tcmItem.Replace("tcm:", string.Empty).Split('-');
            if (arr.Length == 2) return ItemType.Component;

            return (ItemType)int.Parse(arr[2]);
        }

        public static bool IsText(this ItemFieldDefinitionData field)
        {
            return field is SingleLineTextFieldDefinitionData && !field.IsTextSelect() || field is MultiLineTextFieldDefinitionData;
        }

        public static bool IsRichText(this ItemFieldDefinitionData field)
        {
            return field is XhtmlFieldDefinitionData;
        }

        public static bool IsDate(this ItemFieldDefinitionData field)
        {
            return field is DateFieldDefinitionData;
        }

        public static bool IsNumber(this ItemFieldDefinitionData field)
        {
            return field is NumberFieldDefinitionData;
        }

        public static bool IsKeyword(this ItemFieldDefinitionData field)
        {
            return field is KeywordFieldDefinitionData;
        }

        public static bool IsMultimedia(this ItemFieldDefinitionData field)
        {
            return field is MultimediaLinkFieldDefinitionData;
        }

        public static bool IsTextSelect(this ItemFieldDefinitionData field)
        {
            if (field is SingleLineTextFieldDefinitionData)
            {
                SingleLineTextFieldDefinitionData textField = (SingleLineTextFieldDefinitionData)field;
                return textField.List != null && textField.List.Entries != null && textField.List.Entries.Length > 0;
            }
            return false;
        }

        public static bool IsEmbedded(this ItemFieldDefinitionData field)
        {
            return field is EmbeddedSchemaFieldDefinitionData;
        }

        public static bool IsComponentLink(this ItemFieldDefinitionData field)
        {
            return field is ComponentLinkFieldDefinitionData;
        }

        public static bool IsMultiValue(this ItemFieldDefinitionData field)
        {
            return field.MaxOccurs == -1 || field.MaxOccurs > 1;
        }
        
        public static bool IsMandatory(this ItemFieldDefinitionData field)
        {
            return field.MinOccurs == 1;
        }

        #endregion

        #region Text helpers

        public static string GetId(this string tcmId)
        {
            if (string.IsNullOrEmpty(tcmId) || !tcmId.StartsWith("tcm:") || !tcmId.Contains("-"))
                return string.Empty;

            return tcmId.Split('-')[1];
        }

        public static string GetPublicationTcmId(string id)
        {
            ItemType itemType = GetItemType(id);
            if (itemType == ItemType.Publication)
                return id;

            return "tcm:0-" + id.Replace("tcm:", string.Empty).Split('-')[0] + "-1";
        }

        public static string GetBluePrintItemTcmId(string id, string publicationId)
        {
            if (string.IsNullOrEmpty(id) || !id.StartsWith("tcm:") || !id.Contains("-") || string.IsNullOrEmpty(publicationId) || !publicationId.StartsWith("tcm:") || !publicationId.Contains("-"))
                return string.Empty;

            return "tcm:" + publicationId.GetId() + "-" + id.GetId() + (id.Split('-').Length > 2 ? "-" + id.Split('-')[2] : string.Empty);
        }

        public static string PrettyXml(this string xml)
        {
            try
            {
                return XElement.Parse(xml).ToString().Replace(" xmlns=\"\"", "");
            }
            catch (Exception)
            {
                return xml;
            }
        }

        public static string GetInnerXml(this XElement node)
        {
            var reader = node.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        public static string CutPath(this string path, string separator, int maxLength)
        {
            if (path == null || path.Length <= maxLength)
                return path;

            var list = path.Split(separator[0]);
            int itemMaxLength = maxLength / list.Length;

            return string.Join(separator, list.Select(item => item.Cut(itemMaxLength)).ToList());
        }

        public static string Cut(this string str, int maxLength)
        {
            if (maxLength < 5)
                maxLength = 5;

            if (str.Length > maxLength)
            {
                return str.Substring(0, maxLength - 2) + "..";

            }
            return str;
        }

        #endregion

    }
}
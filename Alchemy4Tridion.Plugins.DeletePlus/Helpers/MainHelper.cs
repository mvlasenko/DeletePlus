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
            return (ReadItem(client, tcmItem) != null);
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

        private static void RemoveFolderLinkedSchema(SessionAwareCoreServiceClient client, string folderUri)
        {
            FolderData innerFolder = ReadItem(client, folderUri) as FolderData;
            if (innerFolder == null || innerFolder.LinkedSchema == null)
                return;

            if (innerFolder.IsEditable.Value)
            {
                try
                {
                    //change schema id
                    innerFolder.LinkedSchema.IdRef = "tcm:0-0-0";

                    //make non-mandatory to aviod conflicts with inner components
                    innerFolder.IsLinkedSchemaMandatory = false;

                    client.Save(innerFolder, new ReadOptions());
                }
                catch (Exception)
                {

                }
            }
        }

        private static void RemoveMetadataSchema(SessionAwareCoreServiceClient client, string itemUri)
        {
            RepositoryLocalObjectData item = ReadItem(client, itemUri) as RepositoryLocalObjectData;

            if (item == null || item.MetadataSchema == null)
                return;

            if (item.IsEditable.Value)
            {
                try
                {
                    //change schema id
                    item.MetadataSchema.IdRef = "tcm:0-0-0";

                    client.Save(item, new ReadOptions());
                }
                catch (Exception)
                {

                }
            }
        }

        #endregion

        #region Tridion delete

        public static void Delete(SessionAwareCoreServiceClient client, string tcmItem, bool delete, List<ResultInfo> results)
        {
            ItemType itemType = GetItemType(tcmItem);

            if (itemType == ItemType.Publication)
            {
                DeletePublication(client, tcmItem, delete, results);
            }
            //todo: category
            else if (itemType == ItemType.Folder || itemType == ItemType.StructureGroup)
            {
                DeleteFolderOrStructureGroup(client, tcmItem, delete, results);
            }
            else
            {
                DeleteTridionObject(client, tcmItem, delete, results);
            }
        }

        private static LinkStatus RemoveDependency(SessionAwareCoreServiceClient client, string tcmItem, string tcmDependentItem, bool delete, List<ResultInfo> results)
        {
            ResultInfo result = new ResultInfo();

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

            tcmItem = GetBluePrintTopTcmId(client, tcmItem);
            RepositoryLocalObjectData itemData = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
            result.Item = itemData.ToItem();

            if (delete)
            {
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
                //remove component or keyword link from component
                else if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = CheckRemoveLinkFromComponent(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field change
                    if (status == LinkStatus.Mandatory)
                    {
                        RemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results);
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
                        RemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results);
                    }

                    status = RemoveLinkFromMetadata(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                if (status == LinkStatus.Found)
                    status = RemoveHistory(client, tcmItem, tcmDependentItem, out stackTraceMessage);

                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Success;
                    result.Message = string.Format("Item \"{1}\" was removed from \"{0}\".", itemData == null ? tcmItem : itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
                }
            }
            else
            {
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
                //check if possible to remove component or keyword link from component
                else if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = CheckRemoveLinkFromComponent(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field needs to be changed
                    if (status == LinkStatus.Mandatory)
                    {
                        CheckRemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results);
                    }

                    status = LinkStatus.Found;
                }
                //check if possible to remove component or keyword link from metadata
                else if (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword)
                {
                    status = CheckRemoveLinkFromMetadata(client, tcmItem, tcmDependentItem);

                    //component link is mandatory - schema field needs to be changed
                    if (status == LinkStatus.Mandatory)
                    {
                        CheckRemoveSchemaMandatoryLinkFields(client, itemData, tcmDependentItem, results);
                    }

                    status = LinkStatus.Found;
                }

                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Info;
                    result.Message = string.Format("Remove item \"{1}\" from \"{0}\".", itemData == null ? tcmItem : itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
                }
            }

            if (status == LinkStatus.Error)
            {
                result.Status = Status.Error;
                result.StackTrace = stackTraceMessage;
                result.Message = string.Format("Not able to unlink \"{1}\" from \"{0}\".", itemData == null ? tcmItem : itemData.GetWebDav(), dependentItemData == null ? tcmDependentItem : dependentItemData.GetWebDav());
            }

            if(status != LinkStatus.NotFound)
                results.Add(result);

            return status;
        }

        private static LinkStatus RemoveComponentPresentation(SessionAwareCoreServiceClient client, string tcmPage, string tcmDependentItem, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            PageData page = ReadItem(client, tcmPage) as PageData;
            if (page == null)
                return LinkStatus.NotFound;

            ComponentPresentationData[] newComponentPresentations = page.ComponentPresentations.Where(x => x.Component.IdRef.Split('-')[1] != tcmDependentItem.Split('-')[1] && x.ComponentTemplate.IdRef.Split('-')[1] != tcmDependentItem.Split('-')[1]).ToArray();

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
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
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

            return page.ComponentPresentations.Any(x => x.Component.IdRef.Split('-')[1] == tcmDependentItem.Split('-')[1] || x.ComponentTemplate.IdRef.Split('-')[1] == tcmDependentItem.Split('-')[1]) ? LinkStatus.Found : LinkStatus.NotFound;
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
            List<TbbInfo> tbbList = GetTbbList(templateContent).Where(x => x.TcmId.Split('-')[1] != tcmTbb.Split('-')[1]).ToList();
            return GetTemplateContent(tbbList);
        }

        private static LinkStatus RemoveTbbFromPageTemplate(SessionAwareCoreServiceClient client, string tcmPageTemplate, string tcmTbb, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            PageTemplateData pageTemplate = ReadItem(client, tcmPageTemplate) as PageTemplateData;
            if (pageTemplate == null)
                return LinkStatus.NotFound;

            List<TbbInfo> tbbList = GetTbbList(pageTemplate.Content);
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
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
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
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
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
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
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
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
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
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
            if (tbbList.Any(x => x.TcmId.Split('-')[1] == tcmTbb.Split('-')[1]))
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
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1]))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.Split('-')[1] != linkUri.Split('-')[1]).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        return values;

                    value.Value = elements;
                    newValues.Add(value);
                }
                else if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1])
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
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1]))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.Split('-')[1] != linkUri.Split('-')[1]).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        return LinkStatus.Mandatory;

                    return LinkStatus.Found;
                }
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1])
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
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.IsMultiValue && value.Value is IList && ((IList)value.Value).Cast<XElement>().Any(x => x.Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1]))
                {
                    List<XElement> elements = ((IList)value.Value).Cast<XElement>().Where(x => x.Attribute(linkNs + "href").Value.Split('-')[1] != linkUri.Split('-')[1]).ToList();

                    if (value.IsMandatory && elements.Count == 0)
                        res.Add(schemaUri, value.SchemaField);
                }
                if ((value.SchemaField.IsComponentLink() || value.SchemaField.IsMultimedia() || value.SchemaField.IsKeyword()) && value.Value is XElement && ((XElement)value.Value).Attribute(linkNs + "href") != null && ((XElement)value.Value).Attribute(linkNs + "href").Value.Split('-')[1] == linkUri.Split('-')[1])
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
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
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
                stackTraceMessage = ex.Message;
                return LinkStatus.NotFound;
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

        private static void RemoveSchemaMandatoryLinkFields(SessionAwareCoreServiceClient client, RepositoryLocalObjectData itemData, string tcmDependentItem, List<ResultInfo> results)
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
                if (schemaFieldsData.Fields.Any(x => x.Name == field.Name))
                {
                    schemaFieldsData.Fields.First(x => x.Name == field.Name).MinOccurs = 0;
                }
                if (schemaFieldsData.MetadataFields.Any(x => x.Name == field.Name))
                {
                    schemaFieldsData.MetadataFields.First(x => x.Name == field.Name).MinOccurs = 0;
                }

                try
                {
                    innerSchemaData.Xsd = client.ConvertSchemaFieldsToXsd(schemaFieldsData).ToString();
                    client.Save(innerSchemaData, new ReadOptions());

                    results.Add(new ResultInfo
                    {
                        Status = Status.Info,
                        Item = innerSchemaData.ToItem(),
                        Message = string.Format("Make non-mandatory field \"{0}\" in \"{1}\".", field.Name, innerSchemaData.GetWebDav())
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing folder linked schema for \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Error,
                        StackTrace = ex.StackTrace
                    });
                }
            }
        }

        private static void CheckRemoveSchemaMandatoryLinkFields(SessionAwareCoreServiceClient client, RepositoryLocalObjectData itemData, string tcmDependentItem, List<ResultInfo> results)
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
                    Status = Status.Info,
                    Item = innerSchemaData.ToItem(),
                    Message = string.Format("Make non-mandatory field \"{0}\" in \"{1}\".", field.Name, innerSchemaData.GetWebDav())
                });
            }
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
                if (historyItemUsedItems.Any(x => x.Split('-')[1] == parentTcmId.Split('-')[1]))
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

        public static void DeleteTridionObject(SessionAwareCoreServiceClient client, string tcmItem, bool delete, List<ResultInfo> results, string parentTcmId = "", bool currentVersion = true, int level = 0)
        {
            if (tcmItem.StartsWith("tcm:0-"))
                return;

            tcmItem = GetBluePrintTopTcmId(client, tcmItem);

            ItemType itemType = GetItemType(tcmItem);

            RepositoryLocalObjectData itemData = (RepositoryLocalObjectData)ReadItem(client, tcmItem);

            if (level > 3)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try delete item  manually \"{0}\"", itemData.GetWebDav()),
                    Item = itemData.ToItem(),
                    Status = Status.Error
                });

                return;
            }

            bool isAnyLocalized = IsAnyLocalized(client, tcmItem);

            List<string> usingItems = GetUsingItems(client, tcmItem);
            List<string> usingCurrentItems = GetUsingCurrentItems(client, tcmItem);

            if (currentVersion)
            {
                foreach (string usingItem in usingItems)
                {
                    LinkStatus status = RemoveDependency(client, usingItem, tcmItem, delete, results);
                    if (status == LinkStatus.Error)
                    {
                        return;
                    }

                    //not able to unlink objects - delete whole parent object
                    if (status != LinkStatus.Found)
                    {
                        DeleteTridionObject(client, usingItem, delete, results, tcmItem, usingCurrentItems.Any(x => x == usingItem), level + 1);
                    }
                }
            }

            //remove folder linked schema
            if (itemType == ItemType.Folder)
            {
                try
                {
                    FolderData folder = itemData as FolderData;
                    if (folder != null && folder.LinkedSchema != null && folder.LinkedSchema.IdRef != "tcm:0-0-0")
                    {
                        if (delete)
                            RemoveFolderLinkedSchema(client, tcmItem);

                        if (delete)
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Removed folder linked schema for \"{0}\"", itemData.GetWebDav()),
                                Item = itemData.ToItem(),
                                Status = Status.Success
                            });
                        }

                        else
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Remove folder linked schema for \"{0}\"", itemData.GetWebDav()),
                                Item = itemData.ToItem(),
                                Status = Status.Info
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing folder linked schema for \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Error,
                        StackTrace = ex.StackTrace
                    });
                }
            }

            //remove metadata
            if (itemType == ItemType.Folder || itemType == ItemType.StructureGroup || itemType == ItemType.Publication)
            {
                try
                {
                    if (itemData != null && itemData.MetadataSchema != null && itemData.MetadataSchema.IdRef != "tcm:0-0-0")
                    {
                        if (delete)
                            RemoveMetadataSchema(client, tcmItem);

                        if (delete)
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Removed metadata schema for \"{0}\"", itemData.GetWebDav()),
                                Item = itemData.ToItem(),
                                Status = Status.Success
                            });
                        }

                        else
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Remove metadata schema for \"{0}\"", itemData.GetWebDav()),
                                Item = itemData.ToItem(),
                                Status = Status.Info
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing metadata schema for \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Error,
                        StackTrace = ex.StackTrace
                    });
                }
            }

            if (delete)
            {
                //item is published - not possible to delete - STOP PROCESSING
                if (IsPublished(client, tcmItem))
                {
                    foreach (ItemInfo publishedItem in GetPublishedItems(client, itemData.ToItem()))
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Item \"{0}\" is published", publishedItem.Path),
                            Item = publishedItem,
                            Status = Status.Error
                        });
                    }

                    return;
                }

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
                            Status = Status.Success
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error unlocalizing item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = ex.StackTrace
                        });
                    }
                }

                //undo checkout
                try
                {
                    client.UndoCheckOut(tcmItem, true, new ReadOptions());
                }
                catch (Exception)
                {
                }

                if (!currentVersion)
                {
                    //remove used versions
                    string stackTraceMessage;
                    LinkStatus status = RemoveHistory(client, tcmItem, parentTcmId, out stackTraceMessage);
                    if (status == LinkStatus.Found)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Removed history for item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Success
                        });
                    }
                    else
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error removing history from item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = stackTraceMessage
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
                            Status = Status.Success
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Error deleteing item \"{0}\"", itemData.GetWebDav()),
                            Item = itemData.ToItem(),
                            Status = Status.Error,
                            StackTrace = ex.StackTrace
                        });
                    }
                }
            }
            else
            {
                //item is published - not possible to delete - WARNING
                if (IsPublished(client, tcmItem))
                {
                    foreach (ItemInfo publishedItem in GetPublishedItems(client, itemData.ToItem()))
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unpublish manually item \"{0}\"", publishedItem.Path),
                            Item = publishedItem,
                            Status = Status.Warning
                        });
                    }
                }

                if (isAnyLocalized)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Unlocalize item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Info
                    });
                }

                if (!currentVersion)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Remove old versions of item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Info
                    });
                }
                else
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Delete item \"{0}\"", itemData.GetWebDav()),
                        Item = itemData.ToItem(),
                        Status = Status.Info
                    });
                }
            }
        }

        public static void DeleteFolderOrStructureGroup(SessionAwareCoreServiceClient client, string tcmFolder, bool delete, List<ResultInfo> results, int level = 0)
        {
            if (level > 3)
            {
                RepositoryLocalObjectData itemData = (RepositoryLocalObjectData)ReadItem(client, tcmFolder);

                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try delete item  manually \"{0}\"", itemData.GetWebDav()),
                    Item = itemData.ToItem(),
                    Status = Status.Error
                });

                return;
            }

            List<ResultInfo> folderResults = new List<ResultInfo>();

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
                        DeleteFolderOrStructureGroup(client, childItem.TcmId, delete, folderResults, level + 1);
                    }
                    else
                    {
                        if (ExistsItem(client, childItem.TcmId))
                            DeleteTridionObject(client, childItem.TcmId, delete, folderResults, tcmFolder, true, level);
                    }
                }
            }

            results.AddRange(folderResults.Distinct(new ResultInfoComparer()));

            DeleteTridionObject(client, tcmFolder, delete, results, string.Empty, true, level);
        }

        public static void DeletePublication(SessionAwareCoreServiceClient client, string tcmPublication, bool delete, List<ResultInfo> results, int level = 0)
        {
            PublicationData publication = (PublicationData)ReadItem(client, tcmPublication);

            //delete dependent publications
            List<string> usingItems = GetUsingItems(client, tcmPublication);
            foreach (string usingItem in usingItems)
            {
                ItemType itemType = GetItemType(usingItem);
                if (itemType == ItemType.Publication)
                {
                    DeletePublication(client, usingItem, delete, results, level + 1);
                }
                else
                {
                    DeleteTridionObject(client, usingItem, delete, results, string.Empty, true, level + 1);
                }
            }

            List<ResultInfo> publicationResults = new List<ResultInfo>();

            //delete / inform published items
            List<ItemInfo> pulishedItems = GetItemsByPublication(client, tcmPublication, true).Where(x => x.IsPublished).ToList();
            foreach (ItemInfo publishedItem in pulishedItems)
            {
                DeleteTridionObject(client, publishedItem.TcmId, delete, publicationResults, string.Empty, true, level);
            }

            results.AddRange(publicationResults.Distinct(new ResultInfoComparer()));

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
                        Status = Status.Success
                    });
                }
                else
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Delete publication \"{0}\"", publication.Title),
                        Item = publication.ToItem(),
                        Status = Status.Info
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Error deleting publication \"{0}\"", publication.Title),
                    Item = publication.ToItem(),
                    Status = Status.Error,
                    StackTrace = ex.StackTrace
                });
            }
        }

        #endregion

        #region Tridion publishing

        public static bool IsPublished(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return client.GetListPublishInfo(tcmItem).Any();
        }

        private static List<ItemInfo> GetPublishedItems(SessionAwareCoreServiceClient client, ItemInfo item)
        {
            return client.GetListPublishInfo(item.TcmId).Select(p => new ItemInfo {TcmId = GetBluePrintItemTcmId(item.TcmId, p.Repository.IdRef), Title = item.Title, Icon = item.Icon, ItemType = item.ItemType, IsPublished = true, FromPub = p.Repository.Title, Path = string.IsNullOrEmpty(item.Path) ? string.Empty : item.Path.Replace(item.Path.Trim('\\').Split('\\')[0], p.Repository.Title)}).ToList();
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

        private class ResultInfoComparer : IEqualityComparer<ResultInfo>
        {
            public bool Equals(ResultInfo x, ResultInfo y)
            {
                return x.TcmId == y.TcmId && x.Status == y.Status;
            }

            public int GetHashCode(ResultInfo obj)
            {
                return obj.TcmId.GetHashCode();
            }
        }

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

        public static string GetPublicationTcmId(string id)
        {
            ItemType itemType = GetItemType(id);
            if (itemType == ItemType.Publication)
                return id;

            return "tcm:0-" + id.Replace("tcm:", string.Empty).Split('-')[0] + "-1";
        }

        public static string GetBluePrintItemTcmId(string id, string publicationId)
        {
            if (String.IsNullOrEmpty(id))
                return id;

            return "tcm:" + publicationId.Split('-')[1] + "-" + id.Split('-')[1] + (id.Split('-').Length > 2 ? "-" + id.Split('-')[2] : "");
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

        #endregion

    }
}
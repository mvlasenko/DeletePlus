using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Alchemy4Tridion.Plugins.DeletePlus.Models;
using Tridion.ContentManager.CoreService.Client;

namespace Alchemy4Tridion.Plugins.DeletePlus.Helpers
{
    public static class MainHelper
    {
        #region Tridion items access

        //todo: use it for deleter
        public static bool SavePageTemplate(SessionAwareCoreServiceClient client, string title, string xml, string tcmContainer, string fileExtension, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            if (ExistsItem(client, tcmContainer, title))
            {
                string id = GetItemTcmId(client, tcmContainer, title);
                if (string.IsNullOrEmpty(id))
                    return false;

                PageTemplateData templateData = ReadItem(client, id) as PageTemplateData;
                if (templateData == null)
                    return false;

                if (templateData.BluePrintInfo.IsShared == true)
                {
                    id = GetBluePrintTopTcmId(client, id);

                    templateData = ReadItem(client, id) as PageTemplateData;
                    if (templateData == null)
                        return false;
                }

                try
                {
                    templateData = client.CheckOut(id, true, new ReadOptions()) as PageTemplateData;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;
                    return false;
                }

                if (templateData == null)
                    return false;

                templateData.Content = xml;
                templateData.Title = title;
                templateData.LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } };
                templateData.FileExtension = fileExtension;

                try
                {
                    templateData = (PageTemplateData)client.Update(templateData, new ReadOptions());

                    if (templateData.Content == xml)
                    {
                        client.CheckIn(id, new ReadOptions());
                        return true;
                    }

                    client.UndoCheckOut(id, true, new ReadOptions());
                    return false;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (templateData == null)
                        return false;

                    client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
            }

            try
            {
                PageTemplateData templateData = new PageTemplateData
                {
                    Content = xml,
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    TemplateType = "CompoundTemplate",
                    FileExtension = fileExtension
                };

                templateData = (PageTemplateData)client.Save(templateData, new ReadOptions());
                client.CheckIn(templateData.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        //todo: use it for deleter
        public static bool SaveComponentTemplate(SessionAwareCoreServiceClient client, string title, string xml, string tcmContainer, string outputFormat, bool dynamic, out string stackTraceMessage, params string[] allowedSchemaNames)
        {
            stackTraceMessage = "";

            List<LinkToSchemaData> schemaList = new List<LinkToSchemaData>();

            string tcmPublication = GetPublicationTcmId(tcmContainer);
            List<ItemInfo> allSchemas = GetSchemas(client, tcmPublication);

            if (allowedSchemaNames != null && allowedSchemaNames.Length > 0)
            {
                foreach (string schemaName in allowedSchemaNames)
                {
                    if (allSchemas.Any(x => x.Title == schemaName))
                    {
                        string tcmSchema = allSchemas.First(x => x.Title == schemaName).TcmId;
                        LinkToSchemaData link = new LinkToSchemaData {IdRef = tcmSchema};
                        schemaList.Add(link);
                    }
                }
            }

            if (ExistsItem(client, tcmContainer, title))
            {
                string id = GetItemTcmId(client, tcmContainer, title);
                if (string.IsNullOrEmpty(id))
                    return false;

                ComponentTemplateData templateData = ReadItem(client, id) as ComponentTemplateData;
                if (templateData == null)
                    return false;

                if (templateData.BluePrintInfo.IsShared == true)
                {
                    id = GetBluePrintTopTcmId(client, id);

                    templateData = ReadItem(client, id) as ComponentTemplateData;
                    if (templateData == null)
                        return false;
                }

                try
                {
                    templateData = client.CheckOut(templateData.Id, true, new ReadOptions()) as ComponentTemplateData;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;
                    return false;
                }

                if (templateData == null)
                    return false;

                templateData.Content = xml;
                templateData.Title = title;
                templateData.LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } };
                templateData.OutputFormat = outputFormat;
                templateData.RelatedSchemas = schemaList.ToArray();

                try
                {
                    templateData = (ComponentTemplateData)client.Update(templateData, new ReadOptions());

                    if (templateData.Content == xml)
                    {
                        client.CheckIn(templateData.Id, new ReadOptions());
                        return true;
                    }

                    client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (templateData == null)
                        return false;

                    client.UndoCheckOut(templateData.Id, true, new ReadOptions());
                    return false;
                }
            }

            try
            {
                ComponentTemplateData templateData = new ComponentTemplateData
                {
                    Content = xml,
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    TemplateType = "CompoundTemplate",
                    OutputFormat = outputFormat,
                    IsRepositoryPublishable = dynamic,
                    AllowOnPage = true,
                    RelatedSchemas = schemaList.ToArray()
                };

                templateData = (ComponentTemplateData)client.Save(templateData, new ReadOptions());
                client.CheckIn(templateData.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        //todo: use it for deleter
        public static bool SavePage(SessionAwareCoreServiceClient client, string title, string fileName, string tcmContainer, string tcmPageTemplate, Dictionary<string, string> componentPresentations, out string stackTraceMessage)
        {
            stackTraceMessage = "";

            tcmPageTemplate = ("start-" + tcmPageTemplate).Replace("start-" + (tcmPageTemplate.Split('-'))[0], (tcmContainer.Split('-'))[0]);

            List<ComponentPresentationData> componentPresentationsList = new List<ComponentPresentationData>();
            if (componentPresentations != null && componentPresentations.Count > 0)
            {
                foreach (string tcmComponent in componentPresentations.Keys)
                {
                    string tcmComponentTemplate = componentPresentations[tcmComponent];
                    ComponentPresentationData item = new ComponentPresentationData
                    {
                        Component = new LinkToComponentData { IdRef = tcmComponent },
                        ComponentTemplate = new LinkToComponentTemplateData { IdRef = tcmComponentTemplate }
                    };
                    componentPresentationsList.Add(item);
                }
            }

            if (ExistsItem(client, tcmContainer, title))
            {
                string id = GetItemTcmId(client, tcmContainer, title);
                if (string.IsNullOrEmpty(id))
                    return false;

                PageData page = ReadItem(client, id) as PageData;
                if (page == null)
                    return false;

                if (page.BluePrintInfo.IsShared == true)
                {
                    id = GetBluePrintTopTcmId(client, id);

                    page = ReadItem(client, id) as PageData;
                    if (page == null)
                        return false;
                }

                try
                {
                    page = client.CheckOut(page.Id, true, new ReadOptions()) as PageData;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;
                    return false;
                }

                if (page == null)
                    return false;

                page.Title = title;
                page.FileName = fileName;
                page.LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } };
                page.PageTemplate = new LinkToPageTemplateData { IdRef = tcmPageTemplate };
                page.ComponentPresentations = componentPresentationsList.ToArray();

                try
                {
                    page = (PageData)client.Update(page, new ReadOptions());
                    client.CheckIn(page.Id, new ReadOptions());
                    return true;
                }
                catch (Exception ex)
                {
                    stackTraceMessage = ex.Message;

                    if (page == null)
                        return false;

                    client.UndoCheckOut(page.Id, true, new ReadOptions());
                    return false;
                }
            }

            try
            {
                PageData page = new PageData
                {
                    Title = title,
                    FileName = fileName,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0",
                    IsPageTemplateInherited = false,
                    PageTemplate = new LinkToPageTemplateData { IdRef = tcmPageTemplate },
                    ComponentPresentations = componentPresentationsList.ToArray()
                };

                page = (PageData)client.Save(page, new ReadOptions());
                client.CheckIn(page.Id, new ReadOptions());
                return true;
            }
            catch (Exception ex)
            {
                stackTraceMessage = ex.Message;
                return false;
            }
        }

        public static string CreateFolder(SessionAwareCoreServiceClient client, string title, string tcmContainer)
        {
            try
            {
                FolderData folderData = new FolderData
                {
                    Title = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0"
                };

                folderData = client.Save(folderData, new ReadOptions()) as FolderData;
                if (folderData == null)
                    return string.Empty;

                return folderData.Id;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string CreateFolderChain(SessionAwareCoreServiceClient client, List<string> folderChain, string tcmContainer)
        {
            if (folderChain == null || folderChain.Count == 0 || string.IsNullOrEmpty(tcmContainer))
                return tcmContainer;

            string topFolder = folderChain[0];
            List<ItemInfo> items = GetFoldersByParentFolder(client, tcmContainer);
            if (items.All(x => x.Title != topFolder))
            {
                CreateFolder(client, topFolder, tcmContainer);
                items = GetFoldersByParentFolder(client, tcmContainer);
            }

            string tcmTopFolder = items.First(x => x.Title == topFolder).TcmId;

            return CreateFolderChain(client, folderChain.Skip(1).ToList(), tcmTopFolder);
        }

        public static string CreateStructureGroup(SessionAwareCoreServiceClient client, string title, string tcmContainer)
        {
            try
            {
                StructureGroupData sgData = new StructureGroupData
                {
                    Title = title,
                    Directory = title,
                    LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = tcmContainer } },
                    Id = "tcm:0-0-0"
                };

                sgData = client.Save(sgData, new ReadOptions()) as StructureGroupData;
                if (sgData == null)
                    return string.Empty;

                return sgData.Id;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static ComponentData GetComponent(SessionAwareCoreServiceClient client, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return ReadItem(client, id) as ComponentData;
        }

        public static bool ExistsItem(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return (ReadItem(client, tcmItem) != null);
        }

        public static bool ExistsItem(SessionAwareCoreServiceClient client, string tcmContainer, string itemTitle)
        {
            if (string.IsNullOrEmpty(tcmContainer))
                return false;

            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            return client.GetList(tcmContainer, filter).Any(x => x.Title == itemTitle);
        }

        public static string GetItemTcmId(SessionAwareCoreServiceClient client, string tcmContainer, string itemTitle)
        {
            if (string.IsNullOrEmpty(tcmContainer))
                return string.Empty;

            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            foreach (XElement element in client.GetListXml(tcmContainer, filter).Nodes())
            {
                if (element.Attribute("Title").Value == itemTitle)
                    return element.Attribute("ID").Value;
            }

            return string.Empty;
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

        public static List<ItemInfo> GetItemsByParentContainer(SessionAwareCoreServiceClient client, string tcmContainer, ItemType[] itemTypes)
        {
            return client.GetListXml(tcmContainer, new OrganizationalItemItemsFilterData { ItemTypes = itemTypes }).ToList();
        }

        public static List<ItemInfo> GetFoldersByParentFolder(SessionAwareCoreServiceClient client, string tcmFolder)
        {
            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Folder } }).ToList(ItemType.Folder);
        }

        public static List<ItemInfo> GetFolders(SessionAwareCoreServiceClient client, string tcmFolder, bool recursive)
        {
            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Folder }, Recursive = recursive }).ToList(ItemType.Folder);
        }

        public static List<ItemInfo> GetTbbsByParentFolder(SessionAwareCoreServiceClient client, string tcmFolder)
        {
            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.TemplateBuildingBlock } }).ToList(ItemType.TemplateBuildingBlock);
        }

        public static List<ItemInfo> GetStructureGroupsByParentStructureGroup(SessionAwareCoreServiceClient client, string tcmSG)
        {
            return client.GetListXml(tcmSG, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.StructureGroup } }).ToList(ItemType.StructureGroup);
        }

        public static List<ItemInfo> GetFoldersByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Folder } }).ToList(ItemType.Folder);
        }

        public static List<ItemInfo> GetStructureGroupsByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.StructureGroup } }).ToList(ItemType.StructureGroup);
        }

        public static List<ItemInfo> GetContainersByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Folder, ItemType.StructureGroup } }).ToList();
        }

        public static List<ItemInfo> GetCategoriesByPublication(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            return client.GetListXml(tcmPublication, new RepositoryItemsFilterData { ItemTypes = new[] { ItemType.Category } }).ToList();
        }

        public static List<ItemInfo> GetKeywordsByCategory(SessionAwareCoreServiceClient client, string tcmCategory)
        {
            return client.GetListXml(tcmCategory, new OrganizationalItemItemsFilterData { ItemTypes = new[] { ItemType.Keyword } }).ToList(ItemType.Keyword);
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

        public static List<ItemInfo> GetPublications(SessionAwareCoreServiceClient client)
        {
            return client.GetSystemWideListXml(new PublicationsFilterData()).ToList(ItemType.Publication);
        }

        public static List<ItemInfo> GetPublications(SessionAwareCoreServiceClient client, string filterItemId)
        {
            List<ItemInfo> publications = GetPublications(client);
            var allowedPublications = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = filterItemId } }).Cast<BluePrintNodeData>().Where(x => x.Item != null).Select(x => GetPublicationTcmId(x.Item.Id)).ToList();
            return publications.Where(x => allowedPublications.Any(y => y == x.TcmId)).ToList();
        }

        public static string GetWebDav(this RepositoryLocalObjectData item)
        {
            string webDav = HttpUtility.UrlDecode(item.LocationInfo.WebDavUrl.Replace("/webdav/", string.Empty));
            if (string.IsNullOrEmpty(webDav))
                return string.Empty;

            int dotIndex = webDav.LastIndexOf(".", StringComparison.Ordinal);
            int slashIndex = webDav.LastIndexOf("/", StringComparison.Ordinal);

            return dotIndex >= 0 && dotIndex > slashIndex ? webDav.Substring(0, dotIndex) : webDav;
        }

        public static List<string> GetWebDavChain(this RepositoryLocalObjectData item)
        {
            List<string> res = item.LocationInfo.WebDavUrl.Replace("/webdav/", string.Empty).Split('/').Select(HttpUtility.UrlDecode).ToList();

            if (res.Last().Contains("."))
                res[res.Count - 1] = res.Last().Substring(0, res.Last().LastIndexOf(".", StringComparison.Ordinal));
            
            return res;
        }

        public static List<string> Substract(this List<string> input, List<string> toSubstract)
        {
            if (input == null || toSubstract == null)
                return input;

            return String.Join("|||", input).Replace(String.Join("|||", toSubstract), string.Empty).Split(new [] { "|||" }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

        public static string GetItemContainer(SessionAwareCoreServiceClient client, string tcmItem)
        {
            RepositoryLocalObjectData item = client.Read(tcmItem, new ReadOptions()) as RepositoryLocalObjectData;
            if (item == null)
                return string.Empty;

            return item.LocationInfo.OrganizationalItem.IdRef;
        }

        #endregion

        #region Tridion schemas

        public static List<ItemInfo> GetSchemas(SessionAwareCoreServiceClient client, string tcmPublication)
        {
            ItemInfo folder0 = GetFoldersByPublication(client, tcmPublication)[0];
            return client.GetListXml(folder0.TcmId, new OrganizationalItemItemsFilterData { Recursive = true, ItemTypes = new[] { ItemType.Schema }, SchemaPurposes = new[] { SchemaPurpose.Component, SchemaPurpose.Metadata } }).ToList(ItemType.Schema);
        }

        public static List<ItemInfo> GetSchemas(SessionAwareCoreServiceClient client, string tcmFolder, bool recursive)
        {
            return client.GetListXml(tcmFolder, new OrganizationalItemItemsFilterData { Recursive = recursive, ItemTypes = new[] { ItemType.Schema } }).ToList(ItemType.Schema);
        }

        public static List<ItemFieldDefinitionData> GetSchemaFields(SessionAwareCoreServiceClient client, string tcmSchema)
        {
            SchemaFieldsData schemaFieldsData;

            //todo: hot fix - find better solution
            if (tcmSchema.Contains("-v"))
            {
                string version = tcmSchema.Split('-')[3];

                SchemaData schema = (SchemaData)ReadItem(client, tcmSchema);

                string versionSchemaUri = GetItemTcmId(client, schema.LocationInfo.OrganizationalItem.IdRef, schema.Title + "_" + version);
                if (string.IsNullOrEmpty(versionSchemaUri))
                {
                    versionSchemaUri = DublicateSchema(client, schema, schema.Title + "_" + version);
                }

                schemaFieldsData = client.ReadSchemaFields(versionSchemaUri, false, null);
                if (schemaFieldsData == null || schemaFieldsData.Fields == null)
                    return null;

                return schemaFieldsData.Fields.ToList();
            }

            schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            if (schemaFieldsData == null || schemaFieldsData.Fields == null)
                return null;

            return schemaFieldsData.Fields.ToList();
        }

        public static List<ItemFieldDefinitionData> GetSchemaMetadataFields(SessionAwareCoreServiceClient client, string tcmSchema)
        {
            SchemaFieldsData schemaFieldsData;

            //todo: hot fix - find better solution
            if (tcmSchema.Contains("-v"))
            {
                string version = tcmSchema.Split('-')[3];

                SchemaData schema = (SchemaData)ReadItem(client, tcmSchema);

                string versionSchemaUri = GetItemTcmId(client, schema.LocationInfo.OrganizationalItem.IdRef, schema.Title + "_" + version);
                if (string.IsNullOrEmpty(versionSchemaUri))
                {
                    versionSchemaUri = DublicateSchema(client, schema, schema.Title + "_" + version);
                }

                schemaFieldsData = client.ReadSchemaFields(versionSchemaUri, false, null);
                if (schemaFieldsData == null || schemaFieldsData.MetadataFields == null)
                    return null;

                return schemaFieldsData.MetadataFields.ToList();
            }

            schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            if (schemaFieldsData == null || schemaFieldsData.MetadataFields == null)
                return null;

            return schemaFieldsData.MetadataFields.ToList();
        }

        //todo: hot fix - find better solution
        public static string DublicateSchema(SessionAwareCoreServiceClient client, SchemaData schema, string newName)
        {
            SchemaData newSchema = new SchemaData
            {
                Title = newName,
                Description = schema.Description,
                RootElementName = schema.RootElementName,
                Purpose = schema.Purpose,
                LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = schema.LocationInfo.OrganizationalItem.IdRef } },
                Id = "tcm:0-0-0",
                Xsd = schema.Xsd,
                AllowedMultimediaTypes = schema.AllowedMultimediaTypes,
            };

            newSchema = client.Save(newSchema, new ReadOptions()) as SchemaData;
            if (newSchema == null)
                return "";

            var res = client.CheckIn(newSchema.Id, new ReadOptions());
            return res.Id;
        }

        public static List<T> GetSchemaFields<T>(SessionAwareCoreServiceClient client, string tcmSchema) where T : ItemFieldDefinitionData
        {
            SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            return schemaFieldsData.Fields.Where(x => x is T).Cast<T>().ToList();
        }

        public static List<T> GetSchemaMetadataFields<T>(SessionAwareCoreServiceClient client, string tcmSchema) where T : ItemFieldDefinitionData
        {
            SchemaFieldsData schemaFieldsData = client.ReadSchemaFields(tcmSchema, false, null);
            return schemaFieldsData.MetadataFields.Where(x => x is T).Cast<T>().ToList();
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

        //todo: use it
        public static string DetectComponentSchemaVersion(SessionAwareCoreServiceClient client, ComponentData component)
        {
            string schemaUri = component.Schema.IdRef;

            SchemaData schema = client.Read(schemaUri, null) as SchemaData;
            if (schema == null)
                return string.Empty;

            if (component.VersionInfo.RevisionDate > schema.VersionInfo.RevisionDate)
                return schemaUri;

            List<HistoryItemInfo> schemaHistory = GetItemHistory(client, schemaUri);
            schemaHistory.Reverse();

            HistoryItemInfo historyItem = schemaHistory.FirstOrDefault(x => x.Modified < component.VersionInfo.RevisionDate);

            if (historyItem == null)
                return string.Empty;

            return historyItem.TcmId;
        }

        //todo: use it
        public static string DetectMetadataSchemaVersion(SessionAwareCoreServiceClient client, RepositoryLocalObjectData tridionObject)
        {
            string metadataSchemaUri = tridionObject.MetadataSchema.IdRef;

            SchemaData metadataSchema = client.Read(metadataSchemaUri, null) as SchemaData;
            if (metadataSchema == null)
                return string.Empty;

            if (tridionObject.VersionInfo.RevisionDate > metadataSchema.VersionInfo.RevisionDate)
                return metadataSchemaUri;

            List<HistoryItemInfo> metadataSchemaHistory = GetItemHistory(client, metadataSchemaUri);
            metadataSchemaHistory.Reverse();

            HistoryItemInfo historyItem = metadataSchemaHistory.FirstOrDefault(x => x.Modified < tridionObject.VersionInfo.RevisionDate);

            if (historyItem == null)
                return string.Empty;

            return historyItem.TcmId;
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

        public static object GetComponentFieldData(string value, ItemFieldDefinitionData schemaField)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (schemaField.IsNumber())
                return string.IsNullOrEmpty(value) ? null : (double?)double.Parse(value);

            if (schemaField.IsDate())
                return string.IsNullOrEmpty(value) ? null : (DateTime?)DateTime.Parse(value);

            return value;
        }

        public static object GetComponentSingleValue(ComponentData componentData, ItemFieldDefinitionData schemaField, XNamespace ns)
        {
            if (componentData == null || string.IsNullOrEmpty(componentData.Content))
                return null;

            XDocument doc = XDocument.Parse(componentData.Content);
            XElement element = null;
            
            if (doc.Root != null)
            {
                element = doc.Root.Element(ns + schemaField.Name);
            }

            if (element == null && !string.IsNullOrEmpty(componentData.Metadata))
            {
                XDocument docMeta = XDocument.Parse(componentData.Metadata);
                if (docMeta.Root != null)
                {
                    element = docMeta.Root.Element(ns + schemaField.Name);
                }
            }

            return element.GetComponentFieldData(schemaField);
        }

        public static List<FieldMappingInfo> GetDefaultFieldMapping(SessionAwareCoreServiceClient client, List<ItemFieldDefinitionData> targetFields, string targetSchemaUri)
        {
            List<FieldMappingInfo> fieldMapping = new List<FieldMappingInfo>();
            foreach (FieldInfo targetSchemaLink in GetAllFields(client, targetFields, null, false, true))
            {
                fieldMapping.Add(new FieldMappingInfo { SourceFields = targetFields.Select(x => new FieldInfo { Field = x }).ToList(), SourceFieldFullName = targetSchemaLink.GetFieldFullName(), TargetFields = targetFields.Select(x => new FieldInfo { Field = x }).ToList(), TargetFieldFullName = targetSchemaLink.GetFieldFullName() });
            }

            return fieldMapping;
        }

        public static List<FieldInfo> ExpandChildFields(SessionAwareCoreServiceClient client, List<FieldInfo> list)
        {
            if (list == null)
                return null;

            List<FieldInfo> res = new List<FieldInfo>();
            foreach (FieldInfo field in list)
            {
                if (field.Field.IsComponentLink())
                {
                    string childSchemaId = ((ComponentLinkFieldDefinitionData)field.Field).AllowedTargetSchemas[0].IdRef;
                    SchemaData childSchema = client.Read(childSchemaId, null) as SchemaData;
                    if (childSchema != null)
                    {
                        field.RootElementName = childSchema.RootElementName;
                    }
                }

                res.Add(field);

                if (field.Level < 3 && (field.Field.IsEmbedded() || field.Field.IsComponentLink() && !field.Field.IsMultimediaComponentLink() && ((ComponentLinkFieldDefinitionData)field.Field).AllowedTargetSchemas.Any()))
                {
                    string childSchemaId = field.Field.IsEmbedded() ? 
                        ((EmbeddedSchemaFieldDefinitionData) field.Field).EmbeddedSchema.IdRef : 
                        ((ComponentLinkFieldDefinitionData) field.Field).AllowedTargetSchemas[0].IdRef;

                    var schemaFields = GetSchemaFields(client, childSchemaId);
                    var childFields = schemaFields.Select(x => new FieldInfo { Field = x }).ToList();

                    foreach (FieldInfo childField in childFields)
                    {
                        childField.Parent = field;
                        childField.Level = field.Level + 1;
                    }

                    foreach (FieldInfo childField in ExpandChildFields(client, childFields))
                    {
                        res.Add(childField);
                    }
                }
            }

            return res;
        }

        public static List<FieldInfo> GetAllFields(SessionAwareCoreServiceClient client, List<ItemFieldDefinitionData> schemaFields, List<ItemFieldDefinitionData> metadataFields, bool includeSourceSystemItems, bool includeTargetSystemItems)
        {
            List<FieldInfo> res = new List<FieldInfo>();

            if (schemaFields != null)
            {
                foreach (ItemFieldDefinitionData item in schemaFields)
                {
                    FieldInfo field = new FieldInfo();
                    field.IsMeta = false;
                    field.Field = item;
                    field.Level = 0;
                    res.Add(field);
                }
            }

            if (metadataFields != null)
            {
                foreach (ItemFieldDefinitionData item in metadataFields)
                {
                    FieldInfo field = new FieldInfo();
                    field.IsMeta = true;
                    field.Field = item;
                    field.Level = 0;
                    res.Add(field);
                }
            }

            res = ExpandChildFields(client, res);

            if (includeSourceSystemItems)
            {
                //link to source component
                res.Insert(0, new FieldInfo { Field = new ComponentLinkFieldDefinitionData { Name = "< this component link >" } });
                //new empty embedded schema, with possible child values
                res.Insert(0, new FieldInfo { Field = new ItemFieldDefinitionData { Name = "< new >" } });
                //ignore item
                res.Insert(0, new FieldInfo { Field = new ItemFieldDefinitionData() });
            }
            else if (includeTargetSystemItems)
            {
                //link to target component
                res.Insert(0, new FieldInfo { Field = new ComponentLinkFieldDefinitionData { Name = "< target component link >" } });
            }

            return res;
        }

        public static void SetHistoryMappingTree(List<HistoryItemMappingInfo> HistoryMapping)
        {
            if (HistoryMapping == null)
                return;
            
            foreach (HistoryItemMappingInfo historyMapping in HistoryMapping)
            {
                //back to tree mapping - set ChildFieldMapping property from plain mapping collection
                foreach (FieldMappingInfo mapping in historyMapping.Mapping)
                {
                    List<FieldMappingInfo> childMapping = historyMapping.Mapping.Where(x => x.TargetField.Parent.GetFieldFullName().Trim() == mapping.TargetField.GetFieldFullName().Trim()).ToList();
                    if (childMapping.Count > 0)
                        mapping.ChildFieldMapping = childMapping;
                }

                //back to tree mapping - remove > 0 level items
                historyMapping.Mapping = historyMapping.Mapping.Where(x => x.TargetField.Level == 0).ToList();
            }
        }

        public static List<FieldMappingInfo> GetCurrentMapping(this HistoryMappingInfo historyMapping)
        {
            return historyMapping.First(x => x.Current).Mapping;
        }

        public static List<FieldMappingInfo> GetDetectedMapping(this HistoryMappingInfo historyMapping, DateTime? tridionObjectDate)
        {
            if (tridionObjectDate == null)
                return GetCurrentMapping(historyMapping);

            List<HistoryItemMappingInfo> sortedHistoryItemMapping = new List<HistoryItemMappingInfo>();
            sortedHistoryItemMapping.AddRange(historyMapping.Select(x => x).OrderBy(x => x.Modified));
            sortedHistoryItemMapping.Reverse();

            HistoryItemMappingInfo historyMappingItem = sortedHistoryItemMapping.FirstOrDefault(x => x.Modified < tridionObjectDate);

            return historyMappingItem == null ? GetCurrentMapping(historyMapping) : historyMappingItem.Mapping;
        }

        public static object GetDefaultValue(this FieldMappingInfo mapping)
        {
            return GetComponentFieldData(mapping.DefaultValue, mapping.TargetField.Field);
        }

        public static XElement GetDefaultXmlValue(this FieldMappingInfo mapping, XNamespace ns)
        {
            object defaultValue = mapping.GetDefaultValue();

            if (defaultValue == null)
                return null;

            if (defaultValue is XElement)
                return defaultValue as XElement;

            return new XElement(ns + mapping.TargetField.Field.Name, defaultValue);
        }

        public static XElement GetByXPath(this XElement root, string xPath, XNamespace ns)
        {
            if (root == null || string.IsNullOrEmpty(xPath))
                return null;

            xPath = xPath.Trim('/');
            if (string.IsNullOrEmpty(xPath))
                return null;

            if (xPath.Contains("/"))
            {
                xPath = "/xhtml:" + xPath.Replace("/", "/xhtml:");
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(new NameTable());
                namespaceManager.AddNamespace("xhtml", ns.ToString());
                return root.XPathSelectElement(xPath, namespaceManager);
            }

            return root.Element(ns + xPath);
        }

        public static List<XElement> GetListByXPath(this XElement root, string xPath, XNamespace ns)
        {
            if (root == null || string.IsNullOrEmpty(xPath))
                return null;

            xPath = xPath.Trim('/');

            if (string.IsNullOrEmpty(xPath))
                return null;

            if (xPath.Contains("/"))
            {
                return root.Elements(ns + xPath.Split('/')[0]).ToList().SelectMany(x => x.GetListByXPath(xPath.Substring(xPath.IndexOf('/')), ns)).ToList();
            }

            return root.Elements(ns + xPath).ToList();
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

        public static XElement GetKeywordLink(string id, string title, string fieldName)
        {
            XElement res = GetComponentLink(id, title, fieldName);
            res.Add(title);
            return res;
        }

        private static XElement GetSourceMappedValue(FieldMappingInfo mapping, XElement root, XNamespace sourceNs)
        {
            XElement value;

            List<FieldMappingInfo> childFieldMapping = mapping.ChildFieldMapping != null ? mapping.ChildFieldMapping.Where(x => x.SourceField != null && x.SourceField.Field != null && x.SourceField.Field.Name != null && x.SourceFieldFullName != "< ignore >").ToList() : null;

            if (!mapping.Equals && childFieldMapping != null && childFieldMapping.Count > 0)
            {
                XNamespace ns = string.Empty;
                value = new XElement(ns + mapping.TargetField.Field.Name);

                foreach (FieldMappingInfo childMapping in childFieldMapping)
                {
                    XElement child = GetSourceMappedValue(childMapping, root, sourceNs);
                    if (child != null)
                    {
                        value.Add(child);
                    }
                }
            }
            else
            {
                XElement defaultValue = mapping.GetDefaultXmlValue(sourceNs);

                if (root == null || mapping.SourceFieldFullName == "< new >")
                {
                    value = defaultValue;
                }
                else
                {
                    value = root.GetByXPath(mapping.SourceField.GetFieldNamePath(true), sourceNs);
                    value = value != null ? value.Clone(mapping.TargetField.Field.Name) : defaultValue;
                }
            }

            return value;
        }

        private static List<XElement> GetSourceMappedValues(FieldMappingInfo mapping, XElement root, XNamespace sourceNs)
        {
            if (root == null)
                return new List<XElement>();

            List<XElement> values;

            List<FieldMappingInfo> childFieldMapping = mapping.ChildFieldMapping != null ? mapping.ChildFieldMapping.Where(x => x.SourceField != null && x.SourceField.Field != null && x.SourceField.Field.Name != null && x.SourceFieldFullName != "< ignore >").ToList() : null;

            if (!mapping.Equals && childFieldMapping != null && childFieldMapping.Count > 0)
            {
                values = new List<XElement>();

                int maxCount = childFieldMapping.Max(x => root.GetListByXPath(x.SourceField.GetFieldNamePath(true), sourceNs).Count);
                
                for (int i = 0; i < maxCount; i++)
                {
                    XNamespace ns = string.Empty;
                    XElement value = new XElement(ns + mapping.TargetField.Field.Name);
                    values.Add(value);
                }

                foreach (FieldMappingInfo childMapping in childFieldMapping)
                {
                    XElement childDefaultValue = childMapping.GetDefaultXmlValue(sourceNs);

                    if (childMapping.SourceFieldFullName == "< new >")
                    {
                        foreach (XElement value in values)
                        {
                            value.Add(childDefaultValue);
                        }
                    }
                    else
                    {
                        List<XElement> children = GetSourceMappedValues(childMapping, root, sourceNs);
                        int i = 0;
                        foreach (XElement child in children)
                        {
                            XElement value = values[i];
                            value.Add(child);
                            i++;
                        }
                    }
                }
            }
            else
            {
                XElement defaultValue = mapping.GetDefaultXmlValue(sourceNs);

                if (mapping.SourceFieldFullName == "< new >")
                {
                    values = defaultValue != null ? new List<XElement> { defaultValue } : new List<XElement>();
                }
                else
                {
                    values = root.GetListByXPath(mapping.SourceField.GetFieldNamePath(true), sourceNs);

                    if (values != null && values.Count > 0)
                    {
                        values = values.Select(x => x.Clone(mapping.TargetField.Field.Name)).ToList();
                    }
                    else
                    {
                        values = defaultValue != null ? new List<XElement> { defaultValue } : new List<XElement>();
                    }
                }
            }
            
            return values;
        }

        public static XElement EmbeddedSchemaToComponentLink(this XElement sourceElement, SessionAwareCoreServiceClient client, ComponentLinkFieldDefinitionData targetField, string sourceTcmId, List<FieldMappingInfo> childFieldMapping, string targetFolderUri, int index, List<ResultInfo> results)
        {
            if (sourceElement == null || targetField == null)
                return null;

            if (!targetField.AllowedTargetSchemas.Any())
                return null;

            string targetSchemaUri = targetField.AllowedTargetSchemas[0].IdRef;

            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return null;

            List<ItemFieldDefinitionData> targetFields = GetSchemaFields(client, targetSchemaUri);

            string xml = sourceElement.ToString();

            XNamespace ns = XDocument.Parse(xml).Root.GetDefaultNamespace();

            //get fixed xml
            string newXml = GetFixedContent(client, xml, ns, sourceTcmId, targetSchema, targetSchema.RootElementName, targetFields, targetFolderUri, childFieldMapping, results);

            if (string.IsNullOrEmpty(newXml))
                return null;

            ComponentData sourceComponent = GetComponent(client, sourceTcmId);

            string title = string.Format("[{0:00}0] {1}", index, sourceComponent.Title);

            ResultInfo result = SaveComponent(client, targetSchema, title, newXml, string.Empty, targetFolderUri, false);
            if (result == null)
                return null;

            results.Add(result);

            return GetComponentLink(result.TcmId, title, targetField.Name);
        }

        public static XElement ComponentLinkToEmbeddedSchema(this XElement sourceElement, SessionAwareCoreServiceClient client, EmbeddedSchemaFieldDefinitionData targetField, List<FieldMappingInfo> childFieldMapping, string targetFolderUri, List<ResultInfo> results)
        {
            XNamespace ns = "http://www.w3.org/1999/xlink";
            string sourceComponentUri = sourceElement.Attribute(ns + "href").Value;

            ComponentData sourceComponent = client.Read(sourceComponentUri, new ReadOptions()) as ComponentData;
            if (sourceComponent == null || string.IsNullOrEmpty(sourceComponent.Content))
                return null;

            string sourceSchemaUri = sourceComponent.Schema.IdRef;
            SchemaData sourceSchema = client.Read(sourceSchemaUri, null) as SchemaData;
            if (sourceSchema == null)
                return null;
           
            string targetEmbeddedSchemaUri = targetField.EmbeddedSchema.IdRef;
            SchemaData targetEmbeddedSchema = client.Read(targetEmbeddedSchemaUri, null) as SchemaData;
            if (targetEmbeddedSchema == null)
                return null;

            List<ItemFieldDefinitionData> targetEmbeddedSchemaFields = GetSchemaFields(client, targetEmbeddedSchemaUri);

            string xml = sourceComponent.Content;

            //get fixed xml
            string newXml = GetFixedContent(client, xml, sourceSchema.NamespaceUri, sourceComponentUri, targetEmbeddedSchema, targetField.Name, targetEmbeddedSchemaFields, targetFolderUri, childFieldMapping, results);

            if (string.IsNullOrEmpty(newXml))
                return null;

            return XElement.Parse(newXml);
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

        public static XElement ToXhtml(string html, string rootName)
        {
            XElement x = XElement.Parse(string.Format("<{0}>{1}</{0}>", rootName, HttpUtility.HtmlDecode(html)));

            XNamespace ns = string.Empty;
            XElement res = new XElement(ns + rootName);

            XNamespace nsXhtml = "http://www.w3.org/1999/xhtml";

            if (x.Elements().Any())
            {
                foreach (XElement p in x.Elements())
                {
                    res.Add(new XElement(nsXhtml + p.Name.LocalName, p.Nodes(), p.Attributes()));
                }
            }
            else
            {
                res.Add(x.Value);    
            }

            return res;
        }

        public static List<ComponentFieldData> GetFixedValues(SessionAwareCoreServiceClient client, string sourceXml, XNamespace sourceNs, string sourceTcmId, SchemaData targetSchema, string targetRootElementName, List<ItemFieldDefinitionData> targetFields, string targetFolderUri, List<FieldMappingInfo> fieldMapping, List<ResultInfo> results)
        {
            if (fieldMapping == null || string.IsNullOrEmpty(sourceXml))
                return null;

            XDocument doc = XDocument.Parse(sourceXml);
            if (doc.Root == null)
                return null;

            List<ComponentFieldData> res = new List<ComponentFieldData>();

            foreach (ItemFieldDefinitionData targetField in targetFields)
            {
                FieldMappingInfo mapping = fieldMapping.FirstOrDefault(x => x.TargetField != null && x.TargetField.Field.Name == targetField.Name && x.TargetField.Field.GetFieldType() == targetField.GetFieldType());
                if (mapping == null)
                    continue;

                ComponentFieldData item = new ComponentFieldData();
                item.SchemaField = targetField;

                ItemFieldDefinitionData sourceField = mapping.SourceField.Field;

                if (sourceField == null)
                    continue;

                //construct Component Link to the source component
                if (mapping.SourceField.GetFieldFullName() == "< this component link >")
                {
                    ComponentData component = GetComponent(client, sourceTcmId);
                    item.Value = GetComponentLink(sourceTcmId, component.Title, targetField.Name);
                }

                //construct new Embedded Schema
                else if (mapping.SourceField.GetFieldFullName() == "< new >")
                {
                    //create XElement if embedded schema, for primitive types default values might be used
                    if (mapping.TargetField.Field.IsEmbedded())
                    {
                        if (!targetField.IsMultiValue())
                        {
                            item.Value = GetSourceMappedValue(mapping, doc.Root, sourceNs);
                        }
                        else
                        {
                            item.Value = GetSourceMappedValues(mapping, doc.Root, sourceNs);
                        }
                    }
                }

                else if (!string.IsNullOrEmpty(sourceField.Name) && doc.Root.Element(sourceNs + sourceField.Name) != null)
                {
                    List<XElement> elements = doc.Root.Elements(sourceNs + sourceField.Name).ToList();

                    //transform Embedded Schema into Component Link
                    if (mapping.SourceField.Field.IsEmbedded() && mapping.TargetField.Field.IsComponentLink())
                    {
                        if (!targetField.IsMultiValue())
                        {
                            item.Value = elements.FirstOrDefault().EmbeddedSchemaToComponentLink(client, targetField as ComponentLinkFieldDefinitionData, sourceTcmId, mapping.ChildFieldMapping, targetFolderUri, 0, results);
                        }
                        else
                        {
                            List<XElement> values = new List<XElement>();
                            int index = 0;
                            foreach (XElement element in elements)
                            {
                                index ++;
                                values.Add(element.EmbeddedSchemaToComponentLink(client, targetField as ComponentLinkFieldDefinitionData, sourceTcmId, mapping.ChildFieldMapping, targetFolderUri, index, results));
                            }
                            item.Value = values;
                        }
                    }

                    //transform Component Link into Embedded Schema
                    else if (mapping.SourceField.Field.IsComponentLink() && mapping.TargetField.Field.IsEmbedded())
                    {
                        if (!targetField.IsMultiValue())
                        {
                            item.Value = (elements.FirstOrDefault().GetComponentFieldData(sourceField) as XElement).ComponentLinkToEmbeddedSchema(client, targetField as EmbeddedSchemaFieldDefinitionData, mapping.ChildFieldMapping, targetFolderUri, results);
                        }
                        else
                        {
                            item.Value = elements.Select(x => (x.GetComponentFieldData(sourceField) as XElement).ComponentLinkToEmbeddedSchema(client, targetField as EmbeddedSchemaFieldDefinitionData, mapping.ChildFieldMapping, targetFolderUri, results)).ToList();
                        }
                    }

                    //Embedded Schema or any mapped XElement
                    else
                    {
                        if (!targetField.IsMultiValue())
                        {
                            item.Value = GetSourceMappedValue(mapping, doc.Root, sourceNs);
                        }
                        else
                        {
                            item.Value = GetSourceMappedValues(mapping, doc.Root, sourceNs);
                        }
                    }
                }

                //construct primitive or emebedded from default values
                else
                {
                    if (!targetField.IsMultiValue())
                    {
                        item.Value = GetSourceMappedValue(mapping, doc.Root, sourceNs);
                    }
                    else
                    {
                        item.Value = GetSourceMappedValues(mapping, doc.Root, sourceNs);
                    }
                }

                if (item.Value == null && !string.IsNullOrEmpty(mapping.DefaultValue))
                {
                    item.Value = GetDefaultValue(mapping);
                }

                if (item.Value != null)
                {
                    res.Add(item);
                }
            }

            return res;
        }

        private static string GetFixedContent(SessionAwareCoreServiceClient client, string sourceXml, XNamespace sourceNs, string sourceTcmId, SchemaData targetSchema, string targetRootElementName, List<ItemFieldDefinitionData> targetFields, string targetFolderUri, List<FieldMappingInfo> fieldMapping, List<ResultInfo> results)
        {
            if (targetFields == null || targetFields.Count == 0)
                return string.Empty;

            if (results == null)
                results = new List<ResultInfo>();
            
            ItemType itemType = GetItemType(sourceTcmId);

            if (string.IsNullOrEmpty(targetRootElementName))
                targetRootElementName = targetSchema.RootElementName;

            if (fieldMapping == null)
                fieldMapping = GetDefaultFieldMapping(client, targetFields, targetSchema.Id);

            List<ComponentFieldData> fixedValues = GetFixedValues(client, sourceXml, sourceNs, sourceTcmId, targetSchema, targetRootElementName, targetFields, targetFolderUri, fieldMapping, results);

            if (fixedValues == null || fixedValues.Count == 0)
                return string.Format("<{0} xmlns=\"{1}\" />", targetRootElementName, targetSchema.NamespaceUri);

            bool success = true;

            //check mandatory and empty items
            foreach (ItemFieldDefinitionData schemaField in targetFields)
            {
                if (schemaField.IsMandatory())
                {
                    //stop processing and show message if component contains mandatory empty field
                    ComponentFieldData componentFieldDataValue = fixedValues.FirstOrDefault(x => x.SchemaField.Name == schemaField.Name);
                    if (componentFieldDataValue == null || componentFieldDataValue.Value == null)
                    {
                        success = false;

                        ResultInfo result = new ResultInfo();
                        result.Status = Status.Error;
                        
                        result.Item = item;
                        result.Message = string.Format("Item \"{0}\" contains mandatory empty fields. Please change mapping.", result.WebDav.CutPath("/", 90, true));
                        results.Add(result);
                    }
                }
            }
            if (!success)
                return string.Format("<{0} xmlns=\"{1}\" />", targetRootElementName, targetSchema.NamespaceUri);

            string res = GetComponentXml(targetSchema.NamespaceUri, targetRootElementName, fixedValues).ToString();

            //replace to local publication ids
            List<string> ids = Regex.Matches(res, "tcm:(\\d)+-(\\d)+(-(\\d)+)?").Cast<Match>().Select(x => x.Value).ToList();
            foreach (string id in ids)
            {
                string newId = Regex.Replace(id, "tcm:(\\d)+-", targetSchema.Id.Split('-')[0] + "-");
                if (ExistsItem(client, newId))
                {
                    res = res.Replace(id, newId);
                }
                else
                {
                    ResultInfo result = new ResultInfo();
                    result.ItemType = ItemType.Component;
                    result.TcmId = id;
                    result.Status = Status.Error;
                    RepositoryLocalObjectData item = ReadItem(client, id) as RepositoryLocalObjectData;
                    result.Message = string.Format("Item \"{0}\" doesn't exist in target publication", item == null ? id : item.GetWebDav().CutPath("/", 90, true));
                    results.Add(result);

                    throw new Exception(result.Message);
                }
            }

            //clear unnecessary namespaces
            res = res.Replace(string.Format("xmlns=\"{0}\"", sourceNs), string.Format("xmlns=\"{0}\"", targetSchema.NamespaceUri));
            res = res.Replace(" xmlns=\"\"", string.Empty);
            res = res.Replace(string.Format(" xmlns=\"{0}\"", targetSchema.NamespaceUri), string.Empty);
            res = res.Replace(string.Format("<{0}", targetRootElementName), string.Format("<{0} xmlns=\"{1}\"", targetRootElementName, targetSchema.NamespaceUri));
            
            return res;
        }

        private static ResultInfo SaveComponent(SessionAwareCoreServiceClient client, SchemaData schema, string title, string contentXml, string metadataXml, string folderUri, bool localize)
        {
            ResultInfo result = new ResultInfo();
            result.ItemType = ItemType.Component;

            if (string.IsNullOrEmpty(title))
            {
                result.TcmId = folderUri;
                result.Status = Status.Error;
                result.Message = "Component title is not defined";
            }

            //check existing item
            List<ItemInfo> targetFolderItems = GetItemsByParentContainer(client, folderUri);
            if (targetFolderItems.All(x => x.Title != title))
            {
                //create new component
                try
                {
                    ComponentData component = new ComponentData
                    {
                        Title = title,
                        LocationInfo = new LocationInfo { OrganizationalItem = new LinkToOrganizationalItemData { IdRef = folderUri } },
                        Id = "tcm:0-0-0",
                        Schema = new LinkToSchemaData { IdRef = schema.Id },
                        Content = contentXml,
                        Metadata = metadataXml,
                        IsBasedOnMandatorySchema = false,
                        IsBasedOnTridionWebSchema = true,
                        ApprovalStatus = new LinkToApprovalStatusData { IdRef = "tcm:0-0-0" }
                    };

                    component = (ComponentData)client.Save(component, new ReadOptions());
                    string componentUri = client.CheckIn(component.Id, new ReadOptions()).Id;
                    component = GetComponent(client, componentUri);

                    result.TcmId = component.Id;
                    result.Status = Status.Success;
                    result.Message = string.Format("Component \"{0}\" was created", component.GetWebDav().CutPath("/", 80, true));
                }
                catch (Exception ex)
                {
                    result.TcmId = folderUri;
                    result.Status = Status.Error;
                    result.StackTrace = ex.StackTrace;
                    result.Message = string.Format("Error creating component \"{0}\"", title);
                }
            }
            else
            {
                //update existing component
                string componentUri = targetFolderItems.First(x => x.Title == title).TcmId;

                ComponentData component = GetComponent(client, componentUri);

                //only component of same name and title
                if (component != null && component.Schema.IdRef.GetId() == schema.Id.GetId())
                {
                    if ((component.Content.PrettyXml() == contentXml.PrettyXml() && component.Metadata.PrettyXml() == metadataXml.PrettyXml()))
                        return null;

                    //localize if item is shared
                    if (IsShared(client, componentUri))
                    {
                        if (localize)
                        {
                            Localize(client, componentUri);
                        }
                        else
                        {
                            componentUri = GetBluePrintTopLocalizedTcmId(client, componentUri);
                        }
                    }

                    result.TcmId = componentUri;

                    try
                    {
                        component = (ComponentData)client.CheckOut(componentUri, true, new ReadOptions());
                    }
                    catch
                    {
                    }

                    component.Content = contentXml;
                    component.Metadata = metadataXml;

                    try
                    {
                        client.Update(component, new ReadOptions());
                        client.CheckIn(componentUri, new ReadOptions());

                        result.Status = Status.Success;
                        result.Message = string.Format("Updated component \"{0}\"", component.GetWebDav().CutPath("/", 80, true));
                    }
                    catch (Exception ex)
                    {
                        client.UndoCheckOut(componentUri, true, new ReadOptions());

                        result.Status = Status.Error;
                        result.StackTrace = ex.StackTrace;
                        result.Message = string.Format("Error updating component \"{0}\"", component.GetWebDav().CutPath("/", 80, true));
                    }
                }
                else
                {
                    result.TcmId = folderUri;
                    result.Status = Status.Error;
                    result.Message = string.Format("Error updating component \"{0}\"", title);
                }
            }

            return result;
        }

        private static ResultInfo SaveTridionObjectMetadata(SessionAwareCoreServiceClient client, SchemaData metadataSchema, string title, string metadataXml, string containerUri, bool localize)
        {
            //check existing item
            List<ItemInfo> targetContainerItems = GetItemsByParentContainer(client, containerUri);
            if (targetContainerItems.All(x => x.Title != title))
                return null;

            //update existing tridionObject
            string tridionObjectUri = targetContainerItems.First(x => x.Title == title).TcmId;

            ResultInfo result = new ResultInfo();
            result.ItemType = GetItemType(tridionObjectUri);

            RepositoryLocalObjectData tridionObject = ReadItem(client, tridionObjectUri) as RepositoryLocalObjectData;

            //only tridionObject of same name and title
            if (tridionObject != null && tridionObject.MetadataSchema.IdRef.GetId() == metadataSchema.Id.GetId())
            {
                if ((tridionObject.Metadata.PrettyXml() == metadataXml.PrettyXml()))
                    return null;

                //localize if item is shared
                if (IsShared(client, tridionObjectUri))
                {
                    if (localize)
                    {
                        Localize(client, tridionObjectUri);
                    }
                    else
                    {
                        tridionObjectUri = GetBluePrintTopLocalizedTcmId(client, tridionObjectUri);
                    }
                }

                result.TcmId = tridionObjectUri;

                try
                {
                    tridionObject = client.CheckOut(tridionObjectUri, true, new ReadOptions());
                }
                catch
                {
                }

                tridionObject.Metadata = metadataXml;

                try
                {
                    client.Update(tridionObject, new ReadOptions());
                    client.CheckIn(tridionObjectUri, new ReadOptions());

                    result.Status = Status.Success;
                    result.Message = string.Format("Updated item \"{0}\"", tridionObject.GetWebDav().CutPath("/", 80, true));
                }
                catch (Exception ex)
                {
                    client.UndoCheckOut(tridionObjectUri, true, new ReadOptions());

                    result.Status = Status.Error;
                    result.StackTrace = ex.StackTrace;
                    result.Message = string.Format("Error updating item \"{0}\"", tridionObject.GetWebDav().CutPath("/", 80, true));
                }
            }
            else
            {
                result.TcmId = containerUri;
                result.Status = Status.Error;
                result.Message = string.Format("Error updating page \"{0}\"", title);
            }

            return result;
        }

        public static void ChangeSchemaForComponent(SessionAwareCoreServiceClient client, string componentUri, string sourceSchemaUri, string targetSchemaUri, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceSchemaUri = sourceSchemaUri.GetCurrentVersionTcmId();

            // Open up the source component schema 
            SchemaData sourceSchema = client.Read(sourceSchemaUri, null) as SchemaData;
            if (sourceSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceComponentFields = GetSchemaFields(client, sourceSchemaUri);
            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Change schema for component
            ChangeSchemaForComponent(client, componentUri, sourceSchema, sourceComponentFields, sourceMetadataFields, targetSchema, targetComponentFields, targetMetadataFields, targetFolderUri, localize, historyMapping, results);
        }

        private static void ChangeSchemaForComponent(SessionAwareCoreServiceClient client, string componentUri, SchemaData sourceSchema, List<ItemFieldDefinitionData> sourceComponentFields, List<ItemFieldDefinitionData> sourceMetadataFields, SchemaData targetSchema, List<ItemFieldDefinitionData> targetComponentFields, List<ItemFieldDefinitionData> targetMetadataFields, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(componentUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            ComponentData component = GetComponent(client, componentUri);
            if (component == null)
                return;

            if (!component.Schema.IdRef.GetId().Equals(sourceSchema.Id.GetId()))
            {
                // If the component is not of the schmea that we want to change from, do nothing...
                return;
            }

            if (component.Schema.IdRef.GetId().Equals(targetSchema.Id.GetId()))
            {
                // If the component already has this schema, don't do anything.
                return;
            }

            //detect mapping by date
            List<FieldMappingInfo> fieldMapping = GetDetectedMapping(historyMapping, component.VersionInfo.RevisionDate);

            ResultInfo result = new ResultInfo();
            result.ItemType = ItemType.Component;

            //get fixed xml
            string newContent = GetFixedContent(client, component.Content, sourceSchema.NamespaceUri, componentUri,
                targetSchema, targetSchema.RootElementName, targetComponentFields, targetFolderUri, fieldMapping,
                results);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, component.Metadata, sourceSchema.NamespaceUri, componentUri,
                targetSchema, "Metadata", targetMetadataFields, targetFolderUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newContent) && string.IsNullOrEmpty(newMetadata))
                return;

            //localize if item is shared
            if (IsShared(client, componentUri))
            {
                if (localize)
                {
                    Localize(client, componentUri);
                }
                else
                {
                    componentUri = GetBluePrintTopLocalizedTcmId(client, componentUri);
                }
            }

            result.TcmId = componentUri;

            component = client.TryCheckOut(componentUri, new ReadOptions()) as ComponentData;

            if (component.IsEditable.Value)
            {
                try
                {
                    //rebild component xml
                    component.Content = newContent;

                    //rebuild metadata
                    component.Metadata = newMetadata;

                    //change schema id
                    component.Schema.IdRef = targetSchema.Id;

                    client.Save(component, new ReadOptions());
                    client.CheckIn(componentUri, new ReadOptions());

                    result.Status = Status.Success;
                    result.Message = string.Format("Changed schema for \"{0}\"", component.GetWebDav().CutPath("/", 80, true));
                }
                catch (Exception ex)
                {
                    client.UndoCheckOut(componentUri, true, new ReadOptions());

                    result.Status = Status.Error;
                    result.StackTrace = ex.StackTrace;
                    result.Message = string.Format("Error for \"{0}\"", component.GetWebDav().CutPath("/", 90, true));
                }
            }
            else
            {
                client.UndoCheckOut(componentUri, true, new ReadOptions());

                result.Status = Status.Error;
                result.Message = string.Format("Error for \"{0}\"", component.GetWebDav().CutPath("/", 90, true));
            }

            results.Add(result);
        }

        public static void ChangeSchemasForComponentsInFolder(SessionAwareCoreServiceClient client, string folderUri, string sourceSchemaUri, string targetFolderUri, string targetSchemaUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceSchemaUri = sourceSchemaUri.GetCurrentVersionTcmId();

            // Open up the source component schema 
            SchemaData sourceSchema = client.Read(sourceSchemaUri, null) as SchemaData;
            if (sourceSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceComponentFields = GetSchemaFields(client, sourceSchemaUri);
            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Change container folder linked schema
            ChangeFolderLinkedSchema(client, folderUri, sourceSchemaUri, targetSchemaUri, results);

            // Change inner folder linked schemas
            foreach (ItemInfo item in GetFolders(client, folderUri, true))
            {
                ChangeFolderLinkedSchema(client, item.TcmId, sourceSchemaUri, targetSchemaUri, results);
            }

            // Change schema for components
            foreach (ItemInfo item in GetComponents(client, folderUri, sourceSchemaUri))
            {
                ChangeSchemaForComponent(client, item.TcmId, sourceSchema, sourceComponentFields, sourceMetadataFields, targetSchema, targetComponentFields, targetMetadataFields, targetFolderUri, localize, historyMapping, results);
            }
        }

        public static void FixComponent(SessionAwareCoreServiceClient client, string componentUri, string schemaUri, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            schemaUri = schemaUri.GetCurrentVersionTcmId();

            // Open up the schema
            SchemaData schema = client.Read(schemaUri, null) as SchemaData;
            if (schema == null)
                return;

            List<ItemFieldDefinitionData> componentFields = GetSchemaFields(client, schemaUri);
            List<ItemFieldDefinitionData> metadataFields = GetSchemaMetadataFields(client, schemaUri);

            // Fix component
            FixComponent(client, componentUri, schema, componentFields, metadataFields, targetFolderUri, localize, historyMapping, results);
        }

        private static void FixComponent(SessionAwareCoreServiceClient client, string componentUri, SchemaData schema, List<ItemFieldDefinitionData> componentFields, List<ItemFieldDefinitionData> metadataFields, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(componentUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            ComponentData component = GetComponent(client, componentUri);
            if (component == null || string.IsNullOrEmpty(component.Content))
                return;

            if (!component.Schema.IdRef.GetId().Equals(schema.Id.GetId()))
            {
                // If the component is not of the schema, do nothing...
                return;
            }

            //detect mapping by date
            List<FieldMappingInfo> fieldMapping = GetDetectedMapping(historyMapping, component.VersionInfo.RevisionDate);

            //get fixed xml
            string newContent = GetFixedContent(client, component.Content, schema.NamespaceUri, componentUri, schema, schema.RootElementName, componentFields, targetFolderUri, fieldMapping, results);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, component.Metadata, schema.NamespaceUri, componentUri, schema, "Metadata", metadataFields, targetFolderUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newContent))
                return;

            if (component.Content.PrettyXml() == newContent.PrettyXml())
                return;

            ResultInfo result = SaveComponent(client, schema, component.Title, newContent, newMetadata, component.LocationInfo.OrganizationalItem.IdRef, localize);
            if (result != null)
                results.Add(result);
        }

        public static void FixComponentsInFolder(SessionAwareCoreServiceClient client, string folderUri, string schemaUri, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            schemaUri = schemaUri.GetCurrentVersionTcmId();

            // Open up the schema
            SchemaData schema = client.Read(schemaUri, null) as SchemaData;
            if (schema == null)
                return;

            List<ItemFieldDefinitionData> componentFields = GetSchemaFields(client, schemaUri);
            List<ItemFieldDefinitionData> metadataFields = GetSchemaMetadataFields(client, schemaUri);

            // Fix components
            foreach (ItemInfo item in GetComponents(client, folderUri, schemaUri))
            {
                FixComponent(client, item.TcmId, schema, componentFields, metadataFields, targetFolderUri, localize, historyMapping, results);
            }
        }

        public static void TransformComponent(SessionAwareCoreServiceClient client, string sourceComponentUri, string sourceFolderUri, string sourceSchemaUri, string targetFolderUri, string targetSchemaUri, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceComponentUri))
                return;

            sourceSchemaUri = sourceSchemaUri.GetCurrentVersionTcmId();

            // Open up the source component schema 
            SchemaData sourceSchema = client.Read(sourceSchemaUri, null) as SchemaData;
            if (sourceSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceComponentFields = GetSchemaFields(client, sourceSchemaUri);
            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Change schema for component
            TransformComponent(client, sourceComponentUri, sourceFolderUri, sourceSchema, sourceComponentFields, sourceMetadataFields, targetFolderUri, targetSchema, targetComponentFields, targetMetadataFields, formatString, replacements, localize, historyMapping, results);
        }

        private static void TransformComponent(SessionAwareCoreServiceClient client, string sourceComponentUri, string sourceFolderUri, SchemaData sourceSchema, List<ItemFieldDefinitionData> sourceComponentFields, List<ItemFieldDefinitionData> sourceMetadataFields, string targetFolderUri, SchemaData targetSchema, List<ItemFieldDefinitionData> targetComponentFields, List<ItemFieldDefinitionData> targetMetadataFields, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceComponentUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            ComponentData component = GetComponent(client, sourceComponentUri);
            if (component == null || string.IsNullOrEmpty(component.Content))
                return;

            if (!component.Schema.IdRef.GetId().Equals(sourceSchema.Id.GetId()))
            {
                // If the component is not of the schema, do nothing...
                return;
            }

            //detect mapping by date or get current schema mapping
            List<FieldMappingInfo> fieldMapping = historyMapping.GetDetectedMapping(component.VersionInfo.RevisionDate);

            // create folder chain
            if (!string.IsNullOrEmpty(sourceFolderUri))
            {
                FolderData sourceFolder = client.Read(sourceFolderUri, new ReadOptions()) as FolderData;
                if (sourceFolder != null)
                {
                    List<string> componentWebDavChain = component.GetWebDavChain();
                    List<string> folderChain = componentWebDavChain.Take(componentWebDavChain.Count - 1).ToList().Substract(sourceFolder.GetWebDavChain());
                    targetFolderUri = CreateFolderChain(client, folderChain, targetFolderUri);
                }
            }

            //get fixed xml
            string newContent = GetFixedContent(client, component.Content, sourceSchema.NamespaceUri, sourceComponentUri, targetSchema, targetSchema.RootElementName, targetComponentFields, targetFolderUri, fieldMapping, results);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, component.Metadata, sourceSchema.NamespaceUri, sourceComponentUri, targetSchema, "Metadata", targetMetadataFields, targetFolderUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newContent))
                return;

            List<ComponentFieldData> sourceValues = GetValues(sourceSchema.NamespaceUri, sourceComponentFields, component.Content);
            List<ComponentFieldData> metadataValues = GetValues(sourceSchema.NamespaceUri, sourceMetadataFields, component.Metadata);
            
            string newTitle = GetTransformedName(component.Title, sourceComponentUri, sourceValues, metadataValues, formatString, replacements);

            ResultInfo result = SaveComponent(client, targetSchema, newTitle, newContent, newMetadata, targetFolderUri, localize);
            if (result != null)
                results.Add(result);

            FieldMappingInfo targetComponentLinkMapping = fieldMapping.FirstOrDefault(x => x.TargetFieldFullName == "< target component link >" && x.SourceField != null && x.SourceField.Field != null && !string.IsNullOrEmpty(x.SourceField.Field.Name) && !x.SourceField.Field.Name.StartsWith("<"));
            ComponentLinkFieldDefinitionData targetComponentLink = targetComponentLinkMapping == null ? null : targetComponentLinkMapping.SourceField.Field as ComponentLinkFieldDefinitionData;

            // save component link back to source component
            if (result != null && result.Status == Status.Success && !string.IsNullOrEmpty(result.TcmId) && targetComponentLink != null)
            {
                string pubId = GetPublicationTcmId(component.Id);
                string linkId = GetBluePrintItemTcmId(result.TcmId, pubId);
                XElement cl = GetComponentLink(linkId, newTitle, targetComponentLink.Name);

                ComponentFieldData sourceValue = sourceValues.FirstOrDefault(x => x.SchemaField.Name == targetComponentLink.Name && x.SchemaField.GetFieldType() == targetComponentLink.GetFieldType());
                if (sourceValue == null && !targetComponentLinkMapping.SourceField.IsMeta)
                {
                    sourceValue = new ComponentFieldData();
                    sourceValue.SchemaField = targetComponentLink;
                    sourceValue.Value = cl;
                    sourceValues.Add(sourceValue);
                }

                string newSourceContent = GetComponentXml(sourceSchema.NamespaceUri, sourceSchema.RootElementName, sourceValues).ToString();
                newSourceContent = newSourceContent.Replace(" xmlns=\"\"", string.Empty);

                ComponentFieldData metadataValue = metadataValues.FirstOrDefault(x => x.SchemaField.Name == targetComponentLink.Name && x.SchemaField.GetFieldType() == targetComponentLink.GetFieldType());
                if (metadataValue == null && targetComponentLinkMapping.SourceField.IsMeta)
                {
                    metadataValue = new ComponentFieldData();
                    metadataValue.SchemaField = targetComponentLink;
                    metadataValue.Value = cl;
                    metadataValues.Add(metadataValue);
                }

                string newSourceMetadata = string.Empty;
                XElement newXmlSourceMetadata = GetComponentXml(sourceSchema.NamespaceUri, "Metadata", metadataValues);
                if (newXmlSourceMetadata != null)
                {
                    newSourceMetadata = newXmlSourceMetadata.ToString();
                    newSourceMetadata = newSourceMetadata.Replace(" xmlns=\"\"", string.Empty);
                }
                if (newSourceMetadata == string.Empty && sourceMetadataFields != null && sourceMetadataFields.Count > 0)
                {
                    newSourceMetadata = string.Format("<Metadata xmlns=\"{0}\" />", sourceSchema.NamespaceUri);
                }

                ResultInfo result1 = SaveComponent(client, sourceSchema, component.Title, newSourceContent, newSourceMetadata, component.LocationInfo.OrganizationalItem.IdRef, false);
                if (result1 != null)
                    results.Add(result1);
            }
        }

        public static void TransformComponentsInFolder(SessionAwareCoreServiceClient client, string sourceFolderUri, string sourceSchemaUri, string targetFolderUri, string targetSchemaUri, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceSchemaUri = sourceSchemaUri.GetCurrentVersionTcmId();

            // Open up the source component schema 
            SchemaData sourceSchema = client.Read(sourceSchemaUri, null) as SchemaData;
            if (sourceSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceComponentFields = GetSchemaFields(client, sourceSchemaUri);
            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Transform components
            foreach (ItemInfo item in GetComponents(client, sourceFolderUri, sourceSchemaUri))
            {
                TransformComponent(client, item.TcmId, sourceFolderUri, sourceSchema, sourceComponentFields, sourceMetadataFields, targetFolderUri, targetSchema, targetComponentFields, targetMetadataFields, formatString, replacements, localize, historyMapping, results);
            }
        }

        public static void ChangeMetadataSchemaForTridionObject(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, string sourceMetadataSchemaUri, string targetContainerUri, string targetMetadataSchemaUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the source metadata schema 
            SchemaData sourceMetadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (sourceMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            targetMetadataSchemaUri = targetMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the target metadata schema
            SchemaData targetMetadataSchema = client.Read(targetMetadataSchemaUri, null) as SchemaData;
            if (targetMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetMetadataSchemaUri);

            // Change schema for tridion object
            ChangeMetadataSchemaForTridionObject(client, sourceTridionObjectUri, sourceMetadataSchema, sourceMetadataFields, targetContainerUri, targetMetadataSchema, targetMetadataFields, localize, historyMapping, results);
        }

        private static void ChangeMetadataSchemaForTridionObject(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, SchemaData sourceMetadataSchema, List<ItemFieldDefinitionData> sourceMetadataFields, string targetContainerUri, SchemaData targetMetadataSchema, List<ItemFieldDefinitionData> targetMetadataFields, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceTridionObjectUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            RepositoryLocalObjectData tridionObject = ReadItem(client, sourceTridionObjectUri) as RepositoryLocalObjectData;
            if (tridionObject == null)
                return;

            if (!tridionObject.MetadataSchema.IdRef.GetId().Equals(sourceMetadataSchema.Id.GetId()))
            {
                // If the object is not of the metadata schema that we want to change from, do nothing...
                return;
            }

            if (tridionObject.MetadataSchema.IdRef.GetId().Equals(targetMetadataSchema.Id.GetId()))
            {
                // If the object already has this metadata schema, don't do anything.
                return;
            }

            //detect mapping by date
            List<FieldMappingInfo> fieldMapping = GetDetectedMapping(historyMapping, tridionObject.VersionInfo.RevisionDate);

            ResultInfo result = new ResultInfo();
            result.ItemType = GetItemType(sourceTridionObjectUri);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, tridionObject.Metadata, sourceMetadataSchema.NamespaceUri, sourceTridionObjectUri, targetMetadataSchema, "Metadata", targetMetadataFields, targetContainerUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newMetadata))
                return;

            //localize if item is shared
            if (IsShared(client, sourceTridionObjectUri))
            {
                if (localize)
                {
                    Localize(client, sourceTridionObjectUri);
                }
                else
                {
                    sourceTridionObjectUri = GetBluePrintTopLocalizedTcmId(client, sourceTridionObjectUri);
                }
            }

            result.TcmId = sourceTridionObjectUri;

            tridionObject = client.TryCheckOut(sourceTridionObjectUri, new ReadOptions()) as RepositoryLocalObjectData;

            if (tridionObject != null && tridionObject.IsEditable.Value)
            {
                try
                {
                    //rebuild metadata
                    tridionObject.Metadata = newMetadata;

                    //change schema id
                    tridionObject.MetadataSchema.IdRef = targetMetadataSchema.Id;

                    client.Save(tridionObject, new ReadOptions());
                    client.CheckIn(sourceTridionObjectUri, new ReadOptions());

                    result.Status = Status.Success;
                    result.Message = string.Format("Changed metadata schema for \"{0}\"", tridionObject.GetWebDav().CutPath("/", 80, true));
                }
                catch (Exception ex)
                {
                    client.UndoCheckOut(sourceTridionObjectUri, true, new ReadOptions());

                    result.Status = Status.Error;
                    result.StackTrace = ex.StackTrace;
                    result.Message = string.Format("Error for \"{0}\"", tridionObject.GetWebDav().CutPath("/", 90, true));
                }
            }
            else
            {
                client.UndoCheckOut(sourceTridionObjectUri, true, new ReadOptions());

                result.Status = Status.Error;
                result.Message = string.Format("Error for \"{0}\"", tridionObject.GetWebDav().CutPath("/", 90, true));
            }

            results.Add(result);
        }

        public static void ChangeMetadataSchemasForTridionObjectsInContainer(SessionAwareCoreServiceClient client, string sourceContainerUri, string sourceMetadataSchemaUri, string targetContainerUri, string targetMetadataSchemaUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the source metadata schema 
            SchemaData sourceMetadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (sourceMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            targetMetadataSchemaUri = targetMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the target metadata schema
            SchemaData targetMetadataSchema = client.Read(targetMetadataSchemaUri, null) as SchemaData;
            if (targetMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetMetadataSchemaUri);

            // Change metadata schema for objects
            foreach (ItemInfo item in GetItemsByParentContainer(client, sourceContainerUri, true))
            {
                ChangeMetadataSchemaForTridionObject(client, item.TcmId, sourceMetadataSchema, sourceMetadataFields, targetContainerUri, targetMetadataSchema, targetMetadataFields, localize, historyMapping, results);
            }
        }

        public static void FixTridionObjectMetadata(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, string sourceMetadataSchemaUri, string targetContainerUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the schema
            SchemaData metadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (metadataSchema == null)
                return;

            List<ItemFieldDefinitionData> metadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            // Fix metadata for tridion object
            FixTridionObjectMetadata(client, sourceTridionObjectUri, metadataSchema, metadataFields, targetContainerUri, localize, historyMapping, results);
        }

        private static void FixTridionObjectMetadata(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, SchemaData sourceMetadataSchema, List<ItemFieldDefinitionData> sourceMetadataFields, string targetContainerUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceTridionObjectUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            RepositoryLocalObjectData tridionObject = ReadItem(client, sourceTridionObjectUri) as RepositoryLocalObjectData;
            if (tridionObject == null || string.IsNullOrEmpty(tridionObject.Metadata))
                return;

            if (!tridionObject.MetadataSchema.IdRef.GetId().Equals(sourceMetadataSchema.Id.GetId()))
            {
                // If the tridion object is not of the metadata schema, do nothing...
                return;
            }

            //detect mapping by date
            List<FieldMappingInfo> fieldMapping = GetDetectedMapping(historyMapping, tridionObject.VersionInfo.RevisionDate);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, tridionObject.Metadata, sourceMetadataSchema.NamespaceUri, sourceTridionObjectUri, sourceMetadataSchema, "Metadata", sourceMetadataFields, targetContainerUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newMetadata))
                return;

            if (tridionObject.Metadata.PrettyXml() == newMetadata.PrettyXml())
                return;

            ResultInfo result = SaveTridionObjectMetadata(client, sourceMetadataSchema, tridionObject.Title, newMetadata, tridionObject.LocationInfo.OrganizationalItem.IdRef, localize);
            if (result != null)
                results.Add(result);
        }

        public static void FixMetadataForTridionObjectsInContainer(SessionAwareCoreServiceClient client, string sourceContainerUri, string sourceMetadataSchemaUri, string targetFolderUri, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the schema
            SchemaData metadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (metadataSchema == null)
                return;

            List<ItemFieldDefinitionData> metadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            // Fix metadata for tridion objects
            foreach (ItemInfo item in GetItemsByParentContainer(client, sourceContainerUri, true))
            {
                FixTridionObjectMetadata(client, item.TcmId, metadataSchema, metadataFields, targetFolderUri, localize, historyMapping, results);
            }
        }

        public static void TransformTridionObjectMetadata(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, string sourceContainerUri, string sourceMetadataSchemaUri, string targetFolderUri, string targetSchemaUri, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceTridionObjectUri))
                return;

            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the source metadata schema 
            SchemaData sourceMetadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (sourceMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Change schema for component
            TransformTridionObjectMetadata(client, sourceTridionObjectUri, sourceContainerUri, sourceMetadataSchema, sourceMetadataFields, targetFolderUri, targetSchema, targetComponentFields, targetMetadataFields, formatString, replacements, localize, historyMapping, results);
        }

        private static void TransformTridionObjectMetadata(SessionAwareCoreServiceClient client, string sourceTridionObjectUri, string sourceContainerUri, SchemaData sourceMetadataSchema, List<ItemFieldDefinitionData> sourceMetadataFields, string targetFolderUri, SchemaData targetSchema, List<ItemFieldDefinitionData> targetComponentFields, List<ItemFieldDefinitionData> targetMetadataFields, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            if (string.IsNullOrEmpty(sourceTridionObjectUri))
                return;

            if (results == null)
                results = new List<ResultInfo>();

            RepositoryLocalObjectData tridionObject = ReadItem(client, sourceTridionObjectUri) as RepositoryLocalObjectData;
            if (tridionObject == null || string.IsNullOrEmpty(tridionObject.Metadata))
                return;

            if (!tridionObject.MetadataSchema.IdRef.GetId().Equals(sourceMetadataSchema.Id.GetId()))
            {
                // If the tridion object is not of the metadata schema, do nothing...
                return;
            }

            //detect mapping by date
            List<FieldMappingInfo> fieldMapping = GetDetectedMapping(historyMapping, tridionObject.VersionInfo.RevisionDate);

            // create folder chain
            if (!string.IsNullOrEmpty(sourceContainerUri))
            {
                FolderData sourceFolder = client.Read(sourceContainerUri, new ReadOptions()) as FolderData;
                if (sourceFolder != null)
                {
                    List<string> tridionObjectWebDavChain = tridionObject.GetWebDavChain();
                    List<string> folderChain = tridionObjectWebDavChain.Take(tridionObjectWebDavChain.Count - 1).ToList().Substract(sourceFolder.GetWebDavChain());
                    targetFolderUri = CreateFolderChain(client, folderChain, targetFolderUri);
                }
            }

            //get fixed xml
            string newContent = GetFixedContent(client, tridionObject.Metadata, sourceMetadataSchema.NamespaceUri, sourceTridionObjectUri, targetSchema, targetSchema.RootElementName, targetComponentFields, targetFolderUri, fieldMapping, results);

            //get fixed metadata
            string newMetadata = GetFixedContent(client, tridionObject.Metadata, sourceMetadataSchema.NamespaceUri, sourceTridionObjectUri, targetSchema, "Metadata", targetMetadataFields, targetFolderUri, fieldMapping, results);

            if (string.IsNullOrEmpty(newContent))
                return;

            List<ComponentFieldData> metadataValues = GetValues(sourceMetadataSchema.NamespaceUri, sourceMetadataFields, tridionObject.Metadata);

            string newTitle = GetTransformedName(tridionObject.Title, sourceTridionObjectUri, null, metadataValues, formatString, replacements);

            ResultInfo result = SaveComponent(client, targetSchema, newTitle, newContent, newMetadata, targetFolderUri, localize);
            if (result != null)
                results.Add(result);

            FieldMappingInfo targetComponentLinkMapping = fieldMapping.FirstOrDefault(x => x.TargetFieldFullName == "< target component link >" && x.SourceField != null && x.SourceField.Field != null && !string.IsNullOrEmpty(x.SourceField.Field.Name) && !x.SourceField.Field.Name.StartsWith("<"));
            ComponentLinkFieldDefinitionData targetComponentLink = targetComponentLinkMapping == null ? null : targetComponentLinkMapping.SourceField.Field as ComponentLinkFieldDefinitionData;

            // save component link back to source metadata
            if (result != null && result.Status == Status.Success && !string.IsNullOrEmpty(result.TcmId) && targetComponentLink != null)
            {
                string pubId = GetPublicationTcmId(tridionObject.Id);
                string linkId = GetBluePrintItemTcmId(result.TcmId, pubId);
                XElement cl = GetComponentLink(linkId, newTitle, targetComponentLink.Name);

                ComponentFieldData metadataValue = metadataValues.FirstOrDefault(x => x.SchemaField.Name == targetComponentLink.Name && x.SchemaField.GetFieldType() == targetComponentLink.GetFieldType());
                if (metadataValue == null && targetComponentLinkMapping.SourceField.IsMeta)
                {
                    metadataValue = new ComponentFieldData();
                    metadataValue.SchemaField = targetComponentLink;
                    metadataValue.Value = cl;
                    metadataValues.Add(metadataValue);
                }

                string newSourceMetadata = string.Empty;
                XElement newXmlSourceMetadata = GetComponentXml(sourceMetadataSchema.NamespaceUri, "Metadata", metadataValues);
                if (newXmlSourceMetadata != null)
                {
                    newSourceMetadata = newXmlSourceMetadata.ToString();
                    newSourceMetadata = newSourceMetadata.Replace(" xmlns=\"\"", string.Empty);
                }
                if (newSourceMetadata == string.Empty && sourceMetadataFields != null && sourceMetadataFields.Count > 0)
                {
                    newSourceMetadata = string.Format("<Metadata xmlns=\"{0}\" />", sourceMetadataSchema.NamespaceUri);
                }

                ResultInfo result1 = SaveTridionObjectMetadata(client, sourceMetadataSchema, tridionObject.Title, newSourceMetadata, tridionObject.LocationInfo.OrganizationalItem.IdRef, false);
                if (result1 != null)
                    results.Add(result1);
            }
        }

        public static void TransformMetadataForTridionObjectsInContainer(SessionAwareCoreServiceClient client, string sourceContainerUri, string sourceMetadataSchemaUri, string targetFolderUri, string targetSchemaUri, string formatString, List<ReplacementInfo> replacements, bool localize, HistoryMappingInfo historyMapping, List<ResultInfo> results)
        {
            sourceMetadataSchemaUri = sourceMetadataSchemaUri.GetCurrentVersionTcmId();

            // Open up the source component schema 
            SchemaData sourceMetadataSchema = client.Read(sourceMetadataSchemaUri, null) as SchemaData;
            if (sourceMetadataSchema == null)
                return;

            List<ItemFieldDefinitionData> sourceMetadataFields = GetSchemaMetadataFields(client, sourceMetadataSchemaUri);

            targetSchemaUri = targetSchemaUri.GetCurrentVersionTcmId();

            // Open up the target component schema
            SchemaData targetSchema = client.Read(targetSchemaUri, null) as SchemaData;
            if (targetSchema == null)
                return;

            List<ItemFieldDefinitionData> targetComponentFields = GetSchemaFields(client, targetSchemaUri);
            List<ItemFieldDefinitionData> targetMetadataFields = GetSchemaMetadataFields(client, targetSchemaUri);

            // Transform tridion object metadata into components
            foreach (ItemInfo item in GetItemsByParentContainer(client, sourceContainerUri, true))
            {
                TransformTridionObjectMetadata(client, item.TcmId, sourceContainerUri, sourceMetadataSchema, sourceMetadataFields, targetFolderUri, targetSchema, targetComponentFields, targetMetadataFields, formatString, replacements, localize, historyMapping, results);
            }
        }

        private static void ChangeFolderLinkedSchema(SessionAwareCoreServiceClient client, string folderUri, string sourceSchemaUri, string targetSchemaUri, List<ResultInfo> results)
        {
            if (results == null)
                results = new List<ResultInfo>();

            FolderData innerFolder = client.Read(folderUri, new ReadOptions()) as FolderData;

            if (innerFolder == null)
                return;

            if (innerFolder.LinkedSchema == null)
                return;

            if (!innerFolder.LinkedSchema.IdRef.Equals(sourceSchemaUri))
            {
                // If the component is not of the schmea that we want to change from, do nothing...
                return;
            }

            if (innerFolder.LinkedSchema.IdRef.Equals(targetSchemaUri))
            {
                // If the component already has this schema, don't do anything.
                return;
            }

            ResultInfo result = new ResultInfo();
            result.ItemType = ItemType.Folder;
            result.TcmId = folderUri;

            if (innerFolder.IsEditable.Value)
            {
                try
                {
                    //change schema id
                    innerFolder.LinkedSchema.IdRef = targetSchemaUri;

                    //make non-mandatory to aviod conflicts with inner components
                    innerFolder.IsLinkedSchemaMandatory = false;

                    client.Save(innerFolder, new ReadOptions());

                    result.Status = Status.Success;
                    result.Message = string.Format("Changed schema for folder \"{0}\"", innerFolder.GetWebDav().CutPath("/", 80, true));
                }
                catch (Exception ex)
                {
                    result.Status = Status.Error;
                    result.StackTrace = ex.StackTrace;
                    result.Message = string.Format("Error for folder \"{0}\"", innerFolder.GetWebDav().CutPath("/", 80, true));
                }
            }
            else
            {
                result.Status = Status.Error;
                result.Message = string.Format("Error for folder \"{0}\"", innerFolder.GetWebDav().CutPath("/", 80, true));
            }

            results.Add(result);
        }

        private static void RemoveFolderLinkedSchema(SessionAwareCoreServiceClient client, string folderUri)
        {
            FolderData innerFolder = client.Read(folderUri, new ReadOptions()) as FolderData;

            if (innerFolder == null)
                return;

            if (innerFolder.LinkedSchema == null)
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
            RepositoryLocalObjectData item = client.Read(itemUri, new ReadOptions()) as RepositoryLocalObjectData;

            if (item == null)
                return;

            if (item.MetadataSchema == null)
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

        #region Tridion pages

        public static List<ItemInfo> GetPages(SessionAwareCoreServiceClient client, string tcmComponent)
        {
            return client.GetListXml(tcmComponent, new UsingItemsFilterData { ItemTypes = new[] { ItemType.Page } }).ToList(ItemType.Page);
        }

        #endregion

        #region Tridion delete

        public static void Delete(SessionAwareCoreServiceClient client, string tcmItem, bool delete, string schemaUri, List<ResultInfo> results)
        {
            ItemType itemType = GetItemType(tcmItem);

            if (itemType == ItemType.Folder && !string.IsNullOrEmpty(schemaUri))
            {
                foreach (ItemInfo component in GetComponents(client, tcmItem, schemaUri))
                {
                    DeleteTridionObject(client, component.TcmId, delete, results);
                }
            }
            else if (itemType == ItemType.Publication)
            {
                DeletePublication(client, tcmItem, delete, results);
            }
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
            ItemType itemType = GetItemType(tcmItem);
            ItemType dependentItemType = GetItemType(tcmDependentItem);
            LinkStatus status = LinkStatus.NotFound;
            string stackTraceMessage = "";

            if (delete)
            {
                //remove CP
                if (itemType == ItemType.Page && (dependentItemType == ItemType.Component || dependentItemType == ItemType.ComponentTemplate))
                {
                    status = RemoveComponentPresentation(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove TBB from page template
                if (itemType == ItemType.PageTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = RemoveTbbFromPageTemplate(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove TBB from component template
                if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = RemoveTbbFromComponentTemplate(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                //remove component or keyword link from component
                if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = RemoveLinkFromComponent(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }
                //remove component or keyword link from metadata
                else if (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword)
                {
                    status = RemoveLinkFromMetadata(client, tcmItem, tcmDependentItem, out stackTraceMessage);
                }

                if (status == LinkStatus.Found)
                    status = RemoveHistory(client, tcmItem, tcmDependentItem, out stackTraceMessage);
            }
            else
            {
                //check if possible to remove CP
                if (itemType == ItemType.Page && (dependentItemType == ItemType.Component || dependentItemType == ItemType.ComponentTemplate))
                {
                    status = CheckRemoveComponentPresentation(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove TBB from page template
                if (itemType == ItemType.PageTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = CheckRemoveTbbFromPageTemplate(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove TBB from component template
                if (itemType == ItemType.ComponentTemplate && dependentItemType == ItemType.TemplateBuildingBlock)
                {
                    status = CheckRemoveTbbFromComponentTemplate(client, tcmItem, tcmDependentItem);
                }

                //check if possible to remove component or keyword link from component
                if (itemType == ItemType.Component && (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword))
                {
                    status = CheckRemoveLinkFromComponent(client, tcmItem, tcmDependentItem);
                }
                //check if possible to remove component or keyword link from metadata
                else if (dependentItemType == ItemType.Component || dependentItemType == ItemType.Keyword)
                {
                    status = CheckRemoveLinkFromMetadata(client, tcmItem, tcmDependentItem);
                }
            }

            ResultInfo result = new ResultInfo();
            result.ItemType = itemType;
            result.TcmId = tcmItem;

            RepositoryLocalObjectData item = ReadItem(client, tcmItem) as RepositoryLocalObjectData;
            RepositoryLocalObjectData dependentItem = ReadItem(client, tcmDependentItem) as RepositoryLocalObjectData;

            if (delete)
            {
                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Success;
                    result.Message = string.Format("Item \"{1}\" was removed from \"{0}\".", item == null ? tcmItem : item.GetWebDav().CutPath("/", 90, true), dependentItem == null ? tcmDependentItem : dependentItem.GetWebDav().CutPath("/", 90, true));
                }
                if (status == LinkStatus.Mandatory)
                {
                    result.Status = Status.Error;
                    result.Message = string.Format("Not able to unlink \"{1}\" from \"{0}\".", item == null ? tcmItem : item.GetWebDav().CutPath("/", 90, true), dependentItem == null ? tcmDependentItem : dependentItem.GetWebDav().CutPath("/", 90, true));
                }
            }
            else
            {
                if (status == LinkStatus.Found)
                {
                    result.Status = Status.Info;
                    result.Message = string.Format("Remove item \"{1}\" from \"{0}\".", item == null ? tcmItem : item.GetWebDav().CutPath("/", 90, true), dependentItem == null ? tcmDependentItem : dependentItem.GetWebDav().CutPath("/", 90, true));
                }
                if (status == LinkStatus.Mandatory)
                {
                    result.Status = Status.Warning;
                    result.Message = string.Format("Not able to unlink \"{1}\" from \"{0}\".", item == null ? tcmItem : item.GetWebDav().CutPath("/", 90, true), dependentItem == null ? tcmDependentItem : dependentItem.GetWebDav().CutPath("/", 90, true));
                }
            }

            if (status == LinkStatus.Error)
            {
                result.Status = Status.Error;
                result.StackTrace = stackTraceMessage;
                result.Message = string.Format("Not able to unlink \"{1}\" from \"{0}\".", item == null ? tcmItem : item.GetWebDav().CutPath("/", 90, true), dependentItem == null ? tcmDependentItem : dependentItem.GetWebDav().CutPath("/", 90, true));
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
                    {
                        return values;
                    }

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
                    {
                        return LinkStatus.Mandatory;
                    }
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

        private static string RemoveLinkFromXml(SessionAwareCoreServiceClient client, string schemaUri, string xmlContent, string linkUri)
        {
            if (string.IsNullOrEmpty(xmlContent))
                return xmlContent;

            SchemaData schema = client.Read(schemaUri, null) as SchemaData;
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

            SchemaData schema = client.Read(component.Schema.IdRef, null) as SchemaData;
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

            SchemaData schema = client.Read(component.Schema.IdRef, null) as SchemaData;
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

            SchemaData metadataSchema = client.Read(item.MetadataSchema.IdRef, null) as SchemaData;
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
            if (item == null)
                return LinkStatus.NotFound;

            //todo: test it
            if(item.MetadataSchema == null || item.MetadataSchema.IdRef == "tcm:0-0-0")
                return LinkStatus.NotFound;

            SchemaData metadataSchema = client.Read(item.MetadataSchema.IdRef, null) as SchemaData;
            if (metadataSchema == null)
                return LinkStatus.NotFound;

            List<ItemFieldDefinitionData> metadataSchemaFields = GetSchemaFields(client, item.MetadataSchema.IdRef);

            return CheckRemoveLinkFromValues(client, XElement.Parse(item.Metadata), metadataSchema.NamespaceUri, metadataSchemaFields, tcmLink);
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

            RepositoryLocalObjectData item = (RepositoryLocalObjectData)ReadItem(client, tcmItem);

            if (level > 3)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Recoursion level is bigger than 3. Try to select different item than \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                    TcmId = tcmItem,
                    ItemType = itemType,
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
                    FolderData folder = item as FolderData;
                    if (folder != null && folder.LinkedSchema != null && folder.LinkedSchema.IdRef != "tcm:0-0-0")
                    {
                        if (delete)
                            RemoveFolderLinkedSchema(client, tcmItem);

                        if (delete)
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Removed folder linked schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                                TcmId = tcmItem,
                                ItemType = itemType,
                                Status = Status.Success
                            });
                        }

                        else
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Remove folder linked schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                                TcmId = tcmItem,
                                ItemType = itemType,
                                Status = Status.Info
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing folder linked schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                        TcmId = tcmItem,
                        ItemType = itemType,
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
                    if (item != null && item.MetadataSchema != null && item.MetadataSchema.IdRef != "tcm:0-0-0")
                    {
                        if (delete)
                            RemoveMetadataSchema(client, tcmItem);

                        if (delete)
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Removed metadata schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                                TcmId = tcmItem,
                                ItemType = itemType,
                                Status = Status.Success
                            });
                        }

                        else
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Remove metadata schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                                TcmId = tcmItem,
                                ItemType = itemType,
                                Status = Status.Info
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error removing metadata schema for \"{0}\"", item.GetWebDav().CutPath("/", 80, true)),
                        TcmId = tcmItem,
                        ItemType = itemType,
                        Status = Status.Error,
                        StackTrace = ex.StackTrace
                    });
                }
            }

            try
            {
                if (delete)
                {
                    if (!currentVersion)
                    {
                        //remove used versions
                        string stackTraceMessage;
                        LinkStatus status = RemoveHistory(client, tcmItem, parentTcmId, out stackTraceMessage);
                        if (status == LinkStatus.Error)
                        {
                            results.Add(new ResultInfo
                            {
                                Message = string.Format("Error removing history from item \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                                TcmId = tcmItem,
                                ItemType = itemType,
                                Status = Status.Error,
                                StackTrace = stackTraceMessage
                            });
                        }
                    }
                    else
                    {
                        //unlocalize before delete
                        if (isAnyLocalized)
                        {
                            UnLocalizeAll(client, tcmItem);
                        }

                        //undo checkout
                        try
                        {
                            client.UndoCheckOut(tcmItem, true, new ReadOptions());
                        }
                        catch (Exception)
                        {
                        }

                        //delete used item
                        client.Delete(tcmItem);
                    }
                }

                if (delete)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Deleted item \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                        TcmId = tcmItem,
                        ItemType = itemType,
                        Status = Status.Success
                    });
                }
                else
                {
                    if (isAnyLocalized)
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unlocalize item \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                            TcmId = tcmItem,
                            ItemType = itemType,
                            Status = Status.Info
                        });
                    }

                    if (IsPublished(client, tcmItem))
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Unpublish manually item \"{0}\" published at {1}", item.GetWebDav().CutPath("/", 80, true), GetPublishInfo(client, tcmItem)),
                            TcmId = GetFirstPublishItemTcmId(client, tcmItem),
                            ItemType = itemType,
                            Status = Status.Warning
                        });
                    }

                    if (!currentVersion)
                    {
                        results.Add(new ResultInfo
                        {
                            Message =
                                string.Format("Remove old versions of item \"{0}\"",
                                    item.GetWebDav().CutPath("/", 80, true)),
                            TcmId = tcmItem,
                            ItemType = itemType,
                            Status = Status.Info
                        });
                    }
                    else
                    {
                        results.Add(new ResultInfo
                        {
                            Message = string.Format("Delete item \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                            TcmId = tcmItem,
                            ItemType = itemType,
                            Status = Status.Info
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Error deleting item \"{0}\"", item.GetWebDav().CutPath("/", 90, true)),
                    TcmId = tcmItem,
                    ItemType = itemType,
                    Status = Status.Error,
                    StackTrace = ex.StackTrace
                });
            }
        }

        public static void DeleteFolderOrStructureGroup(SessionAwareCoreServiceClient client, string tcmFolder, bool delete, List<ResultInfo> results, int level = 0, bool onlyCurrentBluePrint = false)
        {
            bool isCurrentBluePrint = tcmFolder == GetBluePrintTopTcmId(client, tcmFolder);
            
            tcmFolder = GetBluePrintBottomTcmId(client, tcmFolder);

            List<ResultInfo> folderResults = new List<ResultInfo>();

            List<ItemInfo> childItems = GetItemsByParentContainer(client, tcmFolder);
            if (onlyCurrentBluePrint)
            {
                childItems = childItems.Where(x => string.IsNullOrEmpty(x.FromPub)).ToList();
            }

            //delete inner items
            foreach (ItemInfo childItem in childItems)
            {
                if (childItem.ItemType == ItemType.Folder || childItem.ItemType == ItemType.StructureGroup)
                {
                    DeleteFolderOrStructureGroup(client, childItem.TcmId, delete, folderResults, level, onlyCurrentBluePrint);
                }
                else
                {
                    if (ExistsItem(client, childItem.TcmId))
                        DeleteTridionObject(client, childItem.TcmId, delete, folderResults, tcmFolder, true, level);
                }
            }

            results.AddRange(folderResults.Distinct(new ResultInfoComparer()));

            //delete folder or SG as an object
            if (!onlyCurrentBluePrint || isCurrentBluePrint)
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

            //unlocalize items
            List<ItemInfo> childItems = GetContainersByPublication(client, tcmPublication);
            foreach (ItemInfo childItem in childItems)
            {
                UnlocalizeFolderOrStructureGroup(client, childItem, delete, publicationResults);
            }

            results.AddRange(publicationResults.Distinct(new ResultInfoComparer()));

            try
            {
                //delete publication as an object
                if (delete)
                {
                    client.Delete(tcmPublication);
                }

                if (delete)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Deleted publication \"{0}\"", publication.Title),
                        TcmId = tcmPublication,
                        ItemType = ItemType.Publication,
                        Status = Status.Success
                    });
                }
                else
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Delete publication \"{0}\"", publication.Title),
                        TcmId = tcmPublication,
                        ItemType = ItemType.Publication,
                        Status = Status.Info
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Error deleting publication \"{0}\"", publication.Title),
                    TcmId = tcmPublication,
                    ItemType = ItemType.Publication,
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

        private static string GetPublishInfo(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return string.Join(", ", client.GetListPublishInfo(tcmItem).Select(p => string.Format("\"{0}\"", client.Read(p.Repository.IdRef, new ReadOptions()).Title)).ToArray());
        }

        private static string GetFirstPublishItemTcmId(SessionAwareCoreServiceClient client, string tcmItem)
        {
            return GetBluePrintItemTcmId(tcmItem, client.GetListPublishInfo(tcmItem).First().Repository.IdRef);
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

        public static string GetBluePrintBottomTcmId(SessionAwareCoreServiceClient client, string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            return list2.Last().Item.Id;
        }

        //todo: improve perfomance of using this
        private static string GetBluePrintTopLocalizedTcmId(SessionAwareCoreServiceClient client, string id)
        {
            if (id.StartsWith("tcm:0-"))
                return id;

            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return id;

            var item = list.Cast<BluePrintNodeData>().FirstOrDefault(x => x.Item != null && x.Item.Id == id);
            if (item == null)
                return id;

            string publicationId = item.Item.BluePrintInfo.OwningRepository.IdRef;

            return GetBluePrintItemTcmId(id, publicationId);
        }

        //todo: check remove this
        public static bool IsLocalized(SessionAwareCoreServiceClient client, string id)
        {
            return id == GetBluePrintTopLocalizedTcmId(client, id) && id != GetBluePrintTopTcmId(client, id);
        }

        public static bool IsAnyLocalized(SessionAwareCoreServiceClient client, string id)
        {
            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return false;

            return list.Cast<BluePrintNodeData>().Any(x => x.Item != null && IsLocalized(client, x.Item.Id));
        }

        //todo: check remove this
        public static bool IsShared(SessionAwareCoreServiceClient client, string id)
        {
            return id != GetBluePrintTopTcmId(client, id) && id != GetBluePrintTopLocalizedTcmId(client, id);
        }

        public static void Localize(SessionAwareCoreServiceClient client, string id)
        {
            if (IsShared(client, id))
                client.Localize(id, new ReadOptions());
        }

        public static void UnLocalize(SessionAwareCoreServiceClient client, string id)
        {
            if(IsLocalized(client, id))
                client.UnLocalize(id, new ReadOptions());
        }

        public static void UnLocalizeAll(SessionAwareCoreServiceClient client, string id)
        {
            if (!IsAnyLocalized(client, id))
                return;

            var list = client.GetSystemWideList(new BluePrintFilterData { ForItem = new LinkToRepositoryLocalObjectData { IdRef = id } });
            if (list == null || list.Length == 0)
                return;

            var list2 = list.Cast<BluePrintNodeData>().Where(x => x.Item != null).ToList();

            foreach (BluePrintNodeData item in list2)
            {
                if(IsLocalized(client, item.Item.Id))
                    UnLocalize(client, item.Item.Id);
            }
        }

        //better perfomance
        //todo: refactor code above to use better perfomance version

        private static void Localize(SessionAwareCoreServiceClient client, ItemInfo item)
        {
            if (item.IsShared)
                client.Localize(item.TcmId, new ReadOptions());
        }

        private static void UnLocalize(SessionAwareCoreServiceClient client, ItemInfo item)
        {
            if (item.IsLocalized)
                client.UnLocalize(item.TcmId, new ReadOptions());
        }

        private static void UnlocalizeTridionObject(SessionAwareCoreServiceClient client, ItemInfo item, bool delete, List<ResultInfo> results)
        {
            RepositoryLocalObjectData itemData = (RepositoryLocalObjectData)ReadItem(client, item.TcmId);

            if (delete)
            {
                try
                {
                    UnLocalize(client, item);

                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Unlocalized item \"{0}\"", itemData.GetWebDav().CutPath("/", 90, true)),
                        TcmId = item.TcmId,
                        ItemType = item.ItemType,
                        Status = Status.Success
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ResultInfo
                    {
                        Message = string.Format("Error unlocalizing item \"{0}\"", itemData.GetWebDav().CutPath("/", 80, true)),
                        TcmId = item.TcmId,
                        ItemType = item.ItemType,
                        Status = Status.Error,
                        StackTrace = ex.StackTrace
                    });
                }
            }
            else
            {
                results.Add(new ResultInfo
                {
                    Message = string.Format("Unlocalize item \"{0}\"", itemData.GetWebDav().CutPath("/", 90, true)),
                    TcmId = item.TcmId,
                    ItemType = item.ItemType,
                    Status = Status.Info
                });
            }
        }

        private static void UnlocalizeFolderOrStructureGroup(SessionAwareCoreServiceClient client, ItemInfo folder, bool delete, List<ResultInfo> results)
        {
            List<ItemInfo> childItems = GetItemsByParentContainer(client, folder.TcmId);

            //unlocalize inner items
            foreach (ItemInfo childItem in childItems)
            {
                if (childItem.ItemType == ItemType.Folder || childItem.ItemType == ItemType.StructureGroup)
                {
                    UnlocalizeFolderOrStructureGroup(client, childItem, delete, results);
                }
                else
                {
                    if (childItem.IsLocalized)
                    {
                        UnlocalizeTridionObject(client, childItem, delete, results);
                    }
                }
            }

            //unlocalize folder or SG as an object
            if (folder.IsLocalized)
            {
                UnlocalizeTridionObject(client, folder, delete, results);
            }
        }

        #endregion

        #region Collection helpers

        public static List<ItemInfo> Intersect(List<ItemInfo>[] arrayOfSets, bool includeEmptySets)
        {
            List<ItemInfo>[] arr = includeEmptySets ? arrayOfSets : arrayOfSets.Where(x => x != null && x.Count > 0).ToArray();

            if (arr.Length == 0 || arr.Length == 1 && arr[0].Count == 0) return new List<ItemInfo>();
            if (arr.Length == 1 && arr[0].Count > 0) return arr[0];

            List<ItemInfo> res = arr[0];
            for (int i = 1; i < arr.Length; i++)
            {
                res = res.Intersect(arr[i], new ItemInfoComparer()).ToList();
            }

            return res;
        }

        private class ItemInfoComparer : IEqualityComparer<ItemInfo>
        {
            public bool Equals(ItemInfo x, ItemInfo y)
            {
                return x.TcmId == y.TcmId;
            }

            public int GetHashCode(ItemInfo obj)
            {
                return obj.TcmId.GetHashCode();
            }
        }

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
                    //todo
                    item.Icon = element.Attributes().Any(x => x.Name == "Icon") ? element.Attribute("Icon").Value : "";
                    item.Path = element.Attributes().Any(x => x.Name == "Path") ? element.Attribute("Path").Value : "";

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
                    //todo
                    item.Icon = element.Attributes().Any(x => x.Name == "Icon") ? element.Attribute("Icon").Value : "";
                    item.Path = element.Attributes().Any(x => x.Name == "Path") ? element.Attribute("Path").Value : "";

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

        public static ItemType GetItemType(string tcmItem)
        {
            if (string.IsNullOrEmpty(tcmItem))
                return ItemType.None;
            
            string[] arr = tcmItem.Replace("tcm:", string.Empty).Split('-');
            if (arr.Length == 2) return ItemType.Component;

            return (ItemType)int.Parse(arr[2]);
        }

        public static void AddPathItem(List<ItemInfo> list, ItemInfo item)
        {
            if (item == null)
                return;

            list.Add(item);

            if (item.Parent != null)
                AddPathItem(list, item.Parent);
        }

        public static List<ItemInfo> SetParent(this List<ItemInfo> list, ItemInfo parent)
        {
            foreach (ItemInfo item in list)
            {
                item.Parent = parent;
            }
            return list;
        }

        public static string GetMimeTypeId(SessionAwareCoreServiceClient client, string filePath)
        {
            List<MultimediaTypeData> allMimeTypes = client.GetSystemWideList(new MultimediaTypesFilterData()).Cast<MultimediaTypeData>().ToList();
            foreach (MultimediaTypeData mt in allMimeTypes)
            {
                foreach (string ext in mt.FileExtensions)
                {
                    if (Path.GetExtension(filePath).ToLower().Replace(".", string.Empty) == ext.ToLower().Replace(".", string.Empty))
                        return mt.Id;
                }
            }
            return string.Empty;
        }

        public static List<ItemInfo> FindCheckedOutItems(SessionAwareCoreServiceClient client)
        {
            return client.GetSystemWideListXml(new RepositoryLocalObjectsFilterData()).ToList();
        }

        public static bool IsCheckedOut(SessionAwareCoreServiceClient client, string id)
        {
            return FindCheckedOutItems(client).Any(x => x.TcmId == id);
        }

        public static FieldType GetFieldType(this ItemFieldDefinitionData field)
        {
            if (field is SingleLineTextFieldDefinitionData)
            {
                return FieldType.SingleLineText;
            }
            if (field is MultiLineTextFieldDefinitionData)
            {
                return FieldType.MultiLineText;
            }
            if (field is XhtmlFieldDefinitionData)
            {
                return FieldType.Xhtml;
            }
            if (field is DateFieldDefinitionData)
            {
                return FieldType.Date;
            }
            if (field is NumberFieldDefinitionData)
            {
                return FieldType.Number;
            }
            if (field is KeywordFieldDefinitionData)
            {
                return FieldType.Keyword;
            }
            if (field is MultimediaLinkFieldDefinitionData)
            {
                return FieldType.Multimedia;
            }
            if (field is ExternalLinkFieldDefinitionData)
            {
                return FieldType.ExternalLink;
            }
            if (field is ComponentLinkFieldDefinitionData)
            {
                return FieldType.ComponentLink;
            }
            if (field is EmbeddedSchemaFieldDefinitionData)
            {
                return FieldType.EmbeddedSchema;
            }
            
            return FieldType.None;
        }

        public static string GetFieldTypeName(this ItemFieldDefinitionData field)
        {
            return field.GetFieldType() == FieldType.None ? string.Empty : field.GetFieldType().ToString();
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

        public static bool IsMultimediaComponentLink(this ItemFieldDefinitionData field)
        {
            ComponentLinkFieldDefinitionData clField = field as ComponentLinkFieldDefinitionData;
            if (clField == null)
                return false;
            return clField.AllowMultimediaLinks;
        }

        public static bool IsMultiValue(this ItemFieldDefinitionData field)
        {
            return field.MaxOccurs == -1 || field.MaxOccurs > 1;
        }
        
        public static bool IsMandatory(this ItemFieldDefinitionData field)
        {
            return field.MinOccurs == 1;
        }

        public static bool IsCastAllowed(this ItemFieldDefinitionData from, ItemFieldDefinitionData to)
        {
            if (from.GetFieldType() == to.GetFieldType())
                return true;

            if (from.GetFieldType() == FieldType.EmbeddedSchema && to.GetFieldType() == FieldType.ComponentLink)
                return true;

            if (from.GetFieldType() == FieldType.ComponentLink && to.GetFieldType() == FieldType.EmbeddedSchema)
                return true;

            if (from.GetFieldType() == FieldType.Number)
            {
                if (to.GetFieldType() == FieldType.SingleLineText)
                    return true;
                if (to.GetFieldType() == FieldType.MultiLineText)
                    return true;
                if (to.GetFieldType() == FieldType.Xhtml)
                    return true;
            }

            if (from.GetFieldType() == FieldType.Date)
            {
                if (to.GetFieldType() == FieldType.SingleLineText)
                    return true;
                if (to.GetFieldType() == FieldType.MultiLineText)
                    return true;
                if (to.GetFieldType() == FieldType.Xhtml)
                    return true;
            }

            if (from.GetFieldType() == FieldType.SingleLineText)
            {
                if (to.GetFieldType() == FieldType.MultiLineText)
                    return true;
                if (to.GetFieldType() == FieldType.Xhtml)
                    return true;
            }

            if (from.GetFieldType() == FieldType.MultiLineText)
            {
                if (to.GetFieldType() == FieldType.SingleLineText)
                    return true;
                if (to.GetFieldType() == FieldType.Xhtml)
                    return true;
            }

            if (from.GetFieldType() == FieldType.Keyword)
            {
                if (to.GetFieldType() == FieldType.SingleLineText)
                    return true;
                if (to.GetFieldType() == FieldType.MultiLineText)
                    return true;
                if (to.GetFieldType() == FieldType.Xhtml)
                    return true;
            }

            return false;
        }

        public static bool IsPrimitive(this ItemFieldDefinitionData field)
        {
            FieldType fieldType = field.GetFieldType();

            return fieldType == FieldType.SingleLineText ||
                fieldType == FieldType.MultiLineText ||
                fieldType == FieldType.Xhtml || 
                fieldType == FieldType.Date ||
                fieldType == FieldType.Number ||
                fieldType == FieldType.Keyword ||
                fieldType == FieldType.ExternalLink;
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
            if (string.IsNullOrEmpty(id))
                return id;

            return "tcm:" + publicationId.Split('-')[1] + "-" + id.Split('-')[1] + (id.Split('-').Length > 2 ? "-" + id.Split('-')[2] : "");
        }

        public static string CutPath(this string path, string separator, int maxLength)
        {
            if (path == null || path.Length <= maxLength)
                return path;

            var list = path.Split(new[] { separator[0] });
            int itemMaxLength = maxLength / list.Length;

            return String.Join(separator, list.Select(item => item.Cut(itemMaxLength)).ToList());
        }

        public static string CutPath(this string path, string separator, int maxLength, bool fullLastItem)
        {
            if (path == null || path.Length <= maxLength)
                return path;

            if (!fullLastItem)
                return path.CutPath(separator, maxLength);

            string lastItem = path.Substring(path.LastIndexOf(separator, StringComparison.Ordinal));

            if (lastItem.Length > maxLength)
                return path.CutPath(separator, maxLength);

            return path.Substring(0, path.LastIndexOf(separator, StringComparison.Ordinal)).CutPath(separator, maxLength - lastItem.Length) + lastItem;
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

        public static string PlainXml(this string xml)
        {
            try
            {
                return Regex.Replace(xml, "\\s+", " ").Replace("> <", "><");
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

        public static string GetFieldFullName(this FieldInfo field, bool includePath)
        {
            if (field == null || field.Field == null || string.IsNullOrEmpty(field.Field.Name))
                return "< ignore >";

            if (field.Field.Name == "< this component link >")
                return "< this component link >";

            if (field.Field.Name == "< target component link >")
                return "< target component link >";

            if (field.Field.Name == "< new >")
                return "< new >";

            string span = "";
            for (int i = 0; i < field.Level; i++)
            {
                span += "  ";
            }

            string path = includePath && field.GetFieldNamePath() != field.Field.Name ? string.Format(" | ({0})", field.GetFieldNamePath()) : "";

            if (field.Field.IsEmbedded())
                return string.Format("{0}{1} | {2}{3}{4}", span, field.Field.Name, ((EmbeddedSchemaFieldDefinitionData)field.Field).EmbeddedSchema.Title, (field.IsMeta ? " | [meta]" : ""), path);

            if (field.Field.IsComponentLink())
            {
                ComponentLinkFieldDefinitionData componentLinkField = ((ComponentLinkFieldDefinitionData)field.Field);
                if (componentLinkField.AllowedTargetSchemas.Any())
                    return string.Format("{0}{1} | {2}{3}{4}", span, componentLinkField.Name, componentLinkField.AllowedTargetSchemas[0].Title, (field.IsMeta ? " | [meta]" : ""), path);
            }

            return string.Format("{0}{1} | {2}{3}{4}", span, field.Field.Name, field.Field.GetFieldTypeName(), (field.IsMeta ? " | [meta]" : ""), path);
        }

        public static string GetFieldFullName(this FieldInfo field)
        {
            return field.GetFieldFullName(true);
        }

        public static string GetFieldNamePath(this FieldInfo field, bool breakComponentLinkPath = false)
        {
            if (field == null)
                return string.Empty;

            if (breakComponentLinkPath && field.Field.IsComponentLink())
                return field.IsMeta ? "Metadata" : field.RootElementName;

            if (field.Parent == null)
                return field.Field.Name;

            return string.Format("{0}/{1}", field.Parent.GetFieldNamePath(breakComponentLinkPath), field.Field.Name);
        }

        public static string GetDomainName(this string url)
        {
            if (!url.Contains(Uri.SchemeDelimiter))
            {
                url = string.Concat(Uri.UriSchemeHttp, Uri.SchemeDelimiter, url);
            }
            Uri uri = new Uri(url);
            return uri.Host;
        }

        public static string GetCurrentVersionTcmId(this string tcmId)
        {
            if (tcmId.Contains("-v"))
                return tcmId.Substring(0, tcmId.IndexOf("-v", StringComparison.Ordinal));
            return tcmId;
        }

        private  static string GetTransformedName(string title, string tcmId, List<ComponentFieldData> componentValues, List<ComponentFieldData> metadataValues, string formatString, List<ReplacementInfo> replacements)
        {
            if (replacements == null || replacements.Count == 0)
                return title;

            List<object> replacementResults = new List<object>();
            foreach (ReplacementInfo replacement in replacements)
            {
                if (replacement.Fragment == "[Title]")
                {
                    replacementResults.Add(string.IsNullOrEmpty(replacement.Regex) ? title : Regex.Match(title, replacement.Regex).Value);
                }
                else if (replacement.Fragment == "[TcmId]")
                {
                    replacementResults.Add(string.IsNullOrEmpty(replacement.Regex) ? tcmId : Regex.Match(tcmId, replacement.Regex).Value);
                }
                else if (replacement.Fragment == "[ID]")
                {
                    replacementResults.Add(string.IsNullOrEmpty(replacement.Regex) ? tcmId.Split('-')[1] : Regex.Match(tcmId.Split('-')[1], replacement.Regex).Value);
                }
                else if (replacement.Field != null && componentValues != null)
                {
                    ComponentFieldData field = componentValues.FirstOrDefault(x => x.SchemaField.Name == replacement.Field.Field.Name);
                    if (field != null)
                    {
                        replacementResults.Add(string.IsNullOrEmpty(replacement.Regex) ? field.Value : Regex.Match(field.Value.ToString(), replacement.Regex).Value);
                    }
                }
                else if (replacement.Field != null && metadataValues != null)
                {
                    ComponentFieldData field = metadataValues.FirstOrDefault(x => x.SchemaField.Name == replacement.Field.Field.Name);
                    if (field != null)
                    {
                        replacementResults.Add(string.IsNullOrEmpty(replacement.Regex) ? field.Value : Regex.Match(field.Value.ToString(), replacement.Regex).Value);
                    }
                }
            }

            return string.Format(formatString, replacementResults.ToArray());
        }

        public static string GetId(this string tcmId)
        {
            if (string.IsNullOrEmpty(tcmId))
                return string.Empty;
            return tcmId.Split('-')[1];
        }

        public static string GetItemCmsUrl(string host, string tcmId, string title = "")
        {
            if (title == "Categories and Keywords" && tcmId.StartsWith("catman-"))
                return string.Format("http://{0}/SDL/#app=wcm&entry=cme&url=%23locationId%3Dcatman-tcm%3A{1}", host, tcmId.Replace("catman-tcm:", ""));

            if (title == "Process Definitions" && tcmId.StartsWith("proc-"))
                return string.Format("http://{0}", host);

            ItemType itemType = GetItemType(tcmId);
            if (itemType == ItemType.Folder || itemType == ItemType.StructureGroup)
            {
                return string.Format("http://{0}/SDL/#app=wcm&entry=cme&url=%23locationId%3Dtcm%3A{1}", host, tcmId.Replace("tcm:", ""));
            }

            return string.Format("http://{0}/WebUI/item.aspx?tcm={1}#id={2}", host, (int)itemType, tcmId);
        }

        public static bool IsHtml(this string html)
        {
            Regex tagRegex = new Regex(@"<[^>]+>");
            return tagRegex.IsMatch(html);
        }

        #endregion

    }
}
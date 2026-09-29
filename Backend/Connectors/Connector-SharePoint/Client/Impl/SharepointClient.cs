using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using RestSharp;
using SmintIo.Portals.Connector.SharePoint.Client.Authenticators;
using SmintIo.Portals.Connector.SharePoint.Extensions;
using SmintIo.Portals.Connector.SharePoint.Models;
using SmintIo.Portals.ConnectorSDK.Clients.Prefab;
using SmintIo.Portals.ConnectorSDK.Models;
using SmintIo.Portals.SDK.Core.Cache;
using SmintIo.Portals.SDK.Core.Http.Prefab.Extensions;
using SmintIo.Portals.SDK.Core.Http.Prefab.Models;
using SmintIo.Portals.SDK.Core.Models.Context;
using SmintIo.Portals.SDK.Core.Rest;
using SmintIo.Portals.SDK.Core.Rest.Prefab;
using SmintIo.Portals.SDK.Core.Rest.Prefab.Exceptions;
using Newtonsoft.Json;

namespace SmintIo.Portals.Connector.SharePoint.Client.Impl
{
    public class SharepointClient : BaseHttpClientApiClient, ISharepointClient
    {
        private const string RootFolderId = "0";

        private static readonly string[] _driveItemFields = new[]
        {
            "id",
            "name",
            "createdBy",
            "createdDateTime",
            "lastModifiedBy",
            "lastModifiedDateTime",
            "parentReference",
            "audio",
            "video",
            "file",
            "fileSystemInfo",
            "folder",
            "image",
            "location",
            "package",
            "photo",
            "publication",
            "remoteItem",
            "root",
            "size",
            "speacialFolder",
            "webUrl"
        };

        private static readonly ICollection<string> _possibleDefaultShareDocumentLists = new HashSet<string>
        {
            "Shared Documents",
            "Freigegebene Dokumente",
            "Documentos Compartidos",
            "Documentos Compartilhados"
        };

        private readonly ILogger _logger;
        private readonly ICache _cache;
        private readonly IPortalsContextModel _portalsContextModel;
        private readonly Func<AuthorizationValuesModel> _getAuthorizationValuesFunc;
        private readonly string _sharepointUrl;
        private readonly string _siteDriveId;
        private readonly string _siteListId;
        private readonly IEnumerable<string> _siteFolderIds;

        private GraphServiceClient _graphServiceClient;
        private IRestSharpClient _restSharpClient;

        public IRequestFailedHandler DefaultRequestFailedHandler { get; private set; }

        /// <summary>
        /// Constructs a new SharepointClient with a function to renew the OAuth2 Access Token.
        /// The <paramref name="getAuthorizationValuesFunc"/> function is called on every Graph API request to renew the token.
        /// </summary>
        /// <param name="logger">Used to perform general logging <see cref="ILogger"/></param>
        /// <param name="cache"><see cref="ICache"/></param>
        /// <param name="portalsContextModel">Used to perform general logging <see cref="IPortalsContextModel"/></param>
        /// <param name="getAuthorizationValuesFunc">A <see cref="Func{T}"/>that performs the refresh</param>
        /// <param name="sharepointUrl">the Url for the sharepoint site</param>
        /// <param name="siteId">the Site ID for the sharepoint site</param>
        public SharepointClient(
            ILogger logger,
            ICache cache,
            IPortalsContextModel portalsContextModel,
            IHttpClientFactory httpClientFactory,
            Func<AuthorizationValuesModel> getAuthorizationValuesFunc,
            string sharepointUrl,
            string siteId,
            string siteDriveId,
            string siteListId,
            IEnumerable<string> siteFolderIds)
            : base(SharepointConnectorStartup.SharepointConnector, portalsContextModel, httpClientFactory, logger)
        {
            _logger = logger;
            _cache = cache;
            _portalsContextModel = portalsContextModel;
            _getAuthorizationValuesFunc = getAuthorizationValuesFunc;
            _sharepointUrl = sharepointUrl;

            SiteId = siteId;
            _siteDriveId = siteDriveId;
            _siteListId = siteListId;
            _siteFolderIds = siteFolderIds;

            IsRootFolderSync = siteFolderIds.Contains(RootFolderId);

            DefaultRequestFailedHandler = new SharepointDefaultRequestFailedHandler(logger);

            CreateGraphApiClient();
            CreateRestApiClient();
        }

        public string SiteId { get; }

        public bool IsRootFolderSync { get; }

        public bool IsSetup => !string.IsNullOrEmpty(SiteId) && _siteFolderIds.Any();

        /// <summary>
        /// Creates GraphApi client with delayed bearer authentication via <see cref="_getAuthorizationValuesFunc"/>.
        /// </summary>
        private void CreateGraphApiClient()
        {
            var authenticationProvider = new DelegateAuthenticationProvider(request =>
            {
                var authorizationValues = _getAuthorizationValuesFunc();

                request.Headers.Authorization = new AuthenticationHeaderValue("bearer", authorizationValues.AccessToken);

                return Task.CompletedTask;
            });

            _graphServiceClient = new GraphServiceClient(authenticationProvider);
        }

        /// <summary>
        /// Creates Rest Api client with delayed bearer authentication via <see cref="_getAuthorizationValuesFunc"/>.
        /// </summary>
        private void CreateRestApiClient()
        {
            if (string.IsNullOrEmpty(_sharepointUrl))
            {
                throw new ExternalDependencyException(
                    ExternalDependencyStatusEnum.NotSetup,
                    "The SharePoint URL is not yet set up");
            }

            var authenticator = new SharepointBearerAuthenticator(_getAuthorizationValuesFunc);

            var restClientOptions = new RestClientOptions(new Uri($"{_sharepointUrl}"))
            {
                Authenticator = authenticator
            };

            var httpClient = HttpClientFactory.CreateClient();
            _restSharpClient = new RestSharpClient(httpClient, restClientOptions);
        }

        public async Task<ICollection<SiteResponse>> GetSitesAsync(string query = null)
        {
            var response = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                    {
                        var request = new RestRequest("_api/search/query?querytext='contentclass:sts_site'&rowlimit=100&rowsperpage=100", Method.Get);

                        SetResponseHeaderType(request);

                        return _restSharpClient.ExecuteTaskAsync<string>(request);
                    },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: "Get sites",
                isGet: false
            ).ConfigureAwait(false);

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                return null;
            }

            var host = new Uri(_sharepointUrl).Host;

            var siteResponses = JObject
                .Parse(response.Content)
                .SelectTokens("$.d.query.PrimaryQueryResult.RelevantResults.Table.Rows.results..Cells.results")
                .Select(t => new SiteResponse
                {
                    Host = host,
                    SiteId = GetTokenValueByProperty<string>(t, "Key", "SiteId"),
                    WebId = GetTokenValueByProperty<string>(t, "Key", "WebId"),
                    WebUrl = GetTokenValueByProperty<string>(t, "Key", "Path"),
                    DisplayName = GetTokenValueByProperty<string>(t, "Key", "Title")
                })
                .Where(sr =>
                    sr.WebUrl.ToLowerInvariant().StartsWith(_sharepointUrl.ToLowerInvariant())
                    && sr.WebUrl.ToLowerInvariant().Contains("/sites/")
                    && (string.IsNullOrEmpty(query) || sr.WebUrl.Contains(query, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();

            return siteResponses;
        }

        public async Task<Site> GetSiteAsync(string siteId)
        {
            var siteResult = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                    {
                        return _graphServiceClient
                            .Sites[siteId]
                            .Request()
                            .Expand(s => s.Lists)
                            .GetAsync();
                    },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get site ({siteId})",
                isGet: true
            ).ConfigureAwait(false);

            return siteResult;
        }

        public async Task<ICollection<ColumnDefinitionResponse>> GetSiteMetadataAsync(string siteId)
        {
            if (string.IsNullOrEmpty(SiteId))
            {
                return null;
            }

            if (string.IsNullOrEmpty(siteId) || !_siteFolderIds.Any())
            {
                return new List<ColumnDefinitionResponse>();
            }

            var site = await GetSiteAsync(siteId).ConfigureAwait(false);

            if (site == null)
            {
                _logger.LogWarning($"The requested site with ID {siteId} was not found");

                return null;
            }

            Microsoft.Graph.List documentList;

            if (!string.IsNullOrEmpty(_siteListId))
            {
                documentList = site.Lists.FirstOrDefault(l => l.Id == _siteListId);
            }
            else
            {
                documentList = site.Lists.FirstOrDefault(l => _possibleDefaultShareDocumentLists.Contains(l.Name));
            }

            if (documentList == null)
            {
                _logger.LogWarning($"The requested site with ID {siteId} does match any of the default shared document lists");

                return null;
            }

            var response = await ExecuteWithBackoffAsync(
               apiFunc: (_) =>
               {
                   var resource = $"/sites/{site.Name}/_api/lists(guid'{documentList.Id}')/fields";
                   var request = new RestRequest(resource, Method.Get);

                   SetResponseHeaderType(request);

                   return _restSharpClient.ExecuteTaskAsync<string>(request);
               },
               requestFailedHandler: DefaultRequestFailedHandler,
               hint: $"Get site metadata ({siteId})",
               isGet: true
           ).ConfigureAwait(false);

            ColumnDefinitionResponse[] columnDefinitionResponses;

            if (response != null && response.Content != null)
            {
                columnDefinitionResponses = JObject
                    .Parse(response.Content)
                    .SelectTokens("$.d.results..[*]") // $.d.results..[?(@.Hidden == 'false')] -- not working.
                    .Select(jToken => new ColumnDefinitionResponse
                    {
                        Id = GetTokenValue<string>(jToken, "Id"),
                        Name = GetTokenValue<string>(jToken, "EntityPropertyName"),
                        DisplayName = GetTokenValue<string>(jToken, "Title"),
                        IsHidden = GetTokenValue<bool>(jToken, "Hidden"),
                        FieldTypeKind = GetTokenValue<int>(jToken, "FieldTypeKind"),
                        TypeAsString = GetTokenValue<string>(jToken, "TypeAsString")
                    })
                    .Where(cd => !cd.IsHidden && !cd.Name.StartsWith("OData", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
            else
            {
                columnDefinitionResponses = Array.Empty<ColumnDefinitionResponse>();
            }

            var columnDefinitionNamesCacheKey = GetColumnDefinitionNamesCacheKey(siteId, _siteListId);
            var columnDefinitionNames = GetColumnDefinitionNames(columnDefinitionResponses);

            await _cache.StoreAsync(columnDefinitionNamesCacheKey, columnDefinitionNames, TimeSpan.FromDays(7)).ConfigureAwait(false);

            await SetCurrencyLocalesAsync(siteId, documentList.Id, columnDefinitionResponses).ConfigureAwait(false);

            return columnDefinitionResponses;
        }

        private async Task SetCurrencyLocalesAsync(string siteId, string documentListId, ColumnDefinitionResponse[] columnDefinitionResponses)
        {
            var currencyColumnDefinitionTasks = columnDefinitionResponses
                .Where(cdr => cdr.FieldType == SharepointFieldType.Currency)
                .Select(cdr => GetColumnDefinitionAsync(siteId, documentListId, cdr.Id))
                .ToArray();

            var currencyColumnDefinitions = await Task.WhenAll(currencyColumnDefinitionTasks).ConfigureAwait(false);

            foreach (var currencyColumnDefinition in currencyColumnDefinitions)
            {
                if (currencyColumnDefinition.Currency == null)
                {
                    continue;
                }

                var columnDefinitionResponse = columnDefinitionResponses.Single(cdr => cdr.Id == currencyColumnDefinition.Id);

                columnDefinitionResponse.CurrencyLocale = currencyColumnDefinition.Currency.Locale;
            }
        }

        private async Task<ColumnDefinition> GetColumnDefinitionAsync(string siteId, string documentListId, string columnId)
        {
            var columnDefinition = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                {
                    return _graphServiceClient
                       .Sites[siteId]
                       .Lists[documentListId]
                       .Columns[columnId]
                       .Request()
                       .GetAsync();
                },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get column definition ({siteId}, {documentListId}, {columnId})",
                isGet: true
            ).ConfigureAwait(false);

            return columnDefinition;
        }

        public async Task<DriveItemListModel> GetFolderDriveItemsAsync(string folderId, string skipToken, int? pageSize)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            // Sync selected asset
            if (!string.IsNullOrEmpty(folderId))
            {
                var folderDriveItemList = await GetChildrenDriveItemsAsync(folderId, skipToken, pageSize).ConfigureAwait(false);

                return folderDriveItemList;
            }

            // Sync root folder
            if (_siteFolderIds.Contains(RootFolderId))
            {
                var rootDriveItemList = await GetRootDriveItemListAsync(skipToken, pageSize).ConfigureAwait(false);

                return rootDriveItemList;
            }

            // Sync UI selected assets
            var assetIds = _siteFolderIds.ToList();

            var folderDriveItems = await GetDriveItemsBatchAsync(assetIds, allowRootFolders: true).ConfigureAwait(false);

            for (var i = folderDriveItems.Count - 1; i >= 0; i--)
            {
                var driveItem = folderDriveItems.ElementAt(i);

                var parentDriveItemAssetId = driveItem.GetParentAssetId();

                // We remove child sub folders, so we don't sync the content multiple times
                if (_siteFolderIds.Contains(parentDriveItemAssetId))
                {
                    folderDriveItems.Remove(driveItem);
                }
            }

            return new DriveItemListModel
            {
                DriveItems = folderDriveItems
            };
        }

        private async Task<bool> CanAccessDriveItemAsync(DriveItem driveItem, bool allowRootFolders)
        {
            var assetId = driveItem.GetAssetId();

            // Top Level - we can access everything
            if (_siteFolderIds.Contains(RootFolderId))
            {
                return true;
            }

            if (allowRootFolders && _siteFolderIds.Contains(assetId))
            {
                return true;
            }

            var parentDriveItemAssetId = driveItem.GetParentAssetId();

            while (true)
            {
                if (_siteFolderIds.Contains(parentDriveItemAssetId))
                {
                    return true;
                }

                // Going all they way to the root, in order to check whether the asset can be accessed

                var parentDriveItemModel = await GetDriveItemRelationshipAsync(parentDriveItemAssetId).ConfigureAwait(false);

                if (parentDriveItemModel == null)
                {
                    return false;
                }

                parentDriveItemAssetId = parentDriveItemModel.ParentAssetId;

                if (string.IsNullOrEmpty(parentDriveItemAssetId))
                {
                    // Root folder reached

                    return false;
                }
            }
        }

        private async Task<DriveItemRelationshipModel> GetDriveItemRelationshipAsync(string assetId)
        {
            var parentDriveItemModel = await _cache.GetAsync<DriveItemRelationshipModel>(assetId).ConfigureAwait(false);

            if (parentDriveItemModel == null)
            {
                var parentDriveItem = await GetDriveItemInternallyAsync(assetId).ConfigureAwait(false);

                if (parentDriveItem == null)
                {
                    return null;
                }

                var driveItemRelationshipModel = new DriveItemRelationshipModel
                {
                    ParentAssetId = parentDriveItem.GetParentAssetId()
                };

                await _cache.StoreAsync(assetId, driveItemRelationshipModel, expiresIn: TimeSpan.FromMinutes(5)).ConfigureAwait(false);

                parentDriveItemModel = driveItemRelationshipModel;
            }

            return parentDriveItemModel;
        }

        private async Task EnsureDriveItemsAccessAsync(ICollection<DriveItem> driveItems, bool allowRootFolders)
        {
            for (var i = driveItems.Count - 1; i >= 0; i--)
            {
                var driveItem = driveItems.ElementAt(i);

                var canAccess = await CanAccessDriveItemAsync(driveItem, allowRootFolders).ConfigureAwait(false);

                if (!canAccess)
                {
                    driveItems.Remove(driveItem);
                }
            }
        }

        private async Task<DriveItemListModel> GetChildrenDriveItemsAsync(string assetId, string skipToken, int? pageSize)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders: true).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            if (!driveItem.IsFolder())
            {
                _logger.LogWarning($"DriveItem wit ID {assetId} is not a folder");

                return null;
            }

            var driveItemListModel = await GetChildrenDriveItemsInternallyAsync(driveItem, skipToken, pageSize).ConfigureAwait(false);

            return driveItemListModel;
        }

        private async Task<DriveItemListModel> GetChildrenDriveItemsInternallyAsync(DriveItem driveItem, string skipToken, int? pageSize)
        {
            var childrenDriveItems = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                {
                    var driveItemChildrenCollectionRequest = _graphServiceClient
                        .Sites[SiteId]
                        .Drives[driveItem.ParentReference.DriveId]
                        .Items[driveItem.Id]
                        .Children
                        .Request()
                        .Expand(i => i.Thumbnails)
                        .Select(string.Join(",", _driveItemFields));

                    driveItemChildrenCollectionRequest.QueryOptions.SetSkipToken(skipToken);
                    driveItemChildrenCollectionRequest.QueryOptions.SetPageSize(pageSize);

                    return driveItemChildrenCollectionRequest.GetAsync();
                },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get children drive items internally ({driveItem.Id})",
                isGet: true
            ).ConfigureAwait(false);

            var nextSkipToken = childrenDriveItems.NextPageRequest?.QueryOptions.GetNextSkipToken();

            return new DriveItemListModel
            {
                DriveItems = childrenDriveItems,
                ContinuationUuid = nextSkipToken
            };
        }

        public async Task<IDictionary<string, string>> GetParentFolderIdsByAssetIdAsync(ICollection<DriveItem> driveItems)
        {
            if (string.IsNullOrEmpty(SiteId) || 
                driveItems == null ||
                !driveItems.Any())
            {
                return null;
            }

            var parentFolderIdsById = new Dictionary<string, string>();

            foreach (var driveItem in driveItems.OrderByDescending(driveItem => driveItem.IsFolder()))
            {
                if (driveItem.CanBeDeleted())
                {
                    continue;
                }

                var parentAssetId = driveItem.GetParentAssetId();

                if (driveItem.IsFolder())
                {
                    parentFolderIdsById.TryAdd(driveItem.GetAssetId(), parentAssetId);
                }

                await GetParentsRecursivelyAsync(parentFolderIdsById, parentAssetId).ConfigureAwait(false);
            }

            return parentFolderIdsById;
        }

        private async Task GetParentsRecursivelyAsync(IDictionary<string, string> parentFolderIdsByAssetId, string assetId)
        {
            if (string.IsNullOrEmpty(assetId) || 
                parentFolderIdsByAssetId.ContainsKey(assetId))
            {
                return;
            }

            var driveItem = await GetDriveItemInternallyAsync(assetId).ConfigureAwait(false);

            if (driveItem == null)
            {
                return;
            }

            var parentAssetId = driveItem.GetParentAssetId();

            parentFolderIdsByAssetId.Add(assetId, parentAssetId);

            await GetParentsRecursivelyAsync(parentFolderIdsByAssetId, parentAssetId).ConfigureAwait(false);
        }

        internal async Task<ICollection<DriveItem>> GetFoldersListAsync(string[] searchTermParts)
        {
            if (string.IsNullOrEmpty(SiteId))
            {
                return null;
            }

            var folderDriveItems = new List<DriveItem>();

            if (searchTermParts == null)
            {
                searchTermParts = [];
            }

            string searchTermPart = null;

            if (searchTermParts.Length > 0)
            {
                searchTermPart = searchTermParts[0];

                if (searchTermParts.Length > 1)
                {
                    searchTermParts = searchTermParts[1..];
                }
                else
                {
                    searchTermParts = [];
                }
            }

            var rootDriveItemList = await GetRootDriveItemListAsync(skipToken: null, pageSize: null).ConfigureAwait(false);

            if (rootDriveItemList == null || !rootDriveItemList.DriveItems.Any())
            {
                return folderDriveItems;
            }

            var rootFoldersDriveItems = rootDriveItemList.DriveItems
                .Where(di => di.IsFolder())
                .ToList();

            foreach (var rootFolderDriveItem in rootFoldersDriveItems)
            {
                if (!string.IsNullOrEmpty(searchTermPart) &&
                    !rootFolderDriveItem.Name.StartsWith(searchTermPart, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                folderDriveItems.Add(rootFolderDriveItem);

                if (!searchTermParts.Any())
                {
                    continue;
                }

                await GetChildrenRecursivelyAsync(folderDriveItems, rootFolderDriveItem, rootFolderDriveItem.Name, searchTermParts, foldersOnly: true).ConfigureAwait(false);
            }

            return folderDriveItems;
        }

        public async Task<ICollection<Drive>> GetSiteDrivesAsync()
        {
            if (string.IsNullOrEmpty(SiteId))
            {
                return null;
            }

            var drives = await ExecuteWithBackoffAsync(
               apiFunc: (_) =>
               {
                    return _graphServiceClient
                        .Sites[SiteId]
                        .Drives
                        .Request()
                        .GetAsync();
               },
               requestFailedHandler: DefaultRequestFailedHandler,
               hint: "Get site drives",
               isGet: false
           ).ConfigureAwait(false);

            return drives;
        }

        public async Task<Drive> GetSiteDriveAsync(string driveId)
        {
            if (string.IsNullOrEmpty(SiteId))
            {
                return null;
            }

            var drive = await ExecuteWithBackoffAsync(
               apiFunc: (_) =>
               {
                    return _graphServiceClient
                        .Sites[SiteId]
                        .Drives[driveId]
                        .Request()
                        .GetAsync();
               },
               requestFailedHandler: DefaultRequestFailedHandler,
               hint: $"Get site drive ({driveId})",
               isGet: true
           ).ConfigureAwait(false);

            return drive;
        }

        private async Task<DriveItemListModel> GetRootDriveItemListAsync(string skipToken, int? pageSize)
        {
            Drive drive;

            if (string.IsNullOrEmpty(_siteDriveId))
            {
                var drives = await GetSiteDrivesAsync().ConfigureAwait(false);

                drive = drives.FirstOrDefault();
            }
            else
            {
                drive = await GetSiteDriveAsync(_siteDriveId).ConfigureAwait(false);
            }

            if (drive == null)
            {
                _logger.LogWarning($"Drive with ID {_siteDriveId} was not found");

                return null;
            }

            var driveItems = await ExecuteWithBackoffAsync(
               apiFunc: (_) =>
               {
                    var driveItemChildrenCollectionRequest = _graphServiceClient
                        .Drives[drive.Id]
                        .Root
                        .Children
                        .Request()
                        .Expand(i => i.Thumbnails)
                        .Select(string.Join(",", _driveItemFields));

                    driveItemChildrenCollectionRequest.QueryOptions.SetSkipToken(skipToken);
                    driveItemChildrenCollectionRequest.QueryOptions.SetPageSize(pageSize);

                    return driveItemChildrenCollectionRequest.GetAsync();
               },
               requestFailedHandler: DefaultRequestFailedHandler,
               hint: "Get root drive item list",
               isGet: false
           ).ConfigureAwait(false);

            var nextSkipToken = driveItems?.NextPageRequest?.QueryOptions.GetNextSkipToken();

            return new DriveItemListModel
            {
                DriveItems = driveItems,
                ContinuationUuid = nextSkipToken
            };
        }

        private async Task GetChildrenRecursivelyAsync(ICollection<DriveItem> folderDriveItems, DriveItem driveItem, string parentFolderName, string[] searchTermParts, bool foldersOnly)
        {
            string searchTermPart = null;

            if (searchTermParts.Length > 0)
            {
                searchTermPart = searchTermParts[0];

                if (searchTermParts.Length > 1)
                {
                    searchTermParts = searchTermParts[1..];
                }
                else
                {
                    searchTermParts = [];
                }
            }

            var driveItemList = await GetChildrenDriveItemsInternallyAsync(driveItem, skipToken: null, pageSize: null).ConfigureAwait(false);

            var childDriveItems = foldersOnly
                ? driveItemList.DriveItems
                    .Where(c => c.IsFolder())
                    .ToList()
                : driveItemList.DriveItems;

            foreach (var childDriveItem in childDriveItems)
            {
                if (!string.IsNullOrEmpty(searchTermPart) &&
                    !childDriveItem.Name.StartsWith(searchTermPart, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(parentFolderName))
                {
                    childDriveItem.Name = $"{parentFolderName} > {childDriveItem.Name}";
                }

                folderDriveItems.Add(childDriveItem);

                if (!searchTermParts.Any())
                {
                    continue;
                }

                await GetChildrenRecursivelyAsync(folderDriveItems, childDriveItem, childDriveItem.Name, searchTermParts, foldersOnly).ConfigureAwait(false);
            }
        }

        internal async Task<DriveItem> GetFolderDriveItemAsync(string assetId)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            var driveItem = await GetDriveItemInternallyAsync(assetId).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            if (!driveItem.IsFolder())
            {
                _logger.LogWarning($"DriveItem wit ID {assetId} is not a folder");

                return null;
            }

            return driveItem;
        }

        public Task<DriveItem> GetDriveItemAsync(string assetId)
        {
            return GetDriveItemAsync(assetId, allowRootFolders: false);
        }

        private async Task<DriveItem> GetDriveItemAsync(string assetId, bool allowRootFolders)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            if (string.IsNullOrEmpty(assetId))
            {
                throw new ArgumentNullException(nameof(assetId));
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders).ConfigureAwait(false);

            return driveItem;
        }

        private async Task<DriveItem> EnsureDriveItemAccessAsync(string assetId, bool allowRootFolders)
        {
            var driveItem = await GetDriveItemInternallyAsync(assetId).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            var canAccess = await CanAccessDriveItemAsync(driveItem, allowRootFolders).ConfigureAwait(false);

            if (!canAccess)
            {
                return null;
            }

            return driveItem;
        }

        private async Task<DriveItem> GetDriveItemInternallyAsync(string assetId)
        {
            (string driveId, string itemId) = assetId.Parse();

            var driveItem = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                    {
                        return GetDriveItemRequest(assetId).GetAsync();
                    },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item internally ({assetId})",
                isGet: true
           ).ConfigureAwait(false);

            return driveItem;
        }

        private IDriveItemRequest GetDriveItemRequest(string assetId)
        {
            (string driveId, string itemId) = assetId.Parse();

            var driveItemRequest = _graphServiceClient
                .Drives[driveId]
                .Items[itemId]
                .Request()
                .Expand(i => i.Thumbnails)
                .Select(string.Join(",", _driveItemFields));

            return driveItemRequest;
        }

        public async Task<IDictionary<string, object>> GetDriveItemAdditionalDataByFieldNamesAsync(DriveItem driveItem)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            if (driveItem == null)
            {
                throw new ArgumentNullException(nameof(driveItem));
            }

            var columnDefinitionNames = await GetCachedColumnDefinitionNamesAsync().ConfigureAwait(false);

            var selectValues = string.Join(", ", columnDefinitionNames);

            var listItemWithFields = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                    {
                        return _graphServiceClient
                            .Drives[driveItem.ParentReference.DriveId]
                            .Items[driveItem.Id]
                            .ListItem
                            .Fields
                            .Request()
                            .Select(selectValues)
                            .GetAsync();
                    },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item additional data by field names ({driveItem.Id})",
                isGet: true
           ).ConfigureAwait(false);

            return listItemWithFields?.AdditionalData?
                .ToDictionary(
                    kvp1 => kvp1.Key,
                    kvp1 => kvp1.Value);
        }

        private async Task<ICollection<string>> GetCachedColumnDefinitionNamesAsync()
        {
            var columnDefinitionNamesCacheKey = GetColumnDefinitionNamesCacheKey(SiteId, _siteListId);

            var columnDefinitionNames = await _cache.GetAsync<List<string>>(columnDefinitionNamesCacheKey).ConfigureAwait(false);

            if (columnDefinitionNames == null)
            {
                var columnDefinitionResponses = await GetSiteMetadataAsync(SiteId).ConfigureAwait(false);

                if (columnDefinitionResponses != null)
                {
                    columnDefinitionNames = GetColumnDefinitionNames(columnDefinitionResponses);
                }
                else
                {
                    columnDefinitionNames = new List<string>();
                }

                await _cache.StoreAsync(columnDefinitionNamesCacheKey, columnDefinitionNames, TimeSpan.FromDays(7)).ConfigureAwait(false);
            }

            return columnDefinitionNames;
        }

        private static List<string> GetColumnDefinitionNames(ICollection<ColumnDefinitionResponse> columnDefinitionResponses)
        {
            return columnDefinitionResponses
                .Select(cdr => cdr.Name)
                .ToList();
        }

        private static string GetColumnDefinitionNamesCacheKey(string siteId, string listId)
        {
            return $"columnDefinitions_{siteId}_{listId}";
        }

        public async Task<ItemPreviewInfo> GetDriveItemPreviewInfoAsync(string assetId)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders: false).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            var itemPreviewInfo = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                    {
                        return _graphServiceClient
                            .Drives[driveItem.ParentReference.DriveId]
                            .Items[driveItem.Id]
                            .Preview()
                            .Request()
                            .PostAsync();
                    },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item preview info ({assetId})",
                isGet: true
           ).ConfigureAwait(false);

            return itemPreviewInfo;
        }

        public async Task<StreamResponse> GetDriveItemContentAsync(string assetId, long? maxFileSizeBytes)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders: false).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            if (maxFileSizeBytes != null)
            {
                if (driveItem.Size == null)
                {
                    _logger.LogWarning($"Found drive item with NULL size ({assetId})");
                }
				else if (driveItem.Size > maxFileSizeBytes)
                {
                    throw new ExternalDependencyException(ExternalDependencyStatusEnum.FileTooLarge, "The file is too large", SharepointConnectorStartup.SharepointConnector);
                }
            }

            var response = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                {
                    return _graphServiceClient
                        .Drives[driveItem.ParentReference.DriveId]
                        .Items[driveItem.Id]
                        .Content
                        .Request()
                        .GetResponseAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);
                },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item content ({assetId})",
                isGet: true
            ).ConfigureAwait(false);

            if (response == null || response.Content == null)
            {
                return null;
            }

            var streamResponse = new StreamResponse
            {
                FileName = driveItem.Name,
                FileSizeInBytes = response.Content.Headers?.ContentLength,
                MediaType = driveItem.File?.MimeType,
                Stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
            };

            return streamResponse;
        }

        public async Task<StreamResponse> GetDriveItemThumbnailContentAsync(string assetId, string thumbnailSetId, string size, long? maxFileSize)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            if (string.IsNullOrEmpty(thumbnailSetId) || string.IsNullOrEmpty(size))
            {
                return null;
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders: false).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            // TODO: max file size bytes not yet supported

            var response = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                {
                    return _graphServiceClient
                       .Drives[driveItem.ParentReference.DriveId]
                       .Items[driveItem.Id]
                       .Thumbnails[thumbnailSetId][size]
                       .Content
                       .Request()
                       .GetResponseAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);
                },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item thumbnail content ({assetId}, {thumbnailSetId}, {size})",
                isGet: true
            ).ConfigureAwait(false);

            if (response == null || response.Content == null)
            {
                return null;
            }

            var streamResponse = new StreamResponse
            {
                FileName = response.Content.Headers?.ContentDisposition?.FileName,
                FileSizeInBytes = response.Content.Headers?.ContentLength,
                MediaType = response.Content.Headers?.ContentType?.MediaType,
                Stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
            };

            return streamResponse;
        }

        public async Task<StreamResponse> GetDriveItemPdfContentAsync(string assetId, long? maxFileSizeBytes)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            var driveItem = await EnsureDriveItemAccessAsync(assetId, allowRootFolders: false).ConfigureAwait(false);

            if (driveItem == null)
            {
                return null;
            }

            // see https://docs.microsoft.com/en-us/graph/api/driveitem-get-content-format?view=graph-rest-1.0&tabs=csharp

            var queryOptions = new List<QueryOption>()
            {
                new QueryOption("format", "pdf")
            };

            // TODO: max file size bytes not yet supported

            var response = await ExecuteWithBackoffAsync(
                apiFunc: (_) =>
                {
                    return _graphServiceClient
                        .Drives[driveItem.ParentReference.DriveId]
                        .Items[driveItem.Id]
                        .Content
                        .Request(queryOptions)
                        .GetResponseAsync(completionOption: HttpCompletionOption.ResponseHeadersRead);
                },
                requestFailedHandler: DefaultRequestFailedHandler,
                hint: $"Get drive item PDF content ({assetId})",
                isGet: true
            ).ConfigureAwait(false);

            if (response == null || response.Content == null)
            {
                return null;
            }

            var streamResponse = new StreamResponse
            {
                FileName = response.Content.Headers?.ContentDisposition?.FileName,
                FileSizeInBytes = response.Content.Headers?.ContentLength,
                MediaType = response.Content.Headers?.ContentType?.MediaType,
                Stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)
            };

            return streamResponse;
        }

        public async Task<DriveItemChangesListModel> GetDriveItemChangesListAsync(string deltaLink)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            IDriveItemDeltaRequest driveItemDeltaRequest = null;

            try
            {
                driveItemDeltaRequest = GetDriveItemDeltaRequest(deltaLink);
            }
            catch (UriFormatException ex)
            {
                if (!string.Equals(ex.Message, "Invalid URI: The format of the URI could not be determined", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                _logger.LogWarning($"Malformed delta link issue reported from SharePoint ({deltaLink}), resetting continuation UUID: {ex}");

                throw new ExternalDependencyException(ExternalDependencyStatusEnum.ContinuationUuidTooOld, "The continuation UUID is too old");
            }

            DriveItemChangesListModel driveItemChangesList;

            try
            {
                driveItemChangesList = await ExecuteWithBackoffAsync(
                    async (_) =>
                        {
                            var driveItemChangesListModel = new DriveItemChangesListModel();

                            IDriveItemDeltaCollectionPage driveItemDeltaCollectionPage;

                            try
                            {
                                driveItemDeltaCollectionPage = await driveItemDeltaRequest.GetAsync().ConfigureAwait(false);
                            }
                            catch (ServiceException e)
                            {
                                if (e.StatusCode == HttpStatusCode.NotFound)
                                {
                                    if (e.RawResponseBody != null && e.RawResponseBody.Contains("Requested site could not be found"))
                                    {
                                        _logger.LogWarning($"Drive item delta request issue reported that site was not found ({_sharepointUrl}): {e}");

                                        throw;
                                    }
                                }

                                if (e.StatusCode == HttpStatusCode.Gone || e.StatusCode == HttpStatusCode.NotFound)
                                {
                                    // If a deltaLink is no longer valid, the service will respond with 410 Gone, or 404 Not Found

                                    _logger.LogWarning($"Drive item delta request issue reported from SharePoint for delta link {deltaLink}, resetting continuation UUID ({_sharepointUrl}): {e}");

                                    driveItemChangesListModel.ContinuationTooOld = true;

                                    return driveItemChangesListModel;
                                }

                                throw;
                            }

                            // We don't track the root folder.

                            var driveItems = driveItemDeltaCollectionPage
                                .Where(driveItem => !string.Equals(driveItem.Name, "root"))
                                .ToList();

                            driveItemChangesListModel.DriveItems = driveItems;

                            // as MS does: https://github.com/microsoftgraph/msgraph-sdk-dotnet-core/blob/6ec55dcc43b884cec3d61aeb3ceb69fe27852157/src/Microsoft.Graph.Core/Tasks/PageIterator.cs

                            if (driveItemDeltaCollectionPage.NextPageRequest != null)
                            {
                                driveItemChangesListModel.ContinuationUuid = driveItemDeltaCollectionPage.NextPageRequest.GetHttpRequestMessage().RequestUri.AbsoluteUri;
                            }

                            if (driveItemDeltaCollectionPage.AdditionalData != null)
                            {
                                if (string.IsNullOrEmpty(driveItemChangesListModel.ContinuationUuid))
                                {
                                    if (driveItemDeltaCollectionPage.AdditionalData.TryGetValue(Constants.OdataInstanceAnnotations.NextLink, out object nextLink) &&
                                        nextLink != null)
                                    {
                                        var nextLinkString = nextLink.ToString();

                                        if (!string.IsNullOrEmpty(nextLinkString))
                                        {
                                            driveItemChangesListModel.ContinuationUuid = nextLinkString;
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(driveItemChangesListModel.ContinuationUuid))
                                {
                                    if (driveItemDeltaCollectionPage.AdditionalData.TryGetValue(Constants.OdataInstanceAnnotations.DeltaLink, out object deltaLink) &&
                                        deltaLink != null)
                                    {
                                        var deltaLinkString = deltaLink.ToString();

                                        if (!string.IsNullOrEmpty(deltaLinkString))
                                        {
                                            driveItemChangesListModel.ContinuationUuid = deltaLinkString;
                                        }
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(driveItemChangesListModel.ContinuationUuid))
                            {
                                throw new Exception($"Continuation UUID could not be resolved. Additional data was: {JsonConvert.SerializeObject(driveItemDeltaCollectionPage.AdditionalData)}");
                            }

                            return driveItemChangesListModel;
                        },
                        requestFailedHandler: DefaultRequestFailedHandler,
                        hint: "Get drive item changed",
                        isGet: false
                    ).ConfigureAwait(false);
            }
            catch (ExternalDependencyException ex)
            when (ex.ErrorCode == ExternalDependencyStatusEnum.CompatiblityIssue)
            {
                // may also happen

                _logger.LogWarning($"Compatibility issue reported from SharePoint for delta link {deltaLink}, resetting continuation UUID: {ex}");

                throw new ExternalDependencyException(ExternalDependencyStatusEnum.ContinuationUuidTooOld, "The continuation UUID is too old");
            }

            if (driveItemChangesList != null && driveItemChangesList.DriveItems != null)
            {
                for (var i = driveItemChangesList.DriveItems.Count - 1; i >= 0; i--)
                {
                    var driveItem = driveItemChangesList.DriveItems.ElementAt(i);

                    // Root folders should be ignored and kept out of scope due to folder navigation

                    var isFolder = driveItem.IsFolder();

                    if (isFolder &&
                        _siteFolderIds.Contains(driveItem.GetAssetId()))
                    {
                        driveItemChangesList.DriveItems.Remove(driveItem);

                        continue;
                    }

                    if (driveItem.Deleted != null)
                    {
                        continue;
                    }

                    var canAccess = await CanAccessDriveItemAsync(driveItem, allowRootFolders: false).ConfigureAwait(false);

                    if (canAccess)
                    {
                        continue;
                    }

                    // We are deleting assets that we no longer have can access to

                    if (isFolder)
                    {
                        // We will not receive notifications for the individual assets, so we will delete them recursively

                        driveItemChangesList.DriveItems.Remove(driveItem);
                        driveItemChangesList.FolderDriveItemsToDelete.Add(driveItem);
                    }
                    else
                    {
                        // Single asset being moved and we cannot access it

                        driveItem.Deleted = new Deleted();
                    }
                }
            }

            return driveItemChangesList;
        }

        private IDriveItemDeltaRequest GetDriveItemDeltaRequest(string deltaLink)
        {
            IDriveItemDeltaRequest driveItemDeltaRequest;

            if (!string.IsNullOrEmpty(deltaLink))
            {
                var driveItemDeltaCollectionPage = new DriveItemDeltaCollectionPage();

                driveItemDeltaCollectionPage.InitializeNextPageRequest(_graphServiceClient, deltaLink);

                driveItemDeltaRequest = driveItemDeltaCollectionPage.NextPageRequest;
            }
            else
            {
                var queryOptions = new List<QueryOption>()
                {
                    new QueryOption("token", "latest")
                };

                if (string.IsNullOrEmpty(_siteDriveId))
                {
                    driveItemDeltaRequest = _graphServiceClient
                        .Sites[SiteId]
                        .Drive
                        .Root
                        .Delta()
                        .Request(queryOptions);
                }
                else
                {
                    driveItemDeltaRequest = _graphServiceClient
                        .Sites[SiteId]
                        .Drives[_siteDriveId]
                        .Root
                        .Delta()
                        .Request(queryOptions);
                }
            }

            if (!driveItemDeltaRequest.QueryOptions.Any(queryOption => string.Equals(queryOption.Name, "$top")))
            {
                driveItemDeltaRequest.QueryOptions.SetPageSize(50);
            }

            return driveItemDeltaRequest;
        }

        public Task<ICollection<DriveItem>> GetDriveItemsBatchAsync(List<string> assetIds)
        {
            return GetDriveItemsBatchAsync(assetIds, allowRootFolders: false);
        }

        private async Task<ICollection<DriveItem>> GetDriveItemsBatchAsync(List<string> assetIds, bool allowRootFolders)
        {
            if (string.IsNullOrEmpty(SiteId) || !_siteFolderIds.Any())
            {
                return null;
            }

            if (assetIds == null || !assetIds.Any())
            {
                return new List<DriveItem>();
            }

            var driveItems = new List<DriveItem>();

            foreach (var assetId in assetIds)
            {
                var driveItem = await GetDriveItemAsync(assetId, allowRootFolders).ConfigureAwait(false);

                if (driveItem == null)
                {
                    continue;
                }

                driveItems.Add(driveItem);
            }

            // GetDriveItemAsync already returned only accessible drive items

            return driveItems;
        }

        private static void SetResponseHeaderType(RestRequest request)
        {
            request.AddHeader("Accept", "application/json;odata=verbose");
        }

        private static T GetTokenValue<T>(JToken jToken, string property)
        {
            var rawValue = jToken.SelectToken(property);

            if (Equals(rawValue, default(T)))
            {
                return default;
            }

            return rawValue.Value<T>();
        }

        private static T GetTokenValueByProperty<T>(JToken jToken, string property, string propertyValue)
        {
            var rawValue = jToken.SelectToken($"$[?(@.{property} == '{propertyValue}')].Value");

            if (Equals(rawValue, default(T)))
            {
                return default;
            }

            return rawValue.Value<T>();
        }
    }
}
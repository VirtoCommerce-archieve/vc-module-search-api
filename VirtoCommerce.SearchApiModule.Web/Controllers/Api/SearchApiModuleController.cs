using Hangfire;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Packaging;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Security;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchApiModule.Web.Model;
using VirtoCommerce.SearchApiModule.Web.Security;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;

namespace VirtoCommerce.SearchApiModule.Web.Controllers.Api
{
    [RoutePrefix("api/search")]
    public class SearchApiModuleController : ApiController
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ISearchConnection _searchConnection;
        private readonly IBrowseFilterService _browseFilterService;
        private readonly IItemBrowsingService _browseService;
        private readonly ICategoryBrowsingService _categoryBrowseService;
        private readonly IStoreService _storeService;
        private readonly IUserNameResolver _userNameResolver;
        private readonly IPushNotificationManager _pushNotifier;

        public SearchApiModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection,
            IBrowseFilterService browseFilterService, IItemBrowsingService browseService,
            ICategoryBrowsingService categoryBrowseService, IStoreService storeService,
            IUserNameResolver userNameResolver, IPushNotificationManager pushNotifier)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _browseFilterService = browseFilterService;
            _browseService = browseService;
            _storeService = storeService;
            _categoryBrowseService = categoryBrowseService;
            _userNameResolver = userNameResolver;
            _pushNotifier = pushNotifier;
        }

        [HttpPost]
        [Route("{storeId}/products")]
        [ResponseType(typeof(ProductSearchResult))]
        public IHttpActionResult SearchProducts(string storeId, ProductSearch criteria)
        {
            var responseGroup = EnumUtility.SafeParse<ItemResponseGroup>(criteria.ResponseGroup, ItemResponseGroup.ItemLarge & ~ItemResponseGroup.ItemProperties);
            var result = SearchProducts(_searchConnection.Scope, storeId, criteria, responseGroup);
            return Ok(result);
        }

        [HttpPost]
        [Route("{storeId}/categories")]
        [ResponseType(typeof(CategorySearchResult))]
        public IHttpActionResult SearchCategories(string storeId, CategorySearch criteria)
        {
            var responseGroup = EnumUtility.SafeParse<CategoryResponseGroup>(criteria.ResponseGroup, CategoryResponseGroup.Full & ~CategoryResponseGroup.WithProperties);
            var result = SearchCategories(_searchConnection.Scope, storeId, criteria, responseGroup);
            return Ok(result);
        }

        /// <summary>
        /// Get search index for specified document type and document id.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("index/{documentType}/{documentId}")]
        [ResponseType(typeof(IndexStatistics))]
        // [CheckPermission(Permission = SearchPredefinedPermissions.Read)]
        public IHttpActionResult GetIndex(string documentType, string documentId)
        {
            var result = new IndexDocument
            {
                Id = documentId,
                BuildDate = DateTime.UtcNow.AddMinutes(-1),
                Content = "function updateStatus() {    \n    if ($scope.index && blade.currentEntity) {        $scope.loading = false;        if (!$scope.index.id)"
            };
            if (new Random().Next(3) == 2)
                result = new IndexDocument(); // index not found

            return Ok(result);
        }

        /// <summary>
        /// Get search index statistics for specified document type and document id.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("index/statistics/{documentType}/{documentId}")]
        [ResponseType(typeof(IndexStatistics))]
        // [CheckPermission(Permission = SearchPredefinedPermissions.Read)]
        public IHttpActionResult GetStatistics(string documentType, string documentId)
        {
            var result = new IndexStatistics
            {
                ItemCount = 94,
                ItemCountCatalog = 156,
                CategoryCount = 24
            };
            return Ok(result);
        }

        /// <summary>
        /// Rebuild the index for specified document type and document id.
        /// </summary>
        /// <param name="documentType"></param>
        /// <param name="documentId"></param>
        /// <param name="UpdateOnly">Rebuild only missing documents</param>
        /// <returns></returns>
        [HttpPut]
        [Route("~/api/searchAPI/index/rebuild/{documentType}/{documentId}")]
        [ResponseType(typeof(SearchPushNotification))]
        [CheckPermission(Permission = SearchPredefinedPermissions.RebuildIndex)]
        public IHttpActionResult Rebuild(string documentType = "", string documentId = "", bool UpdateOnly = false)
        {
            var result = ScheduleRebuildJob(documentType, documentId, UpdateOnly);
            return Ok(result);
        }


        private SearchPushNotification ScheduleRebuildJob(string documentType, string documentId, bool updateOnly)
        {
            var result = new SearchPushNotification(_userNameResolver.GetCurrentUserName());
            result.Title = "Search index rebuild";
            result.DocumentType = documentType;
            result.ProgressLog.Add(new Model.ProgressMessage { Level = ProgressMessageLevel.Info.ToString(), Message = "Initiating index updates job" });

            _pushNotifier.Upsert(result);

            BackgroundJob.Enqueue(() => RebuildBackgroundJob(documentType, documentId, updateOnly, result));

            return result;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public void RebuildBackgroundJob(string documentType, string documentId, bool updateOnly, SearchPushNotification notification)
        {
            try
            {
                notification.Started = DateTime.UtcNow;
                _pushNotifier.Upsert(notification);

                var length = documentType.Length;
                if (updateOnly) length = length / 2;
                for (int i = 0; i < length; i++)
                {
                    System.Threading.Thread.Sleep(900);
                    notification.ProgressLog.Add(new Model.ProgressMessage { Message = documentType + " indexing... " + i });
                    _pushNotifier.Upsert(notification);
                }
            }
            finally
            {
                notification.Finished = DateTime.UtcNow;
                notification.ProgressLog.Add(new Model.ProgressMessage
                {
                    Level = ProgressMessageLevel.Info.ToString(),
                    Message = "Building finished.",
                });
                _pushNotifier.Upsert(notification);
            }
        }

        private CategorySearchResult SearchCategories(string scope, string storeId, CategorySearch criteria, CategoryResponseGroup responseGroup)
        {
            var store = _storeService.GetById(storeId);

            if (store == null)
                return null;

            var serviceCriteria = criteria.AsCriteria<CategorySearchCriteria>(store.Catalog);

            var searchResults = _categoryBrowseService.SearchCategories(scope, serviceCriteria, responseGroup);
            return searchResults;
        }

        private ProductSearchResult SearchProducts(string scope, string storeId, ProductSearch criteria, ItemResponseGroup responseGroup)
        {
            var store = _storeService.GetById(storeId);

            if (store == null)
                return null;

            var context = new Dictionary<string, object>
            {
                { "Store", store },
            };

            var filters = _browseFilterService.GetFilters(context);

            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Catalog, filters);

            var searchResults = _browseService.SearchItems(scope, serviceCriteria, responseGroup);
            return searchResults;
        }
    }
}
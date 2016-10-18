using CacheManager.Core;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Web.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.Platform.Core.Common;

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

        public SearchApiModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, 
            IBrowseFilterService browseFilterService, IItemBrowsingService browseService,
            ICategoryBrowsingService categoryBrowseService, 
            IStoreService storeService)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _browseFilterService = browseFilterService;
            _browseService = browseService;
            _storeService = storeService;
            _categoryBrowseService = categoryBrowseService;
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

        private CategorySearchResult SearchCategories(string scope, string storeId, CategorySearch criteria, CategoryResponseGroup responseGroup)
        {
            var store =  _storeService.GetById(storeId);

            if (store == null)
                return null;

            var serviceCriteria = criteria.AsCriteria<CategorySearchCriteria>(store.Catalog);

            var searchResults = _categoryBrowseService.SearchCategories(scope, serviceCriteria, responseGroup);
            return searchResults;
        }

        private ProductSearchResult SearchProducts(string scope, string storeId, ProductSearch criteria, ItemResponseGroup responseGroup)
        {
            var store =  _storeService.GetById(storeId);

            if (store == null)
                return null;

            var context = new Dictionary<string, object>
            {
                { "Store", store },
            };

            var filters =  _browseFilterService.GetFilters(context);

            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Catalog, filters);

            var searchResults = _browseService.SearchItems(scope, serviceCriteria, responseGroup);
            return searchResults;
        }
    }
}
using CacheManager.Core;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.CatalogModule.Web.Model;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Web.Common;
using VirtoCommerce.Platform.Data.Common;
using VirtoCommerce.SearchApiModule.Web.Model;
using VirtoCommerce.SearchApiModule.Web.Services;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Model.Filters;

namespace VirtoCommerce.SearchApiModule.Web.Controllers.Api
{
    [RoutePrefix("api/search")]
    public class SearchApiModuleController : ApiController
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ISearchConnection _searchConnection;
        private readonly IBrowseFilterService _browseFilterService;
        private readonly IItemBrowsingService _browseService;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ICacheManager<object> _cacheManager;

        public SearchApiModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, 
            IBrowseFilterService browseFilterService, IItemBrowsingService browseService,
            IBlobUrlResolver blobUrlResolver, ICacheManager<object> cacheManager)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _browseFilterService = browseFilterService;
            _browseService = browseService;
            _blobUrlResolver = blobUrlResolver;
            _cacheManager = cacheManager;
        }


        [HttpPost]
        [Route("{store}/products")]
        [ResponseType(typeof(CatalogSearchResult))]
        [ClientCache(Duration = 30)]
        public IHttpActionResult SearchProducts(string store, ProductSearch criteria)
        {
            var result = SearchProducts(_searchConnection.Scope, store, criteria, ItemResponseGroup.ItemLarge);
            return Ok(result.ToWebModel(_blobUrlResolver));
        }

        private Domain.Catalog.Model.SearchResult SearchProducts(string scope, string store, ProductSearch criteria, ItemResponseGroup responseGroup)
        {
            var context = new Dictionary<string, object>
            {
                { "StoreId", store },
            };

            var filters = _cacheManager.Get("GetFilters-" + store, "SearchProducts", TimeSpan.FromMinutes(5), () => _browseFilterService.GetFilters(context));
            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(filters);

            //Load ALL products 
            var searchResults = _browseService.SearchItems(scope, serviceCriteria, responseGroup);
            return searchResults;
        }
    }
}
using CacheManager.Core;
using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Web.Common;
using VirtoCommerce.Platform.Data.Common;
using VirtoCommerce.SearchApiModule.Web.Extensions;
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
        private readonly ISecurityService _securityService;
        private readonly ICacheManager<object> _cacheManager;

        public SearchApiModuleController(ISearchProvider searchProvider, ISearchConnection searchConnection, 
            IBrowseFilterService browseFilterService, IItemBrowsingService browseService, ISecurityService securityService,
            ICacheManager<object> cacheManager)
        {
            _searchProvider = searchProvider;
            _searchConnection = searchConnection;
            _browseFilterService = browseFilterService;
            _browseService = browseService;
            _securityService = securityService;
            _cacheManager = cacheManager;
        }


        [HttpPost]
        [Route("{store}/products")]
        [ResponseType(typeof(ProductSearchResult))]
        [ClientCache(Duration = 30)]
        public IHttpActionResult SearchProducts(string store, ProductSearch criteria)
        {
            var result = SearchProducts(_searchConnection.Scope, store, criteria, ItemResponseGroup.ItemLarge);
            return Ok(result);
        }

        private ProductSearchResult SearchProducts(string scope, string store, ProductSearch criteria, ItemResponseGroup responseGroup)
        {
            var context = new Dictionary<string, object>
            {
                { "StoreId", store },
            };

            var filters = _cacheManager.Get("GetFilters-" + store, "SearchProducts", TimeSpan.FromMinutes(5), () => _browseFilterService.GetFilters(context));

            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(filters);
            serviceCriteria.ApplyRestrictionsForUser(User.Identity.Name, _securityService);

            //Load ALL products 
            var searchResults = _browseService.SearchItems(scope, serviceCriteria, responseGroup);
            return searchResults;
        }
    }
}
using System;
using System.Linq;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    /// <summary>
    /// Another implementation for ICatalogSearchService. Combines indexed and DB search providers.
    /// </summary>
    public class CatalogSearchServiceImpl : ICatalogSearchService
    {
        private readonly CatalogModule.Data.Services.CatalogSearchServiceImpl _catalogSearchServiceImpl_Catalog;
        private readonly IItemBrowsingService _browseService;
        private readonly ISearchConnection _searchConnection;
        private readonly IBlobUrlResolver _blobUrlResolver;

        public CatalogSearchServiceImpl(Func<ICatalogRepository> catalogRepositoryFactory, 
            IItemService itemService, 
            ICatalogService catalogService, 
            ICategoryService categoryService,
            IItemBrowsingService browseService,
            ISearchConnection searchConnection,
            IBlobUrlResolver blobUrlResolver
            )
        {
            _catalogSearchServiceImpl_Catalog = new CatalogModule.Data.Services.CatalogSearchServiceImpl(catalogRepositoryFactory, itemService, catalogService, categoryService);
            _browseService = browseService;
            _searchConnection = searchConnection;
            _blobUrlResolver = blobUrlResolver;
        }

        public SearchResult Search(SearchCriteria criteria)
        {
            SearchResult retVal;
            if (string.IsNullOrEmpty(criteria.Keyword))
            {
                // use original impl. from catalog module
                retVal = _catalogSearchServiceImpl_Catalog.Search(criteria);
            }
            else
            {
                // use indexed search
                retVal = new SearchResult();

                // TODO: create outline for category
                // TODO: implement sorting

                var serviceCriteria = new SimpleCatalogItemSearchCriteria() {
                    RawQuery = criteria.Keyword,
                    Catalog = criteria.CatalogId,
                    StartingRecord = criteria.Skip,
                    RecordsToRetrieve = criteria.Take,
                    WithHidden = true
                };

                var searchResults = _browseService.SearchItems(_searchConnection.Scope, serviceCriteria, ItemResponseGroup.ItemInfo | ItemResponseGroup.Outlines);

                if(searchResults.Products != null)
                {
                    retVal.Products = searchResults.Products.Select(x => x.ToModuleModel(_blobUrlResolver)).ToArray();
                }

                retVal.ProductsTotalCount = (int)searchResults.TotalCount;
            }

            return retVal;
        }
    }
}

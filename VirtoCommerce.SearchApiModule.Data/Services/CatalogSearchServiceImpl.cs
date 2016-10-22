using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

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
        private readonly ISearchProvider _searchProvider;
        private readonly IItemService _itemService;

        public CatalogSearchServiceImpl(Func<ICatalogRepository> catalogRepositoryFactory, 
            IItemService itemService, 
            ICatalogService catalogService, 
            ICategoryService categoryService,
            IItemBrowsingService browseService,
            ISearchConnection searchConnection,
            IBlobUrlResolver blobUrlResolver,
            ISearchProvider searchService
            )
        {
            _catalogSearchServiceImpl_Catalog = new CatalogModule.Data.Services.CatalogSearchServiceImpl(catalogRepositoryFactory, itemService, catalogService, categoryService);
            _browseService = browseService;
            _searchConnection = searchConnection;
            _blobUrlResolver = blobUrlResolver;
            _searchProvider = searchService;
            _itemService = itemService;
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

                SearchItems(_searchConnection.Scope, retVal, serviceCriteria, ItemResponseGroup.ItemInfo | ItemResponseGroup.Outlines);
            }

            return retVal;
        }

        public void SearchItems(string scope, SearchResult results, ISearchCriteria criteria, ItemResponseGroup responseGroup)
        {
            var items = new List<CatalogProduct>();
            var itemsOrderedList = new List<string>();

            var foundItemCount = 0;
            var dbItemCount = 0;
            var searchRetry = 0;

            //var myCriteria = criteria.Clone();
            var myCriteria = criteria;

            ISearchResults<DocumentDictionary> searchResults = null;

            do
            {
                // Search using criteria, it will only return IDs of the items
                searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);

                searchRetry++;

                if (searchResults == null || searchResults.Documents == null)
                {
                    continue;
                }

                //Get only new found itemIds
                var uniqueKeys = searchResults.Documents.Select(x => x.Id.ToString()).Except(itemsOrderedList).ToArray();
                foundItemCount = uniqueKeys.Length;

                if (!searchResults.Documents.Any())
                {
                    continue;
                }

                itemsOrderedList.AddRange(uniqueKeys);

                // if we can determine catalog, pass it to the service
                string catalog = null;
                if (criteria is CatalogItemSearchCriteria)
                {
                    catalog = (criteria as CatalogItemSearchCriteria).Catalog;
                }

                // Now load items from repository
                var currentItems = _itemService.GetByIds(uniqueKeys.ToArray(), responseGroup, catalog);

                var orderedList = currentItems.OrderBy(i => itemsOrderedList.IndexOf(i.Id));
                items.AddRange(orderedList);
                dbItemCount = currentItems.Length;

                //If some items where removed and search is out of sync try getting extra items
                if (foundItemCount > dbItemCount)
                {
                    //Retrieve more items to fill missing gap
                    myCriteria.RecordsToRetrieve += (foundItemCount - dbItemCount);
                }
            }
            while (foundItemCount > dbItemCount && searchResults != null && searchResults.Documents.Any() && searchRetry <= 3 &&
                (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < searchResults.TotalCount);

            results.Products = items.ToArray();

            if (searchResults != null)
                results.ProductsTotalCount = (int)searchResults.TotalCount;
        }
    }
}

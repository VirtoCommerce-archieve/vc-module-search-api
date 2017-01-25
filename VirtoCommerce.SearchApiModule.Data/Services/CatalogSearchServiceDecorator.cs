using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Settings;
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
    public class CatalogSearchServiceDecorator : ICatalogSearchService
    {
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ISearchConnection _searchConnection;
        private readonly ISearchProvider _searchProvider;
        private readonly IItemService _itemService;
        private readonly ISettingsManager _settingsManager;

        public CatalogSearchServiceDecorator(
            ICatalogSearchService catalogSearchService,
            ISearchConnection searchConnection,
            ISearchProvider searchProvider,
            IItemService itemService,
            ISettingsManager settingsManager)
        {
            _catalogSearchService = catalogSearchService;
            _searchConnection = searchConnection;
            _searchProvider = searchProvider;
            _itemService = itemService;
            _settingsManager = settingsManager;
        }

        public SearchResult Search(SearchCriteria criteria)
        {
            SearchResult result;

            var useIndexedSearch = _settingsManager.GetValue("VirtoCommerce.SearchApi.UseCatalogIndexedSearchInManager", true);
            var searchProducts = criteria.ResponseGroup.HasFlag(SearchResponseGroup.WithProducts);

            if (useIndexedSearch && searchProducts && !string.IsNullOrEmpty(criteria.Keyword))
            {
                result = new SearchResult();

                // TODO: create outline for category
                // TODO: implement sorting

                var serviceCriteria = new SimpleCatalogItemSearchCriteria()
                {
                    SearchPhrase = criteria.Keyword,
                    Catalog = criteria.CatalogId,
                    StartingRecord = criteria.Skip,
                    RecordsToRetrieve = criteria.Take,
                    WithHidden = criteria.WithHidden,
                    Locale = "*",
                };

                SearchItems(_searchConnection.Scope, result, serviceCriteria, ItemResponseGroup.ItemInfo | ItemResponseGroup.Outlines);
            }
            else
            {
                // use original impl. from catalog module
                result = _catalogSearchService.Search(criteria);
            }

            return result;
        }

        public void SearchItems(string scope, SearchResult result, ISearchCriteria criteria, ItemResponseGroup responseGroup)
        {
            var items = new List<CatalogProduct>();
            var itemsOrderedList = new List<string>();

            var foundItemCount = 0;
            var dbItemCount = 0;
            var searchRetry = 0;

            //var myCriteria = criteria.Clone();
            var myCriteria = criteria;

            ISearchResults<DocumentDictionary> searchResults;

            do
            {
                // Search using criteria, it will only return IDs of the items
                searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);

                searchRetry++;

                if (searchResults?.Documents == null)
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
                var catalog = (criteria as CatalogItemSearchCriteria)?.Catalog;

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
            while (foundItemCount > dbItemCount && searchResults?.Documents != null && searchResults.Documents.Any() && searchRetry <= 3 &&
                (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < searchResults.TotalCount);

            result.Products = items.ToArray();

            if (searchResults != null)
                result.ProductsTotalCount = (int)searchResults.TotalCount;
        }
    }
}

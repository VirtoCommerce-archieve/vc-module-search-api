using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Converters;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Services;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ItemBrowsingService : IItemBrowsingService
    {
        private readonly IItemService _itemService;
        private readonly ISearchProvider _searchProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;

        public ItemBrowsingService(IItemService itemService,
            ISearchProvider searchService, IBlobUrlResolver blobUrlResolver)
        {
            _searchProvider = searchService;
            _itemService = itemService;
            _blobUrlResolver = blobUrlResolver;
        }

        public virtual ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup)
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
                var uniqueKeys = searchResults.Documents.Select(x=>x.Id.ToString()).Except(itemsOrderedList).ToArray();
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
            while (foundItemCount > dbItemCount && searchResults!=null && searchResults.Documents.Any() && searchRetry <= 3 &&
                (myCriteria.RecordsToRetrieve + myCriteria.StartingRecord) < searchResults.TotalCount);

            var response = new ProductSearchResult();

            if (items != null)
            {
                var productDtos= new ConcurrentBag<CatalogModule.Web.Model.Product>();
                Parallel.ForEach(items, (x) =>
                {
                    productDtos.Add(x.ToWebModel(_blobUrlResolver));
                });
                response.Products = productDtos.OrderBy(i => itemsOrderedList.IndexOf(i.Id)).ToArray();
            }

            if (searchResults != null)
                response.TotalCount = searchResults.TotalCount;

            // TODO need better way to find applied filter values
            var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x => x.Id).ToArray();
            if (searchResults.Facets != null)
            {
                response.Aggregations = searchResults.Facets.Select(g => g.ToModuleModel(appliedFilters)).ToArray();
            }
            return response;
        }
    }
}

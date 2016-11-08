using Newtonsoft.Json;
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
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ItemBrowsingService : IItemBrowsingService
    {
        private readonly IItemService _itemService;
        private readonly ISearchProvider _searchProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly  ISettingsManager _settingsManager;

        public ItemBrowsingService(IItemService itemService,
            ISearchProvider searchService, IBlobUrlResolver blobUrlResolver, ISettingsManager settingsManager)
        {
            _searchProvider = searchService;
            _itemService = itemService;
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingsManager;
        }

        public virtual ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup)
        {
            var response = new ProductSearchResult();
            var searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);

            if (searchResults != null && searchResults.Documents != null)
            {               
                var productDtos = new ConcurrentBag<CatalogModule.Web.Model.Product>();
                var taskList = new List<Task>();

                var documents = searchResults.Documents;             
                if (_settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", false))
                {
                    var fullIndexedDocuments = documents.Where(x => x.ContainsKey("__object") && !string.IsNullOrEmpty(x["__object"].ToString()));
                    documents = documents.Except(fullIndexedDocuments);
                    var deserializeProductsTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {                       
                        Parallel.ForEach(fullIndexedDocuments, new ParallelOptions { MaxDegreeOfParallelism = 5 }, (x) =>
                        {
                            var jsonProduct = x["__object"].ToString();
                            var product = JsonConvert.DeserializeObject(jsonProduct, typeof(CatalogModule.Web.Model.Product)) as CatalogModule.Web.Model.Product;
                            productDtos.Add(product);
                        });
                    });
                    taskList.Add(deserializeProductsTask);
                }

                if (documents.Any())
                {
                    var loadProductsTask = System.Threading.Tasks.Task.Factory.StartNew(() =>
                    {
                        string catalog = null;
                        if (criteria is CatalogItemSearchCriteria)
                        {
                            catalog = (criteria as CatalogItemSearchCriteria).Catalog;
                        }
                        var productIds = documents.Select(x => x.Id.ToString()).Distinct().ToArray();
                        if (productIds.Any())
                        {
                        // Now load items from repository
                        var products = _itemService.GetByIds(productIds, responseGroup, catalog);
                            Parallel.ForEach(products, (x) =>
                            {
                                productDtos.Add(x.ToWebModel(_blobUrlResolver));
                            });
                        }
                    }
                    );
                    taskList.Add(loadProductsTask);
                }

                Task.WaitAll(taskList.ToArray());

                //Preserver original sorting order
                var orderedIds = searchResults.Documents.Select(x => x.Id.ToString()).ToList();
                response.Products = productDtos.OrderBy(i => orderedIds.IndexOf(i.Id)).ToArray();
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchApiModule.Data.Converters;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Services;
using catalogModel = VirtoCommerce.CatalogModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ItemBrowsingService : IItemBrowsingService
    {
        private readonly IItemService _itemService;
        private readonly ISearchProvider _searchProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ISettingsManager _settingsManager;

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
            if (searchResults != null)
            {
                response.TotalCount = searchResults.TotalCount;

                if (searchResults.Documents != null)
                {
                    var productDtos = new ConcurrentBag<catalogModel.Product>();
                    var taskList = new List<Task>();

                    var documents = searchResults.Documents.ToList();

                    if (_settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", true))
                    {
                        var fullIndexedDocuments = documents.Where(x => x.ContainsKey("__object") && !string.IsNullOrEmpty(x["__object"].ToString())).ToList();
                        documents = documents.Except(fullIndexedDocuments).ToList();

                        var deserializeProductsTask = Task.Factory.StartNew(() =>
                        {
                            Parallel.ForEach(fullIndexedDocuments, new ParallelOptions { MaxDegreeOfParallelism = 5 }, doc =>
                            {
                                var jsonProduct = doc["__object"].ToString();
                                var product = JsonConvert.DeserializeObject(jsonProduct, typeof(catalogModel.Product)) as catalogModel.Product;
                                ReduceSearchResult(criteria, responseGroup, product);
                                productDtos.Add(product);
                            });
                        });

                        taskList.Add(deserializeProductsTask);
                    }

                    if (documents.Any())
                    {
                        var loadProductsTask = Task.Factory.StartNew(() =>
                            {
                                string catalog = null;
                                var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
                                if (catalogItemSearchCriteria != null)
                                {
                                    catalog = catalogItemSearchCriteria.Catalog;
                                }

                                var productIds = documents.Select(x => x.Id.ToString()).Distinct().ToArray();
                                if (productIds.Any())
                                {
                                    // Now load items from repository
                                    var products = _itemService.GetByIds(productIds, responseGroup, catalog);
                                    Parallel.ForEach(products, p =>
                                    {
                                        var product = p.ToWebModel(_blobUrlResolver);
                                        ReduceSearchResult(criteria, responseGroup, product);
                                        productDtos.Add(product);
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

                if (searchResults.Facets != null)
                {
                    // TODO: need better way to find applied filter values
                    var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x => x.Id).ToArray();
                    response.Aggregations = searchResults.Facets.Select(g => g.ToModuleModel(appliedFilters)).ToArray();
                }
            }

            return response;
        }


        protected virtual void ReduceSearchResult(ISearchCriteria criteria, ItemResponseGroup responseGroup, catalogModel.Product product)
        {
            if (!responseGroup.HasFlag(ItemResponseGroup.ItemAssets))
            {
                product.Assets = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.ItemAssociations))
            {
                product.Associations = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.ItemEditorialReviews))
            {
                product.Reviews = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.ItemInfo))
            {
                product.Properties = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.Links))
            {
                product.Links = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.Outlines))
            {
                product.Outlines = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.Seo))
            {
                product.SeoInfos = null;
            }

            if (!responseGroup.HasFlag(ItemResponseGroup.Variations))
            {
                product.Variations = null;
            }
        }
    }
}

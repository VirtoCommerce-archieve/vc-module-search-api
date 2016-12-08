using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchApiModule.Data.Converters;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;
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

        public ItemBrowsingService(IItemService itemService, ISearchProvider searchProvider, IBlobUrlResolver blobUrlResolver, ISettingsManager settingsManager)
        {
            _itemService = itemService;
            _searchProvider = searchProvider;
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingsManager;
        }

        public virtual ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup)
        {
            var result = new ProductSearchResult();

            var searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);
            if (searchResults != null)
            {
                var returnProductsFromIndex = _settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", true);

                result.TotalCount = searchResults.TotalCount;
                result.Aggregations = ConvertFacetsToAggregations(searchResults.Facets, criteria);
                result.Products = ConvertDocumentsToProducts(searchResults.Documents?.ToList(), returnProductsFromIndex, criteria, responseGroup);
            }

            return result;
        }

        protected virtual Aggregation[] ConvertFacetsToAggregations(FacetGroup[] facets, ISearchCriteria criteria)
        {
            Aggregation[] result = null;

            if (facets != null && facets.Any())
            {
                // TODO: need better way to find applied filter values
                var appliedFilters = criteria.CurrentFilters.SelectMany(x => x.GetValues()).Select(x => x.Id).ToArray();
                result = facets.Select(g => g.ToModuleModel(appliedFilters)).ToArray();
            }

            return result;
        }


        protected virtual catalogModel.Product[] ConvertDocumentsToProducts(IList<DocumentDictionary> documents, bool returnProductsFromIndex, ISearchCriteria criteria, ItemResponseGroup responseGroup)
        {
            catalogModel.Product[] result = null;

            if (documents != null && documents.Any())
            {
                var productsMap = documents.ToDictionary(doc => doc.Id.ToString(), doc => returnProductsFromIndex ? ConvertDocumentToProduct(doc) : null);

                var missingProductIds = productsMap
                    .Where(kvp => kvp.Value == null)
                    .Select(kvp => kvp.Key)
                    .ToArray();

                if (missingProductIds.Any())
                {
                    // Load items from repository
                    var catalog = (criteria as CatalogItemSearchCriteria)?.Catalog;
                    var catalogProducts = _itemService.GetByIds(missingProductIds, responseGroup, catalog);

                    foreach (var product in catalogProducts)
                    {
                        productsMap[product.Id] = product.ToWebModel(_blobUrlResolver);
                    }
                }

                foreach (var product in productsMap.Values)
                {
                    ReduceSearchResult(criteria, responseGroup, product);
                }

                // Preserve original sorting order
                result = documents.Select(doc => productsMap[doc.Id.ToString()]).ToArray();
            }

            return result;
        }

        protected virtual catalogModel.Product ConvertDocumentToProduct(DocumentDictionary doc)
        {
            catalogModel.Product result = null;

            if (doc.ContainsKey("__object"))
            {
                var obj = doc["__object"];
                result = obj as catalogModel.Product;

                if (result == null)
                {
                    var jobj = obj as JObject;
                    if (jobj != null)
                    {
                        result = jobj.ToObject<catalogModel.Product>();
                    }
                    else
                    {
                        var productString = obj as string;
                        if (!string.IsNullOrEmpty(productString))
                        {
                            result = JsonConvert.DeserializeObject<catalogModel.Product>(productString);
                        }
                    }
                }
            }

            return result;
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

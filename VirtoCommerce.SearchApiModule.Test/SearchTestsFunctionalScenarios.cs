using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using Common.Logging;
using Microsoft.Xunit.Performance;
using Moq;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.CatalogModule.Data.Services;
using VirtoCommerce.CoreModule.Data.Repositories;
using VirtoCommerce.CoreModule.Data.Services;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Commerce.Services;
using VirtoCommerce.Domain.Payment.Model;
using VirtoCommerce.Domain.Payment.Services;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Shipping.Services;
using VirtoCommerce.Domain.Store.Model;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Domain.Tax.Model;
using VirtoCommerce.Domain.Tax.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Assets;
using VirtoCommerce.Platform.Data.ChangeLog;
using VirtoCommerce.Platform.Data.DynamicProperties;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Repositories;
using VirtoCommerce.PricingModule.Data.Services;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.StoreModule.Data.Repositories;
using VirtoCommerce.StoreModule.Data.Services;
using Xunit;

namespace VirtoCommerce.SearchApiModule.Test
{
    [CLSCompliant(false)]
    [Collection("Search")]
    public class SearchTestsFunctionalScenarios : SearchTestsBase
    {
        private const string _scope = "test-func";

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_index_category_demo_data_and_search_using_outline(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            RebuildIndex(provider, CategorySearchCriteria.DocType);

            // find all products in the category
            var criteria = new CategorySearchCriteria
            {
                Outlines = new[] { "4974648a41df4e6ea67ef2ad76d7bbd4" },
            };

            var searchResults = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.True(searchResults.TotalCount > 0, $"Didn't find any categories using {providerType} provider");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_index_product_demo_data_and_search_using_outline(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            RebuildIndex(provider, CatalogItemSearchCriteria.DocType);

            // find all products in the category
            var criteria = new CatalogItemSearchCriteria
            {
                Catalog = GetCatalogId("electronics"),
                Currency = "USD",
                Outlines = new[] { "4974648a41df4e6ea67ef2ad76d7bbd4/c76774f9047d4f18a916b38681c50557*" },
            };

            var ibs = GetItemBrowsingService(provider);
            var searchResults = ibs.SearchItems(_scope, criteria, ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.TotalCount > 0, $"Didn't find any products using {providerType} provider");
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_index_product_demo_data_and_search(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            RebuildIndex(provider, CatalogItemSearchCriteria.DocType);

            // find all products in the category
            var criteria = new CatalogItemSearchCriteria
            {
                Catalog = GetCatalogId("electronics"),
                Currency = "USD"
            };

            // Add facets
            var colorFacet = new AttributeFilter { Key = "color" };

            var brandFacet = new AttributeFilter
            {
                Key = "brand",
                IsLocalized = true,
                Values = new[]
                {
                    new AttributeFilterValue { Id = "Apple", Value = "Apple" },
                    new AttributeFilterValue { Id = "Asus", Value = "Asus" },
                    new AttributeFilterValue { Id = "Samsung", Value = "Samsung" }
                }
            };

            var sizeFacet = new RangeFilter
            {
                Key = "display_size",
                Values = new[]
                {
                    new RangeFilterValue { Id = "0_to_5", Lower = "0", Upper = "5" },
                    new RangeFilterValue { Id = "5_to_10", Lower = "5", Upper = "10" }
                }
            };

            var priceFacet = new PriceRangeFilter
            {
                Currency = "USD",
                Values = new[]
                {
                    new RangeFilterValue { Id = "under-100", Upper = "100" },
                    new RangeFilterValue { Id = "200-600", Lower = "200", Upper = "600" }
                }
            };

            criteria.Add(brandFacet);
            criteria.Add(colorFacet);
            criteria.Add(sizeFacet);
            criteria.Add(priceFacet);

            var ibs = GetItemBrowsingService(provider);
            var searchResults = ibs.SearchItems(_scope, criteria, ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.TotalCount > 0, $"Didn't find any products using {providerType} provider");
            Assert.True(searchResults.Aggregations.Any(), $"Didn't find any aggregations using {providerType} provider");

            Assert.True(GetFacetValuesCount(searchResults, "color") > 0, $"Didn't find any aggregation value for Color using {providerType} provider");

            Assert.Equal(0, GetFacetValue(searchResults, "brand", "Apple"));
            Assert.Equal(2, GetFacetValue(searchResults, "brand", "Asus"));
            Assert.Equal(5, GetFacetValue(searchResults, "brand", "Samsung"));

            criteria = new CatalogItemSearchCriteria { Currency = "USD", Locale = "en-us", SearchPhrase = "sony" };
            searchResults = ibs.SearchItems(_scope, criteria, ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.TotalCount > 0);
        }

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void Can_web_search_products(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            RebuildIndex(provider, CatalogItemSearchCriteria.DocType);

            var store = GetStore("electronics");

            // find all products in the category
            var criteria = new ProductSearch
            {
                Currency = "USD",
                Outline = "*" // find all products
                //Terms = new[] { "price:200-600" }
            };

            var context = new Dictionary<string, object>
            {
                { "Store", store },
            };

            var filterService = GetBrowseFilterService();
            var filters = filterService.GetFilters(context);
            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Id, store.Catalog, filters);
            var ibs = GetItemBrowsingService(provider);

            //Load ALL products 
            var searchResults = ibs.SearchItems(_scope, serviceCriteria, ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.TotalCount > 0, $"Didn't find any products using {providerType} provider");
            Assert.True(searchResults.Aggregations.Length > 0, $"Didn't find any aggregations using {providerType} provider");

            Assert.True(GetFacetValuesCount(searchResults, "color") > 0, $"Didn't find any aggregation value for Color using {providerType} provider");

            Assert.Equal(0, GetFacetValue(searchResults, "brand", "Apple"));
            Assert.Equal(2, GetFacetValue(searchResults, "brand", "Asus"));
            Assert.Equal(5, GetFacetValue(searchResults, "brand", "Samsung"));

            // now test sorting
            criteria = new ProductSearch
            {
                Currency = "USD",
                Outline = "*", // find all products
                Sort = new[] { "name" }
            };

            serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Id, store.Catalog, filters);
            searchResults = ibs.SearchItems(_scope, serviceCriteria, ItemResponseGroup.ItemLarge);

            var productName = searchResults.Products[0].Name;
            Assert.Equal("3DR Solo Quadcopter (No Gimbal)", productName);

            criteria = new ProductSearch
            {
                Currency = "USD",
                Outline = "*", // find all products
                Sort = new[] { "name-desc" }
            };

            serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Id, store.Catalog, filters);
            searchResults = ibs.SearchItems(_scope, serviceCriteria, ItemResponseGroup.ItemLarge);

            productName = searchResults.Products[0].Name;

            Assert.Equal("xFold CINEMA X12 RTF U7", productName);

            // now test filtering by outline
            criteria = new ProductSearch
            {
                Outline = GetCategoryId("Cell phones"),
                Currency = "USD",
                Sort = new[] { "name" }
            };

            serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Id, store.Catalog, filters);
            searchResults = ibs.SearchItems(_scope, serviceCriteria, ItemResponseGroup.ItemLarge);

            Assert.True(searchResults.TotalCount == 6, $"Expected 6, but found {searchResults.TotalCount}");
        }

        #region Performance Tests

        [Benchmark(InnerIterationCount = 10)]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        [Trait("Category", "performance")]
        public void Can_perf_web_search_products(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            RebuildIndex(provider, CatalogItemSearchCriteria.DocType);

            var store = GetStore("electronics");

            // find all products in the category
            var criteria = new ProductSearch
            {
                Currency = "USD"
            };

            var context = new Dictionary<string, object>
            {
                { "Store", store },
            };

            var filterService = GetBrowseFilterService();
            var filters = filterService.GetFilters(context);
            var serviceCriteria = criteria.AsCriteria<CatalogItemSearchCriteria>(store.Id, store.Catalog, filters);
            var ibs = GetItemBrowsingService(provider);

            Benchmark.Iterate(() =>
            {
                var searchResults = ibs.SearchItems(_scope, serviceCriteria, ItemResponseGroup.ItemLarge);
                Assert.Equal(true, searchResults.Products.Any());
            });
        }

        #endregion

        #region Private Helper Methods

        private static void RebuildIndex(ISearchProvider provider, string documentType)
        {
            provider.RemoveAll(_scope, documentType); // ???

            var controller = GetSearchIndexController(provider);
            controller.RemoveIndex(_scope, documentType);
            controller.BuildIndex(_scope, documentType, progressInfo => { });

            // sleep for index to be commited
            Thread.Sleep(5000);
        }

        private static long GetFacetValuesCount(ProductSearchResult results, string fieldName)
        {
            var aggregation = results.Aggregations?.SingleOrDefault(a => a.Field.EqualsInvariant(fieldName));
            return aggregation?.Items?.Length ?? 0;
        }

        private static long GetFacetValue(ProductSearchResult results, string fieldName, string facetKey)
        {
            var aggregation = results.Aggregations?.SingleOrDefault(a => a.Field.EqualsInvariant(fieldName));
            var item = aggregation?.Items.SingleOrDefault(x => x.Value.ToString() == facetKey);
            return item?.Count ?? 0;
        }

        private static string GetCatalogId(string name)
        {
            var catalogRepo = GetCatalogRepository();
            return catalogRepo.Catalogs.Where(c => c.Name == name).Select(c => c.Id).FirstOrDefault();
        }

        private static string GetCategoryId(string name)
        {
            var catalogRepo = GetCatalogRepository();
            return catalogRepo.Categories.Where(c => c.Name == name).Select(c => c.Id).FirstOrDefault();
        }

        private static Store GetStore(string name)
        {
            var storeRepo = GetStoreRepository();
            var storeEntity = storeRepo.Stores.SingleOrDefault(s => s.Name == name);
            var store = storeEntity != null ? GetStoreService().GetById(storeEntity.Id) : null;
            return store;
        }

        private static IItemBrowsingService GetItemBrowsingService(ISearchProvider provider)
        {
            var settings = GetSettingsManager();
            var service = new ItemBrowsingService(GetItemService(), provider, GetBlobUrlResolver(), settings);
            return service;
        }

        private static ISearchIndexController GetSearchIndexController(ISearchProvider provider)
        {
            return new SearchIndexController(GetSettingsManager(), provider,
                new CategoryIndexBuilder(provider, GetSearchService(), GetCategoryService(), GetChangeLogService(), GetCategoryBatchDocumentBuilder()),
                new CatalogItemIndexBuilder(provider, GetSearchService(), GetItemService(), GetPricingService(), GetChangeLogService(), GetProductBatchDocumentBuilder()));
        }

        private static IBatchDocumentBuilder<Category> GetCategoryBatchDocumentBuilder()
        {
            return new CategoryBatchDocumentBuilder(GetCategoryDocumentBuilder());
        }

        private static IDocumentBuilder<Category> GetCategoryDocumentBuilder()
        {
            return new CategoryDocumentBuilder(GetBlobUrlResolver(), GetSettingsManager());
        }

        private static IBatchDocumentBuilder<CatalogProduct> GetProductBatchDocumentBuilder()
        {
            return new ProductBatchDocumentBuilder(GetProductDocumentBuilder());
        }

        private static IDocumentBuilder<CatalogProduct> GetProductDocumentBuilder()
        {
            return new ProductDocumentBuilder(GetBlobUrlResolver(), GetSettingsManager());
        }

        private static IBlobUrlResolver GetBlobUrlResolver()
        {
            return new FileSystemBlobProvider("", "http://samplesite.com");
        }

        private static ICommerceService GetCommerceService()
        {
            return new CommerceServiceImpl(GetCommerceRepository);
        }

        private static ISettingsManager GetSettingsManager()
        {
            var mock = new Mock<ISettingsManager>();
            mock.Setup(s => s.GetModuleSettings("VirtoCommerce.Store")).Returns(new SettingEntry[] { });
            mock.Setup(s => s.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", true)).Returns(true);
            return mock.Object;
        }

        private static ICatalogSearchService GetSearchService()
        {
            return new CatalogSearchServiceImpl(GetCatalogRepository, GetItemService(), GetCatalogService(), GetCategoryService());
        }

        private static IOutlineService GetOutlineService()
        {
            return new OutlineService();
        }

        private static IPricingService GetPricingService()
        {
            var cacheManager = new Mock<ICacheManager<object>>();
            var log = new Mock<ILog>();
            log.Setup(l => l.Error(It.IsAny<Exception>())).Callback((object ex) =>
            {
                Trace.Write(ex.ToString());
            });

            return new PricingServiceImpl(GetPricingRepository, GetItemService(), log.Object, cacheManager.Object, null, null, null);
        }

        private static IBrowseFilterService GetBrowseFilterService()
        {
            return new BrowseFilterService();
        }

        private static IStoreService GetStoreService()
        {
            var settings = GetSettingsManager();
            var shippingService = Mock.Of<IShippingMethodsService>(s => s.GetAllShippingMethods() == new ShippingMethod[] { });
            var paymentService = Mock.Of<IPaymentMethodsService>(s => s.GetAllPaymentMethods() == new PaymentMethod[] { });
            var taxService = Mock.Of<ITaxService>(s => s.GetAllTaxProviders() == new TaxProvider[] { });
            var dpService = GetDynamicPropertyService();

            return new StoreServiceImpl(GetStoreRepository, GetCommerceService(), settings, dpService, shippingService, paymentService, taxService);
        }

        private static IDynamicPropertyService GetDynamicPropertyService()
        {
            var service = new DynamicPropertyService(GetPlatformRepository);
            return service;
        }

        private static ICategoryService GetCategoryService()
        {
            return new CategoryServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService(), GetCacheManager());
        }

        private static ICatalogService GetCatalogService()
        {
            return new CatalogServiceImpl(GetCatalogRepository, GetCommerceService(), GetCacheManager());
        }

        private static IItemService GetItemService()
        {
            return new ItemServiceImpl(GetCatalogRepository, GetCommerceService(), GetOutlineService(), GetCacheManager());
        }

        private static IChangeLogService GetChangeLogService()
        {
            return new ChangeLogService(GetPlatformRepository);
        }

        private static IStoreRepository GetStoreRepository()
        {
            var result = new StoreRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static IPlatformRepository GetPlatformRepository()
        {
            var result = new PlatformRepository("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static IPricingRepository GetPricingRepository()
        {
            var result = new PricingRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static ICatalogRepository GetCatalogRepository()
        {
            var result = new CatalogRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }

        private static ICacheManager<object> GetCacheManager()
        {
            return new Mock<ICacheManager<object>>().Object;
        }

        private static IСommerceRepository GetCommerceRepository()
        {
            var result = new CommerceRepositoryImpl("VirtoCommerce", new EntityPrimaryKeyGeneratorInterceptor(), new AuditableInterceptor(null));
            return result;
        }
        #endregion
    }
}

using System.Web.Http;
using Microsoft.Practices.Unity;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchApiModule.Web.JsonConverters;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchApiModule.Web
{
    public class Module : ModuleBase
    {
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Register index builders
            _container.RegisterType<ISearchIndexBuilder, CategoryIndexBuilder>("category-indexer");
            _container.RegisterType<ISearchIndexBuilder, ProductIndexBuilder>("catalogitem-indexer");

            _container.RegisterType<IDocumentBuilder<Category>, CategoryDocumentBuilder>(nameof(CategoryDocumentBuilder));
            _container.RegisterType<IDocumentBuilder<CatalogProduct>, ProductDocumentBuilder>(nameof(ProductDocumentBuilder));

            _container.RegisterType<IBatchDocumentBuilder<Category>, CategoryBatchDocumentBuilder>(nameof(CategoryBatchDocumentBuilder));
            _container.RegisterType<IBatchDocumentBuilder<CatalogProduct>, ProductBatchDocumentBuilder>(nameof(ProductBatchDocumentBuilder));

            _container.RegisterType<IItemBrowsingService, ItemBrowsingService>();
            _container.RegisterType<ICategoryBrowsingService, CategoryBrowsingService>();
            _container.RegisterType<IBrowseFilterService, BrowseFilterService>();

            //var searchConnection = _container.Resolve<ISearchConnection>();
            //if (searchConnection.Provider.EqualsInvariant(SearchProviders.Lucene.ToString()))
            //{
            //    _container.RegisterType<ISearchQueryBuilder, CatalogLuceneSearchQueryBuilder>("lucene");
            //}
            //else if (searchConnection.Provider.EqualsInvariant(SearchProviders.Elasticsearch.ToString()))
            //{
            //    _container.RegisterType<ISearchQueryBuilder, CatalogElasticSearchQueryBuilder>("elasticsearch");
            //}
            //else if (searchConnection.Provider.EqualsInvariant(SearchProviders.AzureSearch.ToString()))
            //{
            //    _container.RegisterType<ISearchQueryBuilder, CatalogAzureSearchQueryBuilder>("azure-search");
            //}
            _container.RegisterType<ISearchCriteriaPreprocessor, CatalogSearchCriteriaPreprocessor>(nameof(CatalogSearchCriteriaPreprocessor));
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            // Replace original ICatalogSearchService with decorator
            _container.RegisterInstance<ICatalogSearchService>(_container.Resolve<CatalogSearchServiceDecorator>());

            var httpConfiguration = _container.Resolve<HttpConfiguration>();
            httpConfiguration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new ProductSearchJsonConverter());
        }
    }
}

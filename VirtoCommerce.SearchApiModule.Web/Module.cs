using Microsoft.Practices.Unity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.SearchApiModule.Web.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchApiModule.Web.Providers.Lucene;
using VirtoCommerce.SearchApiModule.Web.Services;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchModule.Data.Model.Indexing;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;

namespace VirtoCommerce.SearchApiModule.Web
{
    public class Module : ModuleBase
    {
        private readonly IUnityContainer _container;

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        #region IModule Members

        public override void Initialize()
        {
            base.Initialize();

            // register index builders
            _container.RegisterType<ISearchIndexBuilder, CatalogItemIndexBuilder>("catalogitem-indexer");
            _container.RegisterType<ISearchIndexBuilder, CategoryIndexBuilder>("category-indexer");

            _container.RegisterType<IItemBrowsingService, ItemBrowsingService>();
            _container.RegisterType<ICategoryBrowsingService, CategoryBrowsingService>();
            _container.RegisterType<SearchModule.Data.Model.Filters.IBrowseFilterService, FilterService>();
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            var connection = _container.Resolve<ISearchConnection>();
            if (connection.Provider.EqualsInvariant(SearchProviders.Elasticsearch.ToString()))
            {
                _container.RegisterType<ICatalogIndexedSearchProvider, CatalogElasticSearchProvider>();
            }
            else
            {
                _container.RegisterType<ICatalogIndexedSearchProvider, CatalogLuceneSearchProvider>();
            }         
        }

        #endregion
    }
}

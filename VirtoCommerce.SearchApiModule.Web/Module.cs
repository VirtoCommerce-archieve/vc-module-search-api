using Microsoft.Practices.Unity;
using System;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.SearchApiModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchApiModule.Data.Providers.Lucene;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
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

        #region IModule Members

        public override void Initialize()
        {
            base.Initialize();

            // register index builders
            _container.RegisterType<ISearchIndexBuilder, CategoryIndexBuilder>("category-indexer");
            _container.RegisterType<ISearchIndexBuilder, CatalogItemIndexBuilder>("catalogitem-indexer");

            _container.RegisterType<IItemBrowsingService, ItemBrowsingService>();
            _container.RegisterType<ICategoryBrowsingService, CategoryBrowsingService>();
            _container.RegisterType<IBrowseFilterService, FilterService>();

            // ICatalogSearchService override
            _container.RegisterType<ICatalogSearchService, CatalogSearchServiceImpl>();
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            var searchConnection = _container.Resolve<ISearchConnection>();
            if (searchConnection.Provider.Equals(SearchProviders.Elasticsearch.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _container.RegisterType<ISearchQueryBuilder, CatalogElasticSearchQueryBuilder>("elastic-search");
            }
            else if (searchConnection.Provider.Equals(SearchProviders.Lucene.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _container.RegisterType<ISearchQueryBuilder, CatalogLuceneQueryBuilder>("lucene");
            }
        }

        #endregion
    }
    }

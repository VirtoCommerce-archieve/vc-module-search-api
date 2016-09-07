using Microsoft.Practices.Unity;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.SearchApiModule.Web.Services;

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
            _container.RegisterType<SearchModule.Data.Model.Indexing.ISearchIndexBuilder, CatalogItemIndexBuilder>();
            _container.RegisterType<SearchModule.Data.Model.Indexing.ISearchIndexBuilder, CategoryIndexBuilder>();

            _container.RegisterType<IItemBrowsingService, ItemBrowsingService>();
            _container.RegisterType<ICategoryBrowsingService, CategoryBrowsingService>();
            _container.RegisterType<SearchModule.Data.Model.Filters.IBrowseFilterService, FilterService>();
        }

        #endregion
    }
}

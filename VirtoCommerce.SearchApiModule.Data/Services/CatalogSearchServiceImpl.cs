using System;
using VirtoCommerce.CatalogModule.Data.Repositories;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    /// <summary>
    /// Another implementation for ICatalogSearchService. Combines indexed and DB search providers.
    /// </summary>
    public class CatalogSearchServiceImpl : ICatalogSearchService
    {
        private readonly CatalogModule.Data.Services.CatalogSearchServiceImpl _catalogSearchServiceImpl_Catalog;

        public CatalogSearchServiceImpl(Func<ICatalogRepository> catalogRepositoryFactory, IItemService itemService, ICatalogService catalogService, ICategoryService categoryService)
        {
            _catalogSearchServiceImpl_Catalog = new CatalogModule.Data.Services.CatalogSearchServiceImpl(catalogRepositoryFactory, itemService, catalogService, categoryService);
        }

        public SearchResult Search(SearchCriteria criteria)
        {
            SearchResult retVal;
            if (string.IsNullOrEmpty(criteria.Keyword))
            {
                // use original impl. from catalog module
                retVal = _catalogSearchServiceImpl_Catalog.Search(criteria);
            }
            else
            {
                // use indexed search
                retVal = new SearchResult();
                // mock result:
                var resultProduct = new CatalogProduct
                {
                    Id = "4ed55441810a47da88a483e5a1ee4e94",
                    Images = new[] { new Image { Url = "http://localhost/admin/assets/catalog/1428511277000_1133098.jpg" } },
                    Code = "_MOCK",
                    Name = "_MOCK Phantom 3 Professional Quadcopter with 4K Camera and 3-Axis Gimbal",
                    ProductType = "Physical"
                };

                retVal.Products.Add(resultProduct);
                retVal.ProductsTotalCount = 148;
            }

            return retVal;
        }
    }
}

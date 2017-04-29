using VirtoCommerce.CatalogModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class ProductSearchResult
    {
        public long TotalCount { get; set; }

        public Product[] Products { get; set; }

        public Domain.Catalog.Model.Aggregation[] Aggregations { get; set; }
    }
}

using VirtoCommerce.CatalogModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class CategorySearchResult
    {
        public long TotalCount { get; set; }

        public Category[] Categories { get; set; }
    }
}

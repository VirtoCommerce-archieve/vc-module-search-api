
using VirtoCommerce.CatalogModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class CategorySearchResult
    {
        public CategorySearchResult()
        {

        }

        public Category[] Categories { get; set; }

        public long TotalCount { get; set; }
    }
}
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchApiModule.Data.Model;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public interface ICategoryBrowsingService
    {
        CategorySearchResult SearchCategories(string scope, ISearchCriteria criteria, CategoryResponseGroup responseGroup);
    }
}

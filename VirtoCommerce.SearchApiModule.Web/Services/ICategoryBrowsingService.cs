using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchApiModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Web.Services
{
    public interface ICategoryBrowsingService
    {
        CategorySearchResult SearchCategories(string scope, ISearchCriteria criteria, CategoryResponseGroup responseGroup);
    }
}

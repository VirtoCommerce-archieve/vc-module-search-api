using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchApiModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Web.Services
{
    public interface IItemBrowsingService
    {
        ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup);
    }
}

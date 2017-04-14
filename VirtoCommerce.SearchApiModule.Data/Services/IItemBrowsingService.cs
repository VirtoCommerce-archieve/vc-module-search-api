using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public interface IItemBrowsingService
    {
        ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup);
    }
}

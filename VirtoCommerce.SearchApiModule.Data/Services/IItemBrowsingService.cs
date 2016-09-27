using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchApiModule.Data.Model;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public interface IItemBrowsingService
    {
        ProductSearchResult SearchItems(string scope, ISearchCriteria criteria, ItemResponseGroup responseGroup);
    }
}

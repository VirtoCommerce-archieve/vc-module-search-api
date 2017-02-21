using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Extensions;
using VirtoCommerce.SearchApiModule.Data.Helpers;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class CategorySearch
    {
        /// <summary>
        /// CategoryResponseGroup
        /// </summary>
        public string ResponseGroup { get; set; }

        /// <summary>
        /// CategoryId/CategoryId
        /// </summary>
        public string Outline { get; set; }

        public string[] Terms { get; set; }

        public string[] Sort { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public virtual T AsCriteria<T>(string catalog) where T : CategorySearchCriteria, new()
        {
            var criteria = AbstractTypeFactory<T>.TryCreateInstance();

            // add outline
            criteria.Outlines.Add($"{catalog}{Outline}".Trim());

            #region Filters

            var terms = Terms.AsKeyValues();
            foreach (var term in terms)
            {
                var attr = new AttributeFilter { Key = term.Key, Values = BrowseFilterHelper.CreateAttributeFilterValues(term.Values) };
                criteria.Apply(attr);
            }

            #endregion

            #region Sorting

            var categoryId = Outline.AsCategoryId();
            var sorts = Sort.AsSortInfoes();
            var sortFields = new List<SearchSortField>();
            var priorityFieldName = $"priority_{catalog}_{categoryId}".ToLower();

            if (!sorts.IsNullOrEmpty())
            {
                foreach (var sortInfo in sorts)
                {
                    var fieldName = sortInfo.SortColumn.ToLowerInvariant();
                    var isDescending = sortInfo.SortDirection == SortDirection.Descending;

                    switch (fieldName)
                    {
                        case "priority":
                            sortFields.Add(new SearchSortField(priorityFieldName, isDescending) { IgnoredUnmapped = true });
                            sortFields.Add(new SearchSortField("priority", isDescending));
                            break;
                        case "name":
                        case "title":
                            sortFields.Add(new SearchSortField("name", isDescending));
                            break;
                        default:
                            sortFields.Add(new SearchSortField(fieldName, isDescending));
                            break;
                    }
                }
            }

            if (!sortFields.Any())
            {
                sortFields.Add(new SearchSortField(priorityFieldName, true) { IgnoredUnmapped = true });
                sortFields.Add(new SearchSortField("priority", true));
                sortFields.AddRange(CatalogItemSearchCriteria.DefaultSortOrder.GetSort());
            }

            criteria.Sort = new SearchSort(sortFields.ToArray());

            #endregion

            return criteria;
        }
    }
}

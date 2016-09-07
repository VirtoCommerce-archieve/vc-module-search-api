using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Web.Extensions;
using VirtoCommerce.SearchApiModule.Web.Helpers;
using VirtoCommerce.SearchModule.Data.Model.Filters;
using VirtoCommerce.SearchModule.Data.Model.Search;

namespace VirtoCommerce.SearchApiModule.Web.Model
{
    public class CategorySearch
    {
        /// <summary>
        /// CategoryId/CategoryId
        /// </summary>
        public string Outline { get; set; }

        public string[] Sort { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public virtual T AsCriteria<T>(string catalog) where T : CategorySearchCriteria, new()
        {
            var criteria = new T();

            // add outline
            if (!string.IsNullOrEmpty(Outline))
            {
                criteria.Outlines.Add(string.Format("{0}/{1}", catalog, Outline));
            }

            #region Sorting

            var categoryId = Outline.AsCategoryId();
            var sorts = Sort.AsSortInfoes();
            var sortFields = new List<SearchSortField>();
            var priorityFieldName = string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", catalog, categoryId).ToLower();

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

            return criteria as T;
        }
    }
}
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
    public class ProductSearch
    {
        public string Catalog { get; set; }

        public string Currency { get; set; }

        public string[] Terms { get; set; }

        public string SearchPhrase { get; set; }

        /// <summary>
        /// CatalogId/CategoryId/CategoryId
        /// </summary>
        public string Outline { get; set; }

        public string[] PriceLists { get; set; }

        public string[] Sort { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public virtual T AsCriteria<T>(ISearchFilter[] filters) where T : CatalogItemSearchCriteria, new()
        {
            var criteria = new T();

            criteria.Currency = Currency;

            // Add all filters
            foreach (var filter in filters)
            {
                criteria.Add(filter);
            }

            #region Apply Filters
            var terms = Terms.AsKeyValues();
            if (terms.Any())
            {
                var filtersWithValues = filters
                    .Where(x => (!(x is PriceRangeFilter) || ((PriceRangeFilter)x).Currency.Equals(Currency, StringComparison.OrdinalIgnoreCase)))
                    .Select(x => new { Filter = x, Values = x.GetValues() })
                    .ToList();

                foreach (var term in terms)
                {
                    var filter = filters.SingleOrDefault(x => x.Key.Equals(term.Key, StringComparison.OrdinalIgnoreCase)
                        && (!(x is PriceRangeFilter) || ((PriceRangeFilter)x).Currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase)));

                    // handle special filter term with a key = "tags", it contains just values and we need to determine which filter to use
                    if (filter == null && term.Key == "tags")
                    {
                        foreach (var termValue in term.Values)
                        {
                            // try to find filter by value
                            var foundFilter = filtersWithValues.FirstOrDefault(x => x.Values.Any(y => y.Id.Equals(termValue)));

                            if (foundFilter != null)
                            {
                                filter = foundFilter.Filter;

                                var appliedFilter = BrowseFilterHelper.Convert(filter, term.Values);
                                criteria.Apply(appliedFilter);
                            }
                        }
                    }
                    else
                    {
                        var attributeFilter = filter as AttributeFilter;
                        if (attributeFilter != null && attributeFilter.Values == null)
                        {
                            filter = new AttributeFilter
                            {
                                Key = attributeFilter.Key,
                                Values = BrowseFilterHelper.CreateAttributeFilterValues(term.Values),
                                IsLocalized = attributeFilter.IsLocalized,
                                DisplayNames = attributeFilter.DisplayNames,
                            };
                        }

                        var appliedFilter = BrowseFilterHelper.Convert(filter, term.Values);
                        criteria.Apply(appliedFilter);
                    }
                }
            }
            #endregion

            /*
            #region Facets

            // apply facet filters
            var facets = Facets.AsKeyValues();
            foreach (var facet in facets)
            {
                var filter = filters.SingleOrDefault(
                    x => x.Key.Equals(facet.Key, StringComparison.OrdinalIgnoreCase)
                        && (!(x is PriceRangeFilter)
                            || ((PriceRangeFilter)x).Currency.Equals(criteria.Currency, StringComparison.OrdinalIgnoreCase)));

                var appliedFilter = BrowseFilterHelper.Convert(filter, facet.Values);
                criteria.Apply(appliedFilter);
            }

            #endregion
            */

            // TODO: add sorting, parse things like "name desc", which means sort by field name "name" in descending order. Also handle special cases like price or priority
            // TODO: handle vendor, probably through filters

            #region Sorting

            var categoryId = Outline.AsCategoryId();
            var sorts = Sort.AsSortInfoes();
            var sortFields = new List<SearchSortField>();
            var priorityFieldName = string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", Catalog, categoryId).ToLower();

            if (!sorts.IsNullOrEmpty())
            {

                foreach (var sortInfo in sorts)
                {
                    var fieldName = sortInfo.SortColumn.ToLowerInvariant();
                    var isDescending = sortInfo.SortDirection == SortDirection.Descending;

                    switch (fieldName)
                    {
                        case "price":
                            if (criteria.Pricelists != null)
                            {
                                sortFields.AddRange(
                                    criteria.Pricelists.Select(
                                        priceList =>
                                            new SearchSortField(string.Format(CultureInfo.InvariantCulture, "price_{0}_{1}", criteria.Currency.ToLower(), priceList.ToLower()))
                                            {
                                                IgnoredUnmapped = true,
                                                IsDescending = isDescending,
                                                DataType = SearchSortField.DOUBLE
                                            })
                                        .ToArray());
                            }
                            break;
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
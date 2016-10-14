using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Extensions;
using VirtoCommerce.SearchApiModule.Data.Helpers;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class ProductSearch
    {
        public ProductSearch()
        {
            Take = 20;
        }

        public ItemResponseGroup ResponseGroup { get; set; }

        public string Currency { get; set; }

        public string[] Terms { get; set; }

        public string SearchPhrase { get; set; }

        /// <summary>
        /// CategoryId1/CategoryId2, no catalog should be included in the outline
        /// </summary>
        public string Outline { get; set; }

        public string[] PriceLists { get; set; }

        public string[] Sort { get; set; }

        public int Skip { get; set; }

        public int Take { get; set; }

        public virtual T AsCriteria<T>(string catalog, ISearchFilter[] filters) where T : CatalogItemSearchCriteria, new()
        {
            var criteria = new T();

            criteria.Currency = Currency;
            criteria.Pricelists = PriceLists;
            criteria.SearchPhrase = SearchPhrase;
            criteria.StartingRecord = Skip;
            criteria.RecordsToRetrieve = Take;

            // add outline
            if (!string.IsNullOrEmpty(Outline))
            {
                criteria.Outlines.Add(string.Format("{0}/{1}", catalog, Outline));
            }
            else
            {
                criteria.Outlines.Add(string.Format("{0}*", catalog));
            }

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
                    else if(filter != null) // predefined filter
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
                    else // custom term
                    {
                        if(!term.Key.StartsWith("_")) // ignore system terms, we can't filter by them
                        {
                            var attr = new AttributeFilter { Key = term.Key, Values = BrowseFilterHelper.CreateAttributeFilterValues(term.Values)};
                            criteria.Apply(attr);
                        }
                    }
                }
            }
            #endregion

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
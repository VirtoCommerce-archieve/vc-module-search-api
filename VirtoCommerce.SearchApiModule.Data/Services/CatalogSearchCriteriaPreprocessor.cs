using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Filters;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CatalogSearchCriteriaPreprocessor : ISearchCriteriaPreprocessor
    {
        public virtual void Process(ISearchCriteria criteria)
        {
            var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
            var categorySearchCriteria = criteria as CategorySearchCriteria;

            if (catalogItemSearchCriteria != null)
            {
                AddProductFilters(catalogItemSearchCriteria);
            }
            else if (categorySearchCriteria != null)
            {
                AddCategoryFilters(categorySearchCriteria);
            }
        }


        protected virtual void AddProductFilters(CatalogItemSearchCriteria criteria)
        {
            if (criteria != null)
            {
                //filters.Add(GetRangeFilterExpression("startdate", null, false, criteria.StartDate, true));
                criteria.Apply(CreateDateRangeFilter("startdate", null, criteria.StartDate, false, true));

                if (criteria.StartDateFrom != null)
                {
                    //filters.Add(GetRangeFilterExpression("startdate", criteria.StartDateFrom, false, null, false));
                    criteria.Apply(CreateDateRangeFilter("startdate", criteria.StartDateFrom, null, false, false));
                }

                if (criteria.EndDate != null)
                {
                    //filters.Add(GetRangeFilterExpression("enddate", criteria.EndDate, false, null, false));
                    criteria.Apply(CreateDateRangeFilter("enddate", criteria.EndDate, null, false, false));
                }

                if (!criteria.ClassTypes.IsNullOrEmpty())
                {
                    //result &= CreateQuery("__type", criteria.ClassTypes, false);
                    criteria.Apply(CreateAttributeFilter("__type", criteria.ClassTypes));
                }

                if (!string.IsNullOrEmpty(criteria.Catalog))
                {
                    //result &= CreateQuery("catalog", criteria.Catalog);
                    criteria.Apply(CreateAttributeFilter("catalog", criteria.Catalog));
                }

                if (!criteria.Outlines.IsNullOrEmpty())
                {
                    var outlines = criteria.Outlines.Select(o => o.TrimEnd('/', '*'));
                    criteria.Apply(CreateAttributeFilter("__outline", outlines));
                }

                if (!criteria.WithHidden)
                {
                    //result &= new TermQuery { Field = "status", Value = "visible" };
                    criteria.Apply(CreateAttributeFilter("status", "visible"));
                }
            }
        }

        protected virtual void AddCategoryFilters(CategorySearchCriteria criteria)
        {
            if (!criteria.Outlines.IsNullOrEmpty())
            {
                var outlines = criteria.Outlines.Select(o => o.TrimEnd('/', '*'));
                criteria.Apply(CreateAttributeFilter("__outline", outlines));
            }
        }


        protected virtual ISearchFilter CreateAttributeFilter(string key, string value)
        {
            return new AttributeFilter
            {
                Key = key,
                Values = new[] { new AttributeFilterValue { Value = value } },
            };
        }

        protected virtual ISearchFilter CreateAttributeFilter(string key, IEnumerable<string> values)
        {
            return new AttributeFilter
            {
                Key = key,
                Values = values.Select(v => new AttributeFilterValue { Value = v }).ToArray(),
            };
        }

        protected virtual ISearchFilter CreateDateRangeFilter(string key, DateTime? lower, DateTime? upper, bool includeLower, bool includeUpper)
        {
            return CreateRangeFilter(key, lower?.ToString("O"), upper?.ToString("O"), includeLower, includeUpper);
        }

        protected virtual ISearchFilter CreateRangeFilter(string key, string lower, string upper, bool includeLower, bool includeUpper)
        {
            return new RangeFilter
            {
                Key = key,
                Values = new[]
                {
                    new RangeFilterValue
                    {
                        Lower = lower,
                        Upper = upper,
                        IncludeLower = includeLower,
                        IncludeUpper = includeUpper,
                    }
                },
            };
        }
    }
}

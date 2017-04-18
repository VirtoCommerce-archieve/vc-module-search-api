using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;
using VirtoCommerce.SearchModule.Data.Providers.AzureSearch;

namespace VirtoCommerce.SearchApiModule.Data.Providers.AzureSearch
{
    [CLSCompliant(false)]
    public class CatalogAzureSearchQueryBuilder : AzureSearchQueryBuilder
    {
        protected override void AddFilters(ISearchCriteria criteria, IList<string> filters, IList<IFieldDescriptor> availableFields)
        {
            base.AddFilters(criteria, filters, availableFields);

            var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
            var categorySearchCriteria = criteria as CategorySearchCriteria;

            if (catalogItemSearchCriteria != null)
            {
                AddCatalogItemFilters(catalogItemSearchCriteria, filters);
            }
            else if (categorySearchCriteria != null)
            {
                AddCategoryFilters(categorySearchCriteria, filters);
            }
        }

        protected virtual void AddCategoryFilters(CategorySearchCriteria criteria, IList<string> filters)
        {
            if (criteria?.Outlines != null && criteria.Outlines.Any())
            {
                var outlines = criteria.Outlines.Select(o => o.TrimEnd('/', '*'));
                var filter = GetContainsFilterExpression("__outline", outlines);
                filters.Add(filter);
            }
        }

        protected virtual void AddCatalogItemFilters(CatalogItemSearchCriteria criteria, IList<string> filters)
        {
            if (criteria != null)
            {
                filters.Add(GetRangeFilterExpression("startdate", null, false, criteria.StartDate, true));

                if (criteria.StartDateFrom != null)
                {
                    filters.Add(GetRangeFilterExpression("startdate", criteria.StartDateFrom, false, null, false));
                }

                if (criteria.EndDate != null)
                {
                    filters.Add(GetRangeFilterExpression("enddate", criteria.EndDate, false, null, false));
                }

                if (!criteria.ClassTypes.IsNullOrEmpty())
                {
                    var filter = GetEqualsFilterExpression("__type", criteria.ClassTypes, false);
                    filters.Add(filter);
                }

                if (!string.IsNullOrEmpty(criteria.Catalog))
                {
                    var filter = GetContainsFilterExpression("catalog", new[] { criteria.Catalog });
                    filters.Add(filter);
                }

                if (!criteria.Outlines.IsNullOrEmpty())
                {
                    var outlines = criteria.Outlines.Select(o => o.TrimEnd('/', '*'));
                    var filter = GetContainsFilterExpression("__outline", outlines);
                    filters.Add(filter);
                }

                if (!criteria.WithHidden)
                {
                    var filter = GetEqualsFilterExpression("status", new[] { "visible" }, false);
                    filters.Add(filter);
                }
            }
        }
    }
}

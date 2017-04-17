using Nest;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Search.Criteria;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch;

namespace VirtoCommerce.SearchApiModule.Data.Providers.ElasticSearch
{
    public class CatalogElasticSearchQueryBuilder : ElasticSearchQueryBuilder
    {
        protected override QueryContainer GetQuery<T>(ISearchCriteria criteria)
        {
            var result = base.GetQuery<T>(criteria);

            var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
            var categorySearchCriteria = criteria as CategorySearchCriteria;

            if (catalogItemSearchCriteria != null)
            {
                result &= GetCatalogItemQuery<T>(catalogItemSearchCriteria);
            }
            else if (categorySearchCriteria != null)
            {
                result &= GetCategoryQuery<T>(categorySearchCriteria);
            }

            return result;
        }

        protected virtual QueryContainer GetCategoryQuery<T>(CategorySearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            if (criteria?.Outlines != null && criteria.Outlines.Any())
            {
                result = CreateQuery("__outline", criteria.Outlines, true);
            }

            return result;
        }

        protected virtual QueryContainer GetCatalogItemQuery<T>(CatalogItemSearchCriteria criteria)
            where T : class
        {
            QueryContainer result = null;

            if (criteria != null)
            {
                result &= new DateRangeQuery { Field = "startdate", LessThanOrEqualTo = criteria.StartDate };

                if (criteria.StartDateFrom.HasValue)
                {
                    result &= new DateRangeQuery { Field = "startdate", GreaterThan = criteria.StartDateFrom.Value };
                }

                if (criteria.EndDate.HasValue)
                {
                    result &= new DateRangeQuery { Field = "enddate", GreaterThan = criteria.EndDate.Value };
                }

                if (!criteria.ClassTypes.IsNullOrEmpty())
                {
                    result &= CreateQuery("__type", criteria.ClassTypes, false);
                }

                if (!string.IsNullOrEmpty(criteria.Catalog))
                {
                    result &= CreateQuery("catalog", criteria.Catalog);
                }

                if (!criteria.Outlines.IsNullOrEmpty())
                {
                    result &= CreateQuery("__outline", criteria.Outlines, true);
                }

                if (!criteria.WithHidden)
                {
                    result &= new TermQuery { Field = "status", Value = "visible" };
                }
            }

            return result;
        }
    }
}

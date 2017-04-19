using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Providers.LuceneSearch;

namespace VirtoCommerce.SearchApiModule.Data.Providers.LuceneSearch
{
    public class CatalogLuceneSearchQueryBuilder : LuceneSearchQueryBuilder
    {
        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="availableFields"></param>
        /// <returns></returns>
        public override object BuildQuery<T>(string scope, ISearchCriteria criteria, IList<IFieldDescriptor> availableFields)
        {
            var result = base.BuildQuery<T>(scope, criteria, availableFields) as LuceneSearchQuery;
            var query = result?.Query as BooleanQuery;

            var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
            var categorySearchCriteria = criteria as CategorySearchCriteria;

            if (catalogItemSearchCriteria != null)
            {
                ApplyCatalogItemFilters<T>(catalogItemSearchCriteria, query);
            }
            else if (categorySearchCriteria != null)
            {
                ApplyCategoryFilters<T>(categorySearchCriteria, query);
            }

            return result;
        }

        protected virtual void ApplyCategoryFilters<T>(CategorySearchCriteria criteria, BooleanQuery query)
        {
            if (criteria != null && !criteria.Outlines.IsNullOrEmpty())
            {
                AddQuery("__outline", query, criteria.Outlines);
            }
        }

        protected virtual void ApplyCatalogItemFilters<T>(CatalogItemSearchCriteria criteria, BooleanQuery query)
        {
            if (criteria != null)
            {
                if (!criteria.ClassTypes.IsNullOrEmpty())
                {
                    AddQuery("__type", query, criteria.ClassTypes);
                }

                if (!string.IsNullOrEmpty(criteria.Catalog))
                {
                    AddWildcardQuery("catalog", query, criteria.Catalog);
                }

                if (criteria.Outlines != null && criteria.Outlines.Count > 0)
                {
                    AddQuery("__outline", query, criteria.Outlines);
                }

                if (!criteria.WithHidden)
                {
                    query.Add(new TermQuery(new Term("status", "visible")), Occur.MUST);
                }

                var datesFilterStart = new TermRangeQuery("startdate", criteria.StartDateFrom.HasValue ? DateTools.DateToString(criteria.StartDateFrom.Value, DateTools.Resolution.SECOND) : null, DateTools.DateToString(criteria.StartDate, DateTools.Resolution.SECOND), false, true);
                query.Add(datesFilterStart, Occur.MUST);

                if (criteria.EndDate.HasValue)
                {
                    var datesFilterEnd = new TermRangeQuery("enddate", DateTools.DateToString(criteria.EndDate.Value, DateTools.Resolution.SECOND), null, true, false);
                    query.Add(datesFilterEnd, Occur.MUST);
                }
            }
        }
    }
}

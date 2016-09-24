using System;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using u = Lucene.Net.Util;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;
using VirtoCommerce.SearchApiModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Web.Providers.Lucene
{
    public class CatalogLuceneQueryBuilder : LuceneSearchQueryBuilder
    {
        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public override object BuildQuery<T>(string scope, ISearchCriteria criteria)
        {
            var builder = base.BuildQuery<T>(scope, criteria) as QueryBuilder;
            var query = builder.Query as BooleanQuery;
            var analyzer = new StandardAnalyzer(u.Version.LUCENE_30);

            var fuzzyMinSimilarity = 0.7f;
            var isFuzzySearch = false;

            #region CategorySearchCriteria
            if (criteria is CategorySearchCriteria)
            {
                var c = criteria as CategorySearchCriteria;
                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    AddQuery("__outline", query, c.Outlines);
                }
            }
            #endregion

            #region CatalogItemSearchCriteria
            if (criteria is CatalogItemSearchCriteria)
            {
                var c = criteria as CatalogItemSearchCriteria;
                var datesFilterStart = new TermRangeQuery(
                    "startdate", c.StartDateFrom.HasValue ? DateTools.DateToString(c.StartDateFrom.Value, DateTools.Resolution.SECOND) : null, DateTools.DateToString(c.StartDate, DateTools.Resolution.SECOND), false, true);
                query.Add(datesFilterStart, Occur.MUST);

                if (c.EndDate.HasValue)
                {
                    var datesFilterEnd = new TermRangeQuery(
                        "enddate",
                        DateTools.DateToString(c.EndDate.Value, DateTools.Resolution.SECOND),
                        null,
                        true,
                        false);

                    query.Add(datesFilterEnd, Occur.MUST);
                }

                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    AddQuery("__outline", query, c.Outlines);
                }

                query.Add(new TermQuery(new Term("__hidden", "false")), Occur.MUST);

                if (!String.IsNullOrEmpty(c.Catalog))
                {
                    AddQuery("catalog", query, c.Catalog);
                }

                fuzzyMinSimilarity = c.FuzzyMinSimilarity;
                isFuzzySearch = c.IsFuzzySearch;
            }
            #endregion

            // add standard keyword search

            //else if (criteria is OrderSearchCriteria)
            //{
            //	var c = criteria as OrderSearchCriteria;

            //	if (!String.IsNullOrEmpty(c.CustomerId))
            //	{
            //		AddQuery("customerid", query, c.CustomerId);
            //	}
            //}

            return builder;
        }

    }
}
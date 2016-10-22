using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Providers.Lucene;
using u = Lucene.Net.Util;

namespace VirtoCommerce.SearchApiModule.Data.Providers.Lucene
{
    public class CatalogLuceneQueryBuilder : LuceneSearchQueryBuilder
    {
        /// <summary>
        ///     Builds the query.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public override object BuildQuery<T>(string scope, ISearchCriteria criteria)
        {
            var builder = base.BuildQuery<T>(scope, criteria) as QueryBuilder;
            var query = builder.Query as BooleanQuery;
            var analyzer = new StandardAnalyzer(u.Version.LUCENE_30);

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

                if (!c.WithHidden)
                {
                    query.Add(new TermQuery(new Term("__hidden", "false")), Occur.MUST);
                }

                if (!string.IsNullOrEmpty(c.Catalog))
                {
                    AddQuery("catalog", query, c.Catalog);
                }
            }

            #endregion

            #region SimpleCatalogItemSearchCriteria

            if (criteria is SimpleCatalogItemSearchCriteria)
            {
                var c = criteria as SimpleCatalogItemSearchCriteria;
                var parser = new QueryParser(u.Version.LUCENE_30, "__content", analyzer);
                var parsedQuery = parser.Parse(c.RawQuery);
                query.Add(parsedQuery, Occur.MUST);
            }

            #endregion

            return builder;
        }
    }
}

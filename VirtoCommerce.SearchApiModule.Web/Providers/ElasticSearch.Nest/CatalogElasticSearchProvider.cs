using Nest;
using System;
using VirtoCommerce.SearchApiModule.Web.Model;
using VirtoCommerce.SearchModule.Data.Model.Search.Criterias;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch.Nest;
using VirtoCommerce.SearchModule.Data.Model;
using VirtoCommerce.SearchApiModule.Web.Services;

namespace VirtoCommerce.SearchApiModule.Web.Providers.ElasticSearch.Nest
{
    [CLSCompliant(false)]
    public class CatalogElasticSearchProvider : ElasticSearchProvider, ICatalogIndexedSearchProvider
    {
        public CatalogElasticSearchProvider(ISearchConnection connection)
            :base(connection)
        {

        }
        #region ISearchQueryBuilder Members
        public override SearchRequest BuildQuery<T>(string scope, ISearchCriteria criteria)
        {            
            var builder = base.BuildQuery<T>(scope, criteria);

            #region CategorySearchCriteria
            if (criteria is CategorySearchCriteria)
            {
                var c = criteria as CategorySearchCriteria;
                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    builder.Query &= CreateQuery("__outline", c.Outlines);
                }
            }
            #endregion

            #region CatalogItemSearchCriteria
            if (criteria is CatalogItemSearchCriteria)
            {
                var c = criteria as CatalogItemSearchCriteria;

                builder.Query &= new DateRangeQuery() { Field = "startdate", LessThanOrEqualTo = c.StartDate };

                if (c.StartDateFrom.HasValue)
                {
                    builder.Query &= new DateRangeQuery() { Field = "startdate", GreaterThan = c.StartDateFrom.Value };
                }

                if (c.EndDate.HasValue)
                {
                    builder.Query &= new DateRangeQuery() { Field = "enddate", GreaterThan = c.StartDateFrom.Value };
                }

                builder.Query &= new TermQuery() { Field = "__hidden", Value = false };

                if (c.Outlines != null && c.Outlines.Count > 0)
                {
                    builder.Query &= CreateQuery("__outline", c.Outlines);
                }

                if (!string.IsNullOrEmpty(c.Catalog))
                {
                    builder.Query &= CreateQuery("catalog", c.Catalog);
                }

                if (c.ClassTypes != null && c.ClassTypes.Count > 0)
                {
                    builder.Query &= CreateQuery("__type", c.ClassTypes, false);
                }
            }
            #endregion

            return builder;
        }
        #endregion
    }
}

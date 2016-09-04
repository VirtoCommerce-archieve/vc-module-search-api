using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VirtoCommerce.CatalogModule.Web.Model;
using VirtoCommerce.Domain.Catalog.Model;

namespace VirtoCommerce.SearchApiModule.Web.Model
{
    public class ProductSearchResult
    {
        public ProductSearchResult()
        {

        }

        public Domain.Catalog.Model.Aggregation[] Aggregations { get; set; }

        public Product[] Products { get; set; }

        public int ProductsTotalCount { get; set; }
    }
}
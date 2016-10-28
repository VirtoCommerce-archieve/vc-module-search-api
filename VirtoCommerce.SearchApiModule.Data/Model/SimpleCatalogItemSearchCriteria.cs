using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class SimpleCatalogItemSearchCriteria : CatalogItemSearchCriteria
    {
        public virtual string RawQuery { get;set; }
    }
}

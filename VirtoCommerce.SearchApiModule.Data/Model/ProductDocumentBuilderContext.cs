using System.Collections.Generic;
using VirtoCommerce.Domain.Pricing.Model;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    public class ProductDocumentBuilderContext
    {
        public IList<Price> Prices { get; set; }
    }
}

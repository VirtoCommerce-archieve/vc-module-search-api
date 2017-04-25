using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ProductBatchDocumentBuilder : IBatchDocumentBuilder<CatalogProduct>
    {
        private readonly IDocumentBuilder<CatalogProduct>[] _documentBuilders;

        public ProductBatchDocumentBuilder(params IDocumentBuilder<CatalogProduct>[] documentBuilders)
        {
            _documentBuilders = documentBuilders;
        }

        public void UpdateDocuments(IList<IDocument> documents, IList<CatalogProduct> items, object context)
        {
            if (_documentBuilders != null && documents != null && items != null && documents.Count == items.Count)
            {
                var prices = context as IList<Price>;

                for (var i = 0; i < documents.Count; i++)
                {
                    var document = documents[i];
                    var product = items[i];

                    if (document != null && product != null)
                    {
                        var productPrices = prices?.Where(p => p.ProductId == product.Id).ToArray();

                        var shouldIndex = true;

                        foreach (var documentBuilder in _documentBuilders)
                        {
                            shouldIndex &= documentBuilder.UpdateDocument(document, product, productPrices);
                        }

                        if (!shouldIndex)
                        {
                            documents[i] = null;
                        }
                    }
                }
            }
        }
    }
}

using System.Collections.Generic;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CategoryBatchDocumentBuilder : IBatchDocumentBuilder<Category>
    {
        private readonly IDocumentBuilder<Category>[] _documentBuilders;

        public CategoryBatchDocumentBuilder(IDocumentBuilder<Category>[] documentBuilders)
        {
            _documentBuilders = documentBuilders;
        }

        public void UpdateDocuments(IList<IDocument> documents, IList<Category> items, object context)
        {
            if (_documentBuilders != null && documents != null && items != null && documents.Count == items.Count)
            {
                for (var i = 0; i < documents.Count; i++)
                {
                    var document = documents[i];
                    var product = items[i];

                    if (document != null && product != null)
                    {
                        var shouldIndex = true;

                        foreach (var documentBuilder in _documentBuilders)
                        {
                            shouldIndex &= documentBuilder.UpdateDocument(document, product, null);
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

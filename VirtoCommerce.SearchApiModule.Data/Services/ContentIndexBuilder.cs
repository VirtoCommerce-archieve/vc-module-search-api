using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ContentIndexBuilder : ISearchIndexBuilder
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ISettingsManager _settingsManager;
        private readonly IBlobStorageProvider _storageProvider;

        public string DocumentType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ContentIndexBuilder(
            IBlobStorageProvider storageProvider,
            ISearchProvider searchProvider, 
            ISettingsManager settingsManager)
        {
            _searchProvider = searchProvider;
            _settingsManager = settingsManager;
            _storageProvider = storageProvider;
        }

        public IEnumerable<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException("partition");

            var documents = new ConcurrentBag<IDocument>();

            /*
            if (!partition.Keys.IsNullOrEmpty())
            {
                var items = GetItems(partition.Keys);
                var prices = GetItemPrices(partition.Keys);

                foreach (var item in items)
                {
                    var doc = new ResultDocument();
                    var itemPrices = prices.Where(x => x.ProductId == item.Id).ToArray();
                    var index = IndexItem(doc, item, itemPrices);

                    if (index)
                    {
                        documents.Add(doc);
                    }
                }
            }
            */

            return documents;
        }

        public IEnumerable<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            /*
            var partitions = rebuild || startDate == DateTime.MinValue
                ? GetPartitionsForAll()
                : GetPartitionsForModified(startDate, endDate);

            return partitions;
            */

            return null;
        }

        public void PublishDocuments(string scope, IDocument[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Index(scope, DocumentType, doc);
            }

            _searchProvider.Commit(scope);
            _searchProvider.Close(scope, DocumentType);
        }

        public void RemoveAll(string scope)
        {
            _searchProvider.RemoveAll(scope, DocumentType);
        }

        public void RemoveDocuments(string scope, string[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Remove(scope, DocumentType, "__key", doc);
            }
            _searchProvider.Commit(scope);
        }
    }
}

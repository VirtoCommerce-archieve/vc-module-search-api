using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ContentIndexBuilder : ISearchIndexBuilder
    {
        private readonly ISearchProvider _searchProvider;
        private readonly IBlobStorageProvider _storageProvider;

        public string DocumentType => "page";

        public ContentIndexBuilder(
            IBlobStorageProvider storageProvider,
            ISearchProvider searchProvider)
        {
            _searchProvider = searchProvider;
            _storageProvider = storageProvider;
        }

        public IList<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            var documents = new List<IDocument>();

            if (!partition.Keys.IsNullOrEmpty())
            {
                //var files = GetFiles(partition.Keys);

                //foreach (var file in files)
                //{
                //    var doc = new ResultDocument();
                //    var itemPrices = prices.Where(x => x.ProductId == item.Id).ToArray();
                //    var index = IndexItem(doc, item, itemPrices);

                //    if (index)
                //    {
                //        documents.Add(doc);
                //    }
                //}
            }

            return documents;
        }

        public IList<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            return GetPartitionsForAllPages();
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


        private IList<Partition> GetPartitionsForAllPages()
        {
            var partitions = new List<Partition>();

            var result = _storageProvider.Search("Pages", string.Empty);
            var files = result.Items.Select(x => x.RelativeUrl).ToArray();
            partitions.Add(new Partition(OperationType.Index, files));
            return partitions;
        }

        //private IEnumerable<Partition> GetPartitionsForModifiedPages(DateTime startDate, DateTime endDate)
        //{
        //    var partitions = new List<Partition>();

        //    var categoryChanges = GetCategoryChanges(startDate, endDate);
        //    var deletedCategoryIds = categoryChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
        //    var modifiedCategoryIds = categoryChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

        //    partitions.AddRange(CreatePartitions(OperationType.Remove, deletedCategoryIds));
        //    partitions.AddRange(CreatePartitions(OperationType.Index, modifiedCategoryIds));

        //    return partitions;
        //}

        //protected virtual IList<CatalogProduct> GetFiles(string[] fileRealtiveUrls)
        //{
        //    return _itemService.GetByIds(itemIds, ItemResponseGroup.ItemProperties | ItemResponseGroup.Variations | ItemResponseGroup.ItemEditorialReviews | ItemResponseGroup.Outlines);
        //}
    }
}

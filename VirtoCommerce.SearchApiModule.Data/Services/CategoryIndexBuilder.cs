using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CategoryIndexBuilder : ISearchIndexBuilder
    {
        private const int _partitionSizeCount = 100; // the maximum partition size, keep it smaller to prevent too big of the sql requests and too large messages in the queue

        private readonly ISearchProvider _searchProvider;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ICategoryService _categoryService;
        private readonly IChangeLogService _changeLogService;
        private readonly IDocumentBuilder<Category, object>[] _documentBuilders;

        public CategoryIndexBuilder(
            ISearchProvider searchProvider,
            ICatalogSearchService catalogSearchService,
            ICategoryService categoryService,
            IChangeLogService changeLogService,
            params IDocumentBuilder<Category, object>[] documentBuilders)
        {
            _searchProvider = searchProvider;
            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _changeLogService = changeLogService;
            _documentBuilders = documentBuilders;
        }

        #region ISearchIndexBuilder Members

        public string DocumentType => "category";

        public IEnumerable<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            var partitions = (rebuild || startDate == DateTime.MinValue)
                ? GetPartitionsForAllCategories()
                : GetPartitionsForModifiedCategories(startDate, endDate);

            return partitions;
        }

        public IEnumerable<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            var documents = new ConcurrentBag<IDocument>();

            if (_documentBuilders != null && !partition.Keys.IsNullOrEmpty())
            {
                var categories = _categoryService.GetByIds(partition.Keys, CategoryResponseGroup.WithProperties | CategoryResponseGroup.WithOutlines | CategoryResponseGroup.WithImages | CategoryResponseGroup.WithSeo);
                foreach (var category in categories)
                {
                    var shouldIndex = true;
                    var doc = new ResultDocument();

                    foreach (var documentBuilder in _documentBuilders)
                    {
                        shouldIndex &= documentBuilder.UpdateDocument(doc, category, null);
                    }

                    if (shouldIndex)
                    {
                        documents.Add(doc);
                    }
                }
            }

            return documents;
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

        public void RemoveDocuments(string scope, string[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Remove(scope, DocumentType, "__key", doc);
            }
            _searchProvider.Commit(scope);
        }

        public void RemoveAll(string scope)
        {
            _searchProvider.RemoveAll(scope, DocumentType);
        }

        #endregion

        [Obsolete("Use CategoryDocumentBuilder", true)]
        protected virtual void IndexItem(ResultDocument doc, Category category)
        {
        }

        private IEnumerable<Partition> GetPartitionsForAllCategories()
        {
            var partitions = new List<Partition>();

            var criteria = new SearchCriteria
            {
                ResponseGroup = SearchResponseGroup.WithCategories,
                Take = 0
            };

            var result = _catalogSearchService.Search(criteria);

            // TODO: add paging for categories
            var categoryIds = result.Categories.Select(c => c.Id).ToArray();
            partitions.Add(new Partition(OperationType.Index, categoryIds));
            return partitions;
        }

        private IEnumerable<Partition> GetPartitionsForModifiedCategories(DateTime startDate, DateTime endDate)
        {
            var partitions = new List<Partition>();

            var categoryChanges = GetCategoryChanges(startDate, endDate);
            var deletedCategoryIds = categoryChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
            var modifiedCategoryIds = categoryChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

            partitions.AddRange(CreatePartitions(OperationType.Remove, deletedCategoryIds));
            partitions.AddRange(CreatePartitions(OperationType.Index, modifiedCategoryIds));

            return partitions;
        }

        private List<OperationLog> GetCategoryChanges(DateTime startDate, DateTime endDate)
        {
            var allCategoryChanges = _changeLogService.FindChangeHistory("Category", startDate, endDate).ToList();

            // Return latest operation type for each product
            var result = allCategoryChanges
                .GroupBy(c => c.ObjectId)
                .Select(g => new OperationLog { ObjectId = g.Key, OperationType = g.OrderByDescending(c => c.ModifiedDate).Select(c => c.OperationType).First() })
                .ToList();

            return result;
        }

        private static IEnumerable<Partition> CreatePartitions(OperationType operationType, List<string> allCategoriesIds)
        {
            var partitions = new List<Partition>();

            var totalCount = allCategoriesIds.Count;

            for (var start = 0; start < totalCount; start += _partitionSizeCount)
            {
                var categoryIds = allCategoriesIds.Skip(start).Take(_partitionSizeCount).ToArray();
                partitions.Add(new Partition(operationType, categoryIds));
            }

            return partitions;
        }
    }
}

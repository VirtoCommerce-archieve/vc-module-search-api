using System;
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
        private readonly IBatchDocumentBuilder<Category>[] _batchDocumentBuilders;

        public CategoryIndexBuilder(
            ISearchProvider searchProvider,
            ICatalogSearchService catalogSearchService,
            ICategoryService categoryService,
            IChangeLogService changeLogService,
            params IBatchDocumentBuilder<Category>[] batchDocumentBuilders)
        {
            _searchProvider = searchProvider;
            _catalogSearchService = catalogSearchService;
            _categoryService = categoryService;
            _changeLogService = changeLogService;
            _batchDocumentBuilders = batchDocumentBuilders;
        }

        #region ISearchIndexBuilder Members

        public string DocumentType => "category";

        public IList<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            var partitions = (rebuild || startDate == DateTime.MinValue)
                ? GetPartitionsForAllCategories()
                : GetPartitionsForModifiedCategories(startDate, endDate);

            return partitions;
        }

        public IList<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            var result = new List<IDocument>();

            if (_batchDocumentBuilders != null && !partition.Keys.IsNullOrEmpty())
            {
                var documents = partition.Keys.Select(k => new ResultDocument() as IDocument).ToList();
                var categories = GetCategories(partition.Keys);

                foreach (var batchDocumentBuilder in _batchDocumentBuilders)
                {
                    batchDocumentBuilder.UpdateDocuments(documents, categories, null);
                }

                result.AddRange(documents.Where(d => d != null));
            }

            return result;
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

        protected virtual Category[] GetCategories(string[] categoryIds)
        {
            return _categoryService.GetByIds(categoryIds, CategoryResponseGroup.WithProperties | CategoryResponseGroup.WithOutlines | CategoryResponseGroup.WithImages | CategoryResponseGroup.WithSeo);
        }

        [Obsolete("Use CategoryDocumentBuilder", true)]
        protected virtual void IndexItem(ResultDocument doc, Category category)
        {
        }

        protected virtual IList<Partition> GetPartitionsForAllCategories()
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

        protected virtual IList<Partition> GetPartitionsForModifiedCategories(DateTime startDate, DateTime endDate)
        {
            var partitions = new List<Partition>();

            var categoryChanges = GetCategoryChanges(startDate, endDate);
            var deletedCategoryIds = categoryChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
            var modifiedCategoryIds = categoryChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

            partitions.AddRange(CreatePartitions(OperationType.Remove, deletedCategoryIds));
            partitions.AddRange(CreatePartitions(OperationType.Index, modifiedCategoryIds));

            return partitions;
        }

        protected virtual List<OperationLog> GetCategoryChanges(DateTime startDate, DateTime endDate)
        {
            var allCategoryChanges = _changeLogService.FindChangeHistory("Category", startDate, endDate).ToList();

            // Return latest operation type for each product
            var result = allCategoryChanges
                .GroupBy(c => c.ObjectId)
                .Select(g => new OperationLog { ObjectId = g.Key, OperationType = g.OrderByDescending(c => c.ModifiedDate).Select(c => c.OperationType).First() })
                .ToList();

            return result;
        }

        protected virtual IEnumerable<Partition> CreatePartitions(OperationType operationType, List<string> allCategoriesIds)
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Extensions;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ProductIndexBuilder : ISearchIndexBuilder
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IItemService _itemService;
        private readonly IPricingService _pricingService;
        private readonly IOperationProvider[] _operationProviders;
        private readonly IBatchDocumentBuilder<CatalogProduct>[] _batchDocumentBuilders;

        public ProductIndexBuilder(
            ISearchProvider searchProvider,
            ICatalogSearchService catalogSearchService,
            IItemService itemService,
            IPricingService pricingService,
            IOperationProvider[] operationProviders,
            IBatchDocumentBuilder<CatalogProduct>[] batchDocumentBuilders)
        {
            _searchProvider = searchProvider;
            _catalogSearchService = catalogSearchService;
            _itemService = itemService;
            _pricingService = pricingService;
            _operationProviders = operationProviders;
            _batchDocumentBuilders = batchDocumentBuilders;
        }

        /// <summary>
        /// The maximum items count per partition.
        /// Keep it smaller to prevent too large SQL requests and too large messages in the queue.
        /// </summary>
        protected virtual int PartitionSize => 500;

        #region ISearchIndexBuilder Members

        public virtual string DocumentType => CatalogItemSearchCriteria.DocType;

        public virtual IList<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            var partitions = rebuild || startDate == DateTime.MinValue
                ? GetPartitionsForAllProducts()
                : GetPartitionsForModifiedProducts(startDate, endDate);

            return partitions;
        }

        public virtual IList<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException(nameof(partition));

            //Trace.TraceInformation(string.Format("Processing documents starting {0} of {1} - {2}%", partition.Start, partition.Total, (partition.Start * 100 / partition.Total)));

            var result = new List<IDocument>();

            if (_batchDocumentBuilders != null && !partition.Keys.IsNullOrEmpty())
            {
                var documents = partition.Keys.Select(k => new ResultDocument() as IDocument).ToList();
                var products = GetItems(partition.Keys);
                var prices = GetItemPrices(partition.Keys);

                foreach (var batchDocumentBuilder in _batchDocumentBuilders)
                {
                    batchDocumentBuilder.UpdateDocuments(documents, products, prices);
                }

                result.AddRange(documents.Where(d => d != null));
            }

            return result;
        }

        public virtual void PublishDocuments(string scope, IDocument[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Index(scope, DocumentType, doc);
            }

            _searchProvider.Commit(scope);
            _searchProvider.Close(scope, DocumentType);
        }

        public virtual void RemoveDocuments(string scope, string[] documents)
        {
            foreach (var doc in documents)
            {
                _searchProvider.Remove(scope, DocumentType, "__key", doc);
            }
            _searchProvider.Commit(scope);
        }

        public virtual void RemoveAll(string scope)
        {
            _searchProvider.RemoveAll(scope, DocumentType);
        }

        #endregion

        protected virtual IList<CatalogProduct> GetItems(string[] itemIds)
        {
            return _itemService.GetByIds(itemIds, ItemResponseGroup.ItemProperties | ItemResponseGroup.Variations | ItemResponseGroup.Outlines | ItemResponseGroup.Seo);
        }

        protected virtual IList<Price> GetItemPrices(string[] itemIds)
        {
            var evalContext = new PriceEvaluationContext { ProductIds = itemIds };
            return _pricingService.EvaluateProductPrices(evalContext).ToList();
        }

        [Obsolete("Use ProductDocumentBuilder", true)]
        protected virtual bool IndexItem(ResultDocument doc, CatalogProduct item, Price[] prices)
        {
            return false;
        }

        #region Price Lists Indexing

        #endregion

        protected virtual IList<Partition> GetPartitionsForAllProducts()
        {
            var partitions = new ConcurrentBag<Partition>();

            var result = _catalogSearchService.Search(new SearchCriteria { Take = 0, ResponseGroup = SearchResponseGroup.WithProducts, WithHidden = true });
            var parts = result.ProductsTotalCount / PartitionSize + 1;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

            Parallel.For(0, parts, parallelOptions, index =>
            {
                var criteria = new SearchCriteria
                {
                    Skip = index * PartitionSize,
                    Take = PartitionSize,
                    ResponseGroup = SearchResponseGroup.WithProducts,
                    WithHidden = true
                };

                // TODO: Need to optimize search to return only product IDs
                result = _catalogSearchService.Search(criteria);

                var productIds = result.Products.Select(p => p.Id).ToArray();
                partitions.Add(new Partition(OperationType.Index, productIds));
            });

            return partitions.ToList();
        }

        protected virtual IList<Partition> GetPartitionsForModifiedProducts(DateTime startDate, DateTime endDate)
        {
            var operations = _operationProviders.GetLatestIndexOperationForEachObject(DocumentType, startDate, endDate);

            var partitions = operations.GroupBy(o => o.OperationType)
                .SelectMany(g => CreatePartitions(g.Key, g.Select(o => o.ObjectId).ToList()))
                .ToList();

            return partitions;
        }

        protected virtual IEnumerable<Partition> CreatePartitions(OperationType operationType, IList<string> allProductIds)
        {
            var partitions = new List<Partition>();

            var totalCount = allProductIds.Count;

            for (var start = 0; start < totalCount; start += PartitionSize)
            {
                var productIds = allProductIds.Skip(start).Take(PartitionSize).ToArray();
                partitions.Add(new Partition(operationType, productIds));
            }

            return partitions;
        }
    }
}

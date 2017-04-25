using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CatalogItemIndexBuilder : ISearchIndexBuilder
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IItemService _itemService;
        private readonly IPricingService _pricingService;
        private readonly IChangeLogService _changeLogService;
        private readonly IBatchDocumentBuilder<CatalogProduct>[] _batchDocumentBuilders;

        public CatalogItemIndexBuilder(
            ISearchProvider searchProvider,
            ICatalogSearchService catalogSearchService,
            IItemService itemService,
            IPricingService pricingService,
            IChangeLogService changeLogService,
            params IBatchDocumentBuilder<CatalogProduct>[] batchDocumentBuilders)
        {
            _searchProvider = searchProvider;
            _catalogSearchService = catalogSearchService;
            _itemService = itemService;
            _pricingService = pricingService;
            _changeLogService = changeLogService;
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

                // TODO: Need optimize search to return only product ids
                result = _catalogSearchService.Search(criteria);

                var productIds = result.Products.Select(p => p.Id).ToArray();
                partitions.Add(new Partition(OperationType.Index, productIds));
            });

            return partitions.ToList();
        }

        protected virtual IList<Partition> GetPartitionsForModifiedProducts(DateTime startDate, DateTime endDate)
        {
            var partitions = new List<Partition>();

            var productChanges = GetProductChanges(startDate, endDate);
            var deletedProductIds = productChanges.Where(c => c.OperationType == EntryState.Deleted).Select(c => c.ObjectId).ToList();
            var modifiedProductIds = productChanges.Where(c => c.OperationType != EntryState.Deleted).Select(c => c.ObjectId).ToList();

            partitions.AddRange(CreatePartitions(OperationType.Remove, deletedProductIds));
            partitions.AddRange(CreatePartitions(OperationType.Index, modifiedProductIds));

            return partitions;
        }

        protected virtual List<OperationLog> GetProductChanges(DateTime startDate, DateTime endDate)
        {
            var allProductChanges = _changeLogService.FindChangeHistory("Item", startDate, endDate).ToList();
            var allPriceChanges = _changeLogService.FindChangeHistory("Price", startDate, endDate).ToList();

            var priceIds = allPriceChanges.Select(c => c.ObjectId).ToArray();
            var prices = GetPrices(priceIds);

            // TODO: How to get product for deleted price?
            var productsWithChangedPrice = allPriceChanges
                .Select(c => new { c.ModifiedDate, Price = prices.ContainsKey(c.ObjectId) ? prices[c.ObjectId] : null })
                .Where(x => x.Price != null)
                .Select(x => new OperationLog { ObjectId = x.Price.ProductId, ModifiedDate = x.ModifiedDate, OperationType = EntryState.Modified })
                .ToList();

            allProductChanges.AddRange(productsWithChangedPrice);

            // Return latest operation type for each product
            var result = allProductChanges
                .GroupBy(c => c.ObjectId)
                .Select(g => new OperationLog { ObjectId = g.Key, OperationType = g.OrderByDescending(c => c.ModifiedDate).Select(c => c.OperationType).First() })
                .ToList();

            return result;
        }

        protected virtual IDictionary<string, Price> GetPrices(ICollection<string> priceIds)
        {
            // TODO: Get pageSize and degreeOfParallelism from settings
            return GetPricesWithPagingAndParallelism(priceIds, 1000, 10);
        }

        protected virtual IDictionary<string, Price> GetPricesWithPagingAndParallelism(ICollection<string> priceIds, int pageSize, int degreeOfParallelism)
        {
            IDictionary<string, Price> result;

            if (degreeOfParallelism > 1)
            {
                var dictionary = new ConcurrentDictionary<string, Price>();

                var pages = new List<string[]>();
                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) => pages.Add(ids.ToArray()));

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism };

                Parallel.ForEach(pages, parallelOptions, ids =>
                {
                    var prices = _pricingService.GetPricesById(ids);
                    foreach (var price in prices)
                    {
                        dictionary.AddOrUpdate(price.Id, price, (key, oldValue) => price);
                    }
                });

                result = dictionary;
            }
            else
            {
                var dictionary = new Dictionary<string, Price>();

                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) =>
                {
                    foreach (var price in _pricingService.GetPricesById(ids.ToArray()))
                    {
                        dictionary[price.Id] = price;
                    }
                });

                result = dictionary;
            }

            return result;
        }

        protected virtual IEnumerable<Partition> CreatePartitions(OperationType operationType, List<string> allProductIds)
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

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Domain.Pricing.Model;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using System.IO;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CatalogItemIndexBuilder : ISearchIndexBuilder
    {
        private readonly ISearchProvider _searchProvider;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly IPricingService _pricingService;
        private readonly IItemService _itemService;
        private readonly IChangeLogService _changeLogService;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ISettingsManager _settingsManager;

        public CatalogItemIndexBuilder(
            ISearchProvider searchProvider,
            ICatalogSearchService catalogSearchService,
            IItemService itemService,
            IPricingService pricingService,
            IChangeLogService changeLogService,
            IBlobUrlResolver blobUrlResolver, ISettingsManager settingsManager)
        {
            _searchProvider = searchProvider;
            _catalogSearchService = catalogSearchService;
            _itemService = itemService;
            _pricingService = pricingService;
            _changeLogService = changeLogService;
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingsManager;
        }

        /// <summary>
        /// The maximum items count per partition.
        /// Keep it smaller to prevent too large SQL requests and too large messages in the queue.
        /// </summary>
        protected virtual int PartitionSize { get { return 500; } }

        #region ISearchIndexBuilder Members

        public virtual string DocumentType
        {
            get
            {
                return CatalogItemSearchCriteria.DocType;
            }
        }

        public virtual IEnumerable<Partition> GetPartitions(bool rebuild, DateTime startDate, DateTime endDate)
        {
            var partitions = rebuild || startDate == DateTime.MinValue
                ? GetPartitionsForAllProducts()
                : GetPartitionsForModifiedProducts(startDate, endDate);

            return partitions;
        }

        public virtual IEnumerable<IDocument> CreateDocuments(Partition partition)
        {
            if (partition == null)
                throw new ArgumentNullException("partition");

            //Trace.TraceInformation(string.Format("Processing documents starting {0} of {1} - {2}%", partition.Start, partition.Total, (partition.Start * 100 / partition.Total)));

            var documents = new ConcurrentBag<IDocument>();

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

            return documents;
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
            return _itemService.GetByIds(itemIds, ItemResponseGroup.ItemProperties | ItemResponseGroup.Variations | ItemResponseGroup.ItemEditorialReviews | ItemResponseGroup.Outlines);
        }

        protected virtual IList<Price> GetItemPrices(string[] itemIds)
        {
            var evalContext = new PriceEvaluationContext { ProductIds = itemIds };
            return _pricingService.EvaluateProductPrices(evalContext).ToList();
        }

        protected virtual bool IndexItem(ResultDocument doc, CatalogProduct item, Price[] prices)
        {
            var indexStoreNotAnalyzed = new[] { IndexStore.Yes, IndexType.NotAnalyzed };
            var indexStoreNotAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection };
            var indexStoreAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection };

            doc.Add(new DocumentField("__key", item.Id.ToLower(), indexStoreNotAnalyzed));
            doc.Add(new DocumentField("__type", item.GetType().Name, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("__sort", item.Name, indexStoreNotAnalyzed));

            IndexIsProperty(doc, "product");
            var statusField = (item.IsActive != true || item.MainProductId != null) ? "hidden" : "visible";
            IndexIsProperty(doc, statusField);
            doc.Add(new DocumentField("status", statusField, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("code", item.Code, indexStoreNotAnalyzed));
            IndexIsProperty(doc, item.Code);
            doc.Add(new DocumentField("name", item.Name, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("startdate", item.StartDate, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("enddate", item.EndDate.HasValue ? item.EndDate : DateTime.MaxValue, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("createddate", item.CreatedDate, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("lastmodifieddate", item.ModifiedDate ?? DateTime.MaxValue, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("priority", item.Priority, indexStoreNotAnalyzed));
            doc.Add(new DocumentField("vendor", item.Vendor ?? "", indexStoreNotAnalyzed));
            doc.Add(new DocumentField("lastindexdate", DateTime.UtcNow, indexStoreNotAnalyzed));

            // Add priority in virtual categories to search index
            foreach (var link in item.Links)
            {
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", link.CatalogId, link.CategoryId), link.Priority, indexStoreNotAnalyzed));
            }

            // Add catalogs to search index
            var catalogs = item.Outlines
                .Select(o => o.Items.First().Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var catalogId in catalogs)
            {
                doc.Add(new DocumentField("catalog", catalogId.ToLower(), indexStoreNotAnalyzedStringCollection));
            }

            // Add outlines to search index
            var outlineStrings = GetOutlineStrings(item.Outlines);
            foreach (var outline in outlineStrings)
            {
                doc.Add(new DocumentField("__outline", outline.ToLower(), indexStoreNotAnalyzedStringCollection));
            }

            // Index custom properties
            IndexItemCustomProperties(doc, item);

            if (item.Variations != null)
            {
                if (item.Variations.Any(c => c.ProductType == "Physical"))
                {
                    doc.Add(new DocumentField("type", "physical", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                    IndexIsProperty(doc, "physical");
                }

                if (item.Variations.Any(c => c.ProductType == "Digital"))
                {
                    doc.Add(new DocumentField("type", "digital", new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                    IndexIsProperty(doc, "digital");
                }

                foreach (var variation in item.Variations)
                {
                    IndexItemCustomProperties(doc, variation);
                }
            }

            IndexItemPrices(doc, prices, item);

            // add to content
            doc.Add(new DocumentField("__content", item.Name, indexStoreAnalyzedStringCollection));
            doc.Add(new DocumentField("__content", item.Code, indexStoreAnalyzedStringCollection));

            if (_settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", false))
            {
                using (var memStream = new MemoryStream())
                {
                    var serializer = new JsonSerializer
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.None,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.None,
                    };
                    var itemDto = item.ToWebModel(_blobUrlResolver);
                    //Do not store variations in index
                    itemDto.Variations = null;
                    itemDto.SerializeJson(memStream, serializer);
                    memStream.Seek(0, SeekOrigin.Begin);
                    var value = memStream.ReadToString();
                    // index full web serialized object
                    doc.Add(new DocumentField("__object", value, new[] { IndexStore.Yes, IndexType.Analyzed }));
                }
            }

            return true;
        }

        /// <summary>
        /// is:hidden, property can be used to provide user friendly way of searching products
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="value"></param>
        protected virtual void IndexIsProperty(ResultDocument doc, string value)
        {
            var indexNoStoreNotAnalyzed = new[] { IndexStore.No, IndexType.NotAnalyzed };
            doc.Add(new DocumentField("is", value, indexNoStoreNotAnalyzed));
       }

        protected virtual string[] GetOutlineStrings(IEnumerable<Outline> outlines)
        {
            return outlines
                .SelectMany(ExpandOutline)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        protected virtual IEnumerable<string> ExpandOutline(Outline outline)
        {
            // Outline structure: catalog/category1/.../categoryN/product

            var items = outline.Items
                .Take(outline.Items.Count - 1) // Exclude last item, which is product ID
                .Select(i => i.Id)
                .ToList();

            var catalogId = items.First();

            var result = new List<string>
            {
                catalogId,
                string.Join("/", items)
            };

            // For each child category create a separate outline: catalog/child_category
            if (items.Count > 2)
            {
                result.AddRange(
                    items.Skip(1)
                    .Select(i => string.Join("/", catalogId, i)));
            }

            return result;
        }

        protected virtual void IndexItemCustomProperties(ResultDocument doc, CatalogProduct item)
        {
            var properties = item.Properties;

            foreach (var propValue in item.PropertyValues.Where(x => x.Value != null))
            {
                var propertyName = (propValue.PropertyName ?? "").ToLower();
                var property = properties.FirstOrDefault(x => string.Equals(x.Name, propValue.PropertyName, StringComparison.InvariantCultureIgnoreCase) && x.ValueType == propValue.ValueType);
                var contentField = string.Concat("__content", property != null && property.Multilanguage && !string.IsNullOrWhiteSpace(propValue.LanguageCode) ? "_" + propValue.LanguageCode.ToLower() : string.Empty);

                switch (propValue.ValueType)
                {
                    case PropertyValueType.LongText:
                    case PropertyValueType.ShortText:
                        var stringValue = propValue.Value.ToString();

                        if (!string.IsNullOrWhiteSpace(stringValue)) // don't index empty values
                        {
                            doc.Add(new DocumentField(contentField, stringValue.ToLower(), new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));
                        }

                        break;
                }

                switch (propValue.ValueType)
                {
                    case PropertyValueType.Boolean:
                    case PropertyValueType.DateTime:
                    case PropertyValueType.Number:
                        doc.Add(new DocumentField(propertyName, propValue.Value, new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.LongText:
                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString().ToLowerInvariant(), new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.ShortText: // do not tokenize small values as they will be used for lookups and filters
                        doc.Add(new DocumentField(propertyName, propValue.Value.ToString(), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                        break;
                }
            }
        }

        #region Price Lists Indexing

        protected virtual void IndexItemPrices(ResultDocument doc, Price[] prices, CatalogProduct item)
        {
            foreach (var price in prices)
            {
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "price_{0}_{1}", price.Currency, price.PricelistId).ToLower(), price.EffectiveValue, new[] { IndexStore.No, IndexType.NotAnalyzed }));

                // now save additional pricing fields for convinient user searches, store price with currency and without one
                doc.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "price_{0}", price.Currency), price.EffectiveValue, new[] { IndexStore.No, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField("price", price.EffectiveValue, new[] { IndexStore.No, IndexType.NotAnalyzed }));
            }

            if (prices.Length == 0) // mark product without prices defined
            {
                IndexIsProperty(doc, "unpriced");
            }
            else
            {
                IndexIsProperty(doc, "priced");
            }
        }

        #endregion

        protected virtual IEnumerable<Partition> GetPartitionsForAllProducts()
        {
            var partitions = new ConcurrentBag<Partition>();

            var result = _catalogSearchService.Search(new SearchCriteria { Take = 0, ResponseGroup = SearchResponseGroup.WithProducts, WithHidden = true });
            var parts = result.ProductsTotalCount / PartitionSize + 1;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 5 };

            Parallel.For(0, parts, parallelOptions, (index) =>
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

            return partitions;
        }

        protected virtual IEnumerable<Partition> GetPartitionsForModifiedProducts(DateTime startDate, DateTime endDate)
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Pricing.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class ProductPriceOperationProvider : IOperationProvider
    {
        private readonly IChangeLogService _changeLogService;
        private readonly IPricingService _pricingService;

        public ProductPriceOperationProvider(IChangeLogService changeLogService, IPricingService pricingService)
        {
            _changeLogService = changeLogService;
            _pricingService = pricingService;
        }

        public string DocumentType => CatalogItemSearchCriteria.DocType;

        public IList<Operation> GetOperations(DateTime startDate, DateTime endDate)
        {
            var allOperations = _changeLogService.FindChangeHistory("PriceEntity", startDate, endDate).ToList();
            var priceIds = allOperations.Select(c => c.ObjectId).ToArray();
            var priceIdsAndProductIds = GetProductIds(priceIds);

            // TODO: How to get product for deleted price?
            var result = allOperations
                .Where(o => priceIdsAndProductIds.ContainsKey(o.ObjectId))
                .Select(o => new Operation
                {
                    ObjectId = priceIdsAndProductIds[o.ObjectId],
                    Timestamp = o.ModifiedDate ?? o.CreatedDate,
                    OperationType = OperationType.Index,
                })
                .GroupBy(o => o.ObjectId)
                .Select(g => g.OrderByDescending(o => o.Timestamp).First())
                .ToList();

            return result;
        }


        protected virtual IDictionary<string, string> GetProductIds(ICollection<string> priceIds)
        {
            // TODO: Get pageSize and degreeOfParallelism from settings
            return GetProductIdsWithPagingAndParallelism(priceIds, 1000, 10);
        }

        protected virtual IDictionary<string, string> GetProductIdsWithPagingAndParallelism(ICollection<string> priceIds, int pageSize, int degreeOfParallelism)
        {
            IDictionary<string, string> result;

            if (degreeOfParallelism > 1)
            {
                var dictionary = new ConcurrentDictionary<string, string>();

                var pages = new List<string[]>();
                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) => pages.Add(ids.ToArray()));

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism };

                Parallel.ForEach(pages, parallelOptions, ids =>
                {
                    var prices = _pricingService.GetPricesById(ids);
                    foreach (var price in prices)
                    {
                        var productId = price.ProductId;
                        dictionary.AddOrUpdate(price.Id, productId, (key, oldValue) => productId);
                    }
                });

                result = dictionary;
            }
            else
            {
                var dictionary = new Dictionary<string, string>();

                priceIds.ProcessWithPaging(pageSize, (ids, skipCount, totalCount) =>
                {
                    foreach (var price in _pricingService.GetPricesById(ids.ToArray()))
                    {
                        dictionary[price.Id] = price.ProductId;
                    }
                });

                result = dictionary;
            }

            return result;
        }
    }
}

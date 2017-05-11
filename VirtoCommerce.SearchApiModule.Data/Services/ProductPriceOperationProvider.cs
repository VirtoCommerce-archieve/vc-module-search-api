using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Pricing.Model;
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
            var allPriceChanges = _changeLogService.FindChangeHistory("Price", startDate, endDate).ToList();
            var priceIds = allPriceChanges.Select(c => c.ObjectId).ToArray();
            var prices = GetPrices(priceIds);

            // TODO: How to get product for deleted price?
            var result = allPriceChanges
                .Select(c => new { Timestamp = c.ModifiedDate ?? c.CreatedDate, Price = prices.ContainsKey(c.ObjectId) ? prices[c.ObjectId] : null })
                .Where(x => x.Price != null)
                .Select(x => new Operation { ObjectId = x.Price.ProductId, Timestamp = x.Timestamp, OperationType = OperationType.Index })
                .GroupBy(o => o.ObjectId)
                .Select(g => g.OrderByDescending(o => o.Timestamp).First())
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
    }
}

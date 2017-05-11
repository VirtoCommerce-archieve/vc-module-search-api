using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Extensions
{
    public static class DocumentOperationsProviderExtensions
    {
        /// <summary>
        /// Combines operations from multiple providers and returns latest operation for each object
        /// </summary>
        /// <param name="allProviders"></param>
        /// <param name="documentType"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static IList<Operation> GetLatestIndexOperationForEachObject(this IEnumerable<IOperationProvider> allProviders, string documentType, DateTime startDate, DateTime endDate)
        {
            var providers = allProviders.Where(p => p.DocumentType == documentType).ToArray();

            var result = providers
                .SelectMany(p => p.GetOperations(startDate, endDate))
                .GroupBy(o => o.ObjectId)
                .Select(g => g.OrderByDescending(o => o.Timestamp).First())
                .ToList();

            return result;
        }
    }
}

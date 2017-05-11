using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Extensions
{
    public static class OperationLogExtensions
    {
        /// <summary>
        /// Returns latest operation for each object
        /// </summary>
        /// <param name="allOperations"></param>
        /// <returns></returns>
        public static IList<Operation> GetLatestIndexOperationForEachObject(this IEnumerable<OperationLog> allOperations)
        {
            var result = allOperations
                .GroupBy(o => o.ObjectId)
                .Select(g => g.OrderByDescending(o => o.ModifiedDate ?? o.CreatedDate).First())
                .Select(o => new Operation
                {
                    ObjectId = o.ObjectId,
                    Timestamp = o.ModifiedDate ?? o.CreatedDate,
                    OperationType = o.OperationType == EntryState.Deleted ? OperationType.Remove : OperationType.Index
                })
                .ToList();

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.SearchApiModule.Data.Extensions;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CategoryOperationProvider : IOperationProvider
    {
        private readonly IChangeLogService _changeLogService;

        public CategoryOperationProvider(IChangeLogService changeLogService)
        {
            _changeLogService = changeLogService;
        }

        public string DocumentType => CategorySearchCriteria.DocType;

        public IList<Operation> GetOperations(DateTime startDate, DateTime endDate)
        {
            var allOperations = _changeLogService.FindChangeHistory("Category", startDate, endDate);
            var result = allOperations.GetLatestIndexOperationForEachObject();
            return result;
        }
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Domain.Catalog.Services;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchApiModule.Data.Helpers;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using VirtoCommerce.SearchModule.Core.Model.Search;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CategoryBrowsingService : ICategoryBrowsingService
    {
        private readonly ICategoryService _categoryService;
        private readonly ISearchProvider _searchProvider;
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ISettingsManager _settingsManager;

        public CategoryBrowsingService(
            ICategoryService categoryService,
            ISearchProvider searchService,
            IBlobUrlResolver blobUrlResolver,
            ISettingsManager settingsManager)
        {
            _searchProvider = searchService;
            _categoryService = categoryService;
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingsManager;
        }

        public virtual CategorySearchResult SearchCategories(string scope, ISearchCriteria criteria, CategoryResponseGroup responseGroup)
        {
            var response = new CategorySearchResult();
            var searchResults = _searchProvider.Search<DocumentDictionary>(scope, criteria);

            if (searchResults?.Documents != null)
            {
                var categoryDtos = new ConcurrentBag<CatalogModule.Web.Model.Category>();
                var taskList = new List<Task>();

                var documents = searchResults.Documents;
                if (_settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", true))
                {
                    var fullIndexedDocuments = documents.Where(doc => doc.ContainsKey(IndexHelper.ObjectFieldName) && !string.IsNullOrEmpty(doc[IndexHelper.ObjectFieldName].ToString())).ToList();
                    documents = documents.Except(fullIndexedDocuments).ToList();

                    var deserializeProductsTask = Task.Factory.StartNew(() =>
                    {
                        Parallel.ForEach(fullIndexedDocuments, new ParallelOptions { MaxDegreeOfParallelism = 5 }, doc =>
                        {
                            var category = doc.GetObjectFieldValue<CatalogModule.Web.Model.Category>();
                            categoryDtos.Add(category);
                        });
                    });
                    taskList.Add(deserializeProductsTask);
                }

                if (documents.Any())
                {
                    var loadProductsTask = Task.Factory.StartNew(() =>
                    {
                        string catalog = null;
                        var catalogItemSearchCriteria = criteria as CatalogItemSearchCriteria;
                        if (catalogItemSearchCriteria != null)
                        {
                            catalog = catalogItemSearchCriteria.Catalog;
                        }

                        var categoryIds = documents.Select(x => x.Id.ToString()).Distinct().ToArray();
                        if (categoryIds.Any())
                        {
                            // Now load items from repository
                            var categories = _categoryService.GetByIds(categoryIds, responseGroup, catalog);
                            Parallel.ForEach(categories, x =>
                            {
                                categoryDtos.Add(x.ToWebModel(_blobUrlResolver));
                            });
                        }
                    }
                    );
                    taskList.Add(loadProductsTask);
                }

                Task.WaitAll(taskList.ToArray());

                //Preserver original sorting order
                var orderedIds = searchResults.Documents.Select(x => x.Id.ToString()).ToList();
                response.Categories = categoryDtos.OrderBy(i => orderedIds.IndexOf(i.Id)).ToArray();
            }


            if (searchResults != null)
                response.TotalCount = searchResults.TotalCount;

            return response;
        }
    }
}

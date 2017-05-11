using System;
using System.IO;
using Moq;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchApiModule.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Data.Providers.AzureSearch;
using VirtoCommerce.SearchModule.Data.Providers.ElasticSearch;
using VirtoCommerce.SearchModule.Data.Providers.LuceneSearch;
using VirtoCommerce.SearchModule.Data.Services;
using VirtoCommerce.SearchModule.Data.Services.SearchPhraseParsing;

namespace VirtoCommerce.SearchApiModule.Test
{
    public class SearchTestsBase : IDisposable
    {
        private readonly string _luceneStorageDir = Path.Combine(Path.GetTempPath(), "lucene");

        protected ISearchProvider GetSearchProvider(string searchProvider, string scope, string dataSource = null)
        {
            ISearchProvider provider = null;

            var phraseSearchCriteriaPreprocessor = new PhraseSearchCriteriaPreprocessor(new SearchPhraseParser()) as ISearchCriteriaPreprocessor;
            var catalogSearchCriteriaPreprocessor = new CatalogSearchCriteriaPreprocessor();
            var searchCriteriaPreprocessors = new[] { phraseSearchCriteriaPreprocessor, catalogSearchCriteriaPreprocessor };

            if (searchProvider == "Lucene")
            {
                var connection = new SearchConnection(_luceneStorageDir, scope);
                var queryBuilder = new LuceneSearchQueryBuilder() as ISearchQueryBuilder;
                provider = new LuceneSearchProvider(new[] { queryBuilder }, connection, searchCriteriaPreprocessors);
            }

            if (searchProvider == "Elastic")
            {
                var elasticsearchHost = dataSource ?? Environment.GetEnvironmentVariable("TestElasticsearchHost") ?? "localhost:9200";

                var connection = new SearchConnection(elasticsearchHost, scope);
                var queryBuilder = new ElasticSearchQueryBuilder() as ISearchQueryBuilder;
                var elasticSearchProvider = new ElasticSearchProvider(connection, searchCriteriaPreprocessors, new[] { queryBuilder }, GetSettingsManager()) { EnableTrace = true };
                provider = elasticSearchProvider;
            }

            if (searchProvider == "Azure")
            {
                var azureSearchServiceName = Environment.GetEnvironmentVariable("TestAzureSearchServiceName");
                var azureSearchAccessKey = Environment.GetEnvironmentVariable("TestAzureSearchAccessKey");

                var connection = new SearchConnection(azureSearchServiceName, scope, accessKey: azureSearchAccessKey);
                var queryBuilder = new AzureSearchQueryBuilder() as ISearchQueryBuilder;
                provider = new AzureSearchProvider(connection, searchCriteriaPreprocessors, new[] { queryBuilder });
            }

            if (provider == null)
                throw new ArgumentException($"Search provider '{searchProvider}' is not supported", nameof(searchProvider));

            return provider;
        }

        protected static ISettingsManager GetSettingsManager()
        {
            var mock = new Mock<ISettingsManager>();
            mock.Setup(s => s.GetValue(It.IsAny<string>(), It.IsAny<int>())).Returns((string name, int defaultValue) => defaultValue);
            mock.Setup(s => s.GetValue(It.IsAny<string>(), It.IsAny<bool>())).Returns((string name, bool defaultValue) => defaultValue);
            mock.Setup(s => s.GetModuleSettings("VirtoCommerce.Store")).Returns(new SettingEntry[] { });
            return mock.Object;
        }

        public virtual void Dispose()
        {
            try
            {
                if (Directory.Exists(_luceneStorageDir))
                    Directory.Delete(_luceneStorageDir, true);
            }
            catch
            {
                // ignored
            }
        }
    }
}

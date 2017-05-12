using System;
using VirtoCommerce.SearchApiModule.Data.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;
using Xunit;

namespace VirtoCommerce.SearchApiModule.Test
{
    [CLSCompliant(false)]
    [Collection("Search")]
    [Trait("Category", "CI")]
    public class SearchTests : SearchTestsBase
    {
        private const string _scope = "test";
        private const string _documentType = CatalogItemSearchCriteria.DocType;

        [Theory]
        [InlineData("Lucene")]
        [InlineData("Elastic")]
        [InlineData("Azure")]
        public void CanSearchCatalogItems(string providerType)
        {
            var provider = GetSearchProvider(providerType, _scope);
            SearchTestsHelper.CreateSampleIndex(provider, _scope, _documentType);

            var criteria = new CatalogItemSearchCriteria
            {
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            var results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(12, results.DocCount);
            Assert.Equal(12, results.DocCount);


            // Filter by catalog
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(6, results.DocCount);
            Assert.Equal(6, results.DocCount);


            // Get hidden items
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                WithHidden = true,
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(7, results.DocCount);
            Assert.Equal(7, results.DocCount);


            // Filter by catalog and outlines
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                Outlines = new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda1", "SONY/186d61d8-d843-4675-9f77-ec5ef603fda2" },
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.DocCount);


            // Filter by type
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                ClassTypes = new[] { "type1", "type3" },
                WithHidden = true,
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(5, results.DocCount);
            Assert.Equal(5, results.DocCount);


            // Filter by start date
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                StartDate = DateTime.UtcNow.AddDays(-3),
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(0, results.DocCount);
            Assert.Equal(0, results.DocCount);


            // Filter by start date
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                StartDate = DateTime.UtcNow.AddDays(-2),
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.DocCount);


            // Filter by start date
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                StartDateFrom = DateTime.UtcNow.AddDays(-2),
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(2, results.DocCount);
            Assert.Equal(2, results.DocCount);


            // Filter by end date
            criteria = new CatalogItemSearchCriteria
            {
                Catalog = "goods",
                EndDate = DateTime.UtcNow.AddDays(1),
                StartingRecord = 0,
                RecordsToRetrieve = 20,
            };

            results = provider.Search<DocumentDictionary>(_scope, criteria);

            Assert.Equal(4, results.DocCount);
            Assert.Equal(4, results.DocCount);
        }
    }
}

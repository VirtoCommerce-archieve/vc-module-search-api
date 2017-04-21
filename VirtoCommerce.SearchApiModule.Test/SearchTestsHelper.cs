using System;
using System.Threading;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Test
{
    public class SearchTestsHelper
    {
        public class Price
        {
            public Price(string currency, string pricelist, decimal amount)
            {
                Currency = currency;
                Pricelist = pricelist;
                Amount = amount;
            }

            public string Currency;
            public string Pricelist;
            public decimal Amount;
        }

        public static void CreateSampleIndex(ISearchProvider provider, string scope, string documentType)
        {
            provider.RemoveAll(scope, documentType);

            AddDocuments(provider, scope, documentType, "stuff");
            AddDocuments(provider, scope, documentType, "goods");

            provider.Commit(scope);
            provider.Close(scope, documentType);

            // sleep for index to be commited
            Thread.Sleep(2000);
        }

        public static void AddDocuments(ISearchProvider provider, string scope, string documentType, string catalog)
        {
            provider.Index(scope, documentType, CreateDocument(catalog, "type1", "12345", "sample product", "red", 2, "visible", -1, 1, new[] { new Price("USD", "default", 123.23m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda1", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type1", "red3", "red shirt 2", "red", 4, "visible", -1, 1, new[] { new Price("USD", "default", 200m), new Price("USD", "sale", 99m), new Price("EUR", "sale", 300m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda2", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type2", "sad121", "red shirt", "red", 3, "visible", -2, 2, new[] { new Price("USD", "default", 10m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type2", "32894hjf", "black sox", "black", 10, "visible", -2, 2, new[] { new Price("USD", "default", 243.12m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type3", "another", "black sox2", "silver", 20, "visible", -2, 2, new[] { new Price("USD", "default", 700m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type3", "jdashf", "blue shirt", "Blue", 8, "visible", -2, 2, new[] { new Price("USD", "default", 23.12m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
            provider.Index(scope, documentType, CreateDocument(catalog, "type3", "hhhhhh", "blue shirt", "Blue", 8, "hidden", -2, 2, new[] { new Price("USD", "default", 23.12m) }, new[] { "sony/186d61d8-d843-4675-9f77-ec5ef603fda3", "apple/186d61d8-d843-4675-9f77-ec5ef603fda3" }));
        }

        public static IDocument CreateDocument(string catalog, string type, string key, string name, string color, int size, string status, int startDate, int endDate, Price[] prices, string[] outlines)
        {
            var doc = new ResultDocument();


            doc.Add(new DocumentField("__key", catalog + key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__type", type, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("__sort", "1", new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("status", status, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("is", status, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", "priced", new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", color, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("is", key, new[] { IndexStore.No, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("code", key, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("name", name, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("startdate", DateTime.UtcNow.AddDays(startDate), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("enddate", DateTime.UtcNow.AddDays(endDate), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            foreach (var price in prices)
            {
                doc.Add(new DocumentField($"price_{price.Currency}_{price.Pricelist}".ToLowerInvariant(), price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                doc.Add(new DocumentField($"price_{price.Currency}".ToLowerInvariant(), price.Amount, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            }

            doc.Add(new DocumentField("catalog", catalog, new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
            doc.Add(new DocumentField("color", color, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
            doc.Add(new DocumentField("size", size, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));

            if (outlines != null)
            {
                foreach (var outline in outlines)
                {
                    doc.Add(new DocumentField("__outline", outline, new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection }));
                }
            }

            doc.Add(new DocumentField("__content", name, new[] { IndexStore.Yes, IndexType.Analyzed }));

            return doc;
        }
    }
}

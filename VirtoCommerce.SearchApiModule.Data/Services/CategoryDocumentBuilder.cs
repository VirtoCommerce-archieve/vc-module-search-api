using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.CatalogModule.Web.Converters;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Assets;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchApiModule.Data.Helpers;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class CategoryDocumentBuilder : IDocumentBuilder<Category>
    {
        private readonly IBlobUrlResolver _blobUrlResolver;
        private readonly ISettingsManager _settingsManager;

        public CategoryDocumentBuilder(IBlobUrlResolver blobUrlResolver, ISettingsManager settingsManager)
        {
            _blobUrlResolver = blobUrlResolver;
            _settingsManager = settingsManager;
        }

        public virtual bool UpdateDocument(IDocument document, Category item, object context)
        {
            var indexStoreNotAnalyzed = new[] { IndexStore.Yes, IndexType.NotAnalyzed };
            var indexStoreNotAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.NotAnalyzed, IndexDataType.StringCollection };
            var indexStoreAnalyzedStringCollection = new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection };

            document.Add(new DocumentField("__key", item.Id.ToLowerInvariant(), indexStoreNotAnalyzed));
            document.Add(new DocumentField("__type", item.GetType().Name, indexStoreNotAnalyzed));
            document.Add(new DocumentField("__sort", item.Name, indexStoreNotAnalyzed));
            IndexIsProperty(document, "category");
            var statusField = (item.IsActive != true || item.Id != null) ? "hidden" : "visible";
            IndexIsProperty(document, statusField);
            document.Add(new DocumentField("status", statusField, indexStoreNotAnalyzed));
            document.Add(new DocumentField("code", item.Code, indexStoreNotAnalyzed));
            IndexIsProperty(document, item.Code);
            document.Add(new DocumentField("name", item.Name, indexStoreNotAnalyzed));
            document.Add(new DocumentField("createddate", item.CreatedDate, indexStoreNotAnalyzed));
            document.Add(new DocumentField("lastmodifieddate", item.ModifiedDate ?? DateTime.MaxValue, indexStoreNotAnalyzed));
            document.Add(new DocumentField("priority", item.Priority, indexStoreNotAnalyzed));
            document.Add(new DocumentField("lastindexdate", DateTime.UtcNow, indexStoreNotAnalyzed));

            // Add priority in virtual categories to search index
            foreach (var link in item.Links)
            {
                document.Add(new DocumentField(string.Format(CultureInfo.InvariantCulture, "priority_{0}_{1}", link.CatalogId, link.CategoryId), link.Priority, indexStoreNotAnalyzed));
            }

            // Add catalogs to search index
            var catalogs = item.Outlines
                .Select(o => o.Items.First().Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var catalogId in catalogs)
            {
                document.Add(new DocumentField("catalog", catalogId.ToLowerInvariant(), indexStoreNotAnalyzedStringCollection));
            }

            // Add outlines to search index
            var outlineStrings = GetOutlineStrings(item.Outlines);
            foreach (var outline in outlineStrings)
            {
                document.Add(new DocumentField("__outline", outline.ToLowerInvariant(), indexStoreNotAnalyzedStringCollection));
            }

            // Index custom properties
            IndexItemCustomProperties(document, item);

            // add to content
            document.Add(new DocumentField("__content", item.Name, indexStoreAnalyzedStringCollection));
            document.Add(new DocumentField("__content", item.Code, indexStoreAnalyzedStringCollection));

            if (_settingsManager.GetValue("VirtoCommerce.SearchApi.UseFullObjectIndexStoring", true))
            {
                var itemDto = item.ToWebModel(_blobUrlResolver);
                document.AddObjectFieldValue(itemDto);
            }

            return true;
        }


        /// <summary>
        /// is:hidden, property can be used to provide user friendly way of searching products
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="value"></param>
        protected virtual void IndexIsProperty(IDocument doc, string value)
        {
            doc.Add(new DocumentField("is", value, new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
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
            // Outline structure: catalog/category1/.../categoryN

            var items = outline.Items
                .Take(outline.Items.Count - 1) // Exclude last item, which is category ID
                .Select(i => i.Id)
                .ToList();


            var result = new List<string>
            {
                string.Join("/", items)
            };

            // Add partial outline for each parent:
            // catalog/category1/category2
            // catalog/category1
            // catalog
            if (items.Count > 0)
            {
                for (var i = items.Count; i > 0; i--)
                {
                    result.Add(string.Join("/", items.Take(i)));
                }
            }

            // For each parent category create a separate outline: catalog/parent_category
            if (items.Count > 2)
            {
                var catalogId = items.First();

                result.AddRange(
                    items.Skip(1)
                        .Select(i => string.Join("/", catalogId, i)));
            }

            return result;
        }

        protected virtual void IndexItemCustomProperties(IDocument document, Category item)
        {
            var properties = item.Properties;

            foreach (var propValue in item.PropertyValues.Where(x => x.Value != null))
            {
                var propertyName = propValue.PropertyName.ToLowerInvariant();
                var property = properties.FirstOrDefault(x => string.Equals(x.Name, propValue.PropertyName, StringComparison.InvariantCultureIgnoreCase) && x.ValueType == propValue.ValueType);
                var contentField = string.Concat("__content", property != null && property.Multilanguage && !string.IsNullOrWhiteSpace(propValue.LanguageCode) ? "_" + propValue.LanguageCode.ToLowerInvariant() : string.Empty);

                switch (propValue.ValueType)
                {
                    case PropertyValueType.LongText:
                    case PropertyValueType.ShortText:
                        var stringValue = propValue.Value.ToString();

                        if (!string.IsNullOrWhiteSpace(stringValue)) // don't index empty values
                        {
                            document.Add(new DocumentField(contentField, stringValue.ToLower(), new[] { IndexStore.Yes, IndexType.Analyzed, IndexDataType.StringCollection }));
                        }

                        break;
                }

                switch (propValue.ValueType)
                {
                    case PropertyValueType.Boolean:
                    case PropertyValueType.DateTime:
                    case PropertyValueType.Number:
                        document.Add(new DocumentField(propertyName, propValue.Value, new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.LongText:
                        document.Add(new DocumentField(propertyName, propValue.Value.ToString().ToLowerInvariant(), new[] { IndexStore.Yes, IndexType.Analyzed }));
                        break;
                    case PropertyValueType.ShortText: // do not tokenize small values as they will be used for lookups and filters
                        document.Add(new DocumentField(propertyName, propValue.Value.ToString(), new[] { IndexStore.Yes, IndexType.NotAnalyzed }));
                        break;
                }
            }
        }
    }
}

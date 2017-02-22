using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Services
{
    public class BaseIndexBuilder
    {
        protected const string UserGroupsFieldName = "usergroups";

        protected virtual void IndexUserGroups(ResultDocument doc, Category category)
        {
            // TODO: Consider virtual catalogs where user groups may be different for each category path (outline)

            var values = GetUserGroups(category) ?? new[] { "__any__" };
            if (!values.Any())
            {
                values = new[] { "__none__" };
            }

            doc.RemoveField(UserGroupsFieldName);

            var indexStoreNotAnalyzed = new[] { IndexStore.Yes, IndexType.NotAnalyzed };
            foreach (var value in values)
            {
                doc.Add(new DocumentField(UserGroupsFieldName, value, indexStoreNotAnalyzed));
            }
        }

        protected virtual IList<string> GetUserGroups(Category category)
        {
            var categories = CombineCategoryAndItsParents(category);

            // Get user groups for each category
            var groups = categories
                .Where(c => c.PropertyValues != null)
                .Select(c => c.PropertyValues.Where(v => v.PropertyName.EqualsInvariant(UserGroupsFieldName)).Select(v => (string)v.Value))
                .Where(e => e.Any())
                .ToList();

            string[] result;

            if (groups.Any())
            {
                // Find intersection of groups in all categories
                result = groups
                    .Skip(1)
                    .Aggregate(new HashSet<string>(groups.First(), StringComparer.OrdinalIgnoreCase), (h, e) => { h.IntersectWith(e); return h; })
                    .ToArray();
            }
            else
            {
                result = null;
            }

            return result;
        }

        protected virtual IList<Category> CombineCategoryAndItsParents(Category category)
        {
            var result = new List<Category>();

            if (category != null)
            {
                if (category.Parents != null)
                {
                    result.AddRange(category.Parents);
                }

                result.Add(category);
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VirtoCommerce.CatalogModule.Web.Security;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.SearchApiModule.Web.Model;

namespace VirtoCommerce.SearchApiModule.Web.Extensions
{
    public static class SearchCriteriaExtensions
    {
        public static void ApplyRestrictionsForUser(this CatalogItemSearchCriteria criteria, string userName, ISecurityService securityService)
        {
            // Check global permission
            if (!securityService.UserHasAnyPermission(userName, null, CatalogPredefinedPermissions.Read))
            {
                // Get user 'read' permission scopes
                var readPermissionScopes = securityService.GetUserPermissions(userName)
                    .Where(x => x.Id.StartsWith(CatalogPredefinedPermissions.Read))
                    .SelectMany(x => x.AssignedScopes)
                    .ToList();

                /*
                var catalogScopes = readPermissionScopes.OfType<CatalogSelectedScope>();
                if (catalogScopes.Any())
                {
                    // Filter by selected catalog
                    criteria.CatalogIds = catalogScopes.Select(x => x.Scope)
                                                       .Where(x => !string.IsNullOrEmpty(x))
                                                       .ToArray();
                }

                var categoryScopes = readPermissionScopes.OfType<CatalogSelectedCategoryScope>();
                if (categoryScopes.Any())
                {
                    // Filter by selected categories
                    criteria.CategoryIds = categoryScopes.Select(x => x.Scope)
                                                         .Where(x => !string.IsNullOrEmpty(x))
                                                         .ToArray();
                }
                */
            }
        }
    }
}
using System;
using System.Collections.Generic;
using VirtoCommerce.SearchModule.Core.Model.Search;
using VirtoCommerce.SearchModule.Core.Model.Search.Criterias;

namespace VirtoCommerce.SearchApiModule.Data.Model
{
    // TODO: move to catalog module as it is catalog specific criteria and not generic search one

    public class CatalogItemSearchCriteria : KeywordSearchCriteria
    {
        public const string DocType = "catalogitem";

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogItemSearchCriteria"/> class.
        /// </summary>
        public CatalogItemSearchCriteria()
            : base(DocType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CatalogItemSearchCriteria"/> class.
        /// </summary>
        /// <param name="documentType">Type of the document.</param>
        public CatalogItemSearchCriteria(string documentType)
            : base(documentType)
        {
        }

        /// <summary>
        /// Gets the default sort order.
        /// </summary>
        /// <value>The default sort order.</value>
        public static SearchSort DefaultSortOrder => new SearchSort("__sort", false);

        /// <summary>
        /// Gets or sets the indexes of the search.
        /// </summary>
        /// <value>
        /// The index of the search.
        /// </value>
        public virtual string Catalog { get; set; }

        /// <summary>
        /// Gets or sets the response groups.
        /// </summary>
        /// <value>
        /// The response groups.
        /// </value>
        public virtual IList<string> ResponseGroups { get; set; }

        /// <summary>
        /// Gets or sets the outlines. Outline consists of "Category1/Category2".
        /// </summary>
        /// <example>Everything/digital-cameras</example>
        /// <value>The outlines.</value>
        public virtual IList<string> Outlines { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the class types.
        /// </summary>
        /// <value>The class types.</value>
        public virtual IList<string> ClassTypes { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the start date. The date must be in UTC format as that is format indexes are stored in.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the start date from filter. Used for filtering new products. The date must be in UTC format as that is format indexes are stored in.
        /// </summary>
        /// <value>The start date from.</value>
        public DateTime? StartDateFrom { get; set; }

        /// <summary>
        /// Gets or sets the end date. The date must be in UTC format as that is format indexes are stored in.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Specifies if we search hidden products.
        /// </summary>
        public virtual bool WithHidden { get; set; }
    }
}

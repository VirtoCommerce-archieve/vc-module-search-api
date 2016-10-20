using System;

namespace VirtoCommerce.SearchApiModule.Web.Model
{
    public class IndexDocument
    {
        public string Id { get; set; }

        public DateTime BuildDate { get; set; }

        public string Content { get; set; }
    }
}
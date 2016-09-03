using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VirtoCommerce.SearchApiModule.Web.Extensions
{
    public static class TermsQueryStringExtensions
    {
        public static List<StringKeyValues> AsKeyValues(this string[] query)
        {
            var result = new List<StringKeyValues>();

            if (query != null)
            {
                var nameValueDelimeter = new[] { ':' };
                var valuesDelimeter = new[] { ',' };

                result.AddRange(query
                    .Select(item => item.Split(nameValueDelimeter, 2))
                    .Where(item => item.Length == 2)
                    .Select(item => new StringKeyValues { Key = item[0], Values = item[1].Split(valuesDelimeter, StringSplitOptions.RemoveEmptyEntries) })
                    .GroupBy(item => item.Key)
                    .Select(g => new StringKeyValues { Key = g.Key, Values = g.SelectMany(i => i.Values).Distinct().ToArray() })
                    );
            }

            return result;
        }
    }

    public class StringKeyValues
    {
        public string Key { get; set; }
        public string[] Values { get; set; }
    }
}
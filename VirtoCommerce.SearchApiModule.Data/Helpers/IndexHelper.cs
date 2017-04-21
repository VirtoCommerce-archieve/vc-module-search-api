using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model.Indexing;

namespace VirtoCommerce.SearchApiModule.Data.Helpers
{
    public static class IndexHelper
    {
        public const string ObjectFieldName = "__object";

        public static void AddObjectFieldValue<T>(this IDocument document, T value)
        {
            document.Add(new DocumentField(ObjectFieldName, SerializeObject(value), new[] { IndexStore.Yes, IndexType.No }));
        }

        public static T GetObjectFieldValue<T>(this DocumentDictionary document)
            where T : class
        {
            T result = null;

            if (document.ContainsKey(ObjectFieldName))
            {
                var obj = document[ObjectFieldName];

                result = obj as T;
                if (result == null)
                {
                    var jobj = obj as JObject;
                    if (jobj != null)
                    {
                        result = jobj.ToObject<T>();
                    }
                    else
                    {
                        var productString = obj as string;
                        if (!string.IsNullOrEmpty(productString))
                        {
                            result = DeserializeObject<T>(productString);
                        }
                    }
                }
            }

            return result;
        }


        public static JsonSerializer ObjectSerializer { get; } = new JsonSerializer
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
        };

        public static string SerializeObject(object obj)
        {
            using (var memStream = new MemoryStream())
            {
                obj.SerializeJson(memStream, ObjectSerializer);
                memStream.Seek(0, SeekOrigin.Begin);

                var result = memStream.ReadToString();
                return result;
            }
        }

        public static T DeserializeObject<T>(string str)
        {
            using (var stringReader = new StringReader(str))
            using (var jsonTextReader = new JsonTextReader(stringReader))
            {
                var result = ObjectSerializer.Deserialize<T>(jsonTextReader);
                return result;
            }
        }
    }
}

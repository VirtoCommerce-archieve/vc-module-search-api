using Newtonsoft.Json;

namespace VirtoCommerce.SearchApiModule.Web.Model
{
    public class ProgressMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("level")]
        public string Level { get; set; }
    }
}
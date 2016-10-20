using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace VirtoCommerce.SearchApiModule.Web.Model
{
    public class SearchPushNotification : PushNotification
    {
        public SearchPushNotification(string creator)
            : base(creator)
        {
            ProgressLog = new List<ProgressMessage>();
        }

        [JsonProperty("started")]
        public DateTime? Started { get; set; }

        [JsonProperty("finished")]
        public DateTime? Finished { get; set; }

        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("progressLog")]
        public ICollection<ProgressMessage> ProgressLog;
    }
}

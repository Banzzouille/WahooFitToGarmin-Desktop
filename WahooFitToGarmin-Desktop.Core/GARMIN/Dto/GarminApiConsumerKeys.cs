using System.Text.Json.Serialization;

namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto
{
    public class GarminApiConsumerKeys
    {
        [JsonPropertyName("consumer_key")]
        public string ConsumerKey { get; set; }

        [JsonPropertyName("consumer_secret")]
        public string ConsumerSecret { get; set; }
    }
}

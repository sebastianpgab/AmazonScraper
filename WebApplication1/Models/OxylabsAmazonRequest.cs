using System.Text.Json.Serialization;

namespace WebApplication1.Models
{
    public class OxylabsAmazonRequest
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = "amazon_product";

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = "de";

        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;

        [JsonPropertyName("parse")]
        public bool Parse { get; set; } = true;

        [JsonPropertyName("geo_location")]
        public string GeoLocation { get; set; } = "10115";

        [JsonPropertyName("context")]
        public List<OxylabsContextItem> Context { get; set; } = new();
    }

    public class OxylabsContextItem
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}
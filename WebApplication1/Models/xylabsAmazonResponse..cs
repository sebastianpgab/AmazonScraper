using System.Text.Json.Serialization;

namespace WebApplication1.Models
{
    public class OxylabsAmazonResponse
    {
        [JsonPropertyName("results")]
        public List<OxylabsResult>? Results { get; set; }
    }

    public class OxylabsResult
    {
        [JsonPropertyName("content")]
        public OxylabsAmazonProductContent? Content { get; set; }
    }

    public class OxylabsAmazonProductContent
    {
        [JsonPropertyName("asin")]
        public string? Asin { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("images")]
        public List<string>? Images { get; set; }

        [JsonPropertyName("main_image")]
        public string? MainImage { get; set; }
    }
}

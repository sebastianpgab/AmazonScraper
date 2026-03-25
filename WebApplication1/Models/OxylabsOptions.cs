namespace WebApplication1.Models
{
    public class OxylabsOptions
    {
        public string BaseUrl { get; set; } = "https://realtime.oxylabs.io/";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Domain { get; set; } = "de";
        public string GeoLocation { get; set; } = "10115";
        public string Currency { get; set; } = "EUR";
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxConcurrency { get; set; } = 5;
    }
}

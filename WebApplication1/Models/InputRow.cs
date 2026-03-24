namespace WebApplication1.Models
{
    public class InputRow
    {
        public int ExcelRowNumber { get; set; }

        public string? InternalCode { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Ean { get; set; }
        public string? Asin { get; set; }
        public string? Condition { get; set; }
        public string? Marketplace { get; set; }
        public string? Price { get; set; }

        public string? MarketPrice { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string? Status { get; set; }
    }
}

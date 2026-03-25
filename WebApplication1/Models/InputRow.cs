namespace WebApplication1.Models
{
    public class InputRow
    {
        public int ExcelRowNumber { get; set; }

        public string InternalCode { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Ean { get; set; } = string.Empty;
        public string Asin { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Marketplace { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;

        public string? MarketPrice { get; set; }
        public string? Status { get; set; }
        public byte[]? ImageBytes { get; set; }
    }
}

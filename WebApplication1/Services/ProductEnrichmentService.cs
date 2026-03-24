using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ProductEnrichmentService
    {
        private readonly AmazonLookupService _amazonLookupService;

        public ProductEnrichmentService(AmazonLookupService amazonLookupService)
        {
            _amazonLookupService = amazonLookupService;
        }

        public async Task EnrichAsync(List<InputRow> rows)
        {
            using var semaphore = new SemaphoreSlim(2);

            var tasks = rows.Select(async row =>
            {
                await semaphore.WaitAsync();

                try
                {
                    if (string.IsNullOrWhiteSpace(row.Asin))
                    {
                        row.Status = "ASIN missing";
                        return;
                    }

                    var (price, imageBytes) = await _amazonLookupService.GetProductDataAsync(
                        row.Asin,
                        downloadImage: true
                    );

                    row.MarketPrice = price;
                    row.ImageBytes = imageBytes; // będzie null, bo wyłączyliśmy obrazki

                    if (string.IsNullOrWhiteSpace(price))
                    {
                        row.Status = "Price missing";
                    }
                    else
                    {
                        row.Status = "OK";
                    }
                }
                catch (Exception ex)
                {
                    row.Status = ex.Message;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
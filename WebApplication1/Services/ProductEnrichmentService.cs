using Microsoft.Extensions.Options;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ProductEnrichmentService
    {
        private readonly IAmazonLookupService _amazonLookupService;
        private readonly OxylabsOptions _options;

        public ProductEnrichmentService(
            IAmazonLookupService amazonLookupService,
            IOptions<OxylabsOptions> options)
        {
            _amazonLookupService = amazonLookupService;
            _options = options.Value;
        }

        public async Task EnrichAsync(List<InputRow> rows)
        {
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);

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

                    var result = await _amazonLookupService.GetProductDataAsync(
                        row.Asin,
                        downloadImage: true
                    );

                    row.MarketPrice = result.Price;
                    row.ImageBytes = result.ImageBytes;

                    if (string.IsNullOrWhiteSpace(result.Price))
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
                    row.Status = ShortenStatus(ex.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private static string ShortenStatus(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Unknown error";

            return message.Length <= 150
                ? message
                : message[..150];
        }
    }
}
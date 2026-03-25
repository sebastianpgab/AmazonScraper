using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IAmazonLookupService
    {
        Task<AmazonProductData> GetProductDataAsync(string asin, bool downloadImage = false);
    }
    public class AmazonLookupService : IAmazonLookupService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OxylabsOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public AmazonLookupService(
            IHttpClientFactory httpClientFactory,
            IOptions<OxylabsOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<AmazonProductData> GetProductDataAsync(string asin, bool downloadImage = false)
        {
            if (string.IsNullOrWhiteSpace(_options.Username))
                throw new Exception("Oxylabs Username is EMPTY");

            if (string.IsNullOrWhiteSpace(_options.Password))
                throw new Exception("Oxylabs Password is EMPTY");
            asin = CleanAsin(asin);

            if (string.IsNullOrWhiteSpace(asin) || asin.Length != 10)
                throw new ArgumentException($"Nieprawidłowy ASIN: '{asin}'");

            var product = await GetOxylabsProductAsync(asin);

            if (product == null)
                throw new Exception($"Brak danych produktu dla ASIN '{asin}'");

            var result = new AmazonProductData
            {
                Price = FormatPrice(product.Price, product.Currency),
                ImageUrl = GetBestImageUrl(product)
            };

            if (downloadImage && !string.IsNullOrWhiteSpace(result.ImageUrl))
            {
                result.ImageBytes = await TryDownloadImageAsync(result.ImageUrl);
            }

            return result;
        }

        private async Task<OxylabsAmazonProductContent?> GetOxylabsProductAsync(string asin)
        {
            var client = _httpClientFactory.CreateClient("OxylabsClient");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}")
            );

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var request = new OxylabsAmazonRequest
            {
                Source = "amazon_product",
                Domain = _options.Domain,
                Query = asin,
                Parse = true,
                GeoLocation = _options.GeoLocation,
                Context = new List<OxylabsContextItem>
                {
                    new OxylabsContextItem
                    {
                        Key = "autoselect_variant",
                        Value = true
                    },
                    new OxylabsContextItem
                    {
                        Key = "currency",
                        Value = _options.Currency
                    }
                }
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

            using var content = new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json"
            );

            using var response = await client.PostAsync("v1/queries", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Oxylabs error {(int)response.StatusCode}: {responseText}");

            var oxylabsResponse = JsonSerializer.Deserialize<OxylabsAmazonResponse>(
                responseText,
                _jsonOptions
            );

            return oxylabsResponse?.Results?.FirstOrDefault()?.Content;
        }

        private async Task<byte[]?> TryDownloadImageAsync(string imageUrl)
        {
            var client = _httpClientFactory.CreateClient("ImageClient");

            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsByteArrayAsync();
                }
                catch
                {
                    if (attempt == 2)
                        return null;

                    await Task.Delay(300);
                }
            }

            return null;
        }

        private string? FormatPrice(decimal? price, string? currency)
        {
            if (!price.HasValue)
                return null;

            var finalCurrency = string.IsNullOrWhiteSpace(currency)
                ? _options.Currency
                : currency;

            return $"{price.Value:0.00} {finalCurrency}";
        }

        private static string? GetBestImageUrl(OxylabsAmazonProductContent product)
        {
            if (!string.IsNullOrWhiteSpace(product.MainImage))
                return product.MainImage;

            if (product.Images != null && product.Images.Count > 0)
            {
                return product.Images.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            }

            return null;
        }

        private static string CleanAsin(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .ToUpperInvariant();
        }
    }
}
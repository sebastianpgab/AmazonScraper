using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace WebApplication1.Services
{
    public class AmazonLookupService : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private bool _initialized = false;
        private readonly SemaphoreSlim _initSemaphore = new(1, 1);
        private readonly Random _random = new();

        private async Task EnsureInitializedAsync()
        {
            if (_initialized && _context != null)
                return;

            await _initSemaphore.WaitAsync();

            try
            {
                if (_initialized && _context != null)
                    return;

                _playwright = await Playwright.CreateAsync();

                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                _context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    Locale = "de-DE",
                    ExtraHTTPHeaders = new Dictionary<string, string>
                    {
                        ["Accept-Language"] = "de-DE,de;q=0.9"
                    }
                });

                await _context.AddCookiesAsync(new[]
                {
                    new Cookie
                    {
                        Name = "i18n-prefs",
                        Value = "EUR",
                        Domain = ".amazon.de",
                        Path = "/"
                    }
                });

                // Ograniczamy zbędne zasoby
                await _context.RouteAsync("**/*", async route =>
                {
                    var resourceType = route.Request.ResourceType;

                    if (resourceType == "font" || resourceType == "media")
                    {
                        await route.AbortAsync();
                        return;
                    }

                    await route.ContinueAsync();
                });

                _initialized = true;
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public async Task<(string? price, byte[]? imageBytes)> GetProductDataAsync(string asin, bool downloadImage = false)
        {
            await EnsureInitializedAsync();

            asin = CleanAsin(asin);

            if (string.IsNullOrWhiteSpace(asin) || asin.Length != 10)
                throw new ArgumentException($"Nieprawidłowy ASIN: '{asin}'");

            var url = $"https://www.amazon.de/dp/{asin}?currency=EUR";

            var page = await _context!.NewPageAsync();

            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 30000
                });

                // krótsze, lekkie opóźnienie zamiast 3000 ms
                await page.WaitForTimeoutAsync(_random.Next(600, 1100));

                var bodyLocator = page.Locator("body");
                await bodyLocator.WaitForAsync(new LocatorWaitForOptions
                {
                    Timeout = 5000
                });

                var bodyText = await bodyLocator.InnerTextAsync();

                if (bodyText.Contains("Tut uns leid", StringComparison.OrdinalIgnoreCase) ||
                    bodyText.Contains("keine funktionsfähige Seite", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Amazon zwrócił stronę błędu dla ASIN '{asin}'");
                }

                string? price = null;

                var priceSelectors = new[]
                {
                    "#corePrice_feature_div .a-price .a-offscreen",
                    "#corePriceDisplay_desktop_feature_div .a-price .a-offscreen",
                    "#apex_desktop .a-price .a-offscreen",
                    "#price_inside_buybox",
                    "#priceblock_ourprice",
                    "#priceblock_dealprice"
                };

                foreach (var selector in priceSelectors)
                {
                    var locator = page.Locator(selector);

                    if (await locator.CountAsync() > 0)
                    {
                        var values = await locator.AllInnerTextsAsync();

                        price = values
                            .Select(v => v?.Trim())
                            .FirstOrDefault(v =>
                                !string.IsNullOrWhiteSpace(v) &&
                                (v.Contains("€") || v.Contains("EUR")));

                        if (!string.IsNullOrWhiteSpace(price))
                            break;
                    }
                }

                if (string.IsNullOrWhiteSpace(price))
                {
                    var allPrices = await page.Locator(".a-price .a-offscreen").AllInnerTextsAsync();

                    price = allPrices
                        .Select(v => v?.Trim())
                        .FirstOrDefault(v =>
                            !string.IsNullOrWhiteSpace(v) &&
                            (v.Contains("€") || v.Contains("EUR")));
                }

                byte[]? imageBytes = null;

                if (downloadImage)
                {
                    var imageLocator = page.Locator("#landingImage");

                    var imageUrl = await imageLocator.GetAttributeAsync("data-old-hires");
                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        imageUrl = await imageLocator.GetAttributeAsync("src");
                    }

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                        imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                    }
                }

                return (price, imageBytes);
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        private string CleanAsin(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .ToUpperInvariant();
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null)
                await _context.CloseAsync();

            if (_browser != null)
                await _browser.CloseAsync();

            _playwright?.Dispose();
            _initSemaphore.Dispose();
        }
    }
}
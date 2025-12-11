using PuppeteerSharp;
using System.Text.Json;
using System.Net.Http.Json;
using ScGen.Lib.Shared.DTOs.Requests;
using ScGen.Lib.Shared.DTOs.Responses;
using BuildingBlocks.Patterns.Result;

namespace ScGen.Lib.Shared.Services.PropertyExtractor;

public interface IPropertyExtractorService
{
    Task<Result<ExtractPropertyDataResponse>> ExtractAsync(ExtractPropertyDataRequest request);
}

public sealed class PropertyExtractorService(IConfiguration configuration) : IPropertyExtractorService
{
    private readonly string _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");

    public async Task<Result<ExtractPropertyDataResponse>> ExtractAsync(ExtractPropertyDataRequest request)
    {
        try
        {
            // Step 1: Fetch the page with Puppeteer (renders JavaScript)
            var html = await FetchWithPuppeteerAsync(request.Url);
            
            // Step 2: Extract structured data with OpenAI
            var extractionResult = await ExtractWithOpenAIAsync(html, request.Url);
            
            // Step 3: Validate and return
            var warnings = GenerateWarnings(extractionResult.Data);
            
            var response = new ExtractPropertyDataResponse
            {
                Data = extractionResult.Data,
                Confidence = extractionResult.Confidence,
                Warnings = warnings,
                Source = new ExtractionSource
                {
                    Url = request.Url,
                    Platform = DetectPlatform(request.Url),
                    ExtractedAt = DateTime.UtcNow
                }
            };
            
            return Result<ExtractPropertyDataResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<ExtractPropertyDataResponse>.Failure(
                ResultPatternError.InternalServerError($"Property extraction failed: {ex.Message}")
            );
        }
    }

    private async Task<string> FetchWithPuppeteerAsync(string url)
    {
        // Download browser if not already downloaded
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        // Launch browser in headless mode with optimized settings
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Timeout = 60000, // 60 second timeout for browser launch
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-accelerated-2d-canvas",
                "--disable-gpu",
                "--disable-web-security",
                "--disable-features=IsolateOrigins,site-per-process",
                "--window-size=1920x1080"
            }
        });

        await using var page = await browser.NewPageAsync();
        
        // Increase default timeout for slow pages
        page.DefaultTimeout = 60000; // 60 seconds
        page.DefaultNavigationTimeout = 60000; // 60 seconds
        
        // Set viewport and user agent to mimic real browser
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = 1920,
            Height = 1080
        });
        
        await page.SetUserAgentAsync("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        // Navigate to the page - use a more forgiving wait strategy
        try
        {
            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, // Less strict than networkidle0
                Timeout = 60000 // 60 second timeout
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Navigation warning (non-critical): {ex.Message}");
            // Page might have loaded enough content even if navigation didn't fully complete
        }

        // Wait for content to load
        await Task.Delay(3000); // Give JavaScript time to execute

        // Get the fully rendered HTML
        var html = await page.GetContentAsync();
        
        Console.WriteLine($"✓ Puppeteer fetched {html.Length} characters from {url}");
        
        return html;
    }

    private async Task<(PropertyData Data, Dictionary<string, double> Confidence)> ExtractWithOpenAIAsync(string html, string url)
    {
        var truncatedHtml = html.Length > 150000 ? html.Substring(0, 150000) : html;

        var systemPrompt = @"You are a real estate data extraction expert. Extract structured property data from ANY real estate listing.

Return JSON with this exact schema:
{
  ""propertyAddress"": ""string"",
  ""city"": ""string"",
  ""state"": ""string"",
  ""zipCode"": ""string"",
  ""county"": ""string"",
  ""propertyValue"": number,
  ""totalSquareFootage"": number,
  ""lotSize"": number,
  ""lotSizeUnit"": ""acres"" | ""sqft"",
  ""bedrooms"": number,
  ""bathroomsFull"": number,
  ""bathroomsPartial"": number,
  ""yearBuilt"": number,
  ""propertyType"": ""single_family"" | ""estate"" | ""commercial"",
  ""architecturalStyle"": ""string"",
  ""premiumAmenities"": [""array""],
  ""parkingSpaces"": number,
  ""parkingType"": ""string"",
  ""hvacType"": ""string"",
  ""fireplaces"": number,
  ""fireplaceTypes"": [""array""],
  ""yearRenovated"": number,
  ""overallCondition"": ""excellent"" | ""good"" | ""fair"",
  ""recentUpgrades"": [""array""],
  ""schoolDistrict"": ""string"",
  ""mlsNumber"": ""string"",
  ""description"": ""string"",
  ""propertyImages"": [""image URLs""],
  ""virtualTourUrl"": ""string"",
  ""confidence"": {
    ""fieldName"": 0.0-1.0
  }
}

Use null for fields not found. Set confidence scores accurately.";

        var userPrompt = $"Extract property data from this listing:\n\nURL: {url}\n\nHTML:\n{truncatedHtml}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAiApiKey}");

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.1,
            max_tokens = 4000
        };

        var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentJson = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        
        if (string.IsNullOrEmpty(contentJson))
            throw new Exception("No response from OpenAI");

        var extracted = JsonSerializer.Deserialize<JsonElement>(contentJson);
        
        // Parse into PropertyData object
        var data = new PropertyData
        {
            PropertyAddress = GetStringOrNull(extracted, "propertyAddress"),
            City = GetStringOrNull(extracted, "city"),
            State = GetStringOrNull(extracted, "state"),
            ZipCode = GetStringOrNull(extracted, "zipCode"),
            County = GetStringOrNull(extracted, "county"),
            PropertyValue = GetLongOrNull(extracted, "propertyValue"),
            TotalSquareFootage = GetIntOrNull(extracted, "totalSquareFootage"),
            LotSize = GetDoubleOrNull(extracted, "lotSize"),
            LotSizeUnit = GetStringOrNull(extracted, "lotSizeUnit"),
            Bedrooms = GetIntOrNull(extracted, "bedrooms"),
            BathroomsFull = GetIntOrNull(extracted, "bathroomsFull"),
            BathroomsPartial = GetIntOrNull(extracted, "bathroomsPartial"),
            YearBuilt = GetIntOrNull(extracted, "yearBuilt"),
            PropertyType = GetStringOrNull(extracted, "propertyType"),
            ArchitecturalStyle = GetStringOrNull(extracted, "architecturalStyle"),
            PremiumAmenities = GetStringListOrNull(extracted, "premiumAmenities"),
            ParkingSpaces = GetIntOrNull(extracted, "parkingSpaces"),
            ParkingType = GetStringOrNull(extracted, "parkingType"),
            HvacType = GetStringOrNull(extracted, "hvacType"),
            Fireplaces = GetIntOrNull(extracted, "fireplaces"),
            FireplaceTypes = GetStringListOrNull(extracted, "fireplaceTypes"),
            YearRenovated = GetIntOrNull(extracted, "yearRenovated"),
            OverallCondition = GetStringOrNull(extracted, "overallCondition"),
            RecentUpgrades = GetStringListOrNull(extracted, "recentUpgrades"),
            SchoolDistrict = GetStringOrNull(extracted, "schoolDistrict"),
            MlsNumber = GetStringOrNull(extracted, "mlsNumber"),
            Description = GetStringOrNull(extracted, "description"),
            PropertyImages = GetStringListOrNull(extracted, "propertyImages"),
            VirtualTourUrl = GetStringOrNull(extracted, "virtualTourUrl")
        };

        // Extract confidence scores
        var confidence = new Dictionary<string, double>();
        if (extracted.TryGetProperty("confidence", out var confidenceElement))
        {
            foreach (var prop in confidenceElement.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var score))
                {
                    confidence[prop.Name] = score;
                }
            }
        }

        return (data, confidence);
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static int? GetIntOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt32();
        return null;
    }

    private static long? GetLongOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetInt64();
        return null;
    }

    private static double? GetDoubleOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
            return prop.GetDouble();
        return null;
    }

    private static List<string>? GetStringListOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrEmpty(value))
                        list.Add(value);
                }
            }
            return list.Count > 0 ? list : null;
        }
        return null;
    }

    private static List<string> GenerateWarnings(PropertyData data)
    {
        var warnings = new List<string>();

        if (!data.PropertyValue.HasValue || data.PropertyValue == 0)
            warnings.Add("Missing critical field: Property Value");
        
        if (!data.TotalSquareFootage.HasValue || data.TotalSquareFootage == 0)
            warnings.Add("Missing critical field: Square Footage");
        
        if (!data.Bedrooms.HasValue)
            warnings.Add("Missing critical field: Bedrooms");
        
        if (!data.BathroomsFull.HasValue)
            warnings.Add("Missing critical field: Bathrooms");

        if (data.PropertyValue.HasValue && data.PropertyValue < 10000)
            warnings.Add("Property value seems unusually low. Please verify.");

        if (data.TotalSquareFootage.HasValue && data.TotalSquareFootage < 100)
            warnings.Add("Square footage seems unusually low. Please verify.");

        return warnings;
    }

    private static string DetectPlatform(string url)
    {
        if (url.Contains("sothebysrealty.com", StringComparison.OrdinalIgnoreCase))
            return "sothebys";
        if (url.Contains("christiesrealestate.com", StringComparison.OrdinalIgnoreCase))
            return "christies";
        if (url.Contains("zillow.com", StringComparison.OrdinalIgnoreCase))
            return "zillow";
        if (url.Contains("redfin.com", StringComparison.OrdinalIgnoreCase))
            return "redfin";
        if (url.Contains("realtor.com", StringComparison.OrdinalIgnoreCase))
            return "realtor";
        
        return "unknown";
    }
}


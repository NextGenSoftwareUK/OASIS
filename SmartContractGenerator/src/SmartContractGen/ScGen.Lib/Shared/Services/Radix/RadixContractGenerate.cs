namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractGenerate
{
    private Result<GenerateContractResponse> Validation(IFormFile file)
    {
        if (!file.IsJsonFile())
        {
            _logger.ValidationFailed(nameof(GenerateAsync),
                Messages.InvalidJsonFile, _httpContextAccessor.GetId().ToString());
            return Result<GenerateContractResponse>.Failure(ResultPatternError.BadRequest(Messages.InvalidJsonFile));
        }

        if (file.Length == 0)
        {
            _logger.ValidationFailed(nameof(GenerateAsync),
                Messages.EmptyJson, _httpContextAccessor.GetId().ToString());
            return Result<GenerateContractResponse>.Failure(ResultPatternError.BadRequest(Messages.EmptyJson));
        }

        return Result<GenerateContractResponse>.Success();
    }

    private static string MakeSafeProjectName(string? projectName)
    {
        string safe = Regex.Replace((projectName ?? string.Empty).Trim(), @"[^\w\-]", "_");
        safe = safe.ToLowerInvariant();
        safe = Regex.Replace(safe, @"_+", "_");

        if (Regex.IsMatch(safe, @"^\d"))
            safe = "scrypto_" + safe;

        if (string.IsNullOrWhiteSpace(safe))
            safe = "scrypto_contract";

        return safe;
    }

    private static void ReplaceProjectName(string filePath, string projectName)
    {
        if (!File.Exists(filePath)) return;

        string text = File.ReadAllText(filePath);
        text = Regex.Replace(text, @"name\s*=\s*"".*?""", $"name = \"{projectName}\"");
        File.WriteAllText(filePath, text);
    }

    private async Task<Result<GenerateContractResponse>> CreateResponseAsync(string tempDir, string scryptoCode, JObject jObj,
        CancellationToken token = default)
    {
        DirectoryExtensions.CopyDirectory(_scTemplatePath, tempDir);
        string srcDir = Path.Combine(tempDir, "src");
        if (!Directory.Exists(srcDir))
            Directory.CreateDirectory(srcDir);

        string libPath = Path.Combine(srcDir, "lib.rs");
        await File.WriteAllTextAsync(libPath, scryptoCode, token);

        string projectName = MakeSafeProjectName(jObj["name"]?.ToString() ?? "scrypto_contract");

        string cargoPath = Path.Combine(tempDir, "Cargo.toml");
        ReplaceProjectName(cargoPath, projectName);

        string cargoText = await File.ReadAllTextAsync(cargoPath, token);
        cargoText = Regex.Replace(cargoText, @"scrypto\s*=\s*\{.*?\}", "scrypto = { version = \"1.3.0\" }",
            RegexOptions.Singleline);
        await File.WriteAllTextAsync(cargoPath, cargoText, token);

        string zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        try
        {
            ZipFile.CreateFromDirectory(tempDir, zipPath);
            byte[] zipBytes = await File.ReadAllBytesAsync(zipPath, token);

            return Result<GenerateContractResponse>.Success(new()
            {
                FileName = $"{projectName}.zip",
                Content = zipBytes,
                ContentType = MediaTypeNames.Application.Zip
            });
        }
        finally
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }

    private static JObject ProcessTemplateData(JObject originalJson)
    {
        JObject processedJson = new();

        foreach (JProperty property in originalJson.Properties())
        {
            if (property.Name != "templateData")
                processedJson[property.Name] = property.Value;
        }

        if (originalJson["templateData"] is JObject templateData)
        {
            foreach (JProperty templateProperty in templateData.Properties())
            {
                processedJson[templateProperty.Name] ??= templateProperty.Value;
            }
        }


        return processedJson;
    }

    private void AddDefaultDerive(JObject json)
    {
        string[] sections = ["structs", "enums", "events"];
        foreach (string section in sections)
        {
            if (json[section] is JArray array)
            {
                foreach (JToken item in array)
                {
                    if (item is JObject obj && obj["derive"] is JArray derive)
                    {
                        if (derive.All(d => d.ToString() != "ScryptoSbor"))
                        {
                            derive.Add("ScryptoSbor");
                        }
                    }
                }
            }
        }
    }

    private void LogProcessedJson(JObject processedJson)
    {
        try
        {
            string jsonString = processedJson.ToString(Newtonsoft.Json.Formatting.Indented);
            _logger.LogDebug("Processed JSON: {Json}", jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to log processed JSON: {Error}", ex.Message);
        }
    }
}
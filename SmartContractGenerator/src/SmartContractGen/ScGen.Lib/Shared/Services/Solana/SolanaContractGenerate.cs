namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class SolanaContractGenerate
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


    private void ReplaceProjectName(string filePath, string projectName)
    {
        if (!File.Exists(filePath)) return;
        string text = File.ReadAllText(filePath);
        text = Regex.Replace(text, @"name\s*=\s*"".*""", $"name = \"{projectName}\"");
        File.WriteAllText(filePath, text);
    }


    private void AddOrUpdateAnchorToml(string filePath, string projectName)
    {
        string defaultProgramId = "Fg6PaFpoGXkYsidMpWTK6W2BeZ7FEfcYkg476zPFsLnS";
        // Sanitize project name for TOML: replace spaces/special chars with underscores
        string sanitizedName = Regex.Replace(projectName, @"[^a-zA-Z0-9_]", "_").ToLowerInvariant();
        
        string tomlDefault = "[programs.localnet]\n" + sanitizedName + " = \"" + defaultProgramId +
                             "\"\n\n[provider]\ncluster = \"localnet\"\nwallet = \"~/.config/solana/id.json\"\n";
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, tomlDefault);
            return;
        }

        string text = File.ReadAllText(filePath);
        text = Regex.Replace(
            text,
            @"(\[programs\.localnet\][^\[]*)",
            "[programs.localnet]\n" + sanitizedName + " = \"" + defaultProgramId + "\"\n"
        );
        text = Regex.Replace(
            text,
            @"cluster\s*=\s*"".*""",
            "cluster = \"localnet\""
        );
        text = Regex.Replace(
            text,
            @"wallet\s*=\s*"".*""",
            "wallet = \"~/.config/solana/id.json\""
        );
        File.WriteAllText(filePath, text);
    }

    private async Task<Result<GenerateContractResponse>> CreateResponseAsync(string tempDir, string rustCode, JObject jObj,
        CancellationToken token = default)
    {
        string libPath = Path.Combine(tempDir, "programs", KeyNames.RustMainTemplate, "src", "lib.rs");
        string? libDir = Path.GetDirectoryName(libPath);
        if (!Directory.Exists(libDir))
            if (libDir != null)
                Directory.CreateDirectory(libDir);
        await File.WriteAllTextAsync(libPath, rustCode, token);

        string projectName = (jObj["name"]?.ToString() ?? "anchor_contract").Trim();

        ReplaceProjectName(Path.Combine(tempDir, "Cargo.toml"), projectName);

        string anchorTomlPath = Path.Combine(tempDir, "Anchor.toml");
        AddOrUpdateAnchorToml(anchorTomlPath, projectName);

        string zipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zip");
        ZipFile.CreateFromDirectory(tempDir, zipPath);

        byte[] zipBytes = await File.ReadAllBytesAsync(zipPath, token);

        Directory.Delete(tempDir, true);
        File.Delete(zipPath);


        return Result<GenerateContractResponse>.Success(new()
        {
            Content = zipBytes,
            FileName = projectName + ".zip",
            ContentType = "application/zip"
        });
    }
}
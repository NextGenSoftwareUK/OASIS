using System.Diagnostics;

namespace ScGen.API.Infrastructure.Controllers.V1;

[ApiController]
[Route($"{ApiAddresses.Base}/contracts")]
public sealed class ContractGeneratorController(IContractServiceFactory factory) : BaseController
{
    [HttpPost("generate")]
    public async Task<IActionResult> ContractGenerateAsync([FromForm] GenerateContractRequest request)
    {
        if (!request.JsonFile.IsJsonFile())
            return BadRequest(Messages.InvalidJsonFile);

        Result<GenerateContractResponse> result = await factory.GetGenerator(request.Language).GenerateAsync(request.JsonFile);

        if (!result.IsSuccess) return result.ToActionResult();

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost("compile")]
    public async Task<IActionResult> ContractCompileAsync([FromForm] CompileContractRequest request)
    {
        if (!request.Source.IsContractFile())
            return BadRequest(Messages.InvalidFormatFile);

        Result<CompileContractResponse> compileResult = await factory.GetCompiler(request.Language).CompileAsync(request.Source);

        if (!compileResult.IsSuccess) return compileResult.ToActionResult();

        return await ToZipAsync(compileResult.Value!);
    }


    [HttpPost("deploy")]
    public async Task<IActionResult> ContractDeployAsync([FromForm] DeployContractRequest request)
    {
        // Solana uses keypair as first param, other chains use schema
        var firstParam = request.Language == Lib.Shared.Enums.SmartContractLanguage.Rust 
            ? request.WalletKeypair 
            : request.Schema;
            
        return (await factory.GetDeployer(request.Language).DeployAsync(firstParam, request.CompiledContractFile))
            .ToActionResult();
    }

    [HttpGet("cache-stats")]
    public async Task<IActionResult> GetCacheStatsAsync()
    {
        try
        {
            string sccachePath = FindSccache();
            if (string.IsNullOrEmpty(sccachePath))
            {
                return Ok(new { enabled = false, message = "sccache not found" });
            }

            using Process process = new();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = sccachePath,
                Arguments = "--show-stats",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return Ok(new { enabled = false, message = "sccache error" });
            }

            var stats = ParseSccacheStats(output);
            return Ok(new
            {
                enabled = true,
                compileRequests = stats.compileRequests,
                cacheHits = stats.cacheHits,
                cacheMisses = stats.cacheMisses,
                cacheHitRate = stats.cacheHitRate,
                compilations = stats.compilations,
                averageCompileTime = stats.averageCompileTime,
                averageCacheReadTime = stats.averageCacheReadTime,
                cacheSize = stats.cacheSize
            });
        }
        catch (Exception ex)
        {
            return Ok(new { enabled = false, message = ex.Message });
        }
    }

    private static string FindSccache()
    {
        string[] possiblePaths = [
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin", "sccache"),
            "/usr/local/bin/sccache",
            "/usr/bin/sccache"
        ];

        foreach (string path in possiblePaths)
        {
            if (System.IO.File.Exists(path))
                return path;
        }

        return string.Empty;
    }

    private static (int compileRequests, int cacheHits, int cacheMisses, double cacheHitRate, int compilations, double averageCompileTime, double averageCacheReadTime, string cacheSize) ParseSccacheStats(string output)
    {
        int compileRequests = 0;
        int cacheHits = 0;
        int cacheMisses = 0;
        double cacheHitRate = 0;
        int compilations = 0;
        double averageCompileTime = 0;
        double averageCacheReadTime = 0;
        string cacheSize = "0 MiB";

        foreach (string line in output.Split('\n'))
        {
            if (line.Contains("Compile requests") && !line.Contains("executed"))
                int.TryParse(ExtractNumber(line), out compileRequests);
            else if (line.Contains("Cache hits") && line.Contains("rate") && !line.Contains("Rust"))
            {
                string rateStr = line.Split('%')[0].Trim().Split(' ').Last();
                double.TryParse(rateStr, out cacheHitRate);
            }
            else if (line.Contains("Cache hits") && !line.Contains("rate") && !line.Contains("Rust"))
                int.TryParse(ExtractNumber(line), out cacheHits);
            else if (line.Contains("Cache misses") && !line.Contains("Rust"))
                int.TryParse(ExtractNumber(line), out cacheMisses);
            else if (line.Contains("Compilations") && !line.Contains("Non-"))
                int.TryParse(ExtractNumber(line), out compilations);
            else if (line.Contains("Average compiler"))
            {
                string timeStr = line.Split('s')[0].Trim().Split(' ').Last();
                double.TryParse(timeStr, out averageCompileTime);
            }
            else if (line.Contains("Average cache read hit"))
            {
                string timeStr = line.Split('s')[0].Trim().Split(' ').Last();
                double.TryParse(timeStr, out averageCacheReadTime);
            }
            else if (line.Contains("Cache size"))
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                    cacheSize = $"{parts[^2]} {parts[^1]}";
            }
        }

        return (compileRequests, cacheHits, cacheMisses, cacheHitRate, compilations, averageCompileTime, averageCacheReadTime, cacheSize);
    }

    private static string ExtractNumber(string line)
    {
        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^1] : "0";
    }

    private async Task<IActionResult> ToZipAsync(CompileContractResponse response)
    {
        const string zipFileName = "contract_artifacts.zip";

        using MemoryStream zipStream = new();
        using (ZipArchive archive = new(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry abiEntry = archive.CreateEntry(response.ShemaFileName!);
            await using (Stream entryStream = abiEntry.Open())
                await entryStream.WriteAsync(response.Shema);

            ZipArchiveEntry binEntry = archive.CreateEntry(response.CompiledCodeFileName);
            await using (Stream entryStream = binEntry.Open()) await entryStream.WriteAsync(response.CompiledCode);
        }

        zipStream.Position = 0;

        return File(zipStream.ToArray(), MediaTypeNames.Application.Zip, zipFileName);
    }
}
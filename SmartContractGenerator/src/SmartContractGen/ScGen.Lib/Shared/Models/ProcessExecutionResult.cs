namespace ScGen.Lib.Shared.Models;

public sealed class ProcessExecutionResult
{
    public int ExitCode { get; init; }
    public string StandardOutput { get; init; } = string.Empty;
    public string StandardError { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public TimeSpan Duration { get; init; }
    public int? ProcessId { get; init; }
    public DateTime StartTime { get; init; }


    public string GetErrorMessage()
    {
        if (IsSuccess)
            return string.Empty;

        List<string> parts = [];

        if (ExitCode != 0)
            parts.Add($"Exit code: {ExitCode}");

        if (!string.IsNullOrWhiteSpace(StandardError))
            parts.Add(StandardError.Trim());

        return parts.Count > 0 ? string.Join(" - ", parts) : "Unknown error";
    }
    
    public string GetCombinedOutput()
    {
        List<string> parts = [];

        if (!string.IsNullOrWhiteSpace(StandardOutput))
            parts.Add(StandardOutput.Trim());

        if (!string.IsNullOrWhiteSpace(StandardError))
            parts.Add(StandardError.Trim());

        return string.Join(Environment.NewLine, parts);
    }

    public static ProcessExecutionResult Success(
        string output = "",
        TimeSpan? duration = null,
        int? processId = null)
    {
        return new ProcessExecutionResult
        {
            ExitCode = 0,
            StandardOutput = output,
            StandardError = "",
            IsSuccess = true,
            Duration = duration ?? TimeSpan.Zero,
            ProcessId = processId,
            StartTime = DateTime.UtcNow
        };
    }

    public static ProcessExecutionResult Failure(
        int exitCode,
        string output,
        string error,
        TimeSpan duration)
    {
        return new ProcessExecutionResult
        {
            ExitCode = exitCode,
            StandardOutput = output,
            StandardError = error,
            IsSuccess = false,
            Duration = duration,
            StartTime = DateTime.UtcNow
        };
    }

    public override string ToString()
    {
        return $"ProcessExecutionResult: {(IsSuccess ? "Success" : "Failed")} " +
               $"(ExitCode: {ExitCode}, Duration: {Duration.TotalMilliseconds:F0}ms" +
               $"{(ProcessId.HasValue ? $", PID: {ProcessId}" : "")})";
    }
}
using System.Diagnostics;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class LinuxSecretServiceProcessTests
{
    [Fact]
    public async Task CancellationStopsOwnedCredentialProcess()
    {
        var start = new ProcessStartInfo(OperatingSystem.IsWindows() ? "ping.exe" : "sleep")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in OperatingSystem.IsWindows()
            ? new[] { "-n", "31", "127.0.0.1" } : new[] { "30" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        using var cancellation = new CancellationTokenSource();
        var method = typeof(LinuxSecretServiceSecureSessionStore).GetMethod("WaitForExitAsync",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            var wait = (Task)method.Invoke(null, new object[] { process, cancellation.Token })!;
            process.HasExited.Should().BeFalse();
            cancellation.Cancel();
            Func<Task> operation = async () => await wait.WaitAsync(TimeSpan.FromSeconds(10));
            await operation.Should().ThrowAsync<OperationCanceledException>();
            process.HasExited.Should().BeTrue("cancellation must not leave the owned credential helper running");
        }
        finally
        {
            if (!process.HasExited) process.Kill();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}

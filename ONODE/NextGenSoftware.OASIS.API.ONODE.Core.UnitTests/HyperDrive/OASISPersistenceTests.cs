using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests.HyperDrive
{
    /// <summary>
    /// Regression tests for OASISPersistence.  Previously there was no persistence layer at all —
    /// in-memory state was lost on restart (quota counters reset to zero; discovered peers vanished).
    /// These tests verify the atomic save/load round-trip and the graceful missing-file behaviour.
    /// </summary>
    public class OASISPersistenceTests : IDisposable
    {
        private readonly string _tmpDir;

        public OASISPersistenceTests()
        {
            _tmpDir = Path.Combine(Path.GetTempPath(), $"oasis-persistence-test-{Guid.NewGuid()}");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tmpDir))
                Directory.Delete(_tmpDir, recursive: true);
        }

        // ── missing file ──────────────────────────────────────────────────────

        [Fact]
        public async Task LoadAsync_FileDoesNotExist_ReturnsNull()
        {
            var result = await OASISPersistence.LoadAsync<SampleState>(_tmpDir, "nonexistent.json");
            result.Should().BeNull();
        }

        // ── round-trip ────────────────────────────────────────────────────────

        [Fact]
        public async Task SaveThenLoad_RoundTripsAllFields()
        {
            var state = new SampleState { Count = 42, Label = "hello", Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

            await OASISPersistence.SaveAsync(_tmpDir, "state.json", state);
            var loaded = await OASISPersistence.LoadAsync<SampleState>(_tmpDir, "state.json");

            loaded.Should().NotBeNull();
            loaded!.Count.Should().Be(42);
            loaded.Label.Should().Be("hello");
            loaded.Timestamp.Should().Be(state.Timestamp);
        }

        [Fact]
        public async Task SaveAsync_CreatesDataDirectoryIfAbsent()
        {
            var nested = Path.Combine(_tmpDir, "sub", "dir");
            await OASISPersistence.SaveAsync(nested, "x.json", new SampleState { Count = 1 });
            Directory.Exists(nested).Should().BeTrue();
        }

        // ── overwrite ─────────────────────────────────────────────────────────

        [Fact]
        public async Task SaveAsync_OverwritesExistingFile()
        {
            await OASISPersistence.SaveAsync(_tmpDir, "counter.json", new SampleState { Count = 1 });
            await OASISPersistence.SaveAsync(_tmpDir, "counter.json", new SampleState { Count = 99 });

            var loaded = await OASISPersistence.LoadAsync<SampleState>(_tmpDir, "counter.json");
            loaded!.Count.Should().Be(99);
        }

        // ── atomic write (no .tmp file left behind) ───────────────────────────

        [Fact]
        public async Task SaveAsync_LeavesNoTempFile()
        {
            await OASISPersistence.SaveAsync(_tmpDir, "data.json", new SampleState { Count = 7 });

            Directory.GetFiles(_tmpDir, "*.tmp").Should().BeEmpty(
                "SaveAsync must rename the temp file and not leave a .tmp artefact");
        }

        // ── independent files don't collide ───────────────────────────────────

        [Fact]
        public async Task SaveAsync_TwoFiles_LoadsCorrectFile()
        {
            await OASISPersistence.SaveAsync(_tmpDir, "a.json", new SampleState { Count = 1, Label = "a" });
            await OASISPersistence.SaveAsync(_tmpDir, "b.json", new SampleState { Count = 2, Label = "b" });

            var a = await OASISPersistence.LoadAsync<SampleState>(_tmpDir, "a.json");
            var b = await OASISPersistence.LoadAsync<SampleState>(_tmpDir, "b.json");

            a!.Label.Should().Be("a");
            b!.Label.Should().Be("b");
        }

        private sealed class SampleState
        {
            public int Count { get; set; }
            public string Label { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
    }
}

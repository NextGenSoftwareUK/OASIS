using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Web6.Core.Memory
{
    /// <summary>
    /// Normalised interface for external memory providers (Mem0, Zep, Letta/MemGPT, LangMem, Graphiti).
    /// Registered providers are searched before each AI completion when ExternalMemoryProviders is set on
    /// the CompletionRequest, and injected as a [External Memory] context block into the system message.
    /// </summary>
    public interface IExternalMemoryProvider
    {
        string Name { get; }

        Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5);

        Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null);

        Task DeleteAsync(Guid avatarId, string memoryId);
    }

    public class MemoryEntry
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public double Score { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}

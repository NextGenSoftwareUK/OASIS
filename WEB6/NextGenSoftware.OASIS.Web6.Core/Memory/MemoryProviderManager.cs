using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Web6.Core.Memory
{
    /// <summary>
    /// Registers and queries all configured external memory providers.
    /// Auto-initialises adapters from environment variables on first use.
    /// </summary>
    public class MemoryProviderManager
    {
        private readonly Dictionary<string, IExternalMemoryProvider> _providers = new Dictionary<string, IExternalMemoryProvider>(StringComparer.OrdinalIgnoreCase);

        public static readonly MemoryProviderManager Instance = new MemoryProviderManager();

        private MemoryProviderManager()
        {
            AutoRegisterFromEnvironment();
        }

        public void Register(IExternalMemoryProvider provider)
        {
            _providers[provider.Name] = provider;
        }

        public IReadOnlyList<string> ProviderNames => _providers.Keys.ToList();

        public IExternalMemoryProvider Get(string name)
        {
            _providers.TryGetValue(name, out var p);
            return p;
        }

        /// <summary>
        /// Searches all requested providers (or all registered if names is null/empty) and returns merged results.
        /// </summary>
        public async Task<List<ExternalMemorySearchResult>> SearchAllAsync(Guid avatarId, string query, IList<string> providerNames = null, int topK = 5)
        {
            var targets = providerNames != null && providerNames.Count > 0
                ? providerNames.Select(n => _providers.TryGetValue(n, out var p) ? p : null).Where(p => p != null)
                : _providers.Values;

            var tasks = targets.Select(async p =>
            {
                try
                {
                    var entries = await p.SearchAsync(avatarId, query, topK);
                    return entries.Select(e => new ExternalMemorySearchResult { Provider = p.Name, Entry = e });
                }
                catch
                {
                    return Enumerable.Empty<ExternalMemorySearchResult>();
                }
            });

            var all = await Task.WhenAll(tasks);
            return all.SelectMany(r => r).OrderByDescending(r => r.Entry.Score).Take(topK * 2).ToList();
        }

        /// <summary>
        /// Builds a [External Memory] system context block from search results for injection into AI prompts.
        /// </summary>
        public static string BuildContextBlock(List<ExternalMemorySearchResult> results)
        {
            if (results == null || results.Count == 0) return null;
            var sb = new StringBuilder();
            sb.AppendLine("[External Memory]");
            foreach (var r in results)
                sb.AppendLine($"- [{r.Provider}] {r.Entry.Content}");
            sb.AppendLine("[/External Memory]");
            return sb.ToString();
        }

        private void AutoRegisterFromEnvironment()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MEM0_API_KEY")))
                Register(new Mem0Adapter());

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZEP_API_KEY")))
                Register(new ZepAdapter());

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LETTA_BASE_URL")))
                Register(new LettaAdapter());

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LANGMEM_API_KEY")))
                Register(new LangMemAdapter());

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GRAPHITI_BASE_URL")))
                Register(new GraphitiAdapter());
        }
    }

    public class ExternalMemorySearchResult
    {
        public string Provider { get; set; }
        public MemoryEntry Entry { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OGEditorSDK.MapFormat.Adapters;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Central registry of all format adapters. Holds built-in adapters and
    /// discovers community adapters from the OASIS format-adapters folder.
    /// Thread-safe for reads after initialisation.
    /// </summary>
    public class OGMapFormatRegistry
    {
        private readonly Dictionary<string, IOGMapFormatAdapter> _adapters =
            new Dictionary<string, IOGMapFormatAdapter>(StringComparer.OrdinalIgnoreCase);

        private static OGMapFormatRegistry _instance;
        public static OGMapFormatRegistry Instance => _instance ?? (_instance = CreateDefault());

        private static OGMapFormatRegistry CreateDefault()
        {
            var reg = new OGMapFormatRegistry();
            reg.Register(new QuakeAdapter());
            reg.Register(new Quake2Adapter());
            reg.Register(new Quake3Adapter());
            reg.Register(new DoomAdapter());
            reg.Register(new UDMFAdapter());
            reg.Register(new Doom3Adapter());
            reg.Register(new Duke3DAdapter());
            reg.Register(new Wolf3DAdapter());
            reg.LoadCommunityAdapters();
            return reg;
        }

        public void Register(IOGMapFormatAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            _adapters[adapter.FormatId] = adapter;
        }

        public bool TryGet(string formatId, out IOGMapFormatAdapter adapter) =>
            _adapters.TryGetValue(formatId, out adapter);

        public IOGMapFormatAdapter Get(string formatId)
        {
            if (!_adapters.TryGetValue(formatId, out var a))
                throw new InvalidOperationException($"No format adapter registered for '{formatId}'.");
            return a;
        }

        public IEnumerable<IOGMapFormatAdapter> All => _adapters.Values;

        /// <summary>
        /// Detect format from file extension. Returns null if ambiguous or unknown.
        /// </summary>
        public IOGMapFormatAdapter DetectFromFile(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            IOGMapFormatAdapter match = null;
            foreach (var a in _adapters.Values)
            {
                foreach (var e in a.FileExtensions)
                {
                    if (string.Equals(e, ext, StringComparison.OrdinalIgnoreCase)
                        && a.CanRead(filePath))
                    {
                        if (match != null) return null;  // ambiguous
                        match = a;
                    }
                }
            }
            return match;
        }

        private void LoadCommunityAdapters()
        {
            foreach (var folder in AdapterFolders())
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var dll in Directory.GetFiles(folder, "*.dll"))
                {
                    try { LoadAssemblyAdapters(dll); }
                    catch { /* community adapter load failure is non-fatal */ }
                }
            }
        }

        private static IEnumerable<string> AdapterFolders()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "OASIS", "format-adapters");
            yield return Path.Combine(
                Path.GetDirectoryName(typeof(OGMapFormatRegistry).Assembly.Location) ?? ".",
                "format-adapters");
        }

        private void LoadAssemblyAdapters(string dllPath)
        {
            var asm = Assembly.LoadFrom(dllPath);
            foreach (var type in asm.GetExportedTypes())
            {
                if (!typeof(IOGMapFormatAdapter).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                    continue;
                try
                {
                    var adapter = (IOGMapFormatAdapter)Activator.CreateInstance(type);
                    Register(adapter);
                }
                catch { }
            }
        }
    }
}

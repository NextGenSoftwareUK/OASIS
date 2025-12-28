using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Resolves library dependencies and ensures proper load order
    /// </summary>
    public class LibraryDependencyResolver
    {
        private readonly Dictionary<string, LibraryDependencyInfo> _dependencyGraph;
        private readonly object _lockObject = new object();

        public LibraryDependencyResolver()
        {
            _dependencyGraph = new Dictionary<string, LibraryDependencyInfo>();
        }

        /// <summary>
        /// Registers a library and its dependencies
        /// </summary>
        public void RegisterLibrary(string libraryId, string libraryName, IEnumerable<string> dependencies = null)
        {
            lock (_lockObject)
            {
                if (!_dependencyGraph.TryGetValue(libraryId, out var info))
                {
                    info = new LibraryDependencyInfo
                    {
                        LibraryId = libraryId,
                        LibraryName = libraryName
                    };
                    _dependencyGraph[libraryId] = info;
                }

                if (dependencies != null)
                {
                    info.Dependencies = dependencies.ToList();
                }
            }
        }

        /// <summary>
        /// Resolves load order for libraries based on dependencies
        /// Returns libraries in order they should be loaded (dependencies first)
        /// </summary>
        public OASISResult<List<string>> ResolveLoadOrder(string libraryId)
        {
            var result = new OASISResult<List<string>>();

            try
            {
                lock (_lockObject)
                {
                    var loadOrder = new List<string>();
                    var visited = new HashSet<string>();
                    var visiting = new HashSet<string>();

                    if (!_dependencyGraph.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Library {libraryId} not found in dependency graph.");
                        return result;
                    }

                    ResolveDependenciesRecursive(libraryId, loadOrder, visited, visiting);

                    result.Result = loadOrder;
                    result.Message = $"Resolved load order for {loadOrder.Count} libraries.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error resolving dependencies: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Resolves dependencies recursively using topological sort
        /// </summary>
        private void ResolveDependenciesRecursive(
            string libraryId,
            List<string> loadOrder,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (visited.Contains(libraryId))
                return;

            if (visiting.Contains(libraryId))
            {
                throw new InvalidOperationException($"Circular dependency detected involving library: {libraryId}");
            }

            visiting.Add(libraryId);

            if (_dependencyGraph.TryGetValue(libraryId, out var info) && info.Dependencies != null)
            {
                foreach (var depId in info.Dependencies)
                {
                    ResolveDependenciesRecursive(depId, loadOrder, visited, visiting);
                }
            }

            visiting.Remove(libraryId);
            visited.Add(libraryId);
            loadOrder.Add(libraryId);
        }

        /// <summary>
        /// Gets all dependencies for a library (transitive)
        /// </summary>
        public OASISResult<List<string>> GetAllDependencies(string libraryId)
        {
            var result = new OASISResult<List<string>>();

            try
            {
                lock (_lockObject)
                {
                    var dependencies = new HashSet<string>();
                    var visited = new HashSet<string>();

                    if (!_dependencyGraph.ContainsKey(libraryId))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Library {libraryId} not found in dependency graph.");
                        return result;
                    }

                    CollectDependenciesRecursive(libraryId, dependencies, visited);

                    result.Result = dependencies.ToList();
                    result.Message = $"Found {dependencies.Count} dependencies.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error collecting dependencies: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Collects all dependencies recursively
        /// </summary>
        private void CollectDependenciesRecursive(
            string libraryId,
            HashSet<string> dependencies,
            HashSet<string> visited)
        {
            if (visited.Contains(libraryId))
                return;

            visited.Add(libraryId);

            if (_dependencyGraph.TryGetValue(libraryId, out var info) && info.Dependencies != null)
            {
                foreach (var depId in info.Dependencies)
                {
                    dependencies.Add(depId);
                    CollectDependenciesRecursive(depId, dependencies, visited);
                }
            }
        }

        /// <summary>
        /// Checks if a library has dependencies
        /// </summary>
        public bool HasDependencies(string libraryId)
        {
            lock (_lockObject)
            {
                return _dependencyGraph.TryGetValue(libraryId, out var info) &&
                       info.Dependencies != null &&
                       info.Dependencies.Any();
            }
        }

        /// <summary>
        /// Gets direct dependencies for a library
        /// </summary>
        public List<string> GetDirectDependencies(string libraryId)
        {
            lock (_lockObject)
            {
                if (_dependencyGraph.TryGetValue(libraryId, out var info) && info.Dependencies != null)
                {
                    return info.Dependencies.ToList();
                }
                return new List<string>();
            }
        }

        /// <summary>
        /// Clears the dependency graph
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _dependencyGraph.Clear();
            }
        }

        private class LibraryDependencyInfo
        {
            public string LibraryId { get; set; }
            public string LibraryName { get; set; }
            public List<string> Dependencies { get; set; } = new List<string>();
        }
    }
}



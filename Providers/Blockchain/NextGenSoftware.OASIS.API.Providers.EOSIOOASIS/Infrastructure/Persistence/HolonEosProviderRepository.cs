using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using EosSharp;
using EosSharp.Core;
using EosSharp.Core.Api.v1;
using EosSharp.Core.Providers;
using Newtonsoft.Json;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetTableRows;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Repository;
using NextGenSoftware.OASIS.Common;
using Action = EosSharp.Core.Api.v1.Action;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.Persistence
{
    /// <summary>
    ///     Repository contains CRUD methods for Holon entity.
    /// </summary>
    public class HolonEosProviderRepository : IEosProviderRepository<HolonDto>
    {
        private static readonly string _holonTable = "holon";
        private readonly EosBase _eos;

        private readonly string _eosAccountName;
        private readonly IEosClient _eosClient;

        public HolonEosProviderRepository(IEosClient eosClient, string eosAccountName, string eosChainUrl,
            string eosChainId, string eosAccountPk)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosAccountName = eosAccountName;

            _eos = new CompatibleEosClient(new EosConfigurator
            {
                HttpEndpoint = eosChainUrl.TrimEnd('/'),
                ChainId = eosChainId,
                ExpireSeconds = 60,
                SignProvider = new DefaultSignProvider(eosAccountPk)
            });
        }

        public async Task Create(HolonDto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var pushTransactionResult = await _eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _eosAccountName,
                        authorization = new List<PermissionLevel>
                        {
                            new()
                            {
                                actor = _eosAccountName,
                                permission = "active"
                            }
                        },
                        name = "addholon",
                        data = new
                        {
                            entityId = entity.EntityId,
                            holonId = entity.HolonId,
                            info = entity.Info
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Holon creating request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}.", LogType.Info);
        }

        public async Task Update(HolonDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var holonEntityId = HashUtility.GetNumericHash(id);
            var pushTransactionResult = await _eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _eosAccountName,
                        authorization = new List<PermissionLevel>
                        {
                            new()
                            {
                                actor = _eosAccountName,
                                permission = "active"
                            }
                        },
                        name = "setholon",
                        data = new
                        {
                            entityId = holonEntityId,
                            info = entity.Info
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Holon updating request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}.", LogType.Info);
        }

        public async Task<HolonDto> Read(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A nonempty holon ID is required.", nameof(id));

            var request = new GetTableRowsRequestDto { Code = _eosAccountName, Scope = _eosAccountName, Table = _holonTable };
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var page = await _eosClient.GetTableRows<HolonDto>(request).ConfigureAwait(false);
                if (page?.Rows == null || !page.More.HasValue)
                    throw new InvalidOperationException("EOSIO did not return a complete holon table page.");

                var match = page.Rows.FirstOrDefault(row => Guid.TryParse(row.HolonId, out var rowId) && rowId == id);
                if (match != null)
                    return match;
                if (!page.More.Value)
                    return null;
                if (string.IsNullOrWhiteSpace(page.NextKey) || !cursors.Add(page.NextKey))
                    throw new InvalidOperationException("EOSIO returned an invalid holon table continuation.");
                request.LowerBound = page.NextKey;
            }
        }

        public async Task<ImmutableArray<HolonDto>> ReadAll()
        {
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _holonTable
            });
            return holonTableRows.Rows.ToImmutableArray();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All)
        {
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _holonTable
            });

            var filteredHolons = holonTableRows.Rows.Where(holon =>
            {

                // Parse Info as metadata dictionary
                Dictionary<string, string>? holonMeta = null;
                if (!string.IsNullOrEmpty(holon.Info))
                {
                    try
                    {
                        holonMeta = JsonConvert.DeserializeObject<Dictionary<string, string>>(holon.Info);
                    }
                    catch
                    {
                        // Ignore malformed Info
                        return false;
                    }
                }
                if (holonMeta == null)
                    return false;

                // Match metadata key-value pairs
                if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
                    return true;

                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                {
                    // All key-value pairs must match
                    return metaKeyValuePairs.All(kv => holonMeta.ContainsKey(kv.Key) && holonMeta[kv.Key] == kv.Value);
                }
                else
                {
                    // Any key-value pair matches
                    return metaKeyValuePairs.Any(kv => holonMeta.ContainsKey(kv.Key) && holonMeta[kv.Key] == kv.Value);
                }
            });

            return filteredHolons.ToImmutableArray();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All)
        {
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _holonTable
            });

            var filteredHolons = holonTableRows.Rows.Where(holon =>
            {

                // Parse Info as metadata dictionary
                Dictionary<string, string>? holonMeta = null;
                if (!string.IsNullOrEmpty(holon.Info))
                {
                    try
                    {
                        holonMeta = JsonConvert.DeserializeObject<Dictionary<string, string>>(holon.Info);
                    }
                    catch
                    {
                        // Ignore malformed Info
                        return false;
                    }
                }
                if (holonMeta == null)
                    return false;

                // Match metadata key-value pair
                if (string.IsNullOrEmpty(metaKey))
                    return true;

                return holonMeta.ContainsKey(metaKey) && holonMeta[metaKey] == metaValue;
            });

            return filteredHolons.ToImmutableArray();
        }

        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var holonEntityId = HashUtility.GetNumericHash(id);
            var pushTransactionResult = await _eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _eosAccountName,
                        authorization = new List<PermissionLevel>
                        {
                            new()
                            {
                                actor = _eosAccountName,
                                permission = "active"
                            }
                        },
                        name = "softholon",
                        data = new
                        {
                            entityId = holonEntityId
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Holon soft-deleting request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {id}", LogType.Info);
        }

        public async Task DeleteHard(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A nonempty holon ID is required.", nameof(id));

            var existing = await Read(id).ConfigureAwait(false);
            if (existing == null)
                throw new KeyNotFoundException("The EOSIO holon does not exist.");
            var holonEntityId = existing.EntityId;
            var pushTransactionResult = await _eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _eosAccountName,
                        authorization = new List<PermissionLevel>
                        {
                            new()
                            {
                                actor = _eosAccountName,
                                permission = "active"
                            }
                        },
                        name = "hardholon",
                        data = new
                        {
                            entityId = holonEntityId
                        }
                    }
                }
            }).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pushTransactionResult))
                throw new InvalidOperationException("EOSIO did not acknowledge the holon deletion transaction.");
            HolonDto remaining;
            try
            {
                remaining = await Read(id).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"EOSIO holon deletion {pushTransactionResult} was submitted but readback failed.", error);
            }
            if (remaining != null)
                throw new InvalidOperationException($"EOSIO holon deletion {pushTransactionResult} has not been confirmed by table readback.");

            LoggingManager.Log(
                "Holon hard-deleting request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {id}", LogType.Info);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            _eosClient.Dispose();
        }

        ~HolonEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}

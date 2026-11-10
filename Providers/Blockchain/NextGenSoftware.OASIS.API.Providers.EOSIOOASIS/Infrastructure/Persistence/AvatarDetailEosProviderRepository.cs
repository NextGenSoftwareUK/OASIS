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
    ///     Repository contains CRUD methods for AvatarDetail entity.
    /// </summary>
    public class AvatarDetailEosProviderRepository : IEosProviderRepository<AvatarDetailDto>
    {
        private static readonly string _avatarDetailTable = "avatardetail";
        private readonly EosBase _eos;
        private readonly string _eosAccountName;
        private readonly IEosClient _eosClient;

        public AvatarDetailEosProviderRepository(IEosClient eosClient, string eosAccountName, string eosChainUrl,
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

        public Task Create(AvatarDetailDto entity) => WriteDetailAsync(entity, "adddetail");

        private async Task WriteDetailAsync(AvatarDetailDto entity, string action)
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
                        name = action,
                        data = new
                        {
                            entityId = entity.EntityId,
                            holonId = entity.AvatarId,
                            info = entity.Info
                        }
                    }
                }
            });

            if (string.IsNullOrWhiteSpace(pushTransactionResult))
                throw new InvalidOperationException("EOSIO avatar detail write returned no transaction ID.");

            LoggingManager.Log(
                "Avatar detail request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}.", LogType.Info);
        }

        public async Task<AvatarDetailDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var avatarDetailId = HashUtility.GetNumericHash(id);
            var avatarDetailTableRows = await _eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarDetailTable,
                LowerBound = unchecked((uint)avatarDetailId).ToString(System.Globalization.CultureInfo.InvariantCulture),
                Limit = 1
            });
            return avatarDetailTableRows.Rows.FirstOrDefault(avatarDetailDto =>
                avatarDetailDto.EntityId == avatarDetailId && Guid.TryParse(avatarDetailDto.AvatarId, out var owner) && owner == id);
        }

        public async Task<ImmutableArray<AvatarDetailDto>> ReadAll()
        {
            var request = new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarDetailTable
            };
            var rows = ImmutableArray.CreateBuilder<AvatarDetailDto>();
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var page = await _eosClient.GetTableRows<AvatarDetailDto>(request).ConfigureAwait(false);
                if (page?.Rows == null || !page.More.HasValue)
                    throw new InvalidOperationException("EOSIO did not return a complete avatar detail table page.");

                rows.AddRange(page.Rows);
                if (!page.More.Value)
                    return rows.ToImmutable();
                if (string.IsNullOrWhiteSpace(page.NextKey) || !cursors.Add(page.NextKey))
                    throw new InvalidOperationException("EOSIO returned an invalid avatar detail table continuation.");
                request.LowerBound = page.NextKey;
            }
        }

        public Task Update(AvatarDetailDto entity, Guid id)
            => WriteDetailAsync(entity, "setdetail");

        // Soft delete method not supported by avatar detail entity
        public async Task DeleteSoft(Guid id)
        {
            // Avatar detail is read-only on EOSIO - return without error
            await Task.CompletedTask;
        }

        public async Task DeleteHard(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A nonempty avatar ID is required.", nameof(id));

            var transactionId = await _eos.CreateTransaction(new Transaction
            {
                actions = new List<Action>
                {
                    new()
                    {
                        account = _eosAccountName,
                        authorization = new List<PermissionLevel>
                        {
                            new() { actor = _eosAccountName, permission = "active" }
                        },
                        name = "harddetail",
                        data = new { entityId = HashUtility.GetNumericHash(id), holonId = id.ToString() }
                    }
                }
            }).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new InvalidOperationException("EOSIO avatar detail deletion returned no transaction ID.");
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        // Release http connections, to avoid socket descriptor leak.
        private void ReleaseUnmanagedResources()
        {
            _eosClient.Dispose();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All)
        {
            // Get all avatar details from EOSIO chain
            var avatarDetailTableRows = await _eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarDetailTable
            });

            // Filter by metadata - EOSIO stores metadata in Info field (JSON string)
            var filteredAvatarDetails = avatarDetailTableRows.Rows.Where(avatarDetail =>
            {
                try
                {
                    if (string.IsNullOrEmpty(avatarDetail.Info))
                        return false;

                    // Parse Info JSON to check metadata
                    var infoDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarDetail.Info);
                    if (infoDict == null)
                        return false;

                    if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                    {
                        return metaKeyValuePairs.All(kvp => 
                            infoDict.ContainsKey(kvp.Key) && 
                            infoDict[kvp.Key]?.ToString() == kvp.Value);
                    }
                    else // Or
                    {
                        return metaKeyValuePairs.Any(kvp => 
                            infoDict.ContainsKey(kvp.Key) && 
                            infoDict[kvp.Key]?.ToString() == kvp.Value);
                    }
                }
                catch
                {
                    return false;
                }
            });

            // Convert AvatarDetailDto to HolonDto (they share similar structure)
            var holonDtos = filteredAvatarDetails.Select(avatarDetail => new HolonDto
            {
                EntityId = avatarDetail.EntityId,
                Info = avatarDetail.Info
            });

            return holonDtos.ToImmutableArray();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All)
        {
            // Use the dictionary version with single key-value pair
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await ReadAllByMetaData(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type);
        }

        ~AvatarDetailEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}

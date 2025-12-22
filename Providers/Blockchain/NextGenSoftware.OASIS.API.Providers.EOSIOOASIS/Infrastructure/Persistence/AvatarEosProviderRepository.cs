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
    ///     Repository contains CRUD methods for Avatar entity.
    /// </summary>
    public class AvatarEosProviderRepository : IEosProviderRepository<AvatarDto>
    {
        private static readonly string _avatarTable = "avatar";
        private readonly Eos _eos;

        private readonly string _eosAccountName;
        private readonly IEosClient _eosClient;

        public AvatarEosProviderRepository(IEosClient eosClient, string eosAccountName, string eosChainUrl,
            string eosChainId, string eosAccountPk)
        {
            _eosClient = eosClient ?? throw new ArgumentNullException(nameof(eosClient));
            _eosAccountName = eosAccountName;

            _eos = new Eos(new EosConfigurator
            {
                HttpEndpoint = eosChainUrl,
                ChainId = eosChainId,
                ExpireSeconds = 60,
                SignProvider = new DefaultSignProvider(eosAccountPk)
            });
        }

        public async Task Create(AvatarDto entity)
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
                        name = "addavatar",
                        data = new
                        {
                            entityId = entity.EntityId,
                            avatarId = entity.AvatarId,
                            info = entity.Info
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Avatar creating request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task Update(AvatarDto entity, Guid id)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var avatarId = HashUtility.GetNumericHash(id);
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
                        name = "setavatar",
                        data = new
                        {
                            entityId = avatarId,
                            info = entity.Info
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Avatar updating request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task<AvatarDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var avatarId = HashUtility.GetNumericHash(id);
            var avatarTableRows = await _eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarTable
            });
            return avatarTableRows.Rows.FirstOrDefault(avatarDetailDto => avatarDetailDto.EntityId == avatarId);
        }

        public async Task<ImmutableArray<AvatarDto>> ReadAll()
        {
            var avatarTableRows = await _eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarTable
            });
            return avatarTableRows.Rows.ToImmutableArray();
        }

        public async Task DeleteSoft(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var avatarId = HashUtility.GetNumericHash(id);
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
                        name = "softavatar",
                        data = new
                        {
                            entityId = avatarId
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Avatar soft-deleting request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {id}", LogType.Info);
        }

        public async Task DeleteHard(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var avatarId = HashUtility.GetNumericHash(id);
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
                        name = "hardavatar",
                        data = new
                        {
                            entityId = avatarId
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Avatar hard-deleting request was sent. " +
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

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All)
        {
            // Get all avatars from EOSIO chain
            var avatarTableRows = await _eosClient.GetTableRows<AvatarDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarTable
            });

            // Filter by metadata - EOSIO stores metadata in Info field (JSON string)
            var filteredAvatars = avatarTableRows.Rows.Where(avatar =>
            {
                try
                {
                    if (string.IsNullOrEmpty(avatar.Info))
                        return false;

                    // Parse Info JSON to check metadata
                    var infoDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatar.Info);
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

            // Convert AvatarDto to HolonDto (they share similar structure)
            var holonDtos = filteredAvatars.Select(avatar => new HolonDto
            {
                EntityId = avatar.EntityId,
                Info = avatar.Info
            });

            return holonDtos.ToImmutableArray();
        }

        public async Task<ImmutableArray<HolonDto>> ReadAllByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All)
        {
            // Use the dictionary version with single key-value pair
            var metaKeyValuePairs = new Dictionary<string, string> { { metaKey, metaValue } };
            return await ReadAllByMetaData(metaKeyValuePairs, MetaKeyValuePairMatchMode.All, type);
        }

        ~AvatarEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}
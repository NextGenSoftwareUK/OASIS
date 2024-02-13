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
        private readonly Eos _eos;

        private readonly string _eosAccountName;
        private readonly IEosClient _eosClient;

        public HolonEosProviderRepository(IEosClient eosClient, string eosAccountName, string eosChainUrl,
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
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
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
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
        }

        public async Task<HolonDto> Read(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var holonId = HashUtility.GetNumericHash(id);
            var holonTableRows = await _eosClient.GetTableRows<HolonDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _holonTable
            });
            return holonTableRows.Rows.FirstOrDefault(avatarDetailDto => avatarDetailDto.EntityId == holonId);
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

        // TODO: Implement Send/Push transaction within AbiJsonToBin
        public async Task DeleteHard(Guid id)
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
                        name = "hardholon",
                        data = new
                        {
                            entityId = holonEntityId
                        }
                    }
                }
            });

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
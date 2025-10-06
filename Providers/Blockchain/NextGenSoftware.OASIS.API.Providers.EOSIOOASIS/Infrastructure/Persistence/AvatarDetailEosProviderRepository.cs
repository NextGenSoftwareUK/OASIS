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
        private readonly Eos _eos;
        private readonly string _eosAccountName;
        private readonly IEosClient _eosClient;

        public AvatarDetailEosProviderRepository(IEosClient eosClient, string eosAccountName, string eosChainUrl,
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

        public async Task Create(AvatarDetailDto entity)
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
                        name = "adddetail",
                        data = new
                        {
                            entityId = entity.EntityId,
                            holonId = entity.AvatarId,
                            info = entity.Info
                        }
                    }
                }
            });

            LoggingManager.Log(
                "Avatar detail request was sent. " +
                $"Received transaction hash response: {pushTransactionResult}. " +
                $"Request sent: {JsonConvert.SerializeObject(entity)}", LogType.Info);
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
                Table = _avatarDetailTable
            });
            return avatarDetailTableRows.Rows.FirstOrDefault(avatarDetailDto =>
                avatarDetailDto.EntityId == avatarDetailId);
        }

        public async Task<ImmutableArray<AvatarDetailDto>> ReadAll()
        {
            var avatarDetailTableRows = await _eosClient.GetTableRows<AvatarDetailDto>(new GetTableRowsRequestDto
            {
                Code = _eosAccountName,
                Scope = _eosAccountName,
                Table = _avatarDetailTable
            });
            return avatarDetailTableRows.Rows.ToImmutableArray();
        }

        // Update method not supported for avatar detail entity
        public async Task Update(AvatarDetailDto entity, Guid id)
        {
            throw new NotImplementedException();
        }

        // Soft delete method not supported by avatar detail entity
        public async Task DeleteSoft(Guid id)
        {
            throw new NotImplementedException();
        }

        // Hard delete method not supported by avatar detail entity
        public async Task DeleteHard(Guid id)
        {
            throw new NotImplementedException();
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

        ~AvatarDetailEosProviderRepository()
        {
            ReleaseUnmanagedResources();
        }
    }
}
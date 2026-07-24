using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class IdentityGrpcService : IdentityService.IdentityServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        public override Task<DidReply> CreateDid(CreateDidRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (avatarId == Guid.Empty)
                    return Task.FromResult(new DidReply { IsError = true, Message = "avatarId is required" });
                var manager = new DidManager(avatarId, DNA);
                var result = manager.CreateDid(avatarId);
                if (result.IsError) return Task.FromResult(new DidReply { IsError = true, Message = result.Message });
                return Task.FromResult(new DidReply
                {
                    IsError = false,
                    Did = result.Result?.Id ?? "",
                    DocumentJson = JsonSerializer.Serialize(result.Result)
                });
            }
            catch (Exception ex) { return Task.FromResult(new DidReply { IsError = true, Message = ex.Message }); }
        }

        public override async Task<DidReply> ResolveDid(ResolveDidRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Did))
                    return new DidReply { IsError = true, Message = "did is required" };
                var manager = new DidManager(Guid.Empty, DNA);
                var result = await manager.ResolveDid(request.Did);
                if (result.IsError) return new DidReply { IsError = true, Message = result.Message };
                return new DidReply
                {
                    IsError = false,
                    Did = result.Result?.Id ?? request.Did,
                    DocumentJson = JsonSerializer.Serialize(result.Result)
                };
            }
            catch (Exception ex) { return new DidReply { IsError = true, Message = ex.Message }; }
        }

        public override Task<VcReply> IssueVc(IssueVcRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SubjectDid))
                    return Task.FromResult(new VcReply { IsError = true, Message = "subjectDid is required" });
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (avatarId == Guid.Empty)
                    return Task.FromResult(new VcReply { IsError = true, Message = "avatarId is required" });

                Dictionary<string, object> claims = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(request.ClaimsJson))
                {
                    try { claims = JsonSerializer.Deserialize<Dictionary<string, object>>(request.ClaimsJson) ?? claims; }
                    catch { /* ignore parse error, use empty claims */ }
                }

                var manager = new DidManager(avatarId, DNA);
                var result = manager.IssueCredential(avatarId, request.SubjectDid, claims);
                if (result.IsError) return Task.FromResult(new VcReply { IsError = true, Message = result.Message });
                return Task.FromResult(new VcReply { IsError = false, CredentialJson = JsonSerializer.Serialize(result.Result) });
            }
            catch (Exception ex) { return Task.FromResult(new VcReply { IsError = true, Message = ex.Message }); }
        }

        public override Task<VcVerifyReply> VerifyVc(VerifyVcRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CredentialJson))
                    return Task.FromResult(new VcVerifyReply { IsError = true, Message = "credential_json is required" });
                if (!Guid.TryParse(request.IssuerAvatarId, out var issuerAvatarId) || issuerAvatarId == Guid.Empty)
                    return Task.FromResult(new VcVerifyReply { IsError = true, Message = "issuerAvatarId is required" });

                var credential = JsonSerializer.Deserialize<NextGenSoftware.OASIS.Web6.Core.Managers.VerifiableCredential>(request.CredentialJson);
                if (credential == null)
                    return Task.FromResult(new VcVerifyReply { IsError = true, Message = "Failed to parse credential JSON" });

                var manager = new DidManager(Guid.Empty, DNA);
                var result = manager.VerifyCredential(credential, issuerAvatarId);
                if (result.IsError) return Task.FromResult(new VcVerifyReply { IsError = true, Message = result.Message });
                return Task.FromResult(new VcVerifyReply { IsError = false, Valid = result.Result });
            }
            catch (Exception ex) { return Task.FromResult(new VcVerifyReply { IsError = true, Message = ex.Message }); }
        }

        public override async Task<BaseReply> UpsertKey(UpsertKeyGrpcRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ApiKey))
                    return new BaseReply { IsError = true, Message = "Provider and ApiKey are required" };
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (avatarId == Guid.Empty)
                    return new BaseReply { IsError = true, Message = "avatarId is required" };
                var vault = new KeyVaultManager(avatarId, DNA);
                var result = await vault.SaveProviderKeyAsync(request.Provider.ToLowerInvariant(), request.ApiKey);
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<StringListReply> ListKeyProviders(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var vault = new KeyVaultManager(Guid.Empty, DNA);
                var result = await vault.ListStoredProvidersAsync();
                if (result.IsError) return new StringListReply { IsError = true, Message = result.Message };
                var reply = new StringListReply { IsError = false };
                reply.Values.AddRange(result.Result ?? new List<string>());
                return reply;
            }
            catch (Exception ex) { return new StringListReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> DeleteKey(DeleteKeyRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Provider))
                    return new BaseReply { IsError = true, Message = "provider is required" };
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (avatarId == Guid.Empty)
                    return new BaseReply { IsError = true, Message = "avatarId is required" };
                var vault = new KeyVaultManager(avatarId, DNA);
                var result = await vault.DeleteProviderKeyAsync(request.Provider.ToLowerInvariant());
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }
    }
}

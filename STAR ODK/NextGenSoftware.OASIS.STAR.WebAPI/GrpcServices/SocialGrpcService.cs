using Grpc.Core;
using NextGenSoftware.OASIS.STAR.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GrpcServices
{
    /// <summary>
    /// gRPC service for STAR social features: messaging, STAR inventory items, eggs, and parks.
    /// Messaging and egg APIs are pending underlying manager stabilisation — these stubs return
    /// a clear "not yet available" response so callers can detect the state gracefully.
    /// Parks are implemented via the STAR celestial data layer.
    /// </summary>
    public class SocialGrpcService : SocialService.SocialServiceBase
    {
        // ── Messaging ─────────────────────────────────────────────────────────

        public override Task<MessageListResponse> GetMessages(SocialEmptyMsg request, ServerCallContext context)
        {
            return Task.FromResult(new MessageListResponse
            {
                IsError = true,
                Message = "Messaging API is not yet available in this release."
            });
        }

        public override Task<SocialBoolMsg> SendMessage(SendMessageMsg request, ServerCallContext context)
        {
            return Task.FromResult(new SocialBoolMsg
            {
                IsError = true,
                Message = "Messaging API is not yet available in this release.",
                Result = false
            });
        }

        public override Task<MessageResponse> GetMessageById(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new MessageResponse
            {
                IsError = true,
                Message = "Messaging API is not yet available in this release."
            });
        }

        public override Task<SocialBoolMsg> DeleteMessage(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new SocialBoolMsg
            {
                IsError = true,
                Message = "Messaging API is not yet available in this release.",
                Result = false
            });
        }

        // ── STAR Inventory Items ───────────────────────────────────────────────

        public override Task<InventoryItemListResponse> GetAllInventoryItems(SocialEmptyMsg request, ServerCallContext context)
        {
            return Task.FromResult(new InventoryItemListResponse
            {
                IsError = true,
                Message = "STAR inventory items API is not yet available in this release."
            });
        }

        public override Task<InventoryItemRsp> GetInventoryItemById(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new InventoryItemRsp
            {
                IsError = true,
                Message = "STAR inventory items API is not yet available in this release."
            });
        }

        public override Task<InventoryItemRsp> CreateInventoryItem(InventoryItemMsg request, ServerCallContext context)
        {
            return Task.FromResult(new InventoryItemRsp
            {
                IsError = true,
                Message = "STAR inventory items API is not yet available in this release."
            });
        }

        public override Task<InventoryItemRsp> UpdateInventoryItem(InventoryItemMsg request, ServerCallContext context)
        {
            return Task.FromResult(new InventoryItemRsp
            {
                IsError = true,
                Message = "STAR inventory items API is not yet available in this release."
            });
        }

        public override Task<SocialBoolMsg> DeleteInventoryItem(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new SocialBoolMsg
            {
                IsError = true,
                Message = "STAR inventory items API is not yet available in this release.",
                Result = false
            });
        }

        // ── Eggs ─────────────────────────────────────────────────────────────

        public override Task<EggListResponse> GetAllEggs(SocialEmptyMsg request, ServerCallContext context)
        {
            return Task.FromResult(new EggListResponse
            {
                IsError = true,
                Message = "Eggs API is not yet available in this release."
            });
        }

        public override Task<EggResponse> GetEgg(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new EggResponse
            {
                IsError = true,
                Message = "Eggs API is not yet available in this release."
            });
        }

        public override Task<EggResponse> CreateEgg(EggMessage request, ServerCallContext context)
        {
            return Task.FromResult(new EggResponse
            {
                IsError = true,
                Message = "Eggs API is not yet available in this release."
            });
        }

        public override Task<SocialBoolMsg> DeleteEgg(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new SocialBoolMsg
            {
                IsError = true,
                Message = "Eggs API is not yet available in this release.",
                Result = false
            });
        }

        // ── Parks ─────────────────────────────────────────────────────────────

        public override Task<ParkListResponse> GetAllParks(SocialEmptyMsg request, ServerCallContext context)
        {
            return Task.FromResult(new ParkListResponse
            {
                IsError = true,
                Message = "Parks API is not yet available via gRPC in this release."
            });
        }

        public override Task<ParkResponse> GetPark(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new ParkResponse
            {
                IsError = true,
                Message = "Parks API is not yet available via gRPC in this release."
            });
        }

        public override Task<ParkResponse> CreatePark(ParkMessage request, ServerCallContext context)
        {
            return Task.FromResult(new ParkResponse
            {
                IsError = true,
                Message = "Parks API is not yet available via gRPC in this release."
            });
        }

        public override Task<SocialBoolMsg> DeletePark(SocialIdMsg request, ServerCallContext context)
        {
            return Task.FromResult(new SocialBoolMsg
            {
                IsError = true,
                Message = "Parks API is not yet available via gRPC in this release.",
                Result = false
            });
        }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    //TODO: PLEASE MOVE TO OASIS.CORE ASAP, ALL SERVICES SHOULD BE CONVERTED TO MANAGERS IN OASIS.CORE so they can be shared by other endpoints such as gRPC, GraphQL etc...
//    public class OlandService : IOlandService
//    {
//        private readonly OLANDManager _olandManager;
//        public OlandService()
//        {
//            _olandManager = new OLANDManager();
//        }
        
//        public async Task<OASISResult<IEnumerable<IOLand>>> GetAllOlands()
//        {
//            return await _olandManager.LoadAllOlands();
//        }

//        public async Task<OASISResult<IOLand>> GetOland(Guid olandId)
//        {
//            return await _olandManager.LoadOland(olandId);
//        }

//        public async Task<OASISResult<bool>> DeleteOland(Guid id)
//        {
//            return await _olandManager.DeleteOland(id);
//        }

//        public async Task<OASISResult<string>> CreateOland(ManageOlandUnitRequestDto request)
//        {
//            return await _olandManager.SaveOland(new Oland()
//            {
//                IsRemoved = false,
//                Discount = request.Discount,
//                Price = request.Price,
//                OlandsCount = request.OlandsCount,
//                RightSize = request.RightSize,
//                TopSize = request.TopSize,
//                UnitOfMeasure = request.UnitOfMeasure
//            });
//        }

//        public async Task<OASISResult<string>> UpdateOland(ManageOlandUnitRequestDto request, Guid id)
//        {
//            return await _olandManager.UpdateOland(new Oland()
//            {
//                Id = id,
//                IsRemoved = request.IsRemoved,
//                Discount = request.Discount,
//                Price = request.Price,
//                OlandsCount = request.OlandsCount,
//                RightSize = request.RightSize,
//                TopSize = request.TopSize,
//                UnitOfMeasure = request.UnitOfMeasure
//            });        
//        }
//    }
//}
// ═══════════════════════════════════════════════════════════════════════════════
// ██████  URGENT REVIEW REQUIRED  -  CODE LOST IN A FILE-SPLIT REFACTOR  ██████
// ═══════════════════════════════════════════════════════════════════════════════
//
//  THE 2 METHOD(S) BELOW WERE **DELETED** BY A "SPLIT INTO PARTIAL CLASS FILES"
//  COMMIT AND WERE NEVER RE-ADDED. THEY ARE RESTORED HERE **COMMENTED OUT**, SO
//  NOTHING CHANGES AT RUNTIME UNTIL EACH ONE HAS BEEN REVIEWED.
//
//  >>> ACTION REQUIRED - DECIDE PER METHOD: <<<
//        (A) UNCOMMENT AND KEEP  - the behaviour is still wanted
//        (B) DELETE THE BLOCK    - genuinely superseded or redundant
//
//  NONE OF THESE ARE CALLED ANYWHERE TODAY, SO THE BUILD IS GREEN EITHER WAY.
//  DO NOT LEAVE THIS FILE UNREVIEWED.
//
//  Dropped by : 7b673485a
//  Original   : NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs
//  Restored   : 2026-09-04   (full audit: Docs/FILE_SPLIT_LOST_METHODS.md)
// ═══════════════════════════════════════════════════════════════════════════════

// using System;
// using System.Collections.Generic;
// using System.Linq.Expressions;
// using System.Linq;
// using System.Threading.Tasks;
// using MongoDB.Driver;
// using NextGenSoftware.OASIS.API.Core.Helpers;
// using NextGenSoftware.OASIS.API.Core.Managers;
// using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
// using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
// using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
// using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
// using NextGenSoftware.OASIS.Common;
// using NextGenSoftware.Utilities;

// namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
// {
    // Restored members for AvatarRepository - ALL COMMENTED OUT PENDING REVIEW.
//     public partial class AvatarRepository
//     {
        // ─────────────────────────────────────────────────────────────────
        // REVIEW: GetAllAvatarDetail   (deleted by 7b673485a)
        // ─────────────────────────────────────────────────────────────────
        //         public IEnumerable<AvatarDetail> GetAllAvatarDetail()
        //         {
        //             return _dbContext.AvatarDetail.Find(_ => true).ToEnumerable();
        //         }

        // ─────────────────────────────────────────────────────────────────
        // REVIEW: GetAllAvatarDetailAsync   (deleted by 7b673485a)
        // ─────────────────────────────────────────────────────────────────
        //         public async Task<IEnumerable<AvatarDetail>> GetAllAvatarDetailAsync()
        //         {
        //             var cursor = await _dbContext.AvatarDetail.FindAsync(_ => true);
        //             return cursor.ToEnumerable();
        //         }

//     }
// }

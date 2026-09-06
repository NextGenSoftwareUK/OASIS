// ═══════════════════════════════════════════════════════════════════════════════
// ██████  REVIEWED  -  VERDICT RECORDED PER METHOD BELOW  ██████
// ═══════════════════════════════════════════════════════════════════════════════
//
//  THE METHOD(S) BELOW WERE DELETED BY A "SPLIT INTO PARTIAL CLASS FILES" COMMIT.
//  EACH HAS BEEN CHECKED AGAINST WHAT THE CODEBASE PROVIDES TODAY, AND CARRIES A
//  VERDICT: LINE NAMING ITS REPLACEMENT.
//
//  EVERY ONE IS SUPERSEDED. THE VERDICTS ARE HERE SO THEY CAN BE CHECKED BEFORE
//  THE FILE IS DELETED, RATHER THAN HAVING TO BE TAKEN ON TRUST.
//
//  >>> TO CONFIRM A VERDICT: <<<
//        grep -rn "<ReplacementName>" --include=*.cs .
//        the named replacement should be present and in use.
//
//  ONE VERDICT WAS INITIALLY WRONG: the two NFTManager collection methods were
//  recorded as having no replacement. They were renamed by the OASIS* -> Web4*
//  migration. Treat the rest as reviewed but verifiable, not infallible.
//
//  EVERYTHING IS STILL COMMENTED OUT, SO NOTHING CHANGES AT RUNTIME EITHER WAY.
//  FULL AUDIT: Docs/FILE_SPLIT_LOST_METHODS.md
//  FULL SOURCE: Docs/FILE_SPLIT_LOST_METHODS_SOURCE.md
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
        // VERDICT: SUPERSEDED -> renamed to LoadAllAvatarDetails, used 168 times across the
        //          providers today.
        // ─────────────────────────────────────────────────────────────────
        //         public IEnumerable<AvatarDetail> GetAllAvatarDetail()
        //         {
        //             return _dbContext.AvatarDetail.Find(_ => true).ToEnumerable();
        //         }

        // ─────────────────────────────────────────────────────────────────
        // REVIEW: GetAllAvatarDetailAsync   (deleted by 7b673485a)
        // VERDICT: SUPERSEDED -> renamed to LoadAllAvatarDetailsAsync, used 322 times across
        //          the providers today.
        // ─────────────────────────────────────────────────────────────────
        //         public async Task<IEnumerable<AvatarDetail>> GetAllAvatarDetailAsync()
        //         {
        //             var cursor = await _dbContext.AvatarDetail.FindAsync(_ => true);
        //             return cursor.ToEnumerable();
        //         }

//     }
// }

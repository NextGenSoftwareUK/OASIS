using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true)
        => DeleteAvatarDetailAsync(id, softDelete).GetAwaiter().GetResult();

    public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        =>DeleteCanonicalDetailAsync(id,softDelete);

    private async Task<OASISResult<bool>> DeleteCanonicalDetailAsync(Guid id,bool softDelete,Func<IAvatarDetail,bool> selectedIdentity=null)
    {
        var result = new OASISResult<bool>();
        var dispatched = false;
        string token = null;
        try
        {
            if (id == Guid.Empty) throw new ArgumentException("AvatarDetail identity required.");
            if (!IsProviderActivated || _clientDisposed) throw new InvalidOperationException("DynamoDB provider is not activated.");
            var observed = await ReadLifecycleItemAsync("OasisAvatarDetails", id);
            if (observed.Count == 0)
                return new OASISResult<bool> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "AvatarDetail not found in DynamoDB." };
            var expected = LifecycleJson(observed, id);
            var detail = ReadStoredIdentity<AvatarDetail>(observed, id);
            if(selectedIdentity!=null&&!selectedIdentity(detail))throw new StorageLifecycleConflictException("AvatarDetail identity changed after selection; refusing deletion.");
            var unchanged=softDelete&&detail.IsDeleted&&!detail.IsActive;
            string updated = null;
            if (softDelete&&!unchanged)
            {
                detail.IsActive = false;
                if (!detail.IsDeleted) detail.DeletedDate = DateTime.UtcNow;
                updated = (await StageRecordAsync(detail, JObject.Parse(expected))).ToString(Newtonsoft.Json.Formatting.None);
            }
            var request = new TransactWriteItemsRequest
            {
                ClientRequestToken = Guid.NewGuid().ToString("N"),
                TransactItems = new() {unchanged?LifecycleCheck("OasisAvatarDetails",id,expected):LifecycleWrite("OasisAvatarDetails", id, expected, updated) }
            };
            token = request.ClientRequestToken;
            dispatched = !unchanged;
            var response = await _client.TransactWriteItemsAsync(request);
            if (response?.HttpStatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("DynamoDB AvatarDetail lifecycle lacks an authoritative acknowledgement.");
            result.Result = true;
            result.IsDeleted = true;
            result.IsSaved = true;
            result.DeletedCount = unchanged?0:1;
        }
        catch (TransactionCanceledException cause) when (cause.CancellationReasons?.Exists(reason => reason.Code == "ConditionalCheckFailed") == true
            && cause.CancellationReasons.TrueForAll(reason => reason.Code == "None" || reason.Code == "ConditionalCheckFailed"))
        {
            OASISErrorHandling.HandleError(ref result, "AvatarDetail changed before lifecycle commit.", cause);
            result.ErrorCode = StorageLifecycleConflictException.ErrorCode;
        }
        catch (AmazonDynamoDBException cause) when (IsNativeValidationRejection(cause))
        {
            OASISErrorHandling.HandleError(ref result, cause.Message, cause);
        }
        catch (Exception cause)
        {
            var failure = dispatched ? new DynamoDBMutationReconciliationException("OasisAvatarDetails", id.ToString(), cause, token) : cause;
            OASISErrorHandling.HandleError(ref result, failure.Message, failure);
            if (!dispatched && cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
            else if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }

    private async Task<OASISResult<bool>> DeleteSelectedDetailAsync(Task<OASISResult<IAvatarDetail>> lookup, bool softDelete,Func<IAvatarDetail,bool> selectedIdentity)
    {
        var selected = await lookup;
        if (selected == null) return new OASISResult<bool> { IsError = true, Message = "AvatarDetail selector returned no acknowledgement." };
        if (selected.IsError || selected.Result == null)
            return new OASISResult<bool> { IsError = true, ErrorCode = selected.ErrorCode, Exception = selected.Exception, Message = selected.Message };
        return await DeleteCanonicalDetailAsync(selected.Result.Id,softDelete,selectedIdentity);
    }

    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true)
        => DeleteSelectedDetailAsync(LoadAvatarDetailByEmailAsync(email),softDelete,row=>string.Equals(row.Email,email,StringComparison.OrdinalIgnoreCase));
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true)
        => DeleteAvatarDetailByEmailAsync(email, softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username, bool softDelete = true)
        => DeleteSelectedDetailAsync(LoadAvatarDetailByUsernameAsync(username),softDelete,row=>string.Equals(row.Username,username,StringComparison.OrdinalIgnoreCase));
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username, bool softDelete = true)
        => DeleteAvatarDetailByUsernameAsync(username, softDelete).GetAwaiter().GetResult();
}

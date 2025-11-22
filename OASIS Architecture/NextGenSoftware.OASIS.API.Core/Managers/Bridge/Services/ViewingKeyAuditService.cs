using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;

namespace NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services
{
    public class ViewingKeyAuditService
    {
        public virtual async Task RecordViewingKeyAsync(ViewingKeyAuditEntry entry, CancellationToken token = default)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ViewingKey))
            {
                return;
            }

            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"ViewingKey-{entry.TransactionId}",
                Description = $"Viewing key for {entry.SourceChain} → {entry.DestinationChain}",
                HolonType = Enums.HolonType.Bridge,
                MetaData = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "TransactionId", entry.TransactionId ?? string.Empty },
                    { "ViewingKey", entry.ViewingKey },
                    { "SourceChain", entry.SourceChain ?? string.Empty },
                    { "DestinationChain", entry.DestinationChain ?? string.Empty },
                    { "DestinationAddress", entry.DestinationAddress ?? string.Empty },
                    { "Timestamp", entry.Timestamp.ToString("o") },
                    { "Notes", entry.Notes ?? string.Empty }
                }
            };

            await HolonManager.Instance.SaveHolonAsync(holon);
        }
    }
}


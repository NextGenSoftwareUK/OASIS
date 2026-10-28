using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Default admission for trusted local owned-write outboxes. This
    /// validates the committed actor/selected ownership, not tenant, residency or
    /// encryption grants. Hosts with those constraints supply a stricter policy.</summary>
    public sealed class CommittedOwnerReplicationPolicy : IHolonReplicationAuthorizationPolicy
    {
        public string BindingIdentity => "hyperdrive-committed-owner-v1";

        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationAuthorizationContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (context == null) throw new ArgumentNullException(nameof(context));
            var mutation = context.Mutation;
            var selected = HolonGraphWritePlan.Create(mutation.Roots, mutation.SaveChildren,
                mutation.Recursive, mutation.MaxChildDepth, 0);
            return Task.FromResult(new OASISResult<bool>(mutation.ActorId != Guid.Empty &&
                selected.Nodes.All(node => node.Snapshot.CreatedByAvatarId == mutation.ActorId)));
        }
    }
}

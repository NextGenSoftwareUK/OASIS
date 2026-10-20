using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private async Task<OASISResult<IEnumerable<IHolon>>> SaveGraphsAtomicAsync(IEnumerable<IHolon> source,
            bool saveChildren, bool recursive, int maxChildDepth, int initialDepth,
            IAtomicHolonWritePlan conditionalPlan = null, Guid actorId = default, CancellationToken cancellationToken = default,
            (List<IHolon> Roots, List<PlannedHolonWrite> Nodes)? capturedGraph = null)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var prepared = false;
            try
            {
                if (maxChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if (initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(initialDepth));
                if (source == null) throw new ArgumentNullException(nameof(source));
                var roots = source.ToList();
                if (roots.Count == 0)
                {
                    result.Result = roots;
                    result.IsSaved = true;
                    return result;
                }
                var plan = conditionalPlan == null ? capturedGraph ?? HolonGraphWritePlan.Create(roots, saveChildren, recursive, maxChildDepth, initialDepth)
                    : (Roots: roots, Nodes: conditionalPlan.Nodes.ToList());
                // Preserve Mongo's embedded selected-child representation, but never alias caller nodes.
                var snapshots = plan.Nodes.ToDictionary(node => node.Source, node => node.Snapshot, new HolonReferenceComparer());
                var byId = plan.Nodes.ToDictionary(node => node.Snapshot.Id, node => node.Snapshot);
                foreach (var node in plan.Nodes)
                    node.Snapshot.Children = node.SelectedChildIds.Select(id => (IHolon)byId[id]).ToList();
                var plannedById = plan.Nodes.ToDictionary(node => node.Snapshot.Id);
                var entries = plan.Nodes.Select(node => DataHelper.ConvertOASISHolonToMongoEntity(node.Snapshot)).ToList();
                prepared = true;
                var responses = new List<IHolon>();
                var conditional = conditionalPlan == null ? null : new Repositories.MongoAtomicHolonWrite(conditionalPlan, actorId,
                    () => {
                        var receipt = ConditionalHolonReceipt.Write(responses, conditionalPlan);
                        _ = ConditionalHolonReceipt.Read(receipt, conditionalPlan);
                        return receipt;
                    }, json => { responses.Clear(); responses.AddRange(ConditionalHolonReceipt.Read(json, conditionalPlan)); }, cancellationToken,
                    conditionalPlan is ConditionalHolonPlan {ReplicationEnrollment:not null} edit
                        ?()=>CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(edit.OperationId,edit.ReplicationEnrollment.SourceId,
                            edit.Authority,actorId,edit.Operation,responses,edit.SaveChildren,edit.Recursive,edit.MaxChildDepth)):null);

                var saved = await _holonRepository.ImportAtomicAsync(entries, PreserveCreationAuditFields, preparedEntries =>
                {
                    var acknowledgements = preparedEntries.ToDictionary(entry => entry.HolonId,
                        entry => DataHelper.ConvertMongoEntityToOASISHolon(entry, false));
                    foreach (var node in plan.Nodes)
                    {
                        var committed = acknowledgements[node.Snapshot.Id];
                        committed.Children = node.Snapshot.Children.Select(child => acknowledgements[child.Id]).ToList();
                    }
                    foreach (var entry in preparedEntries)
                        entry.Children = acknowledgements[entry.HolonId].Children;
                    responses.AddRange(roots.Select(root => acknowledgements[snapshots[root].Id]));
                }, (bodies, observed) => PreserveUnselectedGraphChildren(plan.Nodes, bodies, observed), conditional, (record,existing) =>
                {
                    var node=plannedById[record.HolonId];
                    var kind = conditionalPlan is CommittedHolonReplicaPlan
                        ? HolonChildRoleWriteKind.CommittedReplica
                        : conditionalPlan != null && conditionalPlan.Operation != ConditionalHolonOperation.Save
                            ? HolonChildRoleWriteKind.SourceDelete : HolonChildRoleWriteKind.SourceSave;
                    record.ChildRoleManifestJson = HolonChildRolePersistence.Resolve(node, kind,
                        existing != null, existing?.ChildRoleManifestJson);
                }).ConfigureAwait(false);
                if (saved.IsError)
                {
                    result.IsError = true; result.ErrorCount = Math.Max(1, saved.ErrorCount);
                    result.Message = saved.Message;
                    result.Exception = saved.Exception;
                    result.ErrorCode = saved.ErrorCode;
                    return result;
                }
                // A failed preparation/rollback/unknown commit must leave caller identity state unchanged.
                foreach (var node in conditionalPlan == null ? plan.Nodes : Enumerable.Empty<PlannedHolonWrite>())
                {
                    node.Source.Id = node.Snapshot.Id;
                    node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
                }
                result.Result = responses;
                result.IsSaved = conditionalPlan == null || conditionalPlan.Operation == ConditionalHolonOperation.Save;
                result.IsDeleted = conditionalPlan != null && conditionalPlan.Operation != ConditionalHolonOperation.Save;
                result.DeletedCount = result.IsDeleted ? plan.Nodes.Count : 0;
                result.SavedCount = result.IsSaved ? saved.SavedCount : 0;
                result.IsWarning = saved.IsWarning;
                result.WarningCount = saved.WarningCount;
                result.Exception = saved.Exception;
                result.Message = saved.Message;
            }
            catch (Exception error)
            {
                result.IsError = true;
                result.Exception = error;
                result.Message = prepared ? "Mongo graph operation failed." : "Mongo graph preparation failed.";
                if (!prepared) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
            }
            return result;
        }
        private static void PreserveUnselectedGraphChildren(IEnumerable<PlannedHolonWrite> nodes,
            IReadOnlyDictionary<Guid, BsonDocument> bodies, IReadOnlyDictionary<Guid, BsonDocument> observed)
        {
            var selected = nodes.ToDictionary(node => node.Snapshot.Id);
            var childField = BsonClassMap.LookupClassMap(typeof(Entities.Holon)).AllMemberMaps
                .Single(member => member.MemberName == nameof(IHolon.Children)).ElementName;
            var embeddedId = BsonClassMap.LookupClassMap(typeof(Core.Holons.Holon)).AllMemberMaps
                .Single(member => member.MemberName == nameof(IHolon.Id)).ElementName;
            void Preserve(BsonDocument body, PlannedHolonWrite node)
            {
                if (!node.SelectsChildren)
                {
                    if (observed.TryGetValue(node.Snapshot.Id, out var persisted))
                    {
                        body.Remove(childField);
                        if (persisted.TryGetValue(childField, out var children))
                            body.Add(childField, children.DeepClone());
                    }
                    return;
                }
                if (!body.TryGetValue(childField, out var embedded) || embedded.IsBsonNull) return;
                foreach (var value in embedded.AsBsonArray)
                {
                    var child = value.AsBsonDocument;
                    var id = Guid.Parse(child[embeddedId].AsString);
                    if (!selected.TryGetValue(id, out var plannedChild) || plannedChild.Snapshot.ParentHolonId != node.Snapshot.Id)
                        throw new InvalidOperationException("Embedded write evidence does not match the selected graph.");
                    Preserve(child, plannedChild);
                }
            }
            foreach (var node in selected.Values) Preserve(bodies[node.Snapshot.Id], node);
        }
    }
}

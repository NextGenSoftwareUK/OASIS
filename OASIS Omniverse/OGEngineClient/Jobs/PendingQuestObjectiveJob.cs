using System.Threading;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PendingQuestObjectiveJob
{
    public PendingQuestObjectiveJob(string questId, string objectiveId, string? gameSource, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<bool>> completion)
    {
        QuestId = questId;
        ObjectiveId = objectiveId;
        GameSource = gameSource;
        CancellationToken = cancellationToken;
        Completion = completion;
    }

    public string QuestId { get; }
    public string ObjectiveId { get; }
    public string? GameSource { get; }
    public CancellationToken CancellationToken { get; }
    public TaskCompletionSource<OASISResult<bool>> Completion { get; }
}

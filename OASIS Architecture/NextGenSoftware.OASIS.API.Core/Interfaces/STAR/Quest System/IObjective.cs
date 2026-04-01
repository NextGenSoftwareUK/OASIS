using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    /// <summary>
    /// An objective belonging to a Quest. Has requirement and progress dictionaries keyed by game id.
    /// The Objective (string) property is computed from the requirement dictionaries.
    /// </summary>
    public interface IObjective : IQuestObjectiveDictionaries
    {
        Guid Id { get; set; }
        int Order { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        /// <summary>Computed progress summary built from requirement/progress dictionaries (e.g. "Killed 1/10 monsters in ODOOM (10%)").</summary>
        string ProgressSummary { get; }
        bool IsCompleted { get; set; }
        DateTime? CompletedAt { get; set; }
        Guid? CompletedBy { get; set; }
    }
}

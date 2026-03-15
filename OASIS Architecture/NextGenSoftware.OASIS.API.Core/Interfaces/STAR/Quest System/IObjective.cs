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
        /// <summary>Human-readable description built from the requirement dictionaries (e.g. "Kill 5 Zombie in ODOOM and collect Red key within 15 mins."). Serialized as "Objective" in JSON.</summary>
        string ObjectiveText { get; }
        bool IsCompleted { get; set; }
        DateTime? CompletedAt { get; set; }
        Guid? CompletedBy { get; set; }
    }
}

using NextGenSoftware.OASIS.Web6.Core.Enums;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// The composite routing score weights for a given dispatch mode.
    /// CompositeScore = (Wcat * CategoryScore) + (Wspeed * SpeedScore) + (Wcost * CostScore)
    ///                  - (Wpenalty * FailureRate) - (LoopPenalty if recently stalled)
    /// </summary>
    public class ScoringWeights
    {
        public double CategoryWeight { get; set; } = 0.5;
        public double SpeedWeight { get; set; } = 0.2;
        public double CostWeight { get; set; } = 0.2;
        public double FailurePenaltyWeight { get; set; } = 0.3;
        public double LoopPenalty { get; set; } = 0.4;

        public static ScoringWeights ForMode(DispatchMode mode)
        {
            switch (mode)
            {
                case DispatchMode.Serial:
                    // Cost optimised - elevate cost weight.
                    return new ScoringWeights { CategoryWeight = 0.35, SpeedWeight = 0.15, CostWeight = 0.5, FailurePenaltyWeight = 0.3, LoopPenalty = 0.4 };

                case DispatchMode.Parallel:
                    // Accuracy optimised - favour category score.
                    return new ScoringWeights { CategoryWeight = 0.65, SpeedWeight = 0.15, CostWeight = 0.2, FailurePenaltyWeight = 0.3, LoopPenalty = 0.4 };

                case DispatchMode.Decomposed:
                    // Minimise wall-clock time across sub-problems - raise speed weight.
                    return new ScoringWeights { CategoryWeight = 0.4, SpeedWeight = 0.4, CostWeight = 0.2, FailurePenaltyWeight = 0.3, LoopPenalty = 0.4 };

                default:
                    return new ScoringWeights();
            }
        }
    }
}

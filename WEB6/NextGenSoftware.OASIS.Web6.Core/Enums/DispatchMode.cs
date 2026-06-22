namespace NextGenSoftware.OASIS.Web6.Core.Enums
{
    /// <summary>
    /// The dispatch mode used by the FAHRN (Fractal Adaptive Holonic Reasoning Network) controller agent.
    /// </summary>
    public enum DispatchMode
    {
        /// <summary>Dispatch to the single highest-scoring agent, falling over to the next best on stall/timeout. Cost optimised.</summary>
        Serial,

        /// <summary>Dispatch the same problem to every eligible agent in parallel and compare/merge their plans. Accuracy optimised.</summary>
        Parallel,

        /// <summary>Decompose the problem into sub-problems and route each to its best-fit agent, then merge the resulting plans.</summary>
        Decomposed
    }
}

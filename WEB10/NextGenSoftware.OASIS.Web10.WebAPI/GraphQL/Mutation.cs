namespace NextGenSoftware.OASIS.Web10.WebAPI.GraphQL
{
    /// <summary>
    /// Root GraphQL Mutation type for the WEB10 Source layer.
    /// WEB10 is the read-only root-of-truth layer; no write operations are defined at this level.
    /// This stub satisfies the schema requirement so the endpoint registers correctly.
    /// </summary>
    public class Mutation
    {
        /// <summary>
        /// Placeholder — WEB10 is the immutable source-of-truth layer.
        /// No mutations are defined at this level of the OASIS stack.
        /// </summary>
        [GraphQLDescription("WEB10 is the read-only source-of-truth layer. No write operations are available at this level.")]
        public string Placeholder() =>
            "WEB10 (The Source) is the immutable foundation of the OASIS stack. No mutations are defined at this layer.";
    }
}

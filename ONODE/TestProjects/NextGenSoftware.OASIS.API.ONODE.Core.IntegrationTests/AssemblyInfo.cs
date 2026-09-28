using Xunit;

// These integration tests exercise process-wide ONET/OASIS singletons, including
// the one-listener ONETProtocol. Parallel test classes would mutate the same
// listener configuration and make results depend on scheduling order.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

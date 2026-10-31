using System;

namespace NextGenSoftware.OASIS.Common;

// Raised only after a successful query positively observes multiple identities.
public sealed class StorageIdentityAmbiguousException : InvalidOperationException
{
    public StorageIdentityAmbiguousException() : base("The identity query matched multiple records.") { }
}

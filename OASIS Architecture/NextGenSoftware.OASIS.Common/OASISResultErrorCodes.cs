namespace NextGenSoftware.OASIS.Common;

public static class OASISResultErrorCodes
{
    // Authoritative absence after a successful store read, not an I/O failure.
    public const string RecordNotFound = "OASIS_RECORD_NOT_FOUND";
    public const string AmbiguousIdentity = "OASIS_IDENTITY_AMBIGUOUS";
}

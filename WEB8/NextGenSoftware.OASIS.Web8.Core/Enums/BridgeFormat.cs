namespace NextGenSoftware.OASIS.Web8.Core.Enums
{
    /// <summary>The wire formats the WEB8 protocol bridge can translate into/out of the unified MeshMessage envelope.</summary>
    public enum BridgeFormat
    {
        /// <summary>Payload is already a JSON document - passed through as-is.</summary>
        Json,

        /// <summary>Payload is application/x-www-form-urlencoded key=value pairs - parsed into a JSON object.</summary>
        FormUrlEncoded,

        /// <summary>Payload is plain text - wrapped as { "text": "..." }.</summary>
        PlainText
    }
}

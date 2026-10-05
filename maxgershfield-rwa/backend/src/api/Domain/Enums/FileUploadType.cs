namespace Domain.Enums;

/// <summary>
/// Represents the type of file being uploaded to the system.
/// This enum is used to distinguish between different categories of uploaded files,
/// such as files intended for NFT branding or documentation purposes.
/// </summary>
public enum FileType
{
    /// <summary>
    /// Represents an NFT logo file. Typically an image associated with a specific NFT asset.
    /// </summary>
    [EnumMember(Value = "NFT-Logo")]
    NftLogo,

    /// <summary>
    /// Represents a general document file. Can include PDFs, DOCX, and other textual or legal content.
    /// </summary>
    [EnumMember(Value = "Document")]
    Document
}
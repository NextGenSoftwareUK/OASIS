namespace Application.DTOs.File.Requests;

/// <summary>
/// Represents the request data for uploading a file.
/// Contains the file itself and its type for validation and processing.
/// </summary>
/// <param name="File">
/// The file to be uploaded. The <see cref="IFormFile"/> interface represents a file sent with the HTTP request.
/// </param>
/// <param name="Type">
/// The type of the file being uploaded. Allowed values: NFT-Logo, Document.
/// </param>
public sealed record FileUploadRequest(
    [Required] IFormFile File,
    [Required] FileType Type
);
namespace Application.DTOs.File.Responses;

public sealed record FileUploadResponse(
    string Message,
    FileData Data
);

public sealed record FileData(
    string FileId,
    string FileUrl
);

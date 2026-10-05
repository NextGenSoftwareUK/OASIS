namespace Application.Validations;

public static class CreateRwaTokenValidation
{
    public static Result<CreateRwaTokenResponse> CreateValidateNftFields(this CreateRwaTokenRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Title) || Encoding.UTF8.GetByteCount(request.Title) > 32)
            errors.Add(Messages.TitleInvalid);

        if (string.IsNullOrWhiteSpace(request.UniqueIdentifier) ||
            Encoding.UTF8.GetByteCount(request.UniqueIdentifier) > 10)
            errors.Add(Messages.UniqueIdentifierInvalid);

        if (string.IsNullOrWhiteSpace(request.AssetDescription) || request.AssetDescription.Length > 1000)
            errors.Add(Messages.AssetDescriptionInvalid);

        if (string.IsNullOrWhiteSpace(request.Image) ||
            !Uri.IsWellFormedUriString(request.Image, UriKind.Absolute) ||
            request.Image.Length > 200)
            errors.Add(Messages.ImageInvalid);

        if (!string.IsNullOrWhiteSpace(request.ProofOfOwnershipDocument) &&
            (!Uri.IsWellFormedUriString(request.ProofOfOwnershipDocument, UriKind.Absolute) ||
             request.ProofOfOwnershipDocument.Length > 200))
            errors.Add(Messages.ProofOfOwnershipInvalid);

        if (errors.Any())
            return Result<CreateRwaTokenResponse>.Failure(ResultPatternError.BadRequest(string.Join(" | ", errors)));

        return Result<CreateRwaTokenResponse>.Success();
    }

    public static Result<UpdateRwaTokenResponse> UpdateValidateNftFields(this UpdateRwaTokenRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Title) || Encoding.UTF8.GetByteCount(request.Title) > 32)
            errors.Add(Messages.TitleInvalid);

        if (string.IsNullOrWhiteSpace(request.AssetDescription) || request.AssetDescription.Length > 1000)
            errors.Add(Messages.AssetDescriptionInvalid);

        if (errors.Count > 0)
            return Result<UpdateRwaTokenResponse>.Failure(ResultPatternError.BadRequest(string.Join(" | ", errors)));

        return Result<UpdateRwaTokenResponse>.Success();
    }
}
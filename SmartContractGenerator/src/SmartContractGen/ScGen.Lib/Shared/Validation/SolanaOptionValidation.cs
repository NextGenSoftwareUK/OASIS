namespace ScGen.Lib.Shared.Validation;

public static class SolanaOptionValidation
{
    public static BaseResult IsValid(this SolanaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RpcUrl))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.RpcUrlIsRequired));

        if (string.IsNullOrWhiteSpace(options.Pubkey))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.PubKeyIsRequired));

        if (!Uri.TryCreate(options.RpcUrl, UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.RpcUrlMustBeValid));


        return BaseResult.Success();
    }
}
namespace ScGen.Lib.Shared.Validation;

public static class EthereumOptionValidation
{
    public static BaseResult IsValid(this EthereumOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RpcUrl))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.RpcUrlIsRequired));

        if (string.IsNullOrWhiteSpace(options.PrivateKey))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.PrivateKeyIsRequired));

        if (!Uri.TryCreate(options.RpcUrl, UriKind.Absolute, out var uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.RpcUrlMustBeValid));

        if (!options.PrivateKey.StartsWith("0x") || options.PrivateKey.Length != 66)
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.PrivateKeyMustBeValid));

        if (options.GasLimit < 21000)
            return BaseResult.Failure(ResultPatternError.BadRequest(Messages.GasLimitMustBeValid));

        return BaseResult.Success();
    }
}
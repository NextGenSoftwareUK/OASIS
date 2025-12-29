namespace BuildingBlocks.Extensions.Resources;

/// <summary>
/// Centralized accessor for localized resource strings used across the application.
/// 
/// The <c>Messages</c> class provides strongly-typed, localized access to application messages
/// (error messages, notifications, and email templates), simplifying the use of <see cref="ResourceManager"/>.
/// 
/// Each property name maps to a key in a .resx file, and this design:
/// - Reduces hardcoded string usage
/// - Promotes reusability
/// - Enables clean localization and globalization
/// 
/// NOTE: <c>_resources.Get().AsString()</c> is expected to be a custom extension method that resolves the resource
/// string based on the calling property name using reflection or caller info.
/// </summary>
public static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages).FullName!, typeof(Messages).Assembly);

    public static string WalletLinkedAccountFailed => _resources.Get().AsString();
    public static string RegisterUserFailed => _resources.Get().AsString();
    public static string SmtpConnectionFailed => _resources.Get().AsString();
    public static string EmailFailed => _resources.Get().AsString();
    public static string UpdateUserProfileFailed => _resources.Get().AsString();
    public static string ConfirmRestoreAccountFailed => _resources.Get().AsString();
    public static string LoginUserFailed => _resources.Get().AsString();
    public static string LogoutUserFailed => _resources.Get().AsString();
    public static string GenerateTokenFailed => _resources.Get().AsString();
    public static string SolanaNftMintingFailed => _resources.Get().AsString();
    public static string RestoreAccountFailed => _resources.Get().AsString();
    public static string DeleteAccountFailed => _resources.Get().AsString();
    public static string ChangePasswordUserFailed => _resources.Get().AsString();
    public static string CreateNetworkFailed => _resources.Get().AsString();
    public static string CreateRoleFailed => _resources.Get().AsString();
    public static string CreateUserRoleFailed => _resources.Get().AsString();
    public static string UpdateUserRoleFailed => _resources.Get().AsString();
    public static string TokenValidationContextNull => _resources.Get().AsString();
    public static string TokenValidationInvalidTokenData => _resources.Get().AsString();
    public static string TokenValidationInvalidTokenVersion => _resources.Get().AsString();
    public static string DeleteUserRoleFailed => _resources.Get().AsString();
    public static string UpdateRoleFailed => _resources.Get().AsString();
    public static string DeleteRoleFailed => _resources.Get().AsString();
    public static string CreateNetworkTokenFailed => _resources.Get().AsString();
    public static string UpdateNetworkTokenFailed => _resources.Get().AsString();
    public static string CreateOrderFailed => _resources.Get().AsString();
    public static string CreateOrderSuccess => _resources.Get().AsString();
    public static string CreateOrderUnsupported => _resources.Get().AsString();
    public static string CreateOrderAmountFilter => _resources.Get().AsString();
    public static string InvalidSolanaFormat => _resources.Get().AsString();
    public static string InvalidAddressRadixFormat => _resources.Get().AsString();
    public static string DeleteNetworkTokenFailed => _resources.Get().AsString();
    public static string CreateRwaTokenFailed => _resources.Get().AsString();
    public static string UpdateRwaTokenFailed => _resources.Get().AsString();
    public static string UpdateRwaTokenForbidden => _resources.Get().AsString();
    public static string UpdateNetworkFailed => _resources.Get().AsString();
    public static string DeleteNetworkFailed => _resources.Get().AsString();
    public static string SendEmailConfirmationCodeFailed => _resources.Get().AsString();
    public static string ResetPasswordFailed => _resources.Get().AsString();
    public static string ConfirmEmailFailed => _resources.Get().AsString();
    public static string CheckBalanceFailed => _resources.Get().AsString();
    public static string CreateNftPurchaseFailed => _resources.Get().AsString();
    public static string SendNftPurchaseFailed => _resources.Get().AsString();
    public static string ForgotPasswordFailed => _resources.Get().AsString();
    public static string ConfirmEmailInvalidOrExpiredTimeCode => _resources.Get().AsString();
    public static string UserNotFound => _resources.Get().AsString();
    public static string NetworkNotFound => _resources.Get().AsString();
    public static string RwaTokenOwnershipTransferNotFound => _resources.Get().AsString();
    public static string NftLengthRequirement => _resources.Get().AsString();
    public static string WalletNotFound => _resources.Get().AsString();
    public static string OrderNotFound => _resources.Get().AsString();
    public static string RwaTokenNotFound => _resources.Get().AsString();
    public static string NftAlreadyTransferred => _resources.Get().AsString();
    public static string AccountNotFound => _resources.Get().AsString();
    public static string OrderInsufficientFunds => _resources.Get().AsString();
    public static string VirtualAccountNotFound => _resources.Get().AsString();
    public static string CreateNftPurchaseBuyerAccountNotFound => _resources.Get().AsString();
    public static string CannotPurchaseOwnNft => _resources.Get().AsString();
    public static string OrderAlreadyCompleted => _resources.Get().AsString();
    public static string ProofOfOwnershipInvalid => _resources.Get().AsString();
    public static string OrderCanceled => _resources.Get().AsString();
    public static string NetworkTokenNotFound => _resources.Get().AsString();
    public static string ExchangeRateNotFound => _resources.Get().AsString();
    public static string RoleNotFound => _resources.Get().AsString();
    public static string UserRoleNotFound => _resources.Get().AsString();
    public static string UserAlreadyExist => _resources.Get().AsString();
    public static string RoleAlreadyExist => _resources.Get().AsString();
    public static string UserRoleAlreadyExist => _resources.Get().AsString();
    public static string UserEmailAlreadyExist => _resources.Get().AsString();
    public static string UserPhoneAlreadyExist => _resources.Get().AsString();
    public static string UserUserNameAlreadyExist => _resources.Get().AsString();
    public static string WalletLinkedAccountAlreadyExist => _resources.Get().AsString();
    public static string LoginUserIncorrect => _resources.Get().AsString();
    public static string ChangePasswordIncorrect => _resources.Get().AsString();
    public static string ConfirmEmailSubjectMessage => _resources.Get().AsString();
    public static string ConfirmEmailBodyMessage => _resources.Get().AsString();
    public static string ConfirmRestoreAccountSubjectMessage => _resources.Get().AsString();
    public static string ConfirmRestoreAccountBodyMessage => _resources.Get().AsString();
    public static string SendEmailConfirmationSubjectMessage => _resources.Get().AsString();
    public static string ForgotPasswordSubjectMessage => _resources.Get().AsString();
    public static string ForgotPasswordBody1Message => _resources.Get().AsString();
    public static string ForgotPasswordBody2Message => _resources.Get().AsString();
    public static string ResetPasswordInvalidOrExpiredCode => _resources.Get().AsString();
    public static string ResetPasswordSubjectMessage => _resources.Get().AsString();
    public static string RestoreAccountSubjectMessage => _resources.Get().AsString();
    public static string RestoreAccountBodyMessage => _resources.Get().AsString();
    public static string ResetPasswordBodyMessage => _resources.Get().AsString();
    public static string DeleteAccountSubjectMessage => _resources.Get().AsString();
    public static string DeleteAccountBodyMessage => _resources.Get().AsString();
    public static string AccountAlreadyActive => _resources.Get().AsString();
    public static string NetworkAlreadyExist => _resources.Get().AsString();
    public static string NetworkTokenAlreadyExist => _resources.Get().AsString();
    public static string ConfirmRestoreAccountInvalidOrExpiredCode => _resources.Get().AsString();
    public static string RestoreAccountIncorrectFormat => _resources.Get().AsString();
    public static string RadixGetAddressInvalidType => _resources.Get().AsString();
    public static string SelfTransaction => _resources.Get().AsString();
    public static string InsufficientFundsInTechAccount => _resources.Get().AsString();
    public static string InsufficientFunds => _resources.Get().AsString();
    public static string InvalidAmount => _resources.Get().AsString();
    public static string TransactionFailed => _resources.Get().AsString();
    public static string RadixGetTransactionStatusFailed => _resources.Get().AsString();
    public static string IpfsUploadEmptyFile => _resources.Get().AsString();
    public static string IpfsInvalidTypeFile => _resources.Get().AsString();
    public static string IpfsSuccessMessageFile => _resources.Get().AsString();
    public static string IpfsFileNotFound => _resources.Get().AsString();
    public static string IpfsInvalidFormatCid => _resources.Get().AsString();
    public static string TitleInvalid => _resources.Get().AsString();
    public static string AssetDescriptionInvalid => _resources.Get().AsString();
    public static string ImageInvalid => _resources.Get().AsString();
    public static string UniqueIdentifierInvalid => _resources.Get().AsString();
    public static string ConfigurationValueRequired(string key) => _resources.Get().Format(key);

    public static string ConfigurationValueMustBeInteger(string key, string? value) =>
        _resources.Get().Format(key, value);

    public static string ConfigurationValueMustBeBoolean(string key, string? value) =>
        _resources.Get().Format(key, value);

    public static string ConnectionStringNotFound(string name) => _resources.Get().Format(name);
}
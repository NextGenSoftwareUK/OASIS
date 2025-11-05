namespace ScGen.Lib.Shared.Resources;

/// <summary>
/// Centralized accessor for localized resource strings used across the application.
/// 
/// The <c>ResultMessages</c> class provides strongly-typed, localized access to application messages
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
    private static readonly ResourceManager Resources = new(typeof(Messages).FullName!, typeof(Messages).Assembly);
    
    public static string EmptyJson => Resources.Get().AsString();
    public static string FileTooLarge => Resources.Get().AsString();
    public static string EmptyFile => Resources.Get().AsString();
    public static string FileNotFound => Resources.Get().AsString();
    public static string WorkingDirectoryNotFound => Resources.Get().AsString();
    public static string HandlebarTemplateNotFound => Resources.Get().AsString();
    public static string HandlebarTemplateProcessingError => Resources.Get().AsString();
    public static string InvalidJsonFile => Resources.Get().AsString();
    public static string InvalidSolidityFile => Resources.Get().AsString();
    public static string InvalidFormatFile => Resources.Get().AsString();

    public static string RpcUrlIsRequired => Resources.Get().AsString();
    public static string RpcUrlMustBeValid => Resources.Get().AsString();
    public static string PrivateKeyIsRequired => Resources.Get().AsString();
    public static string PubKeyIsRequired => Resources.Get().AsString();
    public static string PrivateKeyMustBeValid => Resources.Get().AsString();
    public static string PrivateKeyMustBeValidHex => Resources.Get().AsString();
    public static string GasLimitMustBeValid => Resources.Get().AsString();
    public static string InvalidAbiFile => Resources.Get().AsString();
    public static string EmptyAbi => Resources.Get().AsString();
    public static string NotFoundAbi => Resources.Get().AsString();
    public static string NotFoundBin => Resources.Get().AsString();
    public static string WasmFileNotFound => Resources.Get().AsString();
    public static string ScryptoProjectNotFound => Resources.Get().AsString();
    public static string RequiredAbi => Resources.Get().AsString();
    public static string InvalidBytecodeFile => Resources.Get().AsString();
    public static string EmptyBytecode => Resources.Get().AsString();
    public static string NodeProcessNotRun => Resources.Get().AsString();
    public static string FailedToStartProcess => Resources.Get().AsString();
    public static string FailedToStartNode => Resources.Get().AsString();
    public static string EthereumNodeNotRunning => Resources.Get().AsString();
    public static string MissingAbiFile => Resources.Get().AsString();

    public static string GanacheFailed => Resources.Get().AsString();
    public static string GanacheSuccessStart => Resources.Get().AsString();
    public static string StartDeploymentInEthereum => Resources.Get().AsString();
    public static string ContractSuccessfullyDeployed => Resources.Get().AsString();
    public static string ContractFailedDeployed => Resources.Get().AsString();
    public static string NotSupportedScGen => Resources.Get().AsString();
    public static string FailedToStartGanache => Resources.Get().AsString();
    public static string FailedToExtractContractAddress => Resources.Get().AsString();
}
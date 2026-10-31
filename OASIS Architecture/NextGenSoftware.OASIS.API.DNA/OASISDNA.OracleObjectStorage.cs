namespace NextGenSoftware.OASIS.API.DNA;

/// <summary>Non-secret OCI configuration. SDK profile and encryption key are persisted separately.</summary>
public class OracleObjectStorageOASISSettings : ProviderSettingsBase
{
    public string ConfigFilePath {get;set;}
    public string ProfileName {get;set;}="DEFAULT";
    public string RegionId {get;set;}
    public string NamespaceName {get;set;}
    public string BucketName {get;set;}
    public string KeyPrefix {get;set;}="oasis/";
    public string StorageEncryptionKeyEnvironmentVariable {get;set;}="OASIS_ORACLE_OBJECT_STORAGE_ENCRYPTION_KEY";
    public bool IsVersionControlEnabled {get;set;}
}

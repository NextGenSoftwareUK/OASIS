namespace NextGenSoftware.OASIS.API.DNA;

/// <summary>Configuration for an existing general-purpose S3 bucket. No implicit provisioning.</summary>
public class AWSS3OASISSettings : ProviderSettingsBase
{
    public string Region { get; set; }
    public string BucketName { get; set; }
    public string KeyPrefix { get; set; } = "oasis/";
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string SessionToken { get; set; }
    public bool UseDefaultCredentials { get; set; }
    public string StorageEncryptionKeyEnvironmentVariable { get; set; } = "OASIS_AWS_S3_STORAGE_ENCRYPTION_KEY";
    public bool IsVersionControlEnabled { get; set; }
}

using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa;

internal static class MfaNativeQualification
{
    public static async Task RunAsync(OASISDNA dna, Guid actor)
    {
        const string ownedUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var mongo = dna.OASIS.StorageProviders.MongoDBOASIS;
        Check(mongo.ConnectionString == ownedUri && mongo.DBName.StartsWith("hotspot_http_", StringComparison.Ordinal),
            "MFA qualification requires the existing owned native fixture.");
        var settings = new MfaSettings { Enabled = true, TimeStepSeconds = 30, CodeDigits = 6, AllowedDriftSteps = 0 };
        var now = DateTime.UtcNow;
        var store = new MongoMfaStore(() => dna);
        var service = new MfaService(() => settings, store, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), () => now);
        Check(!(await service.StatusAsync(actor)).Enabled, "Owned MFA actor already enrolled.");
        var enrolment = await service.BeginEnrolAsync(actor, "owned-mfa@example.invalid");
        Check(!string.IsNullOrWhiteSpace(enrolment.Secret) && enrolment.OtpAuthUri.StartsWith("otpauth://totp/", StringComparison.Ordinal),
            "Actual enrollment did not provide the authenticator contract.");
        var pending = await store.GetAsync(actor, default);
        Check(pending != null && !pending.Enabled && !string.IsNullOrEmpty(pending.PendingSecretCipher) &&
            pending.PendingSecretCipher != enrolment.Secret, "Native pending enrollment was not encrypted or activated too early.");
        var secret = Totp.Base32Decode(enrolment.Secret);
        try
        {
            var codes = await service.ConfirmEnrolAsync(actor, Totp.Code(secret, Totp.Step(now, 30)));
            Check(codes.Count >= 2 && codes.Distinct().Count() == codes.Count, "Actual recovery codes unavailable.");
            var enrolled = await store.GetAsync(actor, default);
            Check(enrolled.Enabled && enrolled.PendingSecretCipher == null && enrolled.RecoveryHashes.Count == codes.Count &&
                !enrolled.RecoveryHashes.Intersect(codes).Any(), "Native enabled MFA record did not retain only recovery hashes.");
            Check(await service.NeedsSecondStepAsync(actor, null), "Enrolled actor bypassed second-step requirement.");
            now = now.AddSeconds(30);
            var challenge = await service.CreateChallengeAsync(actor, "127.0.0.1");
            var accepted = await service.VerifyAsync(challenge, Totp.Code(secret, Totp.Step(now, 30)), null);
            Check(accepted.Success && accepted.AvatarId == actor && !accepted.UsedRecoveryCode, "Actual authenticator verification failed.");
            Check(!(await service.VerifyAsync(challenge, Totp.Code(secret, Totp.Step(now, 30)), null)).Success,
                "Consumed challenge was reusable.");
            var recoveryChallenge = await service.CreateChallengeAsync(actor, "127.0.0.1");
            var recovered = await service.VerifyAsync(recoveryChallenge, null, codes[0]);
            Check(recovered.Success && recovered.UsedRecoveryCode && recovered.AvatarId == actor, "Actual recovery verification failed.");
            Check((await service.StatusAsync(actor)).RecoveryCodesLeft == codes.Count - 1, "Recovery use was not persisted.");
            var usedCodeChallenge = await service.CreateChallengeAsync(actor, "127.0.0.1");
            Check(!(await service.VerifyAsync(usedCodeChallenge, null, codes[0])).Success, "Recovery code was reusable.");
            await service.DisableAsync(actor, null, codes[1], false);
            Check(!(await service.StatusAsync(actor)).Enabled && await store.GetAsync(actor, default) == null,
                "Native MFA disable did not remove the enabled record.");
            var events = await service.EventsAsync(actor, 50);
            Check(events.Any(item => item.Kind == "enrol-confirmed") && events.Any(item => item.Kind == "recovery-used") &&
                events.Any(item => item.Kind == "disabled"), "Actual MFA lifecycle audit incomplete.");
        }
        finally { CryptographicOperations.ZeroMemory(secret); }
        Console.WriteLine("MFA_NATIVE_PROOF: actual service/native Mongo enrollment, authenticator challenge, one-time recovery and disable; not HTTP/login/browser/hosted acceptance.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

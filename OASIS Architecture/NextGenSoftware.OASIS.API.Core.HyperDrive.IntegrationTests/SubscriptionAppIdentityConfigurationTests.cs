using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public void SubscriptionSdkRejectsIncompleteEffectiveAppIdentityBeforeTransport()
    {
        var originalId = Environment.GetEnvironmentVariable("OASIS_APP_ID");
        var originalKey = Environment.GetEnvironmentVariable("OASIS_APP_KEY");
        using var http = new HttpClient(); // Real transport; constructors must not send any request.
        const string authority = "http://127.0.0.1:1";
        try
        {
            // This assembly disables parallel execution; restore both process inputs in finally.
            Environment.SetEnvironmentVariable("OASIS_APP_ID", null);
            Environment.SetEnvironmentVariable("OASIS_APP_KEY", null);
            void Refuse(string? id, string? key)
            {
                var error = Assert.Throws<ArgumentException>(() =>
                    new Web4SubscriptionAuthorizationClient(http, authority, appId: id, appKey: key));
                Assert.Equal("App identity requires both an app ID and an app key, or neither.", error.Message);
                Assert.DoesNotContain("owned-private-key", error.Message);
            }
            Refuse("owned-app", null);
            Refuse(null, "owned-private-key");
            Refuse("owned-app", " ");
            Refuse(" ", "owned-private-key");
            Environment.SetEnvironmentVariable("OASIS_APP_ID", "owned-environment-app");
            Refuse(null, null);
            Environment.SetEnvironmentVariable("OASIS_APP_ID", null);
            Environment.SetEnvironmentVariable("OASIS_APP_KEY", "owned-private-key");
            Refuse(null, null);

            // Complete effective pairs may combine the existing explicit/environment precedence.
            _ = new Web4SubscriptionAuthorizationClient(http, authority, appId: "owned-app");
            Environment.SetEnvironmentVariable("OASIS_APP_ID", "owned-environment-app");
            _ = new Web4SubscriptionAuthorizationClient(http, authority);
            Environment.SetEnvironmentVariable("OASIS_APP_KEY", null);
            _ = new Web4SubscriptionAuthorizationClient(http, authority, appKey: "owned-private-key");
            Environment.SetEnvironmentVariable("OASIS_APP_ID", null);
            _ = new Web4SubscriptionAuthorizationClient(http, authority);
            Console.WriteLine("App identity configuration verified: six incomplete effective configurations rejected at construction; complete explicit/environment combinations and absent optional identity retained. Real HttpClient, no HTTP send, no mocked handler or backend acceptance claim.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_APP_ID", originalId);
            Environment.SetEnvironmentVariable("OASIS_APP_KEY", originalKey);
            Assert.Equal(originalId, Environment.GetEnvironmentVariable("OASIS_APP_ID"));
            Assert.Equal(originalKey, Environment.GetEnvironmentVariable("OASIS_APP_KEY"));
            Console.WriteLine("App identity configuration process environment restored.");
        }
    }
}

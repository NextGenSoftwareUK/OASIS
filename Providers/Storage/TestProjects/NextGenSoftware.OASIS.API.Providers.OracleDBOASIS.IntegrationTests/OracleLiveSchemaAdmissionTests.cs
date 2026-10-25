using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass]
public sealed class OracleLiveSchemaAdmissionTests
{
    [DataTestMethod]
    [DataRow("db", "APP", "APP", "service", "tcps", true)]
    [DataRow("db", "APP", "APP", "service", "TCPS", true)]
    [DataRow("wrong", "APP", "APP", "service", "tcps", false)]
    [DataRow("db", "OTHER", "APP", "service", "tcps", false)]
    [DataRow("db", "APP", "OTHER", "service", "tcps", false)]
    [DataRow("db", "APP", "APP", "wrong", "tcps", false)]
    [DataRow("db", "APP", "APP", "service", "tcp", false)]
    [DataRow("db", "APP", "APP", "service", "", false)]
    public void HostedPolicyRequiresExactApprovedTargetAndObservedTcps(string database, string schema, string user, string service, string protocol, bool accepted)
    {
        var target = new OracleLiveSchemaPreflight.Target(database, schema, user, service, protocol);
        if (accepted) OracleLiveSchemaPreflight.ValidateTarget(target, "db", "APP", "service", true);
        else Assert.ThrowsException<InvalidDataException>(() => OracleLiveSchemaPreflight.ValidateTarget(target, "db", "APP", "service", true));
    }

    [DataTestMethod][DataRow("SYS")][DataRow("SYSTEM")]
    public void AdministratorTargetCannotPassEvenWhenExplicitlyNamed(string user)
        => Assert.ThrowsException<InvalidDataException>(() => OracleLiveSchemaPreflight.ValidateTarget(
            new("db", user, user, "service", "tcps"), "db", user, "service", true));
}

@{
    ActiveWorktrees = @('C:/Source/OASIS', 'C:/Source/Our-World')
    # Snapshot of active-chat ownership, 26 October 2026. Reconfirm when chats change.
    # Paths below are relative to the main repository unless explicitly absolute.
    ActiveProjects = @(
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core'
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Tests/SubscriptionUsage'
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage'
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS'
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS'
        'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests'
        # WEB4/Core owner is active. Retain these pending exact owner confirmation.
        'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI'
        'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests'
        'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests'
        'ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests'
        'ONODE/TestProjects/NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests'
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests'
        'OASIS Architecture/NextGenSoftware.OASIS.Common'
        'OASIS Architecture/NextGenSoftware.OASIS.Contracts'
        'OASIS Architecture/NextGenSoftware.OASIS.API.DNA'
        'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader'
        'ONODE/NextGenSoftware.OASIS.API.ONODE.Core'
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS'
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS'
        'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests'
        'Providers/Storage/Neo4j.Driver.OASIS'
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS'
        'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests'
        'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests'
    )
    Preserve = @(
        'TestResults'
        'C:/Source/OASIS-hyperdrive-v2-gaps/TestResults'
        'C:/Source/OASIS-hyperdrive-v2-gaps/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/node_modules'
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/node_modules'
        'artifacts/railway-secure-backup'
        'artifacts/desktop-protected-sessions'
    )
    ArtifactOutputs = @(
        'artifacts/global-release-validation'
        'artifacts/global-release-optional-version-check'
        'artifacts/unity-edge-validation-project/Library'
        'artifacts/unity-edge-validation-project/Temp'
        'artifacts/unity-edge-validation-project/Logs'
        'artifacts/unity-edge-validation-project/obj'
        'artifacts/unity-edge-validation-project/Build'
        'artifacts/unity-edge-validation-project/Builds'
        'artifacts/native-games/native'
        'artifacts/unity/com.nextgensoftware.oasis.edge'
        'artifacts/unity/com.nextgensoftware.oasis.edge.tgz'
        'artifacts/release-promotion/package-verification'
    )
    # Existing registered old checkout. Only bin/obj are automatically selected;
    # source, linked shared runtimes and native data never become generic cache.
    AdditionalBuildRepositories = @('C:/Source/OASIS-hyperdrive-v2-gaps')
}

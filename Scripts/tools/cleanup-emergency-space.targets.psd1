@{
    ActiveWorktrees = @('C:/Source/OASIS', 'C:/Source/Our-World')
    # Build and test outputs are always regenerable, so nothing here preserves bin/obj
    # or TestResults. Only non-regenerable secret backups are kept.
    # Paths below are relative to the main repository unless explicitly absolute.
    Preserve = @(
        'artifacts/railway-secure-backup'
        'artifacts/desktop-protected-sessions'
        # Hash-pinned reusable DynamoDB runtime and retained history; not regenerable.
        'TestResults/AWSProviderEvidence'
    )
    # Whole-directory test outputs, emptied on every run.
    TestOutputs = @(
        'TestResults'
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
    # Other registered checkouts whose bin/obj and TestResults are also reclaimed.
    AdditionalBuildRepositories = @()
}

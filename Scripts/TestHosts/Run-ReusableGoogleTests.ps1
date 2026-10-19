#requires -Version 7.5
$ErrorActionPreference='Stop'
# One environment and lifecycle owner; never discard unresolved native originals.
& (Join-Path $PSScriptRoot 'Test-RetainedFirestoreDurability.ps1') -VerifyRetainedCheckpoint -RunHistoryTests -RunRegressionTests

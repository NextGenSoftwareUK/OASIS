# Read the attachment referenced by the TRX, not every XML copy VSTest deploys.
param([Parameter(Mandatory)][string]$ResultsDirectory,[Parameter(Mandatory)][string]$ReportName)
$ErrorActionPreference='Stop'
$coverageRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../TestResults/OracleCurrent'))
$coverageResults=[IO.Path]::GetFullPath($ResultsDirectory)
if(!$coverageResults.StartsWith($coverageRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
   [IO.Path]::GetFileName($ReportName) -cne $ReportName -or ! $ReportName.EndsWith('.trx')){
    throw 'A scoped Oracle coverage directory and literal TRX file name are required.'
}
[xml]$coverageTrx=Get-Content -LiteralPath (Join-Path $coverageResults $ReportName) -Raw
$coverageIdentity=Get-Content -LiteralPath (Join-Path $coverageResults 'oracle-coverage-identity.json') -Raw|ConvertFrom-Json
if($coverageIdentity.ProviderSHA256 -cnotmatch '^[A-F0-9]{64}$' -or $coverageIdentity.ProviderPdbSHA256 -cnotmatch '^[A-F0-9]{64}$'){
    throw 'Recorded provider PE/PDB identity is required; never merge coverage from unknown binaries.'
}
$coverageCollectors=@($coverageTrx.TestRun.ResultSummary.CollectorDataEntries.Collector | Where-Object uri -eq 'datacollector://microsoft/CoverletCodeCoverage/1.0')
if($coverageCollectors.Count -ne 1){throw 'Exactly one Coverlet collector entry required in original TRX.'}
$coverageAttachments=@($coverageCollectors[0].UriAttachments.UriAttachment)
if($coverageAttachments.Count -ne 1){throw 'Exactly one referenced Coverlet attachment required.'}
$coverageDeployment=[string]$coverageTrx.TestRun.TestSettings.Deployment.runDeploymentRoot
if($coverageDeployment -in @('.','..') -or $coverageDeployment -notmatch '^[A-Za-z0-9_.-]+$'){throw 'Unexpected VSTest deployment identity.'}
$coverageIn=[IO.Path]::GetFullPath((Join-Path (Join-Path $coverageResults $coverageDeployment) 'In'))
if(!$coverageIn.StartsWith($coverageResults+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
    throw 'Deployment escapes scoped coverage results.'
}
$coverageHref=[string]$coverageAttachments[0].A.href
if([string]::IsNullOrWhiteSpace($coverageHref) -or [IO.Path]::IsPathRooted($coverageHref)){throw 'Relative TRX coverage attachment required.'}
$coveragePath=[IO.Path]::GetFullPath((Join-Path $coverageIn $coverageHref))
if(!$coveragePath.StartsWith($coverageIn+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
   [IO.Path]::GetFileName($coveragePath) -cne 'coverage.cobertura.xml'){
    throw 'TRX attachment escapes its scoped deployment or has unexpected format.'
}
$coverageFile=Get-Item -LiteralPath $coveragePath
[xml]$coverageXml=Get-Content -LiteralPath $coverageFile.FullName -Raw
$coveragePackages=@($coverageXml.coverage.packages.package)
if($coveragePackages.Count -ne 1 -or $coveragePackages[0].name -ne 'NextGenSoftware.OASIS.API.Providers.OracleDBOASIS' -or
   [long]$coverageXml.coverage.'lines-valid' -le 0){throw 'Oracle-only instrumented coverage evidence required.'}
[pscustomobject]@{
    Coverage=$coverageFile.FullName;Bytes=$coverageFile.Length;SHA256=(Get-FileHash -LiteralPath $coverageFile.FullName).Hash
    ProviderSHA256=$coverageIdentity.ProviderSHA256;ProviderPdbSHA256=$coverageIdentity.ProviderPdbSHA256
    LinesValid=[long]$coverageXml.coverage.'lines-valid';LinesCovered=[long]$coverageXml.coverage.'lines-covered'
    BranchesValid=[long]$coverageXml.coverage.'branches-valid';BranchesCovered=[long]$coverageXml.coverage.'branches-covered'
}

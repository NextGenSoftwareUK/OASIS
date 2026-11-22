[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$CandidateRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $CandidateRoot).Path
$programs = [ordered]@{
    WEB4 = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs'
    WEB5 = 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs'
    WEB6 = 'WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs'
    WEB7 = 'WEB7/NextGenSoftware.OASIS.Web7.WebAPI/Program.cs'
    WEB8 = 'WEB8/NextGenSoftware.OASIS.Web8.WebAPI/Program.cs'
    WEB9 = 'WEB9/NextGenSoftware.OASIS.Web9.WebAPI/Program.cs'
    WEB10 = 'WEB10/NextGenSoftware.OASIS.Web10.WebAPI/Program.cs'
}
foreach ($entry in $programs.GetEnumerator()) {
    $source = Get-Content -LiteralPath (Join-Path $root $entry.Value) -Raw
    $jwt = [regex]::Matches($source, '(?m)^\s*app\.UseMiddleware<[^>]*\bJwtMiddleware>\(\);')
    $authorization = [regex]::Matches($source, '(?m)^\s*app\.UseAuthorization\(\);')
    if ($jwt.Count -ne 1 -or $authorization.Count -ne 1 -or $jwt[0].Index -ge $authorization[0].Index) {
        throw "$($entry.Key) must establish its validated session before framework authorization."
    }
    if ($entry.Key -eq 'WEB4') {
        $bridge = [regex]::Matches($source, '(?m)^\s*app\.UseAuthentication\(\);')
        if ($bridge.Count -ne 1 -or $bridge[0].Index -le $jwt[0].Index -or $bridge[0].Index -ge $authorization[0].Index) {
            throw 'WEB4 authentication must bridge the validated session before framework authorization.'
        }
    }
}
Write-Host 'SESSION_PIPELINE_ORDER: all seven source pipelines establish JWT/session identity before authorization; WEB4 validated-principal bridge remains ordered. Source check only, not runtime acceptance.'

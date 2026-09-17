[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..'))
)

$ErrorActionPreference = 'Stop'

# The generic Holon persistence invariant applies to all deployed WEB4-WEB10 services:
# the public Guid is the sole cross-client identity. WEB4, WEB6-WEB10 persist through
# HolonManager/Data and are covered by the provider-level identity resolution. WEB5
# also exposes resource CRUD endpoints, so enforce its POST/PUT transport contract here.
$starControllers = Join-Path $RepositoryRoot 'STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Controllers'
if (-not (Test-Path -LiteralPath $starControllers)) {
    throw "STAR WebAPI controllers were not found at $starControllers."
}

$resourcePostUpdateViolations = [System.Collections.Generic.List[string]]::new()
$putIdentityViolations = [System.Collections.Generic.List[string]]::new()
$postCount = 0
$putCount = 0

foreach ($file in (Get-ChildItem -LiteralPath $starControllers -Filter '*.cs' -File)) {
    $source = [IO.File]::ReadAllText($file.FullName)

    # A bare [HttpPost] is the resource-create route. It must never dispatch to UpdateAsync.
    foreach ($match in [regex]::Matches($source, '(?s)\[HttpPost\]\s*(?:\[[^\]]+\]\s*)*public\s+async\s+Task<IActionResult>\s+(?<method>\w+)\s*\([^\)]*\)\s*\{(?<body>.*?)\n\s*\}')) {
        $postCount++
        if ($match.Groups['body'].Value -match '\.UpdateAsync\s*\(') {
            $resourcePostUpdateViolations.Add("$($file.Name): $($match.Groups['method'].Value)")
        }
    }

    # PUT requests containing a resource body must set its public ID from the route. Request
    # DTO endpoints load the resource first and therefore do not carry a resource Id themselves.
    foreach ($match in [regex]::Matches($source, '(?s)\[HttpPut\("\{id\}"\)\][^\n]*\n(?:\s*\[[^\]]+\]\s*\n)*\s*public\s+async\s+Task<IActionResult>\s+(?<method>\w+)\s*\(Guid\s+id,\s*\[FromBody\]\s+(?<type>[^\s]+)\s+(?<parameter>\w+)\)\s*\{(?<body>.*?)\n\s*\}')) {
        $putCount++
        if ($match.Groups['type'].Value -match 'Request$') { continue }
        $parameter = [regex]::Escape($match.Groups['parameter'].Value)
        $methodWindowLength = [Math]::Min(4096, $source.Length - $match.Index)
        $methodWindow = $source.Substring($match.Index, $methodWindowLength)
        if ($methodWindow -notmatch "(?:$parameter|\(\(IHolon\)$parameter\))\.Id\s*=\s*id\s*;") {
            $putIdentityViolations.Add("$($file.Name): $($match.Groups['method'].Value)")
        }
    }
}

if ($resourcePostUpdateViolations.Count -gt 0) {
    throw "Bare resource POST endpoints calling UpdateAsync: $($resourcePostUpdateViolations -join ', '). Use the manager CreateAsync lifecycle."
}
if ($putIdentityViolations.Count -gt 0) {
    throw "PUT /{id} resource endpoints that do not stamp the route GUID into the body: $($putIdentityViolations -join ', ')."
}

Write-Output "WEB4-WEB10 Holon identity contract: OK (WEB5 resource POST routes inspected: $postCount; PUT /{id} routes inspected: $putCount)."

#Requires -Version 7.0
# Offline contract: the RPC helper returns only the native result, not a task sentinel.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/TelosPublicSourceCurrent/Offline'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Probe-TelosGuardedReadiness.ps1'), [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Probe syntax failed.' }
$definition = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-Rpc'
}, $true)
. ([scriptblock]::Create($definition.Extent.Text))
$utf8 = [Text.UTF8Encoding]::new($false)
$observations = [Collections.Generic.List[object]]::new()
$reply = [Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::OK)
$reply.Content = [Net.Http.StringContent]::new('{"jsonrpc":"2.0","id":1,"result":"0x28"}')
$client = [pscustomobject]@{ Reply = $reply }
$client | Add-Member ScriptMethod SendAsync {
    param($request, $completion)
    return [Threading.Tasks.Task[Net.Http.HttpResponseMessage]]::FromResult($this.Reply)
}
$result = @(Read-Rpc 'eth_chainId' @())
if ($result.Count -ne 1 -or $result[0] -isnot [string] -or $result[0] -cne '0x28' -or $observations.Count -ne 1) {
    throw 'RPC helper leaked non-result pipeline output.'
}
'PASS: one exact native result; no task sentinel, network call or source write.'

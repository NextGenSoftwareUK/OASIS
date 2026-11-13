#Requires -Version 7.0
# Offline HTTP/parser contract; no provider activation or native request.
$ErrorActionPreference = 'Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/EosioPublicSourceCurrent/Offline'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Probe-EosioPublicReadiness.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Probe syntax failed.' }
$definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-Native'},$true)
. ([scriptblock]::Create($definition.Extent.Text))
$utf8=[Text.UTF8Encoding]::new($false);$endpoint='https://native.example.invalid/'
$calls=[Collections.Generic.List[object]]::new()
$reply=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::OK)
$reply.Content=[Net.Http.StringContent]::new('{"account_name":"eosio","created":"2018-06-08T08:08:08.500","ram_quota":9007199254740993}')
$client=[pscustomobject]@{Reply=$reply}
$client|Add-Member ScriptMethod SendAsync {
    param($request,$completion)
    if ($request.RequestUri.AbsoluteUri -cne 'https://native.example.invalid/v1/chain/get_account' -or $request.Method -ne [Net.Http.HttpMethod]::Post) { throw 'Native route/method changed.' }
    return [Threading.Tasks.Task[Net.Http.HttpResponseMessage]]::FromResult($this.Reply)
}
$items=@(Read-Native 'get_account' @{account_name='eosio'})
if ($items.Count -ne 1 -or $items[0].account_name -cne 'eosio' -or $items[0].created -isnot [string] -or
    $items[0].created -cne '2018-06-08T08:08:08.500' -or $items[0].ram_quota.ToString() -cne '9007199254740993') {
    throw 'Native reader leaked task output or changed date/integer representation.'
}
$bad=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::Forbidden)
$bad.Content=[Net.Http.StringContent]::new('{"error":"denied"}');$client.Reply=$bad
$rejected=$false
try { $discard=@(Read-Native 'get_account' @{account_name='eosio'}) }
catch { if ($_.Exception.Message -notlike '*Native HTTP status rejected*') { throw };$rejected=$true }
if (!$rejected -or $calls.Count -ne 2) { throw 'HTTP denial was not rejected exactly once.' }
'PASS: exact single result/date/integer preservation and fail-closed HTTP status; zero native calls.'

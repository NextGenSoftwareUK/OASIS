#Requires -Version 7.0
# Offline parser/transport contract. Never executes the probe's network main.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/BlueSkyPublicSourceCurrent/Offline'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Probe-BlueSkyPublicReadiness.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Probe syntax failed.' }
$definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Read-Profiles'},$true)
. ([scriptblock]::Create($definition.Extent.Text))
$utf8=[Text.UTF8Encoding]::new($false,$true);$endpoint='https://native.example.invalid/'
$calls=[Collections.Generic.List[object]]::new()
$did1='did:plc:z72i7hdynmk6r22z27h6tvur';$did2='did:plc:ewvi7nxzyoun6zhxrhs64oiz'
$reply=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::OK)
$reply.Content=[Net.Http.StringContent]::new('{"profiles":[{"did":"'+$did2+'","handle":"atproto.com"},{"did":"'+$did1+'","handle":"bsky.app"}]}')
$client=[pscustomobject]@{Reply=$reply}
$client|Add-Member ScriptMethod GetAsync {
    param($url,$completion)
    $uri=[Uri]$url
    if ($uri.Host -cne 'native.example.invalid' -or $uri.AbsolutePath -cne '/xrpc/app.bsky.actor.getProfiles' -or $uri.Query -notlike '*actors=did%3Aplc%3A*') { throw 'Bounded encoded GET route differs.' }
    return [Threading.Tasks.Task[Net.Http.HttpResponseMessage]]::FromResult($this.Reply)
}
$rows=Read-Profiles @($did1,$did2)
if ($rows.Count -ne 2 -or $rows[0].did -cne $did2 -or $rows[1].did -cne $did1) { throw 'Returned array changed or leaked task output.' }
$reply=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::OK)
$reply.Content=[Net.Http.StringContent]::new('{"profiles":[{"did":"'+$did1+'","handle":"bsky.app"},{"did":"'+$did1+'","handle":"bsky.app"}]}');$client.Reply=$reply
$rejected=$false
try {$discard=Read-Profiles @($did1,$did2)} catch {if ($_.Exception.Message -notlike '*Complete unique profile selection required*') {throw};$rejected=$true}
if (!$rejected) {throw 'Duplicate profile identity accepted.'}
$reply=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::Forbidden)
$reply.Content=[Net.Http.StringContent]::new('{"error":"denied"}');$client.Reply=$reply;$rejected=$false
try {$discard=Read-Profiles @($did1,$did2)} catch {if ($_.Exception.Message -notlike '*Native HTTP status rejected*') {throw};$rejected=$true}
if (!$rejected -or $calls.Count -ne 3) {throw 'HTTP denial was not observed once without retry.'}
'PASS: bounded encoded GET, exact complete array/no task-output leak, duplicate identity and HTTP denial rejected; zero native requests.'

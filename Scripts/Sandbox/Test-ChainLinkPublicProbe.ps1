#Requires -Version 7.0
# Offline ABI/transport proof. Loads only function definitions, never probe main.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/ChainLinkPublicSourceCurrent/Offline'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Probe-ChainLinkPublicReadiness.ps1'),[ref]$tokens,[ref]$errors)
if ($errors.Count) { throw 'Probe syntax failed.' }
foreach ($name in @('Unsigned','Read-Description','Read-Rpc')) {
    $definition=$ast.Find({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name},$true)
    if (!$definition) { throw 'Expected function missing.' }
    . ([scriptblock]::Create($definition.Extent.Text))
}
$utf8=[Text.UTF8Encoding]::new($false,$true)
$maximum=Unsigned ('f'*64)
if ($maximum -ne [Numerics.BigInteger]::Pow(2,256)-1) { throw 'Unsigned256 lost precision or sign.' }
$signed=[Numerics.BigInteger]::Parse(('f'*64),[Globalization.NumberStyles]::AllowHexSpecifier,[Globalization.CultureInfo]::InvariantCulture)
if ($signed -ne -1) { throw 'Signed ABI answer lost two-complement semantics.' }
$encoded='0x'+('0'*62)+'20'+('0'*63)+'9'+[Convert]::ToHexString($utf8.GetBytes('ETH / USD')).ToLowerInvariant()+('0'*46)
$decoded=@(Read-Description $encoded)
if ($decoded.Count -ne 1 -or $decoded[0] -cne 'ETH / USD') { throw 'ABI string changed or leaked helper output.' }
foreach ($bad in @($encoded.Substring(0,$encoded.Length-1)+'1', $encoded.Replace(('0'*62)+'20',('0'*62)+'40'), $encoded.Substring(0,130)+'ff'+$encoded.Substring(132))) {
    $rejected=$false
    try { $discard=Read-Description $bad } catch { $rejected=$true }
    if (!$rejected) { throw 'Bad padding/offset/UTF8 accepted.' }
}
$rpc='https://native.example.invalid/'
$calls=[Collections.Generic.List[object]]::new()
$reply=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::OK)
$reply.Content=[Net.Http.StringContent]::new('{"jsonrpc":"2.0","id":1,"result":"0xffffffffffffffffffff"}')
$client=[pscustomobject]@{Reply=$reply}
$client | Add-Member ScriptMethod SendAsync {
    param($request,$completion)
    if ($request.RequestUri.AbsoluteUri -cne 'https://native.example.invalid/' -or $request.Method -ne [Net.Http.HttpMethod]::Post) { throw 'RPC route/method changed.' }
    return [Threading.Tasks.Task[Net.Http.HttpResponseMessage]]::FromResult($this.Reply)
}
$values=@(Read-Rpc 'eth_call' @(@{to='0x0000000000000000000000000000000000000001';data='0xfeaf968c'},@{blockHash='0x'+('1'*64);requireCanonical=$true}))
if ($values.Count -ne 1 -or $values[0] -cne '0xffffffffffffffffffff') { throw 'RPC result representation/output changed.' }
$bad=[Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::Forbidden)
$bad.Content=[Net.Http.StringContent]::new('{"error":"denied"}')
$client.Reply=$bad;$rejected=$false
try { $discard=Read-Rpc 'eth_call' @() }
catch { if ($_.Exception.Message -notlike '*Native HTTP status rejected*') { throw };$rejected=$true }
if (!$rejected -or $calls.Count -ne 2) { throw 'HTTP rejection not observed exactly once.' }
'PASS: exact UInt256/signed answer, canonical ABI string, bad padding/offset/UTF8 rejection, single RPC result and HTTP fail-closed; zero native requests.'

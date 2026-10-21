$ErrorActionPreference='Stop'
$output='C:/Source/OASIS-quest-comparison/OASIS/STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/bin/Debug/net10.0'
$config=Join-Path $output 'OASIS_DNA.json'
$rows=[Collections.Generic.List[object]]::new()
$process=$null
function FindToken($node) {
 if(!$node){return $null}
 foreach($key in 'jwtToken','token','accessToken'){if($node.$key -is [string] -and $node.$key){return $node.$key}}
 foreach($p in $node.PSObject.Properties){if($p.Value -is [pscustomobject]){$t=FindToken $p.Value;if($t){return $t}}}
}
try {
 if(Test-Path $config){throw 'Runtime config already exists; refusing to overwrite.'}
 if(Get-NetTCPConnection -LocalPort 18888 -ErrorAction SilentlyContinue){throw 'Comparison port occupied.'}
 $rb=Join-Path (Split-Path (Get-Command railway).Source -Parent) 'node_modules/@railway/cli/bin/railway.exe'
 $vars=(& $rb variables -p 712f50f6-3b7b-46d4-b229-44b49922f16b -e dev -s bb2c3e9a-ec96-44a5-9916-05e06d54b1f4 --json)|ConvertFrom-Json
 if(!$vars.OASIS_DNA_JSON){throw 'Missing dev configuration.'}
 [IO.File]::WriteAllText($config,$vars.OASIS_DNA_JSON)
 $psi=[Diagnostics.ProcessStartInfo]::new('dotnet')
 $psi.ArgumentList.Add((Join-Path $output 'NextGenSoftware.OASIS.STAR.WebAPI.dll'))
 $psi.WorkingDirectory=$output;$psi.UseShellExecute=$false;$psi.CreateNoWindow=$true
 $psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true
 $psi.Environment['PORT']='18888';$psi.Environment['ASPNETCORE_ENVIRONMENT']='Testing'
 $psi.Environment['Web4OasisApiBaseUrl']='https://dev.api.web4.oasisomniverse.one'
 $psi.Environment['SUBSCRIPTION_USAGE_ENABLED']='false'
 $process=[Diagnostics.Process]::Start($psi)
 $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
 $ready=$false
 foreach($attempt in 1..45){
  if($process.HasExited){throw "Historical host exited ($($process.ExitCode))."}
  try{$r=Invoke-WebRequest 'http://127.0.0.1:18888/api/star/status' -TimeoutSec 2 -SkipHttpErrorCheck;if($r.StatusCode -eq 200){$ready=$true;break}}catch{}
  Start-Sleep -Seconds 1
 }
 if(!$ready){throw 'Historical host did not become ready.'}
 $cred=Import-Clixml (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml')
 $login=Invoke-RestMethod 'https://dev.api.web4.oasisomniverse.one/api/avatar/authenticate' -Method Post -ContentType 'application/json' -Body (@{username=$cred.UserName;password=$cred.GetNetworkCredential().Password}|ConvertTo-Json) -TimeoutSec 60
 $token=FindToken $login;$cred=$null;$login=$null
 if(!$token){throw 'No comparison session.'}
 foreach($iteration in 1..2){foreach($target in @(@{Name='Historical 2026-10-06 local';Origin='http://127.0.0.1:18888'},@{Name='Current dev Railway';Origin='https://dev.api.starnet.oasisomniverse.one'})){
  $watch=[Diagnostics.Stopwatch]::StartNew()
  $r=Invoke-WebRequest ($target.Origin+'/api/quests/all-for-avatar/game') -Headers @{Authorization="Bearer $token"} -TimeoutSec 90 -SkipHttpErrorCheck
  $watch.Stop();$json=$r.Content|ConvertFrom-Json -Depth 100
  $payload=$json.result;if($payload.PSObject.Properties.Name -contains 'result'){$payload=$payload.result}
  $ids=@($payload|ForEach-Object {[string]$_.id}|Sort-Object)
  $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($payload|ConvertTo-Json -Depth 100 -Compress))))
  $rows.Add([pscustomobject]@{Target=$target.Name;Iteration=$iteration;Utc=[DateTime]::UtcNow.ToString('O');Status=[int]$r.StatusCode;Milliseconds=$watch.ElapsedMilliseconds;Bytes=$r.RawContentLength;IsError=$json.isError;Count=@($payload).Count;Ids=$ids;PayloadHash=$hash})
  if($r.StatusCode -ne 200 -or $json.isError){throw 'Quest comparison request failed.'}
 }}
} finally {
 $token=$null;$vars=$null
 if($process -and !$process.HasExited){$process.Kill($true);$process.WaitForExit()}
 if($process){
  $outText=$stdout.GetAwaiter().GetResult();$errText=$stderr.GetAwaiter().GetResult()
  # Retain only diagnostic categories; startup output can contain account information.
  $diagnostics=@(($outText+"`n"+$errText)-split "`n"|Where-Object {$_ -match 'Exception|Error|fail:|Now listening|Application started'}|ForEach-Object {$_ -replace '(mongodb(?:\+srv)?://)\S+','$1[REDACTED]'})
  $diagnostics|Set-Content 'C:/Source/OASIS/Docs/WIP/Mongo-Test-Traffic/historical-host-diagnostics.log'
 }
 if(Test-Path $config){Remove-Item -LiteralPath $config}
 $rows|ConvertTo-Json -Depth 8|Set-Content 'C:/Source/OASIS/Docs/WIP/Mongo-Test-Traffic/historical-full-api-comparison.json'
 $rows|Select-Object Target,Iteration,Status,Milliseconds,Bytes,IsError,Count,PayloadHash|Format-Table -AutoSize
}

param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ConsumerOutput,
    [ValidateNotNullOrEmpty()][ValidateSet('Core','Shared')][string[]]$Dependencies=@('Core','Shared')
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($RepositoryRoot)
$consumer=if([IO.Path]::IsPathRooted($ConsumerOutput)){[IO.Path]::GetFullPath($ConsumerOutput)}else{[IO.Path]::GetFullPath((Join-Path $root $ConsumerOutput))}
if(!$consumer.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
    throw 'Provider consumer must belong to the existing repository environment.'
}
$producers=@{
    Core=@{directory='OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0';name='NextGenSoftware.OASIS.API.Core.dll'}
    Shared=@{directory='Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/bin/Debug/net10.0';name='NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll'}
}
function Get-AssemblySha256([string]$Path){
    $stream=[IO.File]::OpenRead($Path)
    $algorithm=[Security.Cryptography.SHA256]::Create()
    try{return [BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','')}
    finally{$algorithm.Dispose();$stream.Dispose()}
}
foreach($dependency in $Dependencies){
    $entry=$producers[$dependency]
    $source=Join-Path (Join-Path $root $entry.directory) $entry.name
    $target=Join-Path $consumer $entry.name
    $expected=Get-AssemblySha256 $source
    $actual=Get-AssemblySha256 $target
    if($actual -ne $expected){throw "Provider consumer dependency is stale: $($entry.name). Rebuild its producer/consumer; do not copy DLLs or start the service."}
    [pscustomobject]@{Dependency=$dependency;SHA256=$actual}
}

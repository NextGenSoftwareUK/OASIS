param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$ConsumerOutput,
    [ValidateNotNullOrEmpty()][ValidateSet('Core','Shared')][string[]]$Dependencies=@('Core','Shared'),
    [switch]$VerifySourceDocuments
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
function Assert-SourceDocuments([string]$AssemblyPath,[string]$ProjectRoot){
    # A version stamp alone cannot establish compiled-source identity. Bind the
    # portable PDB to this exact PE, then compare its local source checksums.
    Add-Type -AssemblyName System.Reflection.Metadata
    $pdbStream=[IO.File]::OpenRead([IO.Path]::ChangeExtension($AssemblyPath,'.pdb'))
    $assemblyStream=$null;$pdbProvider=$null;$pe=$null
    try{
        $assemblyStream=[IO.File]::OpenRead($AssemblyPath)
        $pdbProvider=[System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
        $reader=$pdbProvider.GetMetadataReader()
        $pe=[System.Reflection.PortableExecutable.PEReader]::new($assemblyStream)
        $id=[byte[]]$reader.DebugMetadataHeader.Id
        $codeView=@($pe.ReadDebugDirectory()|Where-Object Type -eq CodeView)
        if($id.Length -lt 16 -or $codeView.Count -ne 1 -or
            [Guid]::new([byte[]]$id[0..15]) -ne $pe.ReadCodeViewDebugDirectoryData($codeView[0]).Guid){
            throw 'Portable PDB does not identify the selected producer assembly.'
        }
        $checked=0
        foreach($handle in $reader.Documents){
            $document=$reader.GetDocument($handle);$name=$reader.GetString($document.Name)
            if(!$name.StartsWith($ProjectRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){continue}
            if(!(Test-Path -LiteralPath $name -PathType Leaf)){throw "Compiled source document is missing: $name"}
            if($reader.GetGuid($document.HashAlgorithm) -ne [Guid]'8829d00f-11b8-4213-878b-770e8597ac16'){
                throw 'Unsupported source checksum algorithm; no source-identity claim can be made.'
            }
            if((Get-AssemblySha256 $name) -ne [Convert]::ToHexString($reader.GetBlobBytes($document.Hash))){
                throw "Compiled source document differs from the current source: $name"
            }
            $checked++
        }
        if($checked -eq 0){throw 'No local project source documents were verified.'}
        return $checked
    }finally{
        if($pe){$pe.Dispose()};if($pdbProvider){$pdbProvider.Dispose()}
        if($assemblyStream){$assemblyStream.Dispose()};$pdbStream.Dispose()
    }
}
foreach($dependency in $Dependencies){
    $entry=$producers[$dependency]
    $source=Join-Path (Join-Path $root $entry.directory) $entry.name
    $target=Join-Path $consumer $entry.name
    $expected=Get-AssemblySha256 $source
    $actual=Get-AssemblySha256 $target
    if($actual -ne $expected){throw "Provider consumer dependency is stale: $($entry.name). Rebuild its producer/consumer; do not copy DLLs or start the service."}
    $identity=[ordered]@{Dependency=$dependency;SHA256=$actual}
    if($VerifySourceDocuments){
        $projectRoot=[IO.Path]::GetFullPath((Join-Path (Join-Path $root $entry.directory) '../../..'))
        $identity.SourceDocuments=Assert-SourceDocuments $source $projectRoot
    }
    [pscustomobject]$identity
}

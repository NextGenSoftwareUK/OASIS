param(
    [string]$NativeDirectory=(Join-Path $PSScriptRoot '../../TestResults/OracleCurrent/NativeCoverage'),
    [string]$UnitDirectory=(Join-Path $PSScriptRoot '../../TestResults/OracleCurrent/UnitCoverage')
)
$ErrorActionPreference='Stop'
$gapNative=& (Join-Path $PSScriptRoot 'Get-OracleCoverageEvidence.ps1') -ResultsDirectory $NativeDirectory -ReportName 'oracle-native-coverage-current.trx'
$gapUnit=& (Join-Path $PSScriptRoot 'Get-OracleCoverageEvidence.ps1') -ResultsDirectory $UnitDirectory -ReportName 'oracle-unit-coverage-current.trx'
if($gapNative.ProviderSHA256 -cne $gapUnit.ProviderSHA256 -or $gapNative.ProviderPdbSHA256 -cne $gapUnit.ProviderPdbSHA256){
    throw 'Coverage provider PE/PDB identities differ; do not combine different builds.'
}
$gapValid=[Collections.Generic.HashSet[string]]::new()
$gapHit=[Collections.Generic.HashSet[string]]::new()
$gapMethods=@{};$gapLineMaps=@()
foreach($gapEvidence in @($gapNative,$gapUnit)){
    [xml]$gapXml=Get-Content -LiteralPath $gapEvidence.Coverage -Raw
    $gapCurrent=[Collections.Generic.HashSet[string]]::new()
    foreach($gapClass in $gapXml.coverage.packages.package.classes.class){
        foreach($gapLine in $gapClass.lines.line){
            $gapKey=$gapClass.filename+'|'+$gapLine.number
            [void]$gapValid.Add($gapKey);[void]$gapCurrent.Add($gapKey)
            if([long]$gapLine.hits -gt 0){[void]$gapHit.Add($gapKey)}
        }
        foreach($gapMethod in $gapClass.methods.method){
            $gapKey=$gapClass.filename+'|'+$gapClass.name+'|'+$gapMethod.name+'|'+$gapMethod.signature
            if(!$gapMethods.ContainsKey($gapKey)){
                $gapName=[string]$gapMethod.name
                if($gapName -eq 'MoveNext' -and $gapClass.name -match '/<([^>]+)>d__'){$gapName=$Matches[1]}
                $gapMethods[$gapKey]=[pscustomobject]@{File=$gapClass.filename;Class=$gapClass.name;Method=$gapName;Lines=@($gapMethod.lines.line).Count;Hit=$false}
            }
            if(@($gapMethod.lines.line|Where-Object {[long]$_.hits -gt 0}).Count){$gapMethods[$gapKey].Hit=$true}
        }
    }
    $gapLineMaps+=,$gapCurrent
}
if(!$gapLineMaps[0].SetEquals($gapLineMaps[1])){throw 'Coverage source line maps differ; do not invent a merged denominator.'}
$gapUncovered=@($gapValid.Where({!$gapHit.Contains($_)}))
[pscustomobject]@{
    Scope='Oracle executable line-hit union only; not interface/provider completion or branch-hit union'
    Native=$gapNative;Unit=$gapUnit;ExecutableLines=$gapValid.Count;CoveredLineUnion=$gapHit.Count
    InstrumentedMethodBodiesWithLines=@($gapMethods.Values|Where-Object Lines -gt 0).Count
    ZeroHitMethodBodies=@($gapMethods.Values|Where-Object {$_.Lines -gt 0 -and !$_.Hit}|Sort-Object File,Method|Select-Object File,Class,Method,Lines)
    MissingLinesByFile=@($gapUncovered|Group-Object {($_ -split '\|')[0]}|Sort-Object Count -Descending|Select-Object Count,Name)
}|ConvertTo-Json -Depth 6

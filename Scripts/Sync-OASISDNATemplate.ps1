param([switch]$UpdateTemplate)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.DNA'
$template = Join-Path $project 'Default/OASIS_DNA.json'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $project 'bin/Release/net10.0/NextGenSoftware.OASIS.API.DNA.dll'))
$document = Get-Content -LiteralPath $template -Raw | ConvertFrom-Json
$script:changes = 0
function Complete-Object($type, $value, $path) {
    $defaults = [Activator]::CreateInstance($type)
    foreach ($property in $type.GetProperties()) {
        if (!$property.CanWrite -or $property.GetIndexParameters().Count) { continue }
        $name = $property.Name
        $field = "$path.$name"
        $member = $value.PSObject.Properties[$name]
        $propertyType = $property.PropertyType
        $isSection = $propertyType.IsClass -and !$propertyType.IsGenericType -and !$propertyType.IsArray -and
            $propertyType -ne [string] -and $propertyType.Namespace -like 'NextGenSoftware.*'
        if (!$member -or ($isSection -and $null -eq $member.Value)) {
            $default = $property.GetValue($defaults)
            if ($isSection) { $default = [pscustomobject]@{} }
            elseif ($null -eq $default -and $propertyType -eq [string]) { $default = '' }
            elseif ($null -eq $default -and $propertyType.IsGenericType) {
                $default = if ($propertyType.Name -like '*Dictionary*') { [pscustomobject]@{} } else { @() }
            }
            $value | Add-Member -NotePropertyName $name -NotePropertyValue $default -Force
            $script:changes++
        }
        if ($isSection) { Complete-Object $propertyType $value.$name $field }
        # Environment-variable names are configuration, never credential values.
        if ($name -match '^(ChainPrivateKey|NodePrivateKey|PrivateKey|ApiKey|ONETApiKey|Password|ResendKey)$' -and
            ![string]::IsNullOrEmpty([string]$value.$name)) {
            $value.$name = ''
            $script:changes++
        }
    }
}
Complete-Object ($assembly.GetType('NextGenSoftware.OASIS.API.DNA.OASISDNA')) $document 'DNA'
if ($UpdateTemplate) {
    $document | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $template -Encoding utf8NoBOM
    Write-Output "Template updated: $script:changes schema/credential corrections."
} elseif ($script:changes) {
    throw "DNA template needs $script:changes schema/credential corrections. Build DNA, then run with -UpdateTemplate and review the diff."
} else { Write-Output 'DNA template schema and credential-field checks passed.' }

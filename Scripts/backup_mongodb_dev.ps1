[CmdletBinding()]
param(
    [string]$OutputRoot = 'artifacts\mongodb-backups',
    [string]$MongoDumpPath
)

& (Join-Path $PSScriptRoot 'backup_mongodb.ps1') -Database OASISAPI_DEV `
    -OutputRoot $OutputRoot -MongoDumpPath $MongoDumpPath

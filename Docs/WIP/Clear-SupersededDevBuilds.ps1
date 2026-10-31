$ErrorActionPreference = 'Stop'
$railwayBinary = Join-Path (Split-Path (Get-Command railway).Source -Parent) 'node_modules/@railway/cli/bin/railway.exe'
$projectId = '712f50f6-3b7b-46d4-b229-44b49922f16b'
$environmentId = 'a31011a2-d047-4057-b5e8-57d52ebcfefe'
$services = (Get-Content Scripts/railway_release_gate.json -Raw | ConvertFrom-Json).services
$report = @()
foreach ($number in 5..10) {
    $serviceId = $services."$number"
    $deployments = @((& $railwayBinary deployment list --project $projectId -e $environmentId -s $serviceId --limit 250 --json) | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0) { throw "Unable to list WEB$number" }
    $queued = @($deployments | Where-Object status -eq 'QUEUED' | Sort-Object createdAt -Descending)
    if ($queued.Count -lt 2) { continue }
    $keep = $queued[0]
    $candidates = @()
    foreach ($deployment in $queued | Select-Object -Skip 1) {
        if ($deployment.meta.branch -ne 'Development' -or !$deployment.meta.commitHash -or !$keep.meta.commitHash) { continue }
        git merge-base --is-ancestor $deployment.meta.commitHash $keep.meta.commitHash
        if ($LASTEXITCODE -ne 0) { continue }
        $candidates += $deployment
    }
    # Recheck actual environment, service and state immediately before cancellation.
    foreach ($batch in @($candidates | Group-Object { [math]::Floor([array]::IndexOf($candidates, $_) / 20) })) {
        $items = @($batch.Group)
        $fields = for ($i=0; $i -lt $items.Count; $i++) { 'd' + $i + ':deployment(id:"' + $items[$i].id + '"){id status environmentId serviceId}' }
        $checked = (& $railwayBinary api ('query{' + ($fields -join ' ') + '}') --compact) | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $checked.errors) { throw 'Deployment recheck failed' }
        $safe = @($checked.data.PSObject.Properties.Value | Where-Object { $_.status -eq 'QUEUED' -and $_.environmentId -eq $environmentId -and $_.serviceId -eq $serviceId -and $_.id -ne $keep.id })
        if (!$safe.Count) { continue }
        $fields = for ($i=0; $i -lt $safe.Count; $i++) { 'd' + $i + ':deploymentCancel(id:"' + $safe[$i].id + '")' }
        $result = (& $railwayBinary api ('mutation{' + ($fields -join ' ') + '}') --compact) | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or $result.errors) { throw 'Cancellation returned errors' }
        for ($i=0; $i -lt $safe.Count; $i++) {
            $report += [pscustomobject]@{ Service="WEB$number"; Id=$safe[$i].id; Cancelled=$result.data."d$i"; Retained=$keep.id }
        }
        $report | ConvertTo-Json -Depth 5 | Set-Content Docs/WIP/superseded-dev-build-cleanup.json
    }
    Write-Output "WEB${number}: retained $($keep.id); cancelled $(@($report | Where-Object Service -eq "WEB$number").Count) superseded queued builds"
}

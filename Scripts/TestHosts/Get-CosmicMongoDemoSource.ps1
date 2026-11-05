param([string]$Root,[switch]$RecoveryMongo)
if ($RecoveryMongo) {
    $recovery = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopMongo82'
    $receipt = Get-Content (Join-Path $recovery 'replica.ready.json') -Raw | ConvertFrom-Json
    $runtime = 'C:/Program Files/MongoDB/Server/8.2/bin/mongod.exe'
    $data = Join-Path $recovery 'data'
    if ($receipt.runtime -ne $runtime -or $receipt.address -ne '127.0.0.1:27029' -or
        $receipt.replica -ne 'oasisRecoveryRs' -or (Get-FileHash -LiteralPath $runtime).Hash -ne $receipt.sha256 -or
        !(Test-Path -LiteralPath (Join-Path $data 'WiredTiger'))) {
        throw 'Recovery Mongo runtime or store differs from the admitted replica.'
    }
} else {
    $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
    $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
    if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Original Mongo store missing.'}
    if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo version differs.'}
}
[pscustomobject]@{Runtime=$runtime; Data=$data}

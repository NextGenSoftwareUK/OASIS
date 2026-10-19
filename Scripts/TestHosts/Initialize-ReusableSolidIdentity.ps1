$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$work=Join-Path $taskRoot 'TestResults/SolidCurrent'
Add-Type -AssemblyName System.Security
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
    $path=Join-Path $work 'identity.dpapi'
    $pending=Join-Path $work 'identity-create.pending'
    if(Test-Path -LiteralPath $path){
        $raw=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($path),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        try{$identity=[Text.Encoding]::UTF8.GetString($raw) | ConvertFrom-Json}finally{[Array]::Clear($raw,0,$raw.Length)}
    }else{
        if(Test-Path -LiteralPath $pending){throw 'Unresolved original account creation; reconcile it, never create another identity.'}
        $discovery=Invoke-RestMethod 'http://localhost:13000/.account/' -Headers @{Accept='application/json'}
        if($discovery.controls.account.create -ne 'http://localhost:13000/.account/account/'){throw 'Unexpected account creation scope.'}
        $guard=[IO.File]::Open($pending,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$guard.Dispose()
        # This mutation is never automatically replayed if its response is lost.
        $identity=Invoke-RestMethod $discovery.controls.account.create -Method Post -ContentType 'application/json' -Body '{}' -Headers @{Accept='application/json'}
        if([string]::IsNullOrWhiteSpace($identity.authorization)){throw 'Issuer returned no account authorization; preserve pending marker.'}
        $raw=[Text.Encoding]::UTF8.GetBytes(($identity | ConvertTo-Json -Depth 20 -Compress))
        try{
            $cipher=[Security.Cryptography.ProtectedData]::Protect($raw,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$output.Write($cipher,0,$cipher.Length)}finally{$output.Dispose()}
        }finally{[Array]::Clear($raw,0,$raw.Length)}
        Remove-Item -LiteralPath $pending
    }
    $index=Invoke-RestMethod 'http://localhost:13000/.account/' -Headers @{Accept='application/json';Authorization="CSS-Account-Token $($identity.authorization)"}
    # Public controls only; never emit authorization, passwords or client secrets.
    function Invoke-OwnedProvisioningStep($name,$uri,$body){
        $saved=Join-Path $work "$name.dpapi"
        $marker=Join-Path $work "$name.pending"
        if(Test-Path -LiteralPath $saved){
            $bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($saved),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            try{return ([Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json)}finally{[Array]::Clear($bytes,0,$bytes.Length)}
        }
        if(Test-Path -LiteralPath $marker){throw "Unresolved original $name provisioning; reconcile before continuing."}
        $guard=[IO.File]::Open($marker,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);$guard.Dispose()
        $result=Invoke-RestMethod $uri -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Compress) -Headers @{Accept='application/json';Authorization="CSS-Account-Token $($identity.authorization)"}
        $bytes=[Text.Encoding]::UTF8.GetBytes(($result | ConvertTo-Json -Depth 20 -Compress))
        try{
            $cipher=[Security.Cryptography.ProtectedData]::Protect($bytes,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $output=[IO.File]::Open($saved,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$output.Write($cipher,0,$cipher.Length)}finally{$output.Dispose()}
        }finally{[Array]::Clear($bytes,0,$bytes.Length)}
        Remove-Item -LiteralPath $marker
        return $result
    }
    $passwordPath=Join-Path $work 'identity-password.dpapi'
    if(!(Test-Path -LiteralPath $passwordPath)){
        $random=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
        $bytes=[Text.Encoding]::UTF8.GetBytes([Convert]::ToBase64String($random))
        try{
            $cipher=[Security.Cryptography.ProtectedData]::Protect($bytes,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $output=[IO.File]::Open($passwordPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$output.Write($cipher,0,$cipher.Length)}finally{$output.Dispose()}
        }finally{[Array]::Clear($bytes,0,$bytes.Length);[Array]::Clear($random,0,$random.Length)}
    }
    $bytes=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($passwordPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try{$password=[Text.Encoding]::UTF8.GetString($bytes)}finally{[Array]::Clear($bytes,0,$bytes.Length)}
    $passwordUri=[Uri]$index.controls.password.create
    if($passwordUri.Authority -ne 'localhost:13000' -or !$passwordUri.AbsolutePath.StartsWith('/.account/account/')){throw 'Unexpected password control scope.'}
    $null=Invoke-OwnedProvisioningStep 'private-login' $passwordUri @{email='oasis-retained-test@example.invalid';password=$password}
    $password=$null
    foreach($kind in @('pod','clientCredentials')){
        $uri=[Uri]$index.controls.account.$kind
        if($uri.Scheme -ne 'http' -or $uri.Authority -ne 'localhost:13000' -or !$uri.AbsolutePath.StartsWith('/.account/account/')){throw 'Unexpected account control scope.'}
        $schema=Invoke-RestMethod $uri -Headers @{Accept='application/json';Authorization="CSS-Account-Token $($identity.authorization)"}
        if($kind -eq 'pod'){
            $pod=Invoke-OwnedProvisioningStep 'private-local-pod' $uri @{name='oasis-auth-local'}
            if([string]::IsNullOrWhiteSpace($pod.webId)){throw 'Created Pod response missing WebID; inspect retained protected response.'}
        }else{
            $client=Invoke-OwnedProvisioningStep 'private-local-client' $uri @{name='oasis-retained-provider-tests';webId=$pod.webId}
            if([string]::IsNullOrWhiteSpace($client.id) -or [string]::IsNullOrWhiteSpace($client.secret)){throw 'Client response missing credential; inspect retained protected response.'}
        }
    }
    Write-Output 'Retained synthetic identity, private Pod and client credential are provisioned; secrets remain DPAPI protected.'
}finally{$lease.Dispose()}

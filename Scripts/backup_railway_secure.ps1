<#
.SYNOPSIS
Creates a password-encrypted, portable backup of the currently linked Railway project.
.DESCRIPTION
Requires PowerShell 7 on .NET 8+ and an authenticated Railway CLI for export.
Exports accessible environments, raw shared/service variables (including DNA and secrets),
service build/deployment/source/domain settings, and a project inventory snapshot.
Database/volume contents, account credentials and settings not exposed by the API are excluded.
Unreadable/sealed variable names are recorded inside the encrypted payload and their count is reported.
Only encrypted data is written by default. Explicit Plaintext export and DecryptPath write
secret-bearing JSON only to the specified OutputPath. Encryption is AES-256-GCM with a random 32-byte salt,
12-byte nonce, 16-byte tag and PBKDF2-HMAC-SHA256 (600000 iterations).
The authenticated format header is stored with the encrypted envelope. No Windows-bound
encryption is used. Keep the backup password separately; there is no password recovery.
OutputPath must be new: existing backups are never overwritten. Read-back verification
decrypts the saved file in memory and compares it with the exported bytes before reporting success.
VerifyPath requires neither Railway access nor a network connection, and writes no plaintext.
Verification does not restore settings. Review the decrypted configuration and environment
targets through a separately authorized restoration process before changing Railway.
.EXAMPLE
pwsh -File Scripts/backup_railway_secure.ps1 -OutputPath C:\Backups\railway.encrypted.json
Prompts securely in the local terminal. Never supply a password through chat or command arguments.
.EXAMPLE
pwsh -STA -File Scripts/backup_railway_secure.ps1 -PasswordDialog -OutputPath C:\Backups\railway.encrypted.json
Uses a Windows password/confirmation dialog. Other platforms use the terminal prompt.
.EXAMPLE
pwsh -File Scripts/backup_railway_secure.ps1 -VerifyPath C:\Backups\railway.encrypted.json
Verifies an existing backup offline using a locally entered password.
.EXAMPLE
pwsh -File Scripts/backup_railway_secure.ps1 -SelfTest
Tests serialized-envelope round trip, wrong-password rejection and tamper rejection with synthetic data.
.EXAMPLE
pwsh -File Scripts/backup_railway_secure.ps1 -DecryptPath C:\Backups\railway.encrypted.json -OutputPath C:\Backups\railway.private.json
Explicitly exports plaintext JSON. The output contains credentials and private keys: store it securely,
never commit or share it, and remove it when no longer needed. No Railway settings are changed.
.EXAMPLE
pwsh -File Scripts/backup_railway_secure.ps1 -Plaintext -OutputPath C:\Backups\railway.private.json
Exports a fresh plain JSON snapshot without password prompting. Encrypt this secret-bearing file
yourself and keep it out of Git. Existing files are never overwritten.
#>
param(
    [string]$OutputPath,
    [string]$VerifyPath,
    [string]$DecryptPath,
    [switch]$Plaintext,
    [switch]$PasswordDialog,
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
if ($VerifyPath -and $DecryptPath) { throw 'Choose verification or decryption, not both.' }
if ($Plaintext -and ($VerifyPath -or $DecryptPath -or $SelfTest)) { throw 'Plaintext export cannot be combined with verification, decryption or self-test.' }
if ($DecryptPath -and !$OutputPath) { throw 'Decryption requires an explicit plaintext OutputPath.' }
# Passwords and exported settings never travel through arguments or plaintext files.
function Get-BackupPassword {
    if ($PasswordDialog) {
        Add-Type -AssemblyName System.Windows.Forms
        $form = New-Object Windows.Forms.Form
        $form.Text = 'Portable Railway backup password'
        $form.Width = 480; $form.Height = 260; $form.StartPosition = 'CenterScreen'
        $label = New-Object Windows.Forms.Label
        $label.Text = if ($DecryptPath) { 'Enter backup password twice. Output will contain plaintext secrets.' } else { 'Enter a strong backup password (16+ characters). Save it separately.' }
        $label.SetBounds(15,15,440,35); $form.Controls.Add($label)
        $box = New-Object Windows.Forms.TextBox
        $box.UseSystemPasswordChar = $true; $box.SetBounds(15,55,430,25); $form.Controls.Add($box)
        $confirm = New-Object Windows.Forms.TextBox
        $confirm.UseSystemPasswordChar = $true; $confirm.SetBounds(15,90,430,25); $form.Controls.Add($confirm)
        $button = New-Object Windows.Forms.Button
        $button.Text = if ($DecryptPath) { 'Decrypt backup' } elseif ($VerifyPath) { 'Verify backup' } else { 'Encrypt backup' }
        $button.SetBounds(290,125,155,30)
        $feedback = New-Object Windows.Forms.Label
        $feedback.ForeColor = [Drawing.Color]::DarkRed
        $feedback.SetBounds(15,165,430,45); $form.Controls.Add($feedback)
        $minimumLength = if ($DecryptPath -or $VerifyPath) { 1 } else { 16 }
        $button.Add_Click({
            if ($box.Text.Length -lt $minimumLength) {
                $feedback.Text = if ($minimumLength -eq 1) { 'Enter your existing backup password.' } else { 'Use at least 16 characters for the new backup password.' }
                return
            }
            if ($box.Text -cne $confirm.Text) {
                $feedback.Text = 'The two password entries do not match. Please re-enter them.'
                return
            }
            $form.DialogResult = 'OK'; $form.Close()
        })
        $form.Controls.Add($button); $form.AcceptButton = $button
        if ($form.ShowDialog() -ne 'OK') { throw 'Backup cancelled; nothing exported.' }
        $password = $box.Text; $form.Dispose(); return $password
    }
    $secure = Read-Host 'Backup password (16+ characters; retain separately)' -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr); $secure.Dispose() }
    $minimumLength = if ($DecryptPath -or $VerifyPath) { 1 } else { 16 }
    if ($password.Length -lt $minimumLength) { throw "Password must contain at least $minimumLength characters." }
    return $password
}
function Protect-Backup([byte[]]$Data, [string]$Password) {
    $salt = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    $nonce = [Security.Cryptography.RandomNumberGenerator]::GetBytes(12)
    $header = 'OASIS-Railway-Backup-v1|PBKDF2-SHA256|600000|AES-256-GCM'
    $key = [Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2($Password,$salt,600000,[Security.Cryptography.HashAlgorithmName]::SHA256,32)
    $cipher = [byte[]]::new($Data.Length); $tag = [byte[]]::new(16)
    $aes = [Security.Cryptography.AesGcm]::new($key,16)
    try { $aes.Encrypt($nonce,$Data,$cipher,$tag,[Text.Encoding]::UTF8.GetBytes($header)) }
    finally { $aes.Dispose(); [Array]::Clear($key,0,$key.Length) }
    return @{header=$header;salt=[Convert]::ToBase64String($salt);nonce=[Convert]::ToBase64String($nonce);tag=[Convert]::ToBase64String($tag);ciphertext=[Convert]::ToBase64String($cipher)}
}
function Unprotect-Backup($Envelope,[string]$Password) {
    if ($Envelope.header -cne 'OASIS-Railway-Backup-v1|PBKDF2-SHA256|600000|AES-256-GCM') { throw 'Unsupported backup format.' }
    $key = [Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2($Password,[Convert]::FromBase64String($Envelope.salt),600000,[Security.Cryptography.HashAlgorithmName]::SHA256,32)
    $cipher = [Convert]::FromBase64String($Envelope.ciphertext); $plain = [byte[]]::new($cipher.Length)
    $aes = [Security.Cryptography.AesGcm]::new($key,16)
    try { $aes.Decrypt([Convert]::FromBase64String($Envelope.nonce),$cipher,[Convert]::FromBase64String($Envelope.tag),$plain,[Text.Encoding]::UTF8.GetBytes($Envelope.header)) }
    finally { $aes.Dispose(); [Array]::Clear($key,0,$key.Length) }
    return ,$plain
}
if ($SelfTest) {
    $data = [Text.Encoding]::UTF8.GetBytes('Synthetic test only')
    $e = Protect-Backup $data 'Synthetic-password-only-123'
    $e = $e | ConvertTo-Json -Compress | ConvertFrom-Json
    if ([Convert]::ToBase64String((Unprotect-Backup $e 'Synthetic-password-only-123')) -cne [Convert]::ToBase64String($data)) { throw 'Round trip failed.' }
    $rejected = $false
    try { $null = Unprotect-Backup $e 'Wrong-password-only-123' } catch { $rejected = $true }
    if (!$rejected) { throw 'Wrong password accepted.' }
    $tag = [Convert]::FromBase64String($e.tag); $tag[0] = $tag[0] -bxor 1; $e.tag = [Convert]::ToBase64String($tag)
    $rejected = $false
    try { $null = Unprotect-Backup $e 'Synthetic-password-only-123' } catch { $rejected = $true }
    if (!$rejected) { throw 'Tampering accepted.' }
    Write-Output 'Encryption round trip, wrong-password rejection and tamper rejection passed.'; exit
}
$password = if ($Plaintext) { $null } else { Get-BackupPassword }
if ($DecryptPath) {
    $plain = $null
    try {
        $plain = Unprotect-Backup (Get-Content -LiteralPath $DecryptPath -Raw | ConvertFrom-Json) $password
        $backup = [Text.Encoding]::UTF8.GetString($plain) | ConvertFrom-Json
        if ($backup.format -ne 1 -or $null -eq $backup.environments) { throw 'Decrypted payload has an unsupported schema.' }
        $full = [IO.Path]::GetFullPath($OutputPath)
        $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($full))
        $stream = [IO.File]::Open($full,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try { $stream.Write($plain,0,$plain.Length) } finally { $stream.Dispose() }
        Write-Output "Decrypted to: $full"
        Write-Warning 'This file contains plaintext secrets/private keys. Keep it secure and out of Git.'
    } finally {
        if ($plain) { [Array]::Clear($plain,0,$plain.Length) }
        $password=$null; $backup=$null
    }
    exit
}
if ($VerifyPath) {
    $plain = Unprotect-Backup (Get-Content -LiteralPath $VerifyPath -Raw | ConvertFrom-Json) $password
    $backup = [Text.Encoding]::UTF8.GetString($plain) | ConvertFrom-Json
    Write-Output "Verified encrypted backup: $($backup.environments.Count) environments. No plaintext files written."
    [Array]::Clear($plain,0,$plain.Length); exit
}
if (!$OutputPath) { throw 'Specify OutputPath for the encrypted backup.' }
function Invoke-RailwayJson([string[]]$Arguments) {
    $raw = & railway @Arguments 2>$null
    if ($LASTEXITCODE -ne 0) { throw 'Railway export request failed. Secret-bearing response suppressed.' }
    try { return ($raw -join "`n" | ConvertFrom-Json -Depth 100) }
    catch { throw 'Railway export returned invalid JSON. Response suppressed.' }
}
# Capture all accessible scalar configuration plus source/domain configuration.
function Get-Selection([string]$Type,[int]$Depth=0) {
    $schema = Invoke-RailwayJson @('api','describe',$Type)
    $fields = foreach ($field in $schema.matches[0].fields) {
        if ($field.args.Count -gt 0) { continue }
        if ($field.namedType -in @('String','ID','Int','Float','Boolean','DateTime','JSON','Builder','RestartPolicyType')) { $field.name }
        elseif ($Depth -lt 3 -and $field.name -in @('source','domains','serviceDomains','customDomains','edgeConfig','resolvedFileConfig')) {
            $nested = Get-Selection $field.namedType ($Depth+1)
            if ($nested) { "$($field.name) { $nested }" }
        }
    }
    return ($fields -join ' ')
}
$project = Invoke-RailwayJson @('status','--json')
$selection = Get-Selection 'ServiceInstance'
$missing = [Collections.Generic.List[string]]::new()
$environments = foreach ($edge in $project.environments.edges) {
    $env = $edge.node
    $sharedQuery = 'query($p:String!,$e:String!){variables(projectId:$p,environmentId:$e,unrendered:true)}'
    $shared = Invoke-RailwayJson @('api',$sharedQuery,'--raw-var',"p=$($project.id)",'--raw-var',"e=$($env.id)",'--compact')
    $sharedValues = if ($shared.data) { $shared.data.variables } else { $shared.variables }
    foreach ($v in $sharedValues.PSObject.Properties) { if ($null -eq $v.Value) { $missing.Add("$($env.name)/shared/$($v.Name)") } }
    $services = foreach ($serviceEdge in $env.serviceInstances.edges) {
        $service = $serviceEdge.node
        $variablesQuery = 'query($p:String!,$e:String!,$s:String!){variables(projectId:$p,environmentId:$e,serviceId:$s,unrendered:true)}'
        $variablesResponse = Invoke-RailwayJson @('api',$variablesQuery,'--raw-var',"p=$($project.id)",'--raw-var',"e=$($env.id)",'--raw-var',"s=$($service.serviceId)",'--compact')
        $vars = if ($variablesResponse.data) { $variablesResponse.data.variables } else { $variablesResponse.variables }
        if ($null -eq $vars) { throw 'Railway service-variable export returned no variable map. Export aborted.' }
        foreach ($v in $vars.PSObject.Properties) { if ($null -eq $v.Value) { $missing.Add("$($env.name)/$($service.serviceName)/$($v.Name)") } }
        $query = 'query($s:String!,$e:String!){serviceInstance(serviceId:$s,environmentId:$e){' + $selection + '}}'
        $settings = Invoke-RailwayJson @('api',$query,'--raw-var',"s=$($service.serviceId)",'--raw-var',"e=$($env.id)",'--compact')
        @{id=$service.serviceId;name=$service.serviceName;variables=$vars;settings=$settings}
    }
    @{id=$env.id;name=$env.name;sharedVariables=$shared;services=@($services)}
}
$payload = @{format=1;createdUtc=[DateTime]::UtcNow.ToString('o');projectSnapshot=$project;environments=@($environments);unreadableVariables=@($missing);excluded=@('Database and volume contents','Account credentials','Settings not exposed by Railway CLI/API')}
if ($Plaintext) {
    $full = [IO.Path]::GetFullPath($OutputPath)
    $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($full))
    $bytes = [Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 100))
    $stream = [IO.File]::Open($full,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
    $readback = [IO.File]::ReadAllBytes($full)
    try {
        if (![Security.Cryptography.CryptographicOperations]::FixedTimeEquals($bytes,$readback)) { throw 'Plaintext backup failed read-back verification.' }
        $null = [Text.Encoding]::UTF8.GetString($readback) | ConvertFrom-Json
    } finally { [Array]::Clear($bytes,0,$bytes.Length); [Array]::Clear($readback,0,$readback.Length) }
    Write-Output "Plain JSON exported and verified: $full"
    Write-Output "Environments: $($environments.Count); unreadable/sealed variables: $($missing.Count)."
    Write-Warning 'This file contains plaintext secrets/private keys. Encrypt it yourself and never commit or share it.'
    exit
}
$bytes = [Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Depth 100 -Compress))
$envelope = Protect-Backup $bytes $password
$verified = Unprotect-Backup $envelope $password
if (![Security.Cryptography.CryptographicOperations]::FixedTimeEquals($bytes,$verified)) { throw 'Backup verification failed.' }
$full = [IO.Path]::GetFullPath($OutputPath)
$null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($full))
$stream = [IO.File]::Open($full,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
try { $encoded = [Text.Encoding]::UTF8.GetBytes(($envelope | ConvertTo-Json -Compress)); $stream.Write($encoded,0,$encoded.Length) }
finally { $stream.Dispose() }
try {
    $stored = Get-Content -LiteralPath $full -Raw | ConvertFrom-Json
    $readback = Unprotect-Backup $stored $password
    if (![Security.Cryptography.CryptographicOperations]::FixedTimeEquals($bytes,$readback)) { throw 'Written backup failed read-back verification.' }
} finally {
    [Array]::Clear($bytes,0,$bytes.Length); [Array]::Clear($verified,0,$verified.Length)
    if ($readback) { [Array]::Clear($readback,0,$readback.Length) }
    $password=$null; $payload=$null
}
Write-Output "Encrypted and verified: $full"
Write-Output "Environments: $($environments.Count); unreadable/sealed variables: $($missing.Count). Database/volume data excluded."

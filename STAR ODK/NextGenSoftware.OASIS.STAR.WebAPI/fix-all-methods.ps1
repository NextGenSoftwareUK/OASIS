# Fix all remaining broken method calls
$files = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    
    # Fix all remaining broken method calls with any method name
    $content = $content -replace "// ([A-Za-z]+) - Method not available", "`$1"
    
    Set-Content -Path $file.FullName -Value $content -NoNewline
    Write-Host "Fixed all methods in: $($file.Name)"
}

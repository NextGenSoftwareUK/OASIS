Add-Type -AssemblyName System.Drawing
$path = Join-Path $PSScriptRoot "13.png"
$b = New-Object System.Drawing.Bitmap(32, 32)
$g = [System.Drawing.Graphics]::FromImage($b)
$g.Clear([System.Drawing.Color]::FromArgb(180, 80, 80))
$g.Dispose()
$b.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$b.Dispose()
Write-Output "Created $path"









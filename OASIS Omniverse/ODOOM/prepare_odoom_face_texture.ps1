[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [Parameter(Mandatory=$true)][string]$DestPath,
    [int]$Width = 33,
    [int]$Height = 30
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Face source not found: $SourcePath"
}
if ($Width -lt 1 -or $Height -lt 1) {
    throw "Invalid target face size: ${Width}x${Height}"
}

$destDir = Split-Path -Parent $DestPath
if (-not [string]::IsNullOrWhiteSpace($destDir) -and -not (Test-Path -LiteralPath $destDir)) {
    New-Item -ItemType Directory -Path $destDir | Out-Null
}

$src = [System.Drawing.Bitmap]::FromFile($SourcePath)
try {
    $drawW = $Width
    $drawH = $Height
    $drawX = 0
    $drawY = 0

    $dst = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $g = [System.Drawing.Graphics]::FromImage($dst)
        try {
            $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.DrawImage($src, $drawX, $drawY, $drawW, $drawH)
        } finally {
            $g.Dispose()
        }
        $dst.Save($DestPath, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $dst.Dispose()
    }

    Write-Host ("[face] Prepared OASFACE from '{0}' -> '{1}' size {2}x{3} (drawn {4}x{5})" -f $SourcePath, $DestPath, $Width, $Height, $drawW, $drawH)
}
finally {
    $src.Dispose()
}

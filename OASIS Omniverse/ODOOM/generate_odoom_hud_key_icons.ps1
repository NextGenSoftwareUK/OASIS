param(
    [Parameter(Mandatory=$true)][string]$OutputDir
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

function Draw-KeyIcon {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][string]$Metal,
        [Parameter(Mandatory=$true)][int]$Width,
        [Parameter(Mandatory=$true)][int]$Height
    )

    $baseW = 24
    $baseH = 12
    $bmp = New-Object System.Drawing.Bitmap($baseW, $baseH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        for ($y = 0; $y -lt $baseH; $y++) {
            for ($x = 0; $x -lt $baseW; $x++) {
                $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            }
        }

        $dark = if ($Metal -eq "gold") { [System.Drawing.Color]::FromArgb(255, 150, 95, 10) } else { [System.Drawing.Color]::FromArgb(255, 95, 105, 130) }
        $mid = if ($Metal -eq "gold") { [System.Drawing.Color]::FromArgb(255, 214, 162, 35) } else { [System.Drawing.Color]::FromArgb(255, 155, 166, 196) }
        $light = if ($Metal -eq "gold") { [System.Drawing.Color]::FromArgb(255, 246, 220, 120) } else { [System.Drawing.Color]::FromArgb(255, 214, 222, 240) }

        function P([int]$x, [int]$y, [System.Drawing.Color]$c) {
            if ($x -ge 0 -and $x -lt $baseW -and $y -ge 0 -and $y -lt $baseH) {
                $bmp.SetPixel($x, $y, $c)
            }
        }

        # Bow
        for ($y = 2; $y -le 8; $y++) { for ($x = 1; $x -le 7; $x++) { P $x $y $dark } }
        for ($y = 3; $y -le 7; $y++) { for ($x = 2; $x -le 6; $x++) { P $x $y $mid } }
        P 3 4 $light; P 4 4 $light; P 5 4 $light
        P 3 5 $light;               P 5 5 $light
        P 3 6 $light; P 4 6 $light; P 5 6 $light

        # Shaft
        for ($y = 5; $y -le 6; $y++) { for ($x = 8; $x -le 17; $x++) { P $x $y $dark } }
        for ($y = 5; $y -le 6; $y++) { for ($x = 9; $x -le 16; $x++) { P $x $y $mid } }
        P 10 5 $light; P 12 5 $light; P 14 5 $light

        # Teeth
        for ($y = 3; $y -le 8; $y++) { for ($x = 18; $x -le 21; $x++) { P $x $y $dark } }
        for ($y = 4; $y -le 7; $y++) { for ($x = 19; $x -le 20; $x++) { P $x $y $mid } }
        for ($y = 4; $y -le 5; $y++) { P 22 $y $dark }
        for ($x = 18; $x -le 22; $x++) { P $x 9 $dark }
        for ($x = 19; $x -le 21; $x++) { P $x 8 $mid }

        if ($Width -eq $baseW -and $Height -eq $baseH) {
            $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
        } else {
            $scaled = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $g = [System.Drawing.Graphics]::FromImage($scaled)
            try {
                $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
                $g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                $g.DrawImage($bmp, 0, 0, $Width, $Height)
                $scaled.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
            } finally {
                $g.Dispose()
                $scaled.Dispose()
            }
        }
    } finally {
        $bmp.Dispose()
    }
}

# Compact general inventory icons
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKGI0.png") -Metal "gold" -Width 12 -Height 6
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKSI0.png") -Metal "silver" -Width 12 -Height 6

# Dedicated key-slot icons (rendered at Doom-key size in status bar)
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKGI2.png") -Metal "gold" -Width 24 -Height 12
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKSI2.png") -Metal "silver" -Width 24 -Height 12

# Larger HUD panel copies
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKGI1.png") -Metal "gold" -Width 24 -Height 12
Draw-KeyIcon -Path (Join-Path $OutputDir "OQKSI1.png") -Metal "silver" -Width 24 -Height 12


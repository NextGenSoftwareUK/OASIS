[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [Parameter(Mandatory=$true)][string]$DestPath,
    [int]$MaxWidth = 0,
    [int]$MaxHeight = 0,
    [int]$PadBottom = 0,
    [switch]$Rotate90CW
)

Add-Type -AssemblyName System.Drawing
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    throw "SourcePath is empty."
}
if ([string]::IsNullOrWhiteSpace($DestPath)) {
    throw "DestPath is empty."
}
if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Input sprite not found: $SourcePath"
}

Write-Host ("[sprite] START source='{0}' dest='{1}' max={2}x{3} padBottom={4} rotate90={5}" -f $SourcePath, $DestPath, $MaxWidth, $MaxHeight, $PadBottom, [bool]$Rotate90CW)

$inBmp = $null
$bmp = $null
$workBmp = $null
$inBmp = [System.Drawing.Bitmap]::FromFile($SourcePath)
try {
    Write-Host ("[sprite] Loaded input size: {0}x{1}" -f $inBmp.Width, $inBmp.Height)
    $targetW = $inBmp.Width
    $targetH = $inBmp.Height
    if ($MaxWidth -gt 0 -or $MaxHeight -gt 0) {
        $sx = if ($MaxWidth -gt 0) { [double]$MaxWidth / [double]$inBmp.Width } else { [double]::PositiveInfinity }
        $sy = if ($MaxHeight -gt 0) { [double]$MaxHeight / [double]$inBmp.Height } else { [double]::PositiveInfinity }
        $s = [Math]::Min($sx, $sy)
        if ([double]::IsInfinity($s) -or $s -le 0) { $s = 1.0 }
        if ($s -gt 1.0) { $s = 1.0 } # don't upscale
        $targetW = [Math]::Max(1, [int][Math]::Round($inBmp.Width * $s))
        $targetH = [Math]::Max(1, [int][Math]::Round($inBmp.Height * $s))
    }
    Write-Host ("[sprite] Target size after scale clamp: {0}x{1}" -f $targetW, $targetH)

    $workBmp = New-Object System.Drawing.Bitmap($targetW, $targetH)
    try {
        $g = [System.Drawing.Graphics]::FromImage($workBmp)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.DrawImage($inBmp, 0, 0, $targetW, $targetH)
        } finally {
            $g.Dispose()
        }

        $bmp = $workBmp

        if ($Rotate90CW) {
            $bmp.RotateFlip([System.Drawing.RotateFlipType]::Rotate90FlipNone)
            Write-Host "[sprite] Applied Rotate90CW."
        }

        $w = $bmp.Width
        $h = $bmp.Height
        Write-Host ("[sprite] Working bitmap: {0}x{1}" -f $w, $h)

        # Estimate border background color
        $sumR = 0; $sumG = 0; $sumB = 0; $count = 0
        for ($x = 0; $x -lt $w; $x++) {
            $ct = $bmp.GetPixel($x, 0)
            $cb = $bmp.GetPixel($x, $h - 1)
            $sumR += $ct.R + $cb.R
            $sumG += $ct.G + $cb.G
            $sumB += $ct.B + $cb.B
            $count += 2
        }
        for ($y = 1; $y -lt ($h - 1); $y++) {
            $cl = $bmp.GetPixel(0, $y)
            $cr = $bmp.GetPixel($w - 1, $y)
            $sumR += $cl.R + $cr.R
            $sumG += $cl.G + $cr.G
            $sumB += $cl.B + $cr.B
            $count += 2
        }

        if ($count -gt 0) {
            $bgR = [int]($sumR / $count)
            $bgG = [int]($sumG / $count)
            $bgB = [int]($sumB / $count)
        } else {
            $bgR = 0; $bgG = 0; $bgB = 0
        }
        Write-Host ("[sprite] Border color estimate: R={0} G={1} B={2}" -f $bgR, $bgG, $bgB)

        # Flood-fill transparent from edges based on border color distance
        $tol = 42
        Write-Host ("[sprite] Flood-fill begin (tol={0})..." -f $tol)
        $visited = New-Object 'bool[,]' $w, $h
        $q = New-Object 'System.Collections.Generic.Queue[System.Drawing.Point]'
        $enqueue = {
            param([int]$xx, [int]$yy)
            if ($xx -lt 0 -or $yy -lt 0 -or $xx -ge $w -or $yy -ge $h) { return }
            if ($visited[$xx, $yy]) { return }
            $visited[$xx, $yy] = $true
            $q.Enqueue([System.Drawing.Point]::new($xx, $yy))
        }

        for ($x = 0; $x -lt $w; $x++) { & $enqueue $x 0; & $enqueue $x ($h - 1) }
        for ($y = 1; $y -lt ($h - 1); $y++) { & $enqueue 0 $y; & $enqueue ($w - 1) $y }

        $ffSteps = 0
        while ($q.Count -gt 0) {
            $p = $q.Dequeue()
            $c = $bmp.GetPixel($p.X, $p.Y)
            if ($c.A -eq 0) { continue }
            $dist = [Math]::Abs($c.R - $bgR) + [Math]::Abs($c.G - $bgG) + [Math]::Abs($c.B - $bgB)
            if ($dist -gt $tol) { continue }
            $bmp.SetPixel($p.X, $p.Y, [System.Drawing.Color]::FromArgb(0, $c.R, $c.G, $c.B))
            & $enqueue ($p.X - 1) $p.Y
            & $enqueue ($p.X + 1) $p.Y
            & $enqueue $p.X ($p.Y - 1)
            & $enqueue $p.X ($p.Y + 1)
            $ffSteps++
            if (($ffSteps % 20000) -eq 0) {
                Write-Host ("[sprite] Flood-fill progress: processed={0} queue={1}" -f $ffSteps, $q.Count)
            }
        }
        Write-Host ("[sprite] Flood-fill done: processed={0}" -f $ffSteps)

        # Normalize alpha to ensure true transparency reaches 0
        $minA = 255
        $maxA = 0
        Write-Host "[sprite] Alpha scan begin..."
        for ($y = 0; $y -lt $h; $y++) {
            for ($x = 0; $x -lt $w; $x++) {
                $a = $bmp.GetPixel($x, $y).A
                if ($a -lt $minA) { $minA = $a }
                if ($a -gt $maxA) { $maxA = $a }
            }
        }
        Write-Host ("[sprite] Alpha range: min={0} max={1}" -f $minA, $maxA)

        if ($minA -gt 0 -and $maxA -gt $minA) {
            $den = $maxA - $minA
            Write-Host "[sprite] Alpha normalization begin..."
            for ($y = 0; $y -lt $h; $y++) {
                for ($x = 0; $x -lt $w; $x++) {
                    $c = $bmp.GetPixel($x, $y)
                    $na = [int](($c.A - $minA) * 255 / $den)
                    if ($na -lt 8) { $na = 0 }
                    $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($na, $c.R, $c.G, $c.B))
                }
                if (($y % 64) -eq 0 -and $h -gt 128) {
                    Write-Host ("[sprite] Alpha normalization row {0}/{1}" -f $y, $h)
                }
            }
            Write-Host "[sprite] Alpha normalization done."
        } else {
            Write-Host "[sprite] Alpha normalization skipped."
        }

        if ($PadBottom -gt 0) {
            Write-Host ("[sprite] Applying bottom padding: +{0}px" -f $PadBottom)
            $padded = New-Object System.Drawing.Bitmap($w, ($h + $PadBottom))
            try {
                $gp = [System.Drawing.Graphics]::FromImage($padded)
                try {
                    $gp.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
                    $gp.DrawImage($bmp, 0, 0, $w, $h)
                } finally {
                    $gp.Dispose()
                }
                $bmp.Dispose()
                $bmp = $padded
                $w = $bmp.Width
                $h = $bmp.Height
                $padded = $null
            } finally {
                if ($padded -ne $null) { $padded.Dispose() }
            }
            Write-Host ("[sprite] Padded size: {0}x{1}" -f $w, $h)
        }

        $outDir = Split-Path -Parent $DestPath
        if (-not (Test-Path -LiteralPath $outDir)) {
            New-Item -ItemType Directory -Path $outDir -Force | Out-Null
        }
        Write-Host "[sprite] Saving PNG..."
        $bmp.Save($DestPath, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host ("[sprite] DONE in {0} ms" -f $swTotal.ElapsedMilliseconds)
    } finally {
        if ($bmp -ne $null) { $bmp.Dispose() }
        elseif ($workBmp -ne $null) { $workBmp.Dispose() }
    }
} finally {
    if ($inBmp -ne $null) { $inBmp.Dispose() }
}


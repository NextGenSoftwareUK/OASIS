[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$SourcePath,
    [Parameter(Mandatory=$true)][string]$DestPath,
    [int]$MaxWidth = 0,
    [int]$MaxHeight = 0,
    [int]$PadBottom = 0,
    [switch]$Rotate90CW,
    [switch]$WriteGrabOffsets
)

Add-Type -AssemblyName System.Drawing
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

function Get-Crc32 {
    param([byte[]]$Bytes)
    [uint32]$crc = 4294967295
    foreach ($b in $Bytes) {
        $crc = $crc -bxor [uint32]$b
        for ($i = 0; $i -lt 8; $i++) {
            if (($crc -band 1) -ne 0) {
                $crc = (($crc -shr 1) -bxor [uint32]3988292384)
            } else {
                $crc = ($crc -shr 1)
            }
        }
    }
    return ($crc -bxor [uint32]4294967295)
}

function To-BigEndianBytesUInt32 {
    param([uint32]$Value)
    $b = [BitConverter]::GetBytes($Value)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
    return $b
}

function To-BigEndianBytesInt32 {
    param([int]$Value)
    $b = [BitConverter]::GetBytes($Value)
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($b) }
    return $b
}

function Write-PngWithGrAb {
    param(
        [string]$PngPath,
        [int]$GrabX,
        [int]$GrabY
    )

    [byte[]]$png = [System.IO.File]::ReadAllBytes($PngPath)
    if ($png.Length -lt 33) {
        throw "PNG too small: $PngPath"
    }
    [byte[]]$sig = 137,80,78,71,13,10,26,10
    for ($i = 0; $i -lt 8; $i++) {
        if ($png[$i] -ne $sig[$i]) {
            throw "Invalid PNG signature: $PngPath"
        }
    }

    # First chunk should be IHDR; insert grAb immediately after it.
    [uint32]$ihdrLen = ([uint32]$png[8] -shl 24) -bor ([uint32]$png[9] -shl 16) -bor ([uint32]$png[10] -shl 8) -bor [uint32]$png[11]
    $firstChunkTotal = 4 + 4 + [int]$ihdrLen + 4
    $insertPos = 8 + $firstChunkTotal
    if ($insertPos -gt $png.Length) {
        throw "IHDR chunk parse failed: $PngPath"
    }

    [byte[]]$type = [System.Text.Encoding]::ASCII.GetBytes("grAb")
    [byte[]]$data = New-Object byte[] 8
    [byte[]]$xBytes = To-BigEndianBytesInt32 $GrabX
    [byte[]]$yBytes = To-BigEndianBytesInt32 $GrabY
    [Array]::Copy($xBytes, 0, $data, 0, 4)
    [Array]::Copy($yBytes, 0, $data, 4, 4)

    [byte[]]$crcInput = New-Object byte[] 12
    [Array]::Copy($type, 0, $crcInput, 0, 4)
    [Array]::Copy($data, 0, $crcInput, 4, 8)
    [uint32]$crc = Get-Crc32 $crcInput

    [byte[]]$lenBytes = To-BigEndianBytesUInt32 8
    [byte[]]$crcBytes = To-BigEndianBytesUInt32 $crc

    [byte[]]$chunk = New-Object byte[] 20
    [Array]::Copy($lenBytes, 0, $chunk, 0, 4)
    [Array]::Copy($type, 0, $chunk, 4, 4)
    [Array]::Copy($data, 0, $chunk, 8, 8)
    [Array]::Copy($crcBytes, 0, $chunk, 16, 4)

    [byte[]]$out = New-Object byte[] ($png.Length + $chunk.Length)
    [Array]::Copy($png, 0, $out, 0, $insertPos)
    [Array]::Copy($chunk, 0, $out, $insertPos, $chunk.Length)
    [Array]::Copy($png, $insertPos, $out, $insertPos + $chunk.Length, $png.Length - $insertPos)
    [System.IO.File]::WriteAllBytes($PngPath, $out)
}

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

        $isCompactKeySprite = ($PadBottom -le 0 -and $MaxWidth -gt 0 -and $MaxWidth -le 18 -and $MaxHeight -gt 0 -and $MaxHeight -le 24)
        $hasTrueTransparency = $false
        $minAProbe = 255
        for ($y = 0; $y -lt $h; $y++) {
            for ($x = 0; $x -lt $w; $x++) {
                $a = $bmp.GetPixel($x, $y).A
                if ($a -lt $minAProbe) { $minAProbe = $a }
                if ($a -eq 0) { $hasTrueTransparency = $true; break }
            }
            if ($hasTrueTransparency) { break }
        }

        $needsEdgeBackgroundCleanup = $isCompactKeySprite -or (-not $isCompactKeySprite -and -not $hasTrueTransparency)
        if ($needsEdgeBackgroundCleanup) {
            if ($isCompactKeySprite) {
                Write-Host "[sprite] Key cleanup enabled."
            } else {
                Write-Host ("[sprite] Non-key cleanup enabled (no true transparency detected, minAlpha={0})." -f $minAProbe)
            }
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
            Write-Host ("[sprite] Edge cleanup border color estimate: R={0} G={1} B={2}" -f $bgR, $bgG, $bgB)

            # Flood-fill transparent from edges based on border color distance
            $tol = if ($isCompactKeySprite) { 42 } else { 52 }
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

            if ($isCompactKeySprite -and $minA -gt 0 -and $maxA -gt $minA) {
                $den = $maxA - $minA
                Write-Host "[sprite] Alpha normalization begin..."
                for ($y = 0; $y -lt $h; $y++) {
                    for ($x = 0; $x -lt $w; $x++) {
                        $c = $bmp.GetPixel($x, $y)
                        $na = [int](($c.A - $minA) * 255 / $den)
                        if ($na -lt 8) { $na = 0 }
                        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($na, $c.R, $c.G, $c.B))
                    }
                }
                Write-Host "[sprite] Alpha normalization done."
            } elseif (-not $isCompactKeySprite) {
                Write-Host "[sprite] Non-key alpha finalize: making remaining foreground pixels fully opaque."
                for ($y = 0; $y -lt $h; $y++) {
                    for ($x = 0; $x -lt $w; $x++) {
                        $c = $bmp.GetPixel($x, $y)
                        if ($c.A -gt 0 -and $c.A -lt 255) {
                            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B))
                        }
                    }
                }
            } else {
                Write-Host "[sprite] Alpha normalization skipped."
            }
        } else {
            Write-Host "[sprite] Existing transparency detected; preserving source alpha."
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
        if ($WriteGrabOffsets) {
            $maxOpaqueY = -1
            $minOpaqueX = $w
            $maxOpaqueX = -1
            for ($yy = 0; $yy -lt $h; $yy++) {
                for ($xx = 0; $xx -lt $w; $xx++) {
                    $a = $bmp.GetPixel($xx, $yy).A
                    if ($a -gt 0) {
                        if ($xx -lt $minOpaqueX) { $minOpaqueX = $xx }
                        if ($xx -gt $maxOpaqueX) { $maxOpaqueX = $xx }
                        if ($yy -gt $maxOpaqueY) { $maxOpaqueY = $yy }
                    }
                }
            }
            $grabX = if ($maxOpaqueX -ge $minOpaqueX) { [int][Math]::Round(($minOpaqueX + $maxOpaqueX + 1) / 2.0) } else { [int][Math]::Round($w / 2.0) }
            $grabY = if ($maxOpaqueY -ge 0) { $maxOpaqueY + 1 } else { $h }
            Write-PngWithGrAb -PngPath $DestPath -GrabX $grabX -GrabY $grabY
            Write-Host ("[sprite] Wrote grAb offsets: x={0} y={1}" -f $grabX, $grabY)
        }
        Write-Host ("[sprite] DONE in {0} ms" -f $swTotal.ElapsedMilliseconds)
    } finally {
        if ($bmp -ne $null) { $bmp.Dispose() }
        elseif ($workBmp -ne $null) { $workBmp.Dispose() }
    }
} finally {
    if ($inBmp -ne $null) { $inBmp.Dispose() }
}


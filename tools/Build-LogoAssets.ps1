$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$assetRoot = Join-Path $PSScriptRoot '../src/ProjectHub.App/Assets'
$pngPath = Join-Path $assetRoot 'ProjectHub.png'
$icoPath = Join-Path $assetRoot 'ProjectHub.ico'
$source = [System.Drawing.Image]::FromFile($pngPath)

try {
    $sizes = @(256,128,64,48,32,16)
    $frames = [System.Collections.Generic.List[byte[]]]::new()
    foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size,$size,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($source,0,0,$size,$size)
            $memory = [System.IO.MemoryStream]::new()
            try {
                $bitmap.Save($memory,[System.Drawing.Imaging.ImageFormat]::Png)
                $frames.Add($memory.ToArray())
            } finally { $memory.Dispose() }
        } finally { $graphics.Dispose(); $bitmap.Dispose() }
    }

    $stream = [System.IO.File]::Create($icoPath)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i=0; $i -lt $sizes.Count; $i++) {
            $size = $sizes[$i]; $frame = $frames[$i]; $dimension = if ($size -eq 256) { 0 } else { $size }
            $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Length); $writer.Write([uint32]$offset)
            $offset += $frame.Length
        }
        foreach ($frame in $frames) { $writer.Write($frame) }
        $writer.Flush()
    } finally { $writer.Dispose(); $stream.Dispose() }
} finally { $source.Dispose() }

Get-Item $pngPath,$icoPath | Select-Object FullName,Length

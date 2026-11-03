param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$sourcePath = (Resolve-Path -LiteralPath $InputPath).Path
$targetPath = [System.IO.Path]::GetFullPath($OutputPath)
if ($sourcePath -eq $targetPath -or [System.IO.Path]::GetExtension($targetPath) -ne '.ico') {
    throw 'Choose a separate .ico output file.'
}

$source = [System.Drawing.Image]::FromFile($sourcePath)
try {
    $frames = foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $png = [System.IO.MemoryStream]::new()
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $scale = [Math]::Min($size / $source.Width, $size / $source.Height)
            $width = [int][Math]::Round($source.Width * $scale)
            $height = [int][Math]::Round($source.Height * $scale)
            $rectangle = [System.Drawing.Rectangle]::new([int](($size - $width) / 2), [int](($size - $height) / 2), $width, $height)
            $graphics.DrawImage($source, $rectangle)
            $bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
            [pscustomobject]@{ Size = $size; Data = $png.ToArray() }
        }
        finally {
            $png.Dispose()
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }

    [void][System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($targetPath))
    $mode = [System.IO.FileMode]::CreateNew
    if ($Force) { $mode = [System.IO.FileMode]::Create }
    $output = [System.IO.File]::Open($targetPath, $mode, [System.IO.FileAccess]::Write)
    $writer = [System.IO.BinaryWriter]::new($output)
    try {
        # ICO header and directory; each frame contains a transparent PNG.
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$frames.Count)
        $offset = 6 + 16 * $frames.Count
        foreach ($frame in $frames) {
            $dimension = [byte]0
            if ($frame.Size -lt 256) { $dimension = [byte]$frame.Size }
            $writer.Write($dimension)
            $writer.Write($dimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$frame.Data.Length)
            $writer.Write([uint32]$offset)
            $offset += $frame.Data.Length
        }
        foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Data) }
    }
    finally { $writer.Dispose() }
}
finally { $source.Dispose() }

Write-Output "Created icon: $targetPath"

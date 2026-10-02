$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$target = Join-Path (Split-Path -Parent $PSScriptRoot) 'examples/minimal-page'
$image = New-Object System.Drawing.Bitmap 800,600
$g = [System.Drawing.Graphics]::FromImage($image)
try {
    $g.Clear([System.Drawing.Color]::WhiteSmoke)
    $g.FillEllipse([System.Drawing.Brushes]::LightGray,120,100,300,300)
    $g.FillRectangle([System.Drawing.Brushes]::DarkGray,440,250,220,180)
    $image.Save((Join-Path $target 'art.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally { $g.Dispose(); $image.Dispose() }
$bubble = New-Object System.Drawing.Bitmap 300,180
$g = [System.Drawing.Graphics]::FromImage($bubble)
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::Black),3
try {
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.FillEllipse([System.Drawing.Brushes]::White,5,5,270,165)
    $g.DrawEllipse($pen,5,5,270,165)
    $bubble.Save((Join-Path $target 'balloon.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally { $pen.Dispose(); $g.Dispose(); $bubble.Dispose() }

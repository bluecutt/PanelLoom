param([string]$OutputDirectory, [switch]$ValidateOnly)
$ErrorActionPreference = 'Stop'
$nativeRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $nativeRoot 'artifacts'))
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $artifactsRoot 'private-fixtures' }
$captureRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $captureRoot.StartsWith($artifactsRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'BASELINE_OUTPUT_SCOPE: output must be inside native artifacts.' }
if ($ValidateOnly) { exit 0 }
$configPath = Join-Path $artifactsRoot 'baseline-sources.local.json'
$config = [IO.File]::ReadAllText($configPath) | ConvertFrom-Json
$protected = @($config.editors) + @($config.projects | ForEach-Object { $_.path })
$before = @{}; foreach ($file in $protected) { $before[$file] = (Get-FileHash -LiteralPath $file).Hash }
[void][IO.Directory]::CreateDirectory($captureRoot)
$editor = $config.editors[1]
$source = [IO.File]::ReadAllText($editor)
$gate = $source.IndexOf('if ([string]::IsNullOrWhiteSpace($Project))')
if ($gate -lt 0) { throw 'Legacy headless loader gate not found.' }
$core = (Join-Path (Split-Path -Parent $editor) 'Single-Line-Core.ps1').Replace("'", "''")
$prefix = $source.Substring(0, $gate).Replace(". (Join-Path `$PSScriptRoot 'Single-Line-Core.ps1')", ". '$core'")
. ([scriptblock]::Create($prefix))
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System.Drawing;
public static class LegacyDrawProbe {
 public static void Draw(Graphics g, Image image, RectangleF dest) { g.DrawImage(image,dest,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel); }
}
'@
$results = @()
try {
    foreach ($fixture in $config.projects) {
        $raw = [IO.File]::ReadAllText($fixture.path) | ConvertFrom-Json
        $base = $raw.assetBase
        if (-not [IO.Path]::IsPathRooted($base)) { $base = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $fixture.path) $base)) }
        $raw.assetBase = $base
        if ($fixture.borderMode) { $raw.border.mode = $fixture.borderMode }
        $copy = Join-Path $captureRoot ($fixture.name + '.json')
        [IO.File]::WriteAllText($copy, ($raw | ConvertTo-Json -Depth 100), [Text.UTF8Encoding]::new($false))
        Read-Project -Path $copy
        $geometry = @()
        foreach ($panel in (Get-SortedPanels)) {
            $image = Get-ImageForPanel $panel
            $rect = Get-TransformedImageRect -Panel $panel -Image $image
            $bits=$image.LockBits([Drawing.Rectangle]::new(0,0,$image.Width,$image.Height),[Drawing.Imaging.ImageLockMode]::ReadOnly,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try { $pixelBytes=New-Object byte[] ([math]::Abs($bits.Stride)*$image.Height); [Runtime.InteropServices.Marshal]::Copy($bits.Scan0,$pixelBytes,0,$pixelBytes.Length); $sha=[Security.Cryptography.SHA256]::Create(); try {$pixelHash=[BitConverter]::ToString($sha.ComputeHash($pixelBytes)).Replace('-','')} finally {$sha.Dispose()} }
            finally {$image.UnlockBits($bits)}
            $geometry += [pscustomobject]@{ id=$panel.Id; z=$panel.ZIndex; readingOrder=$panel.ReadingOrder; points=(Convert-PointsForJson $panel.Polygon); rect=@([double]$rect.X,[double]$rect.Y,[double]$rect.Width,[double]$rect.Height); pixelHash=$pixelHash; pixelFormat=[string]$image.PixelFormat; flags=$image.Flags; dpi=$image.HorizontalResolution }
            if (($fixture.name -eq 'P05Legacy' -and $panel.Id -eq 'U01') -or ($fixture.name -eq 'P09Legacy' -and $panel.Id -eq 'A01')) {
                $isolated=[Drawing.Bitmap]::new($script:PageWidth*$script:ExportScale,$script:PageHeight*$script:ExportScale,[Drawing.Imaging.PixelFormat]::Format24bppRgb)
                $g=[Drawing.Graphics]::FromImage($isolated)
                try { $g.Clear([Drawing.Color]::White); $g.CompositingMode='SourceOver'; $g.CompositingQuality='HighQuality'; $g.InterpolationMode='HighQualityBicubic'; $g.SmoothingMode='AntiAlias'; $g.PixelOffsetMode='HighQuality'; Draw-PanelImage $g $panel $script:ExportScale; $isolated.Save((Join-Path $captureRoot ($fixture.name + '-' + $panel.Id + '-isolated.png'))) }
                finally {$g.Dispose();$isolated.Dispose()}
                $probe=[Drawing.Bitmap]::new($script:PageWidth*$script:ExportScale,$script:PageHeight*$script:ExportScale,[Drawing.Imaging.PixelFormat]::Format24bppRgb);$pg=[Drawing.Graphics]::FromImage($probe);$pp=[Drawing.Drawing2D.GraphicsPath]::new()
                try { $pg.Clear([Drawing.Color]::White);$pg.CompositingMode='SourceOver';$pg.CompositingQuality='HighQuality';$pg.InterpolationMode='HighQualityBicubic';$pg.SmoothingMode='AntiAlias';$pg.PixelOffsetMode='HighQuality';$pp.AddPolygon((Get-PointArray $panel.Polygon $script:ExportScale));$pg.SetClip($pp);if($panel.ClearFrame){$pg.FillPath([Drawing.Brushes]::White,$pp)}; $dest=New-RectF ($rect.X*$script:ExportScale) ($rect.Y*$script:ExportScale) ($rect.Width*$script:ExportScale) ($rect.Height*$script:ExportScale);[LegacyDrawProbe]::Draw($pg,$image,$dest);$probe.Save((Join-Path $captureRoot ($fixture.name+'-'+$panel.Id+'-csharp-framework.png'))) }
                finally {$pp.Dispose();$pg.Dispose();$probe.Dispose()}
            }
        }
        [IO.File]::WriteAllText((Join-Path $captureRoot ($fixture.name + '.geometry.json')), ($geometry | ConvertTo-Json -Depth 100), [Text.UTF8Encoding]::new($false))
        if ($script:Panels.Count -ne $fixture.panels -or $script:Balloons.Count -ne $fixture.balloons) { throw "Fixture count mismatch: $($fixture.name)" }
        $output = Join-Path $captureRoot ($fixture.name + '.png')
        $bitmap = Render-Page -RenderScale $script:ExportScale
        try {
            $bitmap.Save($output, [Drawing.Imaging.ImageFormat]::Png)
            $results += [pscustomobject]@{ name=$fixture.name; source=$fixture.path; sourceHash=$before[$fixture.path]; copy=$copy; copyHash=(Get-FileHash $copy).Hash; output=$output; outputHash=(Get-FileHash $output).Hash; width=$bitmap.Width; height=$bitmap.Height; panels=$script:Panels.Count; balloons=$script:Balloons.Count; scale=$script:ExportScale; borderMode=$script:BorderMode }
        } finally { $bitmap.Dispose() }
    }
} finally { Dispose-Images }
foreach ($file in $protected) { if ((Get-FileHash -LiteralPath $file).Hash -ne $before[$file]) { throw "Protected original changed: $file" } }
$manifest = [pscustomobject]@{ capturedUtc=[DateTime]::UtcNow.ToString('o'); editor=$editor; editorHash=$before[$editor]; protectedHashes=$before; fixtures=$results }
[IO.File]::WriteAllText((Join-Path $captureRoot 'manifest.json'), ($manifest | ConvertTo-Json -Depth 100), [Text.UTF8Encoding]::new($false))
$results | Select-Object name,panels,balloons,scale,borderMode,width,height | Format-Table

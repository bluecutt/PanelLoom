param([Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($Directory).TrimEnd('\')+'\'
if(-not(Test-Path -LiteralPath $root -PathType Container)){throw 'Package directory missing'}
$failures=New-Object Collections.Generic.List[string]
$files=@(Get-ChildItem -LiteralPath $root -Recurse -File)
foreach($file in $files) {
 $relative=$file.FullName.Substring($root.Length)
 if($relative -match '(?i)(^|[\\/])(Data|artifacts|private-fixtures|\.tools|\.superpowers|\.git|manga_context_assets|参考图)([\\/]|$)' -or $file.Name -match '(?i)\.pdb$|\.log$|\.recovery\.json$|local\.settings|baseline-sources|\.user$|^\.env'){$failures.Add('Private path: '+$relative)}
 if($file.Extension -in @('.md','.html','.svg','.json','.yaml','.yml','.ps1','.cmd','.csv','.cs','.xaml','.props','.csproj','.config','.txt')) {
  $content=[IO.File]::ReadAllText($file.FullName)
  if($content -match '(?i)[A-Z]:[\\/]+Users[\\/]+' -or $content -match '(?i)[A-Z]:[\\/]+comic_create' -or $content -match '(?i)data:image/[^;]+;base64,' -or $content -match '(?i)(sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{20,})'){$failures.Add('Private content: '+$relative)}
 }
}
if($failures.Count){throw ($failures -join "`n")}
Write-Output ('PUBLIC_SCAN_PASS '+$files.Count+' files')

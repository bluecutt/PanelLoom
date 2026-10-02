param([switch]$IncludeSourceExport)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scratch=Join-Path $root ('artifacts/release-contents-tests/'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($scratch)
$helper=Join-Path $root 'scripts/Copy-PublicRootDocs.ps1'
$terms=@('ASSET-TERMS.md','ASSET-TERMS.en.md','ASSET-TERMS.html','ASSET-TERMS.en.html','LICENSE-SCOPE.md','LICENSE-SCOPE.en.md','LICENSE-SCOPE.html','LICENSE-SCOPE.en.html')
$failures=New-Object Collections.Generic.List[string]
$script:passed=0
function Assert($condition,$message){if(-not $condition){throw $message}}
function Check([string]$name,[scriptblock]$action){try{& $action;$script:passed++;Write-Output ('PASS '+$name)}catch{$failures.Add($name+': '+$_.Exception.Message);Write-Output ('FAIL '+$name+': '+$_.Exception.Message)}}
function Fixture($path,$content){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,$content,(New-Object Text.UTF8Encoding $false))}
function SameFiles($source,$destination){
 $source=[IO.Path]::GetFullPath($source).TrimEnd('\')
 $files=@(Get-ChildItem -LiteralPath $source -Recurse -File)
 Assert (@(Get-ChildItem -LiteralPath $destination -Recurse -File).Count -eq $files.Count) 'Case file count changed'
 foreach($file in $files){$relative=$file.FullName.Substring($source.Length+1);$copy=Join-Path $destination $relative;Assert (Test-Path -LiteralPath $copy -PathType Leaf) ('Missing '+$relative);Assert ((Get-FileHash -LiteralPath $file.FullName).Hash -eq (Get-FileHash -LiteralPath $copy).Hash) ('Changed '+$relative)}
}
$fixture=Join-Path $scratch 'fixture'
foreach($name in @('README.md','LICENSE','CREDITS.md','CONTRIBUTING.md')+$terms){Fixture (Join-Path $fixture $name) ('Public fixture '+$name)}
Fixture (Join-Path $fixture 'case-studies/P10/guide/full-workflow.en.html') '<p>Offline tutorial fixture</p>'
Fixture (Join-Path $fixture 'case-studies/ASSET-TERMS.md') 'Local layout practice fixture'
Fixture (Join-Path $fixture 'case-studies/ASSET-TERMS.en.md') 'Local layout practice fixture'
Fixture (Join-Path $fixture 'case-studies/asset-manifest.json') '{"schemaVersion":1}'
Fixture (Join-Path $fixture 'case-studies/P10/project.json') '{"version":2}'
$png=Join-Path $fixture 'case-studies/P10/guide/image.png'
[IO.File]::WriteAllBytes($png,[byte[]]@(137,80,78,71,13,10,26,10))
$destination=Join-Path $scratch 'copy'
& $helper -SourceDirectory $fixture -DestinationDirectory $destination
Check 'RootAssetTermsAndLicenseScope' {foreach($name in $terms){$copy=Join-Path $destination $name;Assert (Test-Path -LiteralPath $copy -PathType Leaf) ('Missing '+$name);Assert ((Get-FileHash -LiteralPath $copy).Hash -eq (Get-FileHash -LiteralPath (Join-Path $fixture $name)).Hash) ('Changed '+$name)}}
Check 'CaseTreeIncludesImagesProjectsAndHtml' {Assert (Test-Path -LiteralPath (Join-Path $destination 'case-studies')) 'Case directory missing';SameFiles (Join-Path $fixture 'case-studies') (Join-Path $destination 'case-studies')}
Check 'NeutralOnlySourceRemainsSupported' {
 $neutral=Join-Path $scratch 'neutral';foreach($name in @('README.md','LICENSE','CREDITS.md','CONTRIBUTING.md')){Fixture (Join-Path $neutral $name) 'Neutral fixture'}
 $output=Join-Path $scratch 'neutral-copy';& $helper -SourceDirectory $neutral -DestinationDirectory $output
 Assert (@(Get-ChildItem -LiteralPath $output -Recurse -File).Count -eq 4) 'Neutral documents changed'
}
Check 'CaseWithoutRootTermsRejected' {
 $unsafe=Join-Path $scratch 'incomplete';foreach($name in @('README.md','LICENSE','CREDITS.md','CONTRIBUTING.md')){Fixture (Join-Path $unsafe $name) 'Public fixture'}
 Fixture (Join-Path $unsafe 'case-studies/README.md') 'Case fixture'
 $rejected=$false;try{& $helper -SourceDirectory $unsafe -DestinationDirectory (Join-Path $scratch 'unsafe-copy')}catch{$rejected=$true}
 Assert $rejected 'Case was copied without its license scope and asset terms'
}
if($IncludeSourceExport){Check 'ActualSourceExportIncludesCompleteCasesAndTerms' {
 $version='0.0.0-contents.'+[Guid]::NewGuid().ToString('N')
 & (Join-Path $root 'scripts/Export-Source.ps1') -Version $version
 $export=Join-Path $root ('dist/ComicEditor-source-'+$version)
 foreach($name in $terms){Assert (Test-Path -LiteralPath (Join-Path $export $name) -PathType Leaf) ('Source export missing '+$name)}
 SameFiles (Join-Path $root 'case-studies') (Join-Path $export 'case-studies')
}}
Write-Output ('RELEASE_CONTENTS_TESTS '+$script:passed+' passed; '+$failures.Count+' failed')
if($failures.Count){throw ($failures -join "`n")}

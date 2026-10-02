param([string]$SdkPath,[switch]$IncludeSourceExport)
$ErrorActionPreference='Stop'
$softwareRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$testRoot=Join-Path $softwareRoot ('artifacts/packaging-tests/'+[Guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$passed=0
function Assert($condition,$message){if(-not $condition){throw $message}}
function WriteFixture($path,$text){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,$text,(New-Object Text.UTF8Encoding $false))}
function MustReject([scriptblock]$action){$rejected=$false;try{& $action}catch{$rejected=$true};Assert $rejected 'Expected rejection'}
$publish=Join-Path $testRoot 'publish';$cache=Join-Path $testRoot 'cache';$out=Join-Path $testRoot 'notices'
WriteFixture (Join-Path $publish 'ComicEditor.runtimeconfig.json') '{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.12"},{"name":"Microsoft.WindowsDesktop.App","version":"10.0.12"}]}}'
WriteFixture (Join-Path $cache 'microsoft.netcore.app.runtime.win-x64/10.0.12/LICENSE.TXT') 'core MIT fixture'
WriteFixture (Join-Path $cache 'microsoft.netcore.app.runtime.win-x64/10.0.12/THIRD-PARTY-NOTICES.TXT') 'core notices fixture'
WriteFixture (Join-Path $cache 'microsoft.windowsdesktop.app.runtime.win-x64/10.0.12/LICENSE') 'desktop MIT fixture'
$helper=Join-Path $softwareRoot 'scripts/Copy-RuntimeNotices.ps1'
Assert (Test-Path -LiteralPath $helper) 'Runtime notices helper missing'
& $helper -PublishDirectory $publish -CacheDirectory $cache -Destination $out
Assert ((Get-Content -LiteralPath (Join-Path $out 'LICENSE.dotnet.txt') -Raw).Trim() -eq 'core MIT fixture') 'Runtime license must come from actual included runtime'
Assert ((Get-Content -LiteralPath (Join-Path $out 'licenses/Microsoft.WindowsDesktop.App/10.0.12/LICENSE') -Raw).Trim() -eq 'desktop MIT fixture') 'Desktop license missing'
$passed++
MustReject {& $helper -PublishDirectory $publish -CacheDirectory (Join-Path $testRoot 'missing-cache') -Destination (Join-Path $testRoot 'missing-out')};$passed++
$unknown=Join-Path $testRoot 'unknown';WriteFixture (Join-Path $unknown 'unknown.runtimeconfig.json') '{"runtimeOptions":{"includedFrameworks":[{"name":"Unknown.Framework","version":"10.0.12"}]}}'
MustReject {& $helper -PublishDirectory $unknown -CacheDirectory $cache -Destination (Join-Path $testRoot 'unknown-out')};$passed++
MustReject {& $helper -PublishDirectory (Join-Path $testRoot 'missing-publish') -CacheDirectory $cache -Destination (Join-Path $testRoot 'empty-out')};$passed++
$htmlFixture=Join-Path $testRoot 'unsafe-html'
WriteFixture (Join-Path $htmlFixture 'guide.html') ('<p>'+[string]::Join('\',@('D:','Users','private-author','secret'))+'</p>')
MustReject {& (Join-Path $softwareRoot 'scripts/Scan-Public-Package.ps1') -Directory $htmlFixture};$passed++
if($SdkPath){
 $previous=$env:COMIC_EDITOR_DOTNET_PATH
 try{$env:COMIC_EDITOR_DOTNET_PATH=$SdkPath;$resolved=& (Join-Path $softwareRoot 'scripts/Resolve-Dotnet.ps1');Assert ([IO.Path]::GetFullPath($resolved) -eq [IO.Path]::GetFullPath($SdkPath)) 'Explicit existing SDK was not selected';$passed++}finally{$env:COMIC_EDITOR_DOTNET_PATH=$previous}
}
if($IncludeSourceExport){
 $version='0.0.0-test.'+[Guid]::NewGuid().ToString('N')
 & (Join-Path $softwareRoot 'scripts/Export-Source.ps1') -Version $version
 $sourceRoot=Join-Path $softwareRoot ('dist/ComicEditor-source-'+$version)
 foreach($name in @('LICENSE','CREDITS.md','CONTRIBUTING.md','CONTRIBUTING.en.md','CONTRIBUTING.en.html','README.zh-CN.md','README.html','README.zh-CN.html','docs/quick-start.en.html','docs/new-windows-trial.en.html','skills/comic-page-workflow/assets/example-page/art.png','skills/comic-page-workflow/assets/example-page/balloon.png','docs/images/neutral/assembled-example.png')){Assert (Test-Path -LiteralPath (Join-Path $sourceRoot $name)) ('Source export missing '+$name)}
 Assert ((Get-FileHash -LiteralPath (Join-Path $sourceRoot 'LICENSE')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $softwareRoot 'LICENSE')).Hash) 'License bytes changed in source export'
 $discussion='docs/2026-10-01-'+[string]::Concat([char]0x4EA4,[char]0x4E92,[char]0x95EE,[char]0x9898,[char]0x6838,[char]0x9A8C,[char]0x4E0E,[char]0x4F18,[char]0x5316,[char]0x8BA8,[char]0x8BBA)+'.md'
 $privateDocs=@($discussion,'docs/2026-10-01-usability-upgrade-progress.md',($discussion -replace '\.md$','.html'),'docs/2026-10-01-usability-upgrade-progress.html')
 foreach($privateDoc in $privateDocs){Assert (-not(Test-Path -LiteralPath (Join-Path $sourceRoot $privateDoc))) ('Private development record entered source export: '+$privateDoc)}
 $passed++
 $rootDocs=Join-Path $testRoot 'app-root-docs'
 & (Join-Path $softwareRoot 'scripts/Copy-PublicRootDocs.ps1') -SourceDirectory $softwareRoot -DestinationDirectory $rootDocs
 foreach($name in @('README.md','README.html','README.zh-CN.md','README.zh-CN.html','LICENSE','CREDITS.md','CONTRIBUTING.md','CONTRIBUTING.en.md','CONTRIBUTING.en.html')){Assert (Test-Path -LiteralPath (Join-Path $rootDocs $name)) ('Application root document missing '+$name)}
 Assert (-not(Test-Path -LiteralPath (Join-Path $rootDocs 'ComicEditor.sln'))) 'Root document helper copied unrelated source'
 $passed++
}
Write-Output ('PACKAGING_TEST_PASS '+$passed+' checks')

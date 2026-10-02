param([Parameter(Mandatory=$true)][string]$PublishDirectory,[Parameter(Mandatory=$true)][string]$CacheDirectory,[Parameter(Mandatory=$true)][string]$Destination,[string]$SupplementalNotices)
$ErrorActionPreference='Stop'
$known=@{'Microsoft.NETCore.App'=@('microsoft.netcore.app.runtime.win-x64','LICENSE.TXT');'Microsoft.WindowsDesktop.App'=@('microsoft.windowsdesktop.app.runtime.win-x64','LICENSE')}
$configs=@(Get-ChildItem -LiteralPath $PublishDirectory -Filter '*.runtimeconfig.json' -File)
if(-not $configs.Count){throw 'No published runtime configuration'}
$frameworks=@($configs | ForEach-Object {(Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json).runtimeOptions.includedFrameworks} | Sort-Object name,version -Unique)
if(-not $frameworks.Count){throw 'Self-contained includedFrameworks missing'}
$copies=New-Object Collections.Generic.List[object]
foreach($framework in $frameworks){
 if(-not $known.ContainsKey($framework.name) -or $framework.version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?$'){throw 'Unreviewed runtime framework'}
 $package=$known[$framework.name][0];$license=$known[$framework.name][1]
 $packageDirectory=Join-Path $CacheDirectory ($package+'/'+$framework.version)
 $names=@($license);if($framework.name -eq 'Microsoft.NETCore.App'){$names+='THIRD-PARTY-NOTICES.TXT'}
 foreach($name in $names){
  $source=Join-Path $packageDirectory $name;if(-not(Test-Path -LiteralPath $source -PathType Leaf)){throw ('Runtime notice missing: '+$package+'/'+$framework.version+'/'+$name)}
  $relative='licenses/'+$framework.name+'/'+$framework.version+'/'+$name
  $copies.Add([ordered]@{source=$source;path=$relative;framework=$framework.name;version=$framework.version;package=$package})
 }
}
$core=@($copies | Where-Object {$_.framework -eq 'Microsoft.NETCore.App'})
if(-not $core.Count){throw 'Core runtime notices missing'}
if(@($core | ForEach-Object {$_.version} | Sort-Object -Unique).Count -ne 1){throw 'Conflicting core runtime versions'}
$coreLicense=$core | Where-Object {$_.path.EndsWith('/LICENSE.TXT')}
$coreNotice=$core | Where-Object {$_.path.EndsWith('/THIRD-PARTY-NOTICES.TXT')}
$copies.Add([ordered]@{source=$coreLicense.source;path='LICENSE.dotnet.txt';framework='Microsoft.NETCore.App';version=$coreLicense.version;package=$coreLicense.package})
$copies.Add([ordered]@{source=$coreNotice.source;path='ThirdPartyNotices.dotnet.txt';framework='Microsoft.NETCore.App';version=$coreNotice.version;package=$coreNotice.package})
if($SupplementalNotices){if(-not(Test-Path -LiteralPath $SupplementalNotices -PathType Leaf)){throw 'Supplemental notices missing'};$copies.Add([ordered]@{source=$SupplementalNotices;path='ThirdPartyNotices.sdk-supplement.txt';framework='supplemental-notices';version='';package='build-sdk-notices'})}
foreach($entry in $copies){if(Test-Path -LiteralPath (Join-Path $Destination $entry.path)){throw ('Notice destination exists: '+$entry.path)}}
if(Test-Path -LiteralPath (Join-Path $Destination 'runtime-notices-manifest.json')){throw 'Notice manifest exists'}
$entries=@(foreach($entry in $copies){$target=Join-Path $Destination $entry.path;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $target));Copy-Item -LiteralPath $entry.source -Destination $target;[ordered]@{path=$entry.path;framework=$entry.framework;version=$entry.version;package=$entry.package;sha256=(Get-FileHash -LiteralPath $target).Hash}})
[IO.File]::WriteAllText((Join-Path $Destination 'runtime-notices-manifest.json'),([ordered]@{schemaVersion=1;files=$entries}|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding $false))
Write-Output ('RUNTIME_NOTICES_PASS '+$entries.Count+' files')

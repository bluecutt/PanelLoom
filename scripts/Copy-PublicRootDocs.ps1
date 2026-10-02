param([Parameter(Mandatory=$true)][string]$SourceDirectory,[Parameter(Mandatory=$true)][string]$DestinationDirectory)
$ErrorActionPreference='Stop'
$terms=@('ASSET-TERMS.md','ASSET-TERMS.en.md','ASSET-TERMS.html','ASSET-TERMS.en.html','LICENSE-SCOPE.md','LICENSE-SCOPE.en.md','LICENSE-SCOPE.html','LICENSE-SCOPE.en.html')
$names=@('README.md','README.html','README.zh-CN.md','README.zh-CN.html','LICENSE','CREDITS.md','CREDITS.html','CONTRIBUTING.md','CONTRIBUTING.html','CONTRIBUTING.en.md','CONTRIBUTING.en.html')+$terms
foreach($required in @('README.md','LICENSE','CREDITS.md','CONTRIBUTING.md')){if(-not(Test-Path -LiteralPath (Join-Path $SourceDirectory $required) -PathType Leaf)){throw ('Required root document missing: '+$required)}}
$cases=Join-Path $SourceDirectory 'case-studies'
if(Test-Path -LiteralPath $cases -PathType Container){
 foreach($required in $terms+@('case-studies/ASSET-TERMS.md','case-studies/ASSET-TERMS.en.md','case-studies/asset-manifest.json')){
  if(-not(Test-Path -LiteralPath (Join-Path $SourceDirectory $required) -PathType Leaf)){throw ('Required case license or manifest missing: '+$required)}
 }
}
[void][IO.Directory]::CreateDirectory($DestinationDirectory)
foreach($name in $names){$source=Join-Path $SourceDirectory $name;if(Test-Path -LiteralPath $source -PathType Leaf){Copy-Item -LiteralPath $source -Destination (Join-Path $DestinationDirectory $name)}}
if(Test-Path -LiteralPath $cases -PathType Container){
 $caseRoot=[IO.Path]::GetFullPath($cases).TrimEnd('\')
 foreach($file in Get-ChildItem -LiteralPath $caseRoot -Recurse -File){
  $relative=$file.FullName.Substring($caseRoot.Length+1)
  $destination=Join-Path (Join-Path $DestinationDirectory 'case-studies') $relative
  [void][IO.Directory]::CreateDirectory((Split-Path -Parent $destination))
  Copy-Item -LiteralPath $file.FullName -Destination $destination
 }
}

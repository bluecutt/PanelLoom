param([Parameter(Mandatory=$true)][string]$First,[Parameter(Mandatory=$true)][string]$Second,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$target=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $target){throw 'Merge destination must be new'}
$entries=@{}
foreach($source in @($First,$Second)) {
 $root=[IO.Path]::GetFullPath($source).TrimEnd('\')+'\'
 if(-not (Test-Path -LiteralPath $root -PathType Container)){throw 'Publish source missing'}
 foreach($file in Get-ChildItem -LiteralPath $root -Recurse -File) {
  if($file.Extension -eq '.pdb'){continue}
  $relative=$file.FullName.Substring($root.Length)
  if($entries.ContainsKey($relative)) {
   if((Get-FileHash -LiteralPath $entries[$relative]).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){throw ('Shared publish file conflict: '+$relative)}
  }else{$entries[$relative]=$file.FullName}
 }
}
# Complete preflight before creating a destination; never last-writer-wins.
New-Item -ItemType Directory -Path $target | Out-Null
foreach($relative in $entries.Keys) {
 $output=Join-Path $target $relative
 [void][IO.Directory]::CreateDirectory((Split-Path -Parent $output))
 Copy-Item -LiteralPath $entries[$relative] -Destination $output
}


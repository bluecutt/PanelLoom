param([Parameter(Mandatory=$true)][string]$PlanPath,[Parameter(Mandatory=$true)][string]$CliPath,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Case destination exists; choose a new directory'}
$plan=[IO.File]::ReadAllText([IO.Path]::GetFullPath($PlanPath))|ConvertFrom-Json
if($plan.schemaVersion -ne 1 -or -not @($plan.cases).Count){throw 'Invalid case plan'}
function InvokeCli([string[]]$command){$raw=& $CliPath @command;if($LASTEXITCODE -ne 0){throw ('CLI failed: '+($command -join ' '))};$r=($raw -join "`n")|ConvertFrom-Json;if(-not $r.success){throw ('CLI rejected: '+$r.code)};return $r.data}
function Inside($root,$relative){if([IO.Path]::IsPathRooted($relative) -or $relative -match ':|^[/\\]'){throw 'Relative path required'};$r=[IO.Path]::GetFullPath($root).TrimEnd('\')+'\';$p=[IO.Path]::GetFullPath((Join-Path $r $relative));if(-not $p.StartsWith($r,[StringComparison]::OrdinalIgnoreCase)){throw 'Path escapes case root'};return $p}
function SaveJson($path,$value){[void][IO.Directory]::CreateDirectory((Split-Path -Parent $path));[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 80),(New-Object Text.UTF8Encoding $false))}
function Fingerprint($path){return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
$allowed=@{}
foreach($entry in $plan.allowedFiles){$p=[IO.Path]::GetFullPath($entry.path);if(-not(Test-Path -LiteralPath $p -PathType Leaf) -or ((Get-Item -LiteralPath $p).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Allowed source missing or redirected'};if($entry.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or (Fingerprint $p) -ne $entry.sha256){throw 'Allowed source hash mismatch'};$allowed[$p]=$entry.sha256}
$inspected=@{}
$caseIds=@{}
foreach($case in $plan.cases){
 if($case.caseId -notmatch '^[A-Z0-9_-]+$' -or $caseIds.ContainsKey($case.caseId)){throw 'Invalid or duplicate case ID'};$caseIds[$case.caseId]=$true
 if($case.textStatus -notin @('lettered','blank')){throw 'Unknown text status'}
 $p=[IO.Path]::GetFullPath($case.projectPath)
 if(-not $allowed.ContainsKey($p) -or $allowed[$p] -ne $case.projectSha256){throw 'Project not approved'}
 $state=InvokeCli @('inspect','--project',$p)
 if($state.hash -ne $case.projectSha256 -or @($state.project.panels).Count -ne $case.panelCount -or @($state.project.balloons).Count -ne $case.balloonCount){throw 'Source project identity/count mismatch'}
 foreach($a in $state.assets){$source=[IO.Path]::GetFullPath($a.absolutePath);if($a.missing -or $a.error -or -not $allowed.ContainsKey($source) -or $allowed[$source] -ne $a.sha256){throw 'Unapproved or changed image'}}
 foreach($s in $case.supplements){$source=[IO.Path]::GetFullPath($s.sourcePath);if(-not $allowed.ContainsKey($source)){throw 'Unapproved supplement'};[void](Inside (Join-Path $out $case.caseId) $s.relativePath)}
 $inspected[$case.caseId]=$state
 if($case.referenceProjectPath){
  $refPath=[IO.Path]::GetFullPath($case.referenceProjectPath)
  if(-not $allowed.ContainsKey($refPath) -or $allowed[$refPath] -ne $case.referenceProjectSha256){throw 'Edited reference project not approved'}
  $ref=InvokeCli @('inspect','--project',$refPath)
  if($ref.hash -ne $case.referenceProjectSha256 -or @($ref.project.panels).Count -ne $case.panelCount -or @($ref.project.balloons).Count -ne $case.balloonCount -or @($ref.assets).Count -ne @($state.assets).Count){throw 'Edited reference identity/count mismatch'}
  foreach($a in $ref.assets){
   $source=[IO.Path]::GetFullPath($a.absolutePath)
   $matching=@($state.assets|Where-Object {$_.kind -eq $a.kind -and $_.objectId -eq $a.objectId})
   if($a.missing -or $a.error -or -not $allowed.ContainsKey($source) -or $allowed[$source] -ne $a.sha256 -or $matching.Count -ne 1 -or $matching[0].sha256 -ne $a.sha256){throw 'Edited reference must use the same approved object assets'}
  }
  $inspected[$case.caseId+'-reference']=$ref
 }
}
[void][IO.Directory]::CreateDirectory($out)
$work=Join-Path (Split-Path -Parent $PSScriptRoot) ('artifacts/case-packaging/'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($work)
Add-Type -AssemblyName System.Drawing
$manifestCases=New-Object Collections.Generic.List[object]
foreach($case in $plan.cases){
 $caseRoot=Join-Path $out $case.caseId;[void][IO.Directory]::CreateDirectory($caseRoot)
 foreach($folder in @('work','output')){[void][IO.Directory]::CreateDirectory((Join-Path $caseRoot $folder))}
 $bundlePath=Join-Path $work $case.caseId
 [void](InvokeCli @('bundle','--project',[IO.Path]::GetFullPath($case.projectPath),'--out-dir',$bundlePath))
 $bundle=InvokeCli @('inspect','--project',(Join-Path $bundlePath 'project.json'))
 $doc=$bundle.project
 $doc|Add-Member -NotePropertyName assetBase -NotePropertyValue '../assets' -Force
 $doc|Add-Member -NotePropertyName outputDirectory -NotePropertyValue '../output' -Force
 $doc|Add-Member -NotePropertyName pageId -NotePropertyValue $case.caseId -Force
 $doc|Add-Member -NotePropertyName projectName -NotePropertyValue ($case.caseId+' practice start') -Force
 $doc|Add-Member -NotePropertyName phase -NotePropertyValue 'practice-start' -Force
 $assets=New-Object Collections.Generic.List[object]
 foreach($a in $bundle.assets){
  if($a.objectId -notmatch '^[A-Za-z0-9_-]+$'){throw 'Unsafe object ID'}
  $sub=if($a.kind -eq 'panel'){'panels'}else{'text-objects'}
  $extension=[IO.Path]::GetExtension($a.absolutePath).ToLowerInvariant()
  $relative=$case.caseId+'/assets/'+$sub+'/'+$a.objectId+$extension
  $target=Inside $out $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $target));Copy-Item -LiteralPath $a.absolutePath -Destination $target
  if((Fingerprint $target) -ne $a.sha256){throw 'Copied image hash mismatch'}
  $list=if($a.kind -eq 'panel'){$doc.panels}else{$doc.balloons};$object=$list|Where-Object {$_.id -eq $a.objectId}
  $object.sourceImage=$sub+'/'+$a.objectId+$extension
  $object.label=$a.objectId
  $assets.Add([ordered]@{id=$a.objectId;path=$relative;sha256=$a.sha256;width=$a.width;height=$a.height;kind=$a.kind;role='approved-original';usageTerms='local-layout-only'})
 }
 $json=$doc|ConvertTo-Json -Depth 80
 if($json -match '(?i)[A-Z]:[\\/]|data:image/[^;]+;base64,|sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{20,}'){throw 'Private metadata remains; review source project'}
 $start=Join-Path $caseRoot 'projects/start.json';SaveJson $start $doc
 if($case.referenceProjectPath){
  $doc=$inspected[$case.caseId+'-reference'].project
  $doc|Add-Member -NotePropertyName assetBase -NotePropertyValue '../assets' -Force
  $doc|Add-Member -NotePropertyName outputDirectory -NotePropertyValue '../output' -Force
  $doc|Add-Member -NotePropertyName pageId -NotePropertyValue $case.caseId -Force
  foreach($a in $assets){$objects=if($a.kind -eq 'panel'){$doc.panels}else{$doc.balloons};$o=$objects|Where-Object {$_.id -eq $a.id};$o.sourceImage=$a.path.Substring(($case.caseId+'/assets/').Length);$o.label=$a.id}
 }
 $doc|Add-Member -NotePropertyName projectName -NotePropertyValue ($case.caseId+' practice reference') -Force
 $doc|Add-Member -NotePropertyName phase -NotePropertyValue 'practice-reference' -Force
 if($case.demoOverrides){if($case.demoOverrides.borderMode){$doc.border|Add-Member -NotePropertyName mode -NotePropertyValue $case.demoOverrides.borderMode -Force};if($case.demoOverrides.exportScale){$doc.canvas.exportScale=$case.demoOverrides.exportScale}}
 $reference=Join-Path $caseRoot 'projects/reference.json';SaveJson $reference $doc
 if(([IO.File]::ReadAllText($reference)) -match '(?i)[A-Z]:[\\/]+Users[\\/]|[A-Z]:[\\/]+comic_create|data:image/[^;]+;base64,'){throw 'Private edited reference metadata remains'}
 [void](InvokeCli @('validate','--project',$start));[void](InvokeCli @('validate','--project',$reference))
 (Get-Item -LiteralPath $start).IsReadOnly=$true
 (Get-Item -LiteralPath $reference).IsReadOnly=$true
 foreach($s in $case.supplements){
  $relative=$case.caseId+'/'+$s.relativePath;$target=Inside $out $relative;[void][IO.Directory]::CreateDirectory((Split-Path -Parent $target));Copy-Item -LiteralPath $s.sourcePath -Destination $target
  $bitmap=[Drawing.Bitmap]::FromFile($target);try{$w=$bitmap.Width;$h=$bitmap.Height}finally{$bitmap.Dispose()}
  if((Fingerprint $target) -ne $allowed[[IO.Path]::GetFullPath($s.sourcePath)]){throw 'Supplement hash mismatch'}
  $assets.Add([ordered]@{id=('reference-'+$assets.Count);path=$relative;sha256=(Fingerprint $target);width=$w;height=$h;kind='reference';role=$s.role;usageTerms='local-layout-only'})
 }
 $output=Join-Path $caseRoot 'reference-output/editor-reference.png';[void][IO.Directory]::CreateDirectory((Split-Path -Parent $output))
 $render=InvokeCli @('render','--project',$reference,'--out',$output)
 $quality=[ordered]@{width=$render.width;height=$render.height;warnings=@($render.warnings);role='editor-reference';note='Rendered from the supplied reference project; historical master stored separately.'}
 SaveJson (Join-Path $caseRoot 'reference-output/quality.json') $quality
 $projects=@(foreach($p in @($start,$reference)){[ordered]@{path=($case.caseId+'/projects/'+[IO.Path]::GetFileName($p));sha256=(Fingerprint $p);panelCount=$case.panelCount;balloonCount=$case.balloonCount}})
 $refs=@([ordered]@{path=($case.caseId+'/reference-output/editor-reference.png');sha256=(Fingerprint $output);width=$render.width;height=$render.height;role='editor-reference';usageTerms='local-layout-only'})
 $manifestCases.Add([ordered]@{caseId=$case.caseId;panelCount=$case.panelCount;balloonCount=$case.balloonCount;textStatus=$case.textStatus;assets=@($assets.ToArray());projects=$projects;references=$refs})
}
$manifest=[ordered]@{schemaVersion=1;product='PanelLoom';editorFormat='ComicPanelEditorProject';editorVersion=2;usageTerms='local-layout-only';cases=@($manifestCases.ToArray())}
SaveJson (Join-Path $out 'asset-manifest.json') $manifest
[IO.File]::WriteAllText((Join-Path $out 'ASSET-TERMS.md'),"# Case asset terms`n`nLocal non-destructive layout practice and local saving are permitted.`nRepainting, AI redrawing, derivative art and external publication or redistribution of originals, screenshots or practice outputs are prohibited.`nThese case assets are separate from the software license.`n",(New-Object Text.UTF8Encoding $false))
[IO.File]::WriteAllText((Join-Path $out 'README.md'),"# PanelLoom local case practice`n`nRead ASSET-TERMS.md before use. Copy projects/start.json to a working copy within the same projects directory. Open with ComicEditor.exe; compare with reference.json and editor-reference.png. Historical images are separate teaching references.`n",(New-Object Text.UTF8Encoding $false))
& (Join-Path $PSScriptRoot 'Test-CasePracticePackage.ps1') -Directory $out -CliPath $CliPath
Write-Output (Join-Path $out 'asset-manifest.json')

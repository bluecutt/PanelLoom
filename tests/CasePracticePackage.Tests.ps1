param([Parameter(Mandatory=$true)][string]$CliPath)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$run=Join-Path $root ('artifacts/case-tests/'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($run)
function Assert($ok,$message){if(-not $ok){throw $message}}
function SaveJson($path,$value){if(Test-Path -LiteralPath $path){(Get-Item -LiteralPath $path).IsReadOnly=$false};[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 60),(New-Object Text.UTF8Encoding $false))}
function MustReject([scriptblock]$action,$reason){$failed=$false;try{& $action}catch{$failed=$true};Assert $failed ('Expected rejection: '+$reason)}
$new=Join-Path $root 'scripts/New-CasePracticePackage.ps1'
$check=Join-Path $root 'scripts/Test-CasePracticePackage.ps1'
Assert (Test-Path -LiteralPath $new) 'Case package builder missing'
Assert (Test-Path -LiteralPath $check) 'Case package checker missing'
$original=Join-Path $run 'original';[void][IO.Directory]::CreateDirectory($original)
foreach($file in @('project.json','art.png','balloon.png')){Copy-Item -LiteralPath (Join-Path $root ('examples/minimal-page/'+$file)) -Destination (Join-Path $original $file)}
$project=Join-Path $original 'project.json'
$allow=@(Get-ChildItem -LiteralPath $original -File|ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$sourceHashes=@{};foreach($f in $allow){$sourceHashes[$f.path]=$f.sha256}
$plan=@{schemaVersion=1;allowedFiles=$allow;cases=@(@{caseId='TEST';projectPath=$project;projectSha256=$sourceHashes[$project];panelCount=2;balloonCount=1;textStatus='lettered';supplements=@(@{sourcePath=(Join-Path $original 'art.png');relativePath='reference-output/historical.png';role='historical-master'})})}
$planFile=Join-Path $run 'plan.json';SaveJson $planFile $plan
$package=Join-Path $run ([string]::Concat([char]0x4E2D,[char]0x6587,' practice'))
& $new -PlanPath $planFile -CliPath $CliPath -OutputDirectory $package
$receipt=@(& $check -Directory $package -CliPath $CliPath)
Assert ($receipt[-1] -match '^CASE_PACKAGE_PASS') 'Expected checker success'
Assert (Get-Item -LiteralPath (Join-Path $package 'TEST/projects/start.json')).IsReadOnly 'Start project should be read-only'
Assert (Get-Item -LiteralPath (Join-Path $package 'TEST/projects/reference.json')).IsReadOnly 'Reference project should be read-only'
$manifest=[IO.File]::ReadAllText((Join-Path $package 'asset-manifest.json'))|ConvertFrom-Json
Assert ($manifest.cases[0].assets.Count -eq 4) 'All original objects and historical image must be included'
Assert ($manifest.cases[0].projects.Count -eq 2) 'Start and reference projects required'
Assert ($manifest.cases[0].assets[0].usageTerms -eq 'local-layout-only') 'Per-asset terms missing'
Assert (-not([IO.File]::ReadAllText((Join-Path $package 'TEST/projects/reference.json'))).Contains($original)) 'Author path leaked'
$passed=1
MustReject {& $new -PlanPath $planFile -CliPath $CliPath -OutputDirectory $package} 'existing output';$passed++
$relocated=Join-Path $run 'relocated practice';Copy-Item -LiteralPath $package -Destination $relocated -Recurse
& $check -Directory $relocated -CliPath $CliPath;$passed++
$bad=Join-Path $run 'hash-bad';Copy-Item -LiteralPath $package -Destination $bad -Recurse
$asset=Join-Path $bad $manifest.cases[0].assets[0].path;[IO.File]::AppendAllText($asset,'changed')
MustReject {& $check -Directory $bad -CliPath $CliPath} 'changed bytes';$passed++
$bad=Join-Path $run 'missing-image';Copy-Item -LiteralPath $package -Destination $bad -Recurse
$p=Join-Path $bad 'TEST/projects/start.json';$doc=[IO.File]::ReadAllText($p)|ConvertFrom-Json;$doc.panels[0].sourceImage='panels/not-present.png';SaveJson $p $doc
MustReject {& $check -Directory $bad -CliPath $CliPath} 'missing image';$passed++
$bad=Join-Path $run 'absolute-ref';Copy-Item -LiteralPath $package -Destination $bad -Recurse
$p=Join-Path $bad 'TEST/projects/start.json';$doc=[IO.File]::ReadAllText($p)|ConvertFrom-Json;$doc.panels[0].sourceImage=Join-Path $original 'art.png';SaveJson $p $doc
MustReject {& $check -Directory $bad -CliPath $CliPath} 'absolute reference';$passed++
$bad=Join-Path $run 'wrong-count';Copy-Item -LiteralPath $package -Destination $bad -Recurse
$p=Join-Path $bad 'TEST/projects/start.json';$doc=[IO.File]::ReadAllText($p)|ConvertFrom-Json;$doc.panels=@($doc.panels[0]);SaveJson $p $doc
MustReject {& $check -Directory $bad -CliPath $CliPath} 'wrong object count';$passed++
$badPlan=$plan|ConvertTo-Json -Depth 50|ConvertFrom-Json;$badPlan.allowedFiles=@($badPlan.allowedFiles|Where-Object {$_.path -ne (Join-Path $original 'balloon.png')});$badFile=Join-Path $run 'unapproved-plan.json';SaveJson $badFile $badPlan
MustReject {& $new -PlanPath $badFile -CliPath $CliPath -OutputDirectory (Join-Path $run 'unapproved-output')} 'unapproved image';$passed++
Assert (-not(Test-Path -LiteralPath (Join-Path $run 'unapproved-output'))) 'Rejected build created output'
$badPlan=$plan|ConvertTo-Json -Depth 50|ConvertFrom-Json;$badPlan.cases[0].supplements[0].relativePath='../escape.png';$badFile=Join-Path $run 'escape-plan.json';SaveJson $badFile $badPlan
MustReject {& $new -PlanPath $badFile -CliPath $CliPath -OutputDirectory (Join-Path $run 'escape-output')} 'path escape';$passed++
$finalProject=Join-Path $original 'edited.json';$finalDoc=[IO.File]::ReadAllText($project)|ConvertFrom-Json
$finalDoc.balloons[0].transform.x+=71;SaveJson $finalProject $finalDoc
$referencePlan=$plan|ConvertTo-Json -Depth 60|ConvertFrom-Json
$finalHash=(Get-FileHash -LiteralPath $finalProject).Hash
$referencePlan.allowedFiles+=@{path=$finalProject;sha256=$finalHash}
$referencePlan.cases[0]|Add-Member referenceProjectPath $finalProject
$referencePlan.cases[0]|Add-Member referenceProjectSha256 $finalHash
$referencePlanFile=Join-Path $run 'with-reference.json';SaveJson $referencePlanFile $referencePlan
$referencePackage=Join-Path $run 'reference-layout';& $new -PlanPath $referencePlanFile -CliPath $CliPath -OutputDirectory $referencePackage
$actualStart=[IO.File]::ReadAllText((Join-Path $referencePackage 'TEST/projects/start.json'))|ConvertFrom-Json
$actualReference=[IO.File]::ReadAllText((Join-Path $referencePackage 'TEST/projects/reference.json'))|ConvertFrom-Json
Assert ($actualReference.balloons[0].transform.x -eq $finalDoc.balloons[0].transform.x) 'Reference must preserve the explicitly selected edited layout'
Assert ($actualStart.balloons[0].transform.x -ne $actualReference.balloons[0].transform.x) 'Start and final reference must remain distinct';$passed++
$referencePlan.allowedFiles=@($referencePlan.allowedFiles|Where-Object {$_.path -ne $finalProject});SaveJson $referencePlanFile $referencePlan
MustReject {& $new -PlanPath $referencePlanFile -CliPath $CliPath -OutputDirectory (Join-Path $run 'unapproved-reference')} 'unapproved edited reference';$passed++
Assert (-not(Test-Path -LiteralPath (Join-Path $run 'unapproved-reference'))) 'Reference rejection created output'
foreach($file in $allow){Assert ((Get-FileHash -LiteralPath $file.path).Hash -eq $sourceHashes[$file.path]) 'Original was changed'}
Write-Output ('CASE_TEST_PASS '+$passed+' checks')

param([Parameter(Mandatory=$true)][string]$PackageDirectory)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object Text.UTF8Encoding $false
$root=[IO.Path]::GetFullPath($PackageDirectory)
foreach($name in @('ComicEditor.exe','ComicEditor.Cli.exe','ComicEditor.runtimeconfig.json','ComicEditor.Cli.runtimeconfig.json','ComicEditor.deps.json','ComicEditor.Cli.deps.json','coreclr.dll','hostfxr.dll','hostpolicy.dll','PresentationFramework.dll','release-manifest.json','LICENSE.dotnet.txt','ThirdPartyNotices.dotnet.txt')){if(-not(Test-Path -LiteralPath (Join-Path $root $name))){throw ('Required release file missing: '+$name)}}
$manifest=([IO.File]::ReadAllText((Join-Path $root 'release-manifest.json')) | ConvertFrom-Json)
foreach($entry in $manifest.files){$path=Join-Path $root $entry.path;if(-not(Test-Path -LiteralPath $path)-or (Get-FileHash -LiteralPath $path).Hash -ne $entry.sha256){throw ('Manifest mismatch: '+$entry.path)}}
foreach($name in @('ComicEditor','ComicEditor.Cli')) {
 $configuration=([IO.File]::ReadAllText((Join-Path $root ($name+'.runtimeconfig.json'))) | ConvertFrom-Json).runtimeOptions
 if($configuration.framework -or $configuration.frameworks -or -not $configuration.includedFrameworks){throw 'Entrypoint is framework-dependent'}
}
$nativeRoot=Split-Path -Parent $PSScriptRoot
$testRoot=Join-Path $nativeRoot ('artifacts/package-tests/'+[Guid]::NewGuid().ToString()+'/中文 空格 便携应用')
[void][IO.Directory]::CreateDirectory($testRoot)
Get-ChildItem -LiteralPath $root -Force | Copy-Item -Destination $testRoot -Recurse
$env:PATH=$env:SystemRoot+'\System32;'+$env:SystemRoot
$env:DOTNET_ROOT=Join-Path $testRoot 'not-a-dotnet-installation'
$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
$env:DOTNET_MULTILEVEL_LOOKUP='0'
$cli=Join-Path $testRoot 'ComicEditor.Cli.exe'
$project=Join-Path $testRoot 'examples/minimal-page/project.json'
$caps=(& $cli capabilities | ConvertFrom-Json);if($LASTEXITCODE -ne 0 -or -not $caps.success){throw 'Self-contained CLI failed'}
foreach($action in @('object.reorder','balloon.panelOcclusion','panel.snap')){if($action -notin $caps.data.actions.name){throw ('Upgraded action missing: '+$action)}}
foreach($file in @('docs/2026-10-01-usability-upgrade-acceptance.md','docs/agent-api.md','skills/comic-page-workflow/references/editor-agent-interface.md','schemas/response-v1.schema.json')){if(-not(Test-Path -LiteralPath (Join-Path $testRoot $file))){throw ('Upgraded documentation missing: '+$file)}}
$checked=(& $cli validate --project $project | ConvertFrom-Json);if($LASTEXITCODE -ne 0 -or -not $checked.success){throw 'Relocated project failed'}
$inspected=(& $cli inspect --project $project | ConvertFrom-Json);if($LASTEXITCODE -ne 0){throw 'Portable inspection failed'}
$request=Join-Path $testRoot 'test-patch.json';$output=Join-Path $testRoot 'test-project.json';$objectId=$inspected.data.project.panels[0].id
$patch=[ordered]@{apiVersion=1;requestId=[Guid]::NewGuid().ToString();baseProjectHash=$inspected.data.hash;operations=@([ordered]@{op='object.reorder';targetId=$objectId;args=[ordered]@{kind='panel';direction='down'}})}
[IO.File]::WriteAllText($request,($patch|ConvertTo-Json -Depth 8),(New-Object Text.UTF8Encoding $false))
$applied=(& $cli apply --project $project --patch $request --out $output | ConvertFrom-Json);if($LASTEXITCODE -ne 0 -or $applied.data.outcomes[0].code -ne 'NoChange'){throw 'Portable outcome/no-op receipt failed'}
$rendered=(& $cli render --project $project --out (Join-Path $testRoot 'test-output.png') --scale 2 | ConvertFrom-Json);if($LASTEXITCODE -ne 0 -or $rendered.data.width -ne 1280 -or $rendered.data.height -ne 1800){throw 'Relocated render failed'}
$gui=Start-Process -FilePath (Join-Path $testRoot 'ComicEditor.exe') -ArgumentList @('--project',('"'+$project+'"'),'--portable-data') -WorkingDirectory $testRoot -WindowStyle Hidden -PassThru
try {
 $deadline=[DateTime]::UtcNow.AddSeconds(15)
 do{$gui.Refresh();if($gui.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 100}while(-not $gui.HasExited -and [DateTime]::UtcNow -lt $deadline)
 if($gui.HasExited -or $gui.MainWindowHandle -eq 0 -or -not $gui.Responding){throw 'Self-contained GUI did not create responding MainWindow'}
 $registry=Join-Path $testRoot 'Data/Sessions'
 $session=(& $cli session list --registry-dir $registry | ConvertFrom-Json).data.sessions | Where-Object {$_.processId -eq $gui.Id} | Select-Object -First 1
 if(-not $session){throw 'Self-contained session missing'}
 $snapshot=(& $cli session snapshot --session $session.sessionId --registry-dir $registry | ConvertFrom-Json)
 if($LASTEXITCODE -ne 0 -or $snapshot.data.projectPath -ne $project -or $snapshot.data.allowWrite){throw 'Self-contained GUI source mismatch'}
 Write-Output ('PACKAGE_RUNTIME_PASS relocated='+$testRoot+'; GUI='+$gui.Id+'; CLI=API1; image=1280x1800; local runtimes only')
}finally{if(-not $gui.HasExited){$gui.CloseMainWindow() | Out-Null}}

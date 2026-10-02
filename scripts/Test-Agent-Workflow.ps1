param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$ProjectPath,[switch]$LeaveOpen)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=New-Object Text.UTF8Encoding $false
$env:DOTNET_ROOT=Join-Path (Split-Path -Parent $PSScriptRoot) '.tools/dotnet'
$cli=Join-Path ([IO.Path]::GetFullPath($PackageDirectory)) 'ComicEditor.Cli.exe'
$source=[IO.Path]::GetFullPath($ProjectPath);$before=(Get-FileHash -LiteralPath $source).Hash
$response=(& $cli open --project $source --portable-data | ConvertFrom-Json)
if($LASTEXITCODE -ne 0 -or -not $response.success){throw 'CLI launch failed'}
if($response.data.state -ne 'LaunchRequested' -or $response.data.visible){throw 'Launch receipt must not claim desktop visibility'}
$created=Get-Process -Id $response.data.processId
try {
 $deadline=[DateTime]::UtcNow.AddSeconds(20)
 do {$created.Refresh();if($created.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 200}while([DateTime]::UtcNow -lt $deadline -and -not $created.HasExited)
 if($created.HasExited -or $created.MainWindowHandle -eq 0 -or -not $created.Responding){throw 'No responding native MainWindow'}
 $registry=Join-Path $PackageDirectory 'Data/Sessions'
 $listed=(& $cli session list --registry-dir $registry | ConvertFrom-Json)
 $session=$listed.data.sessions | Where-Object {$_.processId -eq $created.Id} | Select-Object -First 1
 if(-not $session){throw 'Session registration missing'}
 $snapshot=(& $cli session snapshot --session $session.sessionId --registry-dir $registry | ConvertFrom-Json)
 if($LASTEXITCODE -ne 0 -or $snapshot.data.allowWrite -or $snapshot.data.projectPath -ne $source){throw 'Live startup snapshot mismatch'}
 if((Get-FileHash -LiteralPath $source).Hash -ne $before){throw 'Original project changed'}
 [pscustomobject]@{State=$response.data.state;VisibleClaim=$response.data.visible;ProcessId=$created.Id;WindowHandle=$created.MainWindowHandle;Responding=$created.Responding;SessionId=$session.sessionId;ReadOnly=$true;SourceHashPreserved=$true} | ConvertTo-Json
}finally{if(-not $LeaveOpen -and -not $created.HasExited){$created.CloseMainWindow() | Out-Null}}

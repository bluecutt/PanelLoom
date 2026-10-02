# Native editor adapter

## 0.2 ordering and inset relationships

Retrieve the current file/live snapshot, then query capabilities. For a user-requested relative edit, build a fresh ordinary API1 patch using those current hash/revision values. Keep every unrelated source, transform, polygon, text object and export scale.

Example operations (replace IDs with the user's selected stable IDs):

```json
[
  {"op":"object.reorder","targetId":"X","args":{"kind":"balloon","direction":"up"}},
  {"op":"balloon.panelOcclusion","targetId":"X","args":{"panelId":"B","position":"front","autoOrder":true}}
]
```

Reorder moves exactly one same-kind neighbor; a boundary returns NoChange. Front/back/inherit controls only the named panel relationship, not `clipPanelId`. `balloon.clip` separately selects a stable panel ID or empty string for page-wide display. From 0.2.0-preview.2, optional autoOrder=true finds the nearest legal same-kind position for only the selected balloon; other objects' mutual order and records remain unchanged. Verify allowedArgs before using it with an older editor. Missing/false retains strict cycle rejection. Occlusion outcomes are Placed/OrderAdjusted/NoChange with signed layerDelta (positive toward front, negative toward back). Locked targets and genuinely contradictory relations still return an atomic error; do not rewrite other rules to force success. For snapping, send panel.snap with the selected zero-based edge index and inspect `data.outcomes`: Moved includes targetPanelId/targetEdgeIndex/distance; AlreadyAligned, NoCandidate and Unsafe must not be reported as movement. Dry-run outcomes must agree with apply. Live no-ops keep their actual revision and hash. Treat fields in capabilities and receipt data as protocol values, not translated UI labels.

Read the project's `comic-project.json`; resolve editorCliPath relative to that file, and contracts relative to the project root. The executable may be moved; do not assume a personal drive or user directory. Query `capabilities`: envelope apiVersion=1, data.projectFormat=ComicPanelEditorProject, data.projectVersion=2. Actions and commands in that result are authoritative.

## Offline file route

`init --manifest manifest.json --out working.json` accepts API1, a complete v2 `project` and explicit `assetMap` entries {kind,id,sourceImage}. Source paths resolve through the project's assetBase relative to the manifest. Save-as rebases that base automatically; images are neither cropped nor re-encoded. `assets/init-manifest-example.json` is runnable from its packaged position.

`inspect --project working.json` returns full project, file hash, dimensions and source hashes. Copy `assets/patch-example.json`, generate a fresh UUID and replace its baseProjectHash with the current inspect hash. A zero placeholder is not a usable current hash. `asset.replace` uses an exact kind and ID; preserve retains positioning, refit contains a changed aspect. The same filename with changed bytes is detected on inspection/render.

```powershell
$configPath = [IO.Path]::GetFullPath('comic-project.json')
$config = [IO.File]::ReadAllText($configPath) | ConvertFrom-Json
$cli = [IO.Path]::GetFullPath((Join-Path (Split-Path $configPath) $config.editorCliPath))
$cap = & $cli capabilities | ConvertFrom-Json
if ($LASTEXITCODE -ne 0 -or $cap.apiVersion -ne 1 -or $cap.data.projectVersion -ne 2) { throw 'Incompatible editor' }
& $cli init --manifest manifest.json --out working.json
if ($LASTEXITCODE -ne 0) { throw 'Initialization failed' }
& $cli open --project working.json
# User adjusts and saves working.json in the GUI before the next commands.
$state = & $cli inspect --project working.json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Inspection failed' }
$patch = [IO.File]::ReadAllText('patch-template.json') | ConvertFrom-Json
$patch.requestId = [Guid]::NewGuid().ToString()
$patch.baseProjectHash = $state.data.hash
[IO.File]::WriteAllText([IO.Path]::GetFullPath('patch.json'), ($patch | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding $false))
& $cli apply --project working.json --patch patch.json --out revised.json --dry-run
if ($LASTEXITCODE -ne 0) { throw 'Dry run rejected' }
& $cli apply --project working.json --patch patch.json --out revised.json
if ($LASTEXITCODE -ne 0) { throw 'Apply failed' }
& $cli render --project revised.json --out final.png --scale 2.5
if ($LASTEXITCODE -ne 0) { throw 'Export failed' }
```

Existing output needs explicit overwrite plus its matching if-output-hash. Prefer new outputs. Never use apply to overwrite a GUI's unsaved edits via stale disk state. `bundle --project revised.json --out-dir new-folder` copies explicitly referenced assets, preserving unknown project metadata; review private metadata before public sharing.

## Live route

`session list` → `session snapshot --session UUID` → API1 patch with returned baseRevision and baseProjectHash → `session apply --session UUID --patch patch.json`.

User must enable GUI write access. Busy means dragging, modal UI or uncommitted input; wait for the user to finish, then refresh snapshot rather than queueing an old edit. Stale revision/hash rejects. Same successful requestId/content returns original receipt throughout that session, even after Undo; a new intentional operation needs a new UUID. SESSION_RESULT_UNKNOWN means retry identical ID or inspect, not presumed rollback.

Save separately using `session save --session UUID --revision N --base-hash HASH --out final.json`. Current disk hash is checked. --portable-data or --registry-dir locates a portable session explicitly. stdout is a JSON receipt, never pixels. Only local same-user pipes are exposed; no remote URL or arbitrary code execution.

## Common mistakes

- Process ID/LaunchRequested is not evidence the user saw a window.
- Normal letters within one balloon keep consistent size; emphasis is selected by dialogue emotion, not randomly varied characters.
- Masking a source is not a reason to crop its original file before import.
- Exporting a larger canvas does not create missing source detail; check the quality warning.
- Final export is already assembled with text. Do not automatically run a whole-page Image iteration afterward.

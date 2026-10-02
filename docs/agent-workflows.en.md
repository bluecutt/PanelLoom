# Agent and creator collaboration

[English](agent-workflows.en.md) | [中文](agent-workflows.md)

Use `ComicEditor.Cli.exe` from the complete application folder. Query `capabilities` first: its commands, actions and allowed arguments are authoritative. This candidate supports API v1 and version 2 projects. The [API reference](agent-api.md), schemas and neutral examples describe the request structure.

## File-based initialization and output

1. Read the creator-approved framework, selected original panel images, independent lettered objects and placement instructions.
2. Build an init manifest with `apiVersion`, the complete v2 `project` and explicit `assetMap` entries. Keep original images intact; panel polygons control their visible area.
3. Initialize, inspect and validate a new working file.

```powershell
$cli = '.\ComicEditor.Cli.exe'
& $cli capabilities
& $cli init --manifest '.\skills\comic-page-workflow\assets\init-manifest-example.json' --out '.\working.json'
& $cli inspect --project '.\working.json'
& $cli validate --project '.\working.json'
& $cli open --project '.\working.json'
```

This example uses packaged neutral assets. `open` returns `LaunchRequested`; confirm that the creator can see the window before reporting that it is open. The creator adjusts and saves the working project.

Read that newest file before modifying it. A patch needs API1, a fresh UUID `requestId`, the current `baseProjectHash` from `inspect`, and registered operations. For example, `object.reorder` uses `targetId` and `{ "kind": "balloon", "direction": "up" }`. Use the actual selected ID from the project. Copy the Skill's patch example as a template and replace its placeholder hash, ID and request ID.

```powershell
& $cli apply --project '.\working.json' --patch '.\patch.json' --out '.\working-next.json' --dry-run
# Continue only after a successful receipt and review of its changes.
& $cli apply --project '.\working.json' --patch '.\patch.json' --out '.\working-next.json'
& $cli render --project '.\working-next.json' --out '.\final.png' --scale 2.5
& $cli bundle --project '.\working-next.json' --out-dir '.\portable-page'
```

Use new output paths. Existing outputs require explicit overwrite with their matching hash. `asset.replace` changes only the named panel or balloon; choose `preserve` or `refit` explicitly when dimensions change. Preserve unrelated transforms, polygons, sources, clipping, layers and export settings. Bundling copies referenced assets; review metadata and asset permissions before sharing your own bundle.

## Work on an open editor session

```powershell
& $cli session list
& $cli session snapshot --session SESSION_UUID
& $cli session apply --session SESSION_UUID --patch '.\live-patch.json' --dry-run
& $cli session apply --session SESSION_UUID --patch '.\live-patch.json'
& $cli session save --session SESSION_UUID --revision CURRENT_REVISION --base-hash CURRENT_HASH --out '.\saved-next.json'
```

Replace placeholders with actual values. A live patch contains the latest `baseRevision` and `baseProjectHash` returned by the snapshot. Read a fresh snapshot after applying, before saving. For portable sessions, use `--registry-dir` to select the application's `Data/Sessions` folder when needed.

The GUI starts read-only for Agent writes. The creator enables **允许 Agent 修改本次会话** (Allow Agent to modify this session). Changes operate on the current unsaved layout. One batch can be undone together. Clearing permission takes effect immediately. Dragging, modal dialogs and uncommitted inputs can return `Busy`; let the creator finish and refresh the snapshot. Do not overwrite live manual work with an older disk file.

Reusing a successful request ID with identical content returns its original receipt. A new intended action needs a new UUID. Different content with the same ID is rejected. `SESSION_RESULT_UNKNOWN` indicates an uncertain communication result: retry the identical request or inspect current state. Saving is separate; an unexpected external file change rejects the save.

## Clipping, occlusion and actual outcomes

`balloon.clip` controls the display scope. `balloon.panelOcclusion` controls front/back/inherit relative to a named panel; it does not change clipping. In preview.2 and later, `autoOrder:true` can move the selected balloon to its nearest legal same-kind position while preserving other objects' relative order. Query support before using it. Omitting it retains strict conflict rejection. Contradictory relations and locked targets remain atomic errors.

```json
{
  "op": "balloon.panelOcclusion",
  "targetId": "SELECTED_BALLOON_ID",
  "args": { "panelId": "INSET_PANEL_ID", "position": "back", "autoOrder": true }
}
```

Read `data.outcomes`: occlusion reports `Placed`, `OrderAdjusted` or `NoChange`, with signed `layerDelta`. Positive moves toward the front, negative toward the back. Relation and automatic ordering form one Undo transaction. `object.reorder` moves one actual same-kind neighbor. `panel.snap` reports `Moved`, `AlreadyAligned`, `NoCandidate` or `Unsafe`; overall `success:true` alone does not prove an edge moved.

## Local operation and verification

Sessions use local same-user Windows pipes. No remote listener, model-call endpoint or arbitrary-code endpoint is exposed. Send image paths; stdout contains JSON receipts, with no image Base64. Offline file operations remain available.

The recipient [Windows trial checklist](new-windows-trial.en.md) covers portable launch, layout, save/reopen, export and optional live authorization checks. Another-machine compatibility remains pending until that trial is recorded.

[Codex setup](codex-setup.en.md) · [Quick start](quick-start.en.md) · [Contribution guide](../CONTRIBUTING.en.md)

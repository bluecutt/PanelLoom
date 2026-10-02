# Agent operations appendix

[English](agent-appendix.en.md) | [中文](agent-appendix.md)

PanelLoom provides a file-based CLI, project JSON and local live sessions. Use `ComicEditor.Cli.exe` from the full application package and query capabilities before constructing requests.

```powershell
$cli = 'D:\PanelLoom\ComicEditor.Cli.exe'
$caseRoot = 'D:\PanelLoom-cases'
& $cli capabilities
& $cli inspect --project "$caseRoot\P10\projects\reference.json"
& $cli validate --project "$caseRoot\P10\projects\reference.json"
```

Replace these example paths with your own extracted folders. `reference.json` contains the approved demonstration layout; `start.json` preserves the historical initialization. Inspect actual dimensions, IDs, counts and scope before creating a separate working copy.

## Initialize an approved layout

An init manifest contains `apiVersion`, a complete `project` and `assetMap`. Entries identify `kind`, `id` and `sourceImage`. Use creator-approved assets. Keep the full images: polygons determine framing. Source paths resolve through the manifest project's `assetBase`.

```powershell
& $cli init --manifest manifest.json --out working.json
& $cli inspect --project working.json
& $cli validate --project working.json
& $cli render --project working.json --out practice.png --scale 2.5
```

The application package includes schemas and runnable neutral examples. Use those when constructing your own manifest. In the supplied cases, preserve the original reference files and save work separately.

## Dry-run, apply and receipts

`inspect` returns the current file hash. An API1 patch needs a fresh UUID `requestId`, that `baseProjectHash` and registered actions. Review the dry-run receipt before producing a new project.

```powershell
& $cli apply --project working.json --patch patch.json --out working-next.json --dry-run
& $cli apply --project working.json --patch patch.json --out working-next.json
```

`balloon.clip` controls display scope. `balloon.panelOcclusion` controls front/back relative to a named panel. To move the selected balloon to the nearest valid layer when required, explicitly pass `autoOrder:true` after checking capability support. Read `data.outcomes` for actual changes and warnings; a successful envelope alone does not prove that snapping or layer movement occurred.

## Work on a creator's open project

Use `session list`, then `session snapshot`. Build the patch from the current unsaved state and latest revision/hash. The creator enables **允许 Agent 修改本次会话** (Allow Agent to modify this session); it is read-only by default. Review a dry run, then apply one batch. Save separately after layout approval.

If the editor is busy, wait for the creator to finish and refresh the snapshot. If communication leaves the result unknown, inspect state or retry the identical request ID and content. Give a new intended action a fresh ID. Send paths to local images; CLI responses are JSON receipts.

Case images permit local non-destructive layout practice and local saves. New drawing, AI redraws and public outputs use your own assets. See [asset terms](../ASSET-TERMS.en.md).

[Quick exercise](quick-start.en.md) · [Full P10 workflow](../P10/guide/full-workflow.en.md)

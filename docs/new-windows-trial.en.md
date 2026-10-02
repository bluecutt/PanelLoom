# Trial on another Windows computer

[English](new-windows-trial.en.md) | [中文](new-windows-trial.md)

This is the remaining release gate. Local extraction checks have a separate report. A fresh-computer result is recorded only after someone performs this trial on another Windows x64 machine.

## Prepare

Record the Windows version/build, x64 architecture, package filename, ZIP SHA-256 and test date. Compare the hash with the delivery manifest before extraction. Extract the whole application folder into a writable location with spaces or non-ASCII characters. Retain all runtime files. The self-contained application should run without a .NET SDK; building source is a separate task.

The application is unsigned. If Windows security blocks it, record the exact message and file involved, and verify the source. Keep security software enabled. A blocked launch is an unresolved test result, not a passing launch. Do not add blanket exclusions or change system security settings for this trial.

## Required manual checks

Use `examples/minimal-page/project.json` first and save new work under a separate test folder. Use the restricted case pack only after reading its asset terms.

| Check | Expected result | Result and evidence |
| --- | --- | --- |
| Launch | `ComicEditor.exe` opens a visible, responding window | Pending |
| Open neutral project | Panel image and bubble load without missing assets | Pending |
| Object selection | Selecting the panel/bubble module switches the corresponding edit target | Pending |
| Image framing | Drag updates live; wheel on the canvas scales the selected image | Pending |
| Navigation | Ctrl + wheel zooms the page; sidebar wheel scrolls without changing an image | Pending |
| Panel geometry | Vertex/edge editing changes the selected polygon and can be undone | Pending |
| Bubble geometry | Drag, resize, rotation and opacity operate on the selected bubble | Pending |
| Scope | Panel clipping hides overflow; page-wide mode allows crossing panels | Pending |
| Layers and inset occlusion | Up/down moves one layer; front/back relationships to an inset work and Undo restores the prior state | Pending |
| Borders and snap | Border remains complete above a lower bubble; snap reports moved/aligned/no candidate/unsafe appropriately | Pending |
| Controls | Applied settings retain open sections; color chooser and fixed export scales remain usable | Pending |
| Save and reopen | Save a new JSON file; reopening retains framing, polygons, bubbles, clipping and layers | Pending |
| PNG export | Neutral page at 2× is 1280 × 1800; inspect actual dimensions and sharpness | Pending |

The neutral page has one panel and one bubble. For multiple panels/inset checks, create a disposable project with your own assets, or use the private case pack locally. Save copies and leave reference projects intact.

## Required CLI checks

Open PowerShell in the extracted application folder. Create a new `trial-output` folder; use unused output filenames on reruns.

```powershell
& '.\ComicEditor.Cli.exe' capabilities
& '.\ComicEditor.Cli.exe' validate --project '.\examples\minimal-page\project.json'
& '.\ComicEditor.Cli.exe' inspect --project '.\examples\minimal-page\project.json'
& '.\ComicEditor.Cli.exe' render --project '.\examples\minimal-page\project.json' --out '.\trial-output\neutral-2x.png' --scale 2
```

Expect successful JSON receipts, API1/project v2, the actions `object.reorder`, `balloon.panelOcclusion`, `panel.snap`, and 1280 × 1800 output. Record both the exit code and receipt for failures.

## Optional Codex and Agent integration checks

Install the Skill in a disposable creative project following [Codex setup](codex-setup.en.md). Verify Skill discovery and its neutral initialization example without generating images. For live checks, open only that test project: snapshot is readable; writes are denied until permission is enabled; one approved batch can be undone; disabling permission denies subsequent writes. Stale revision/hash and busy state must reject unsafe changes. Follow the [Agent guide](agent-workflows.en.md).

These optional checks depend on the recipient's Codex environment. Record skipped items separately; a CLI pass does not prove Skill discovery or image-generation access.

## Return a result

Record: tester, date, Windows build/architecture, application version `0.2.0-preview.4`, package revision `docs.1`, ZIP hash, extraction location category, whether .NET SDK was already installed, every item as Pass/Fail/Not tested, and exact error text. Use a neutral project for screenshots or shared reproduction files. Restricted case screenshots and practice output remain local.

Status: **PENDING_OTHER_WINDOWS_TRIAL**. A successful local test on the development computer does not close this gate.

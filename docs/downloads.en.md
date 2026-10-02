# Downloads and Practice

[中文](downloads.md) | [English](downloads.en.md)

Open the [0.2.0-preview.4 release](https://github.com/bluecutt/PanelLoom/releases/tag/v0.2.0-preview.4) and select the files you need. The introduction, tutorials, teaching images, source, Skill, and downloads are provided through GitHub.

| Attachment | Purpose |
| --- | --- |
| [Portable Windows x64 ZIP](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-0.2.0-preview.4-docs.3-win-x64.zip) | Extract it and run `ComicEditor.exe`; keep the complete folder and companion files |
| [Matching source ZIP](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-source-0.2.0-preview.4-docs.3.zip) | Read, build, and contribute; retain both software and asset terms |
| [Full practice pack v10](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-case-practice-v10.zip) | P09/P10 source assets, text objects, initial and reference projects, and full bilingual guides |
| [SHA-256 manifest](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/SHA256SUMS.txt) | Verify version, integrity, and download source |

Extract the application and open its bundled `case-studies/P10/projects/reference.json`. If you downloaded the standalone practice pack, open its `P10/projects/reference.json` instead. Use **项目 → 另存为** (Project → Save as) to create a working copy. `reference.json` contains the approved layout; `start.json` preserves the historical initialization. Follow the [case quick start](../case-studies/guide/quick-start.en.md).

Manual editing of existing assets needs no Codex installation, SDK, or model API. To create new work in Codex, follow the [Skill setup guide](codex-setup.en.md) with your own project and requirements.

Read the [asset terms](../ASSET-TERMS.en.md) before practice. Software uses AGPL-3.0-only; case materials follow their separate terms and GitHub platform permissions.

The application version is `0.2.0-preview.4`; the packaging revision is `docs.3`. This revision connects publication links and documentation. The accepted EXE and DLL files and project layouts remain unchanged.

Run `Get-FileHash -Algorithm SHA256 -LiteralPath 'full path to the downloaded file'` in PowerShell and compare the result with `SHA256SUMS.txt` from the same release. Windows may show an unsigned-application warning at first launch. Verify the download source and file integrity first.

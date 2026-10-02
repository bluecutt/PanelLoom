# PanelLoom

[English](README.md) | [中文](README.zh-CN.md)

Comic Creation Workflow & Manual Page Assembler.

| Page example 1 · P09 | Page example 2 · P10 |
| --- | --- |
| [![Finished P09 page](case-studies/P09/reference-output/historical-final.png)](case-studies/P09/reference-output/historical-final.png) | [![Finished P10 page](case-studies/P10/reference-output/editor-reference.png)](case-studies/P10/reference-output/editor-reference.png) |

A Codex-oriented workflow for making individual comic pages, paired with a native Windows x64 page assembler and a reusable Skill. Creators and ChatGPT discuss dialogue and visual design, select a framework and performances from layout drafts, redraw panels individually, generate independent lettered objects, and finish framing and layout in the editor.

The creator decides the story, selects candidates, and approves the final result. ChatGPT helps organize requirements, generate assets, initialize the project, and check resources. Click either preview to view its full-size image. P10 uses the creator's approved layout in the native editor.

[Downloads](docs/downloads.en.md) · [Quick start](docs/quick-start.en.md) · [Illustrated P10 tutorial](case-studies/P10/guide/full-workflow.en.md) · [Codex setup](docs/codex-setup.en.md) · [Agent guide](docs/agent-workflows.en.md)

## Compose → Separate → Assemble

A full-page layout draft establishes reading order, shots, and space for text. Select the page framework and each panel's performance independently. Redraw selected crops into complete finished panels, generate the lettered bubbles separately, then assemble and export the page locally.

Retries stay local to one asset. Correct a hand or expression by replacing its panel; revise dialogue or typography by replacing its bubble. Accepted assets remain available. Reducing whole-page regeneration helps avoid cumulative blur, noise, and changes to unrelated areas during detail and lettering edits.

Replaceable assets and undoable layout changes make iteration more forgiving. Creators retain control through panel selection, refinement, and manual adjustments. Export renders from the complete source images.

## Install and get started

### Use the Windows editor

Use a Windows x64 computer. Manual layout with existing assets works in the portable application without installing Codex, the .NET SDK, or a model API.

1. After publication, download the portable Windows x64 ZIP and full practice pack from this repository's **Releases**. See the [download guide](docs/downloads.en.md) for files and verification steps.
2. Extract the complete application ZIP into your tools folder. Keep the EXE, DLLs, and companion files together, then double-click `ComicEditor.exe`.
3. Extract the practice pack. In the application, click **打开工程** (Open project) and select `P10/projects/reference.json` to view the finished example layout.
4. Use **项目 → 另存为** (Project → Save as) to create a working copy before moving images, arranging bubbles, and exporting. Follow the [quick start](docs/quick-start.en.md) for canvas controls.

### Start new work in Codex

New artwork requires image-generation capability in your current Codex environment. Prepare your own creative project folder, story, character references, and visual requirements.

1. Copy the complete `skills/comic-page-workflow` folder from your downloaded PanelLoom directory into `.agents/skills/comic-page-workflow` in your creative project. If a Skill with the same name already exists, compare the versions and confirm how to update it while preserving the original.
2. Use the [project configuration example](skills/comic-page-workflow/assets/project-config-example.json) to prepare `comic-project.json` at your creative project root. Set `editorCliPath` to your local `ComicEditor.Cli.exe` and configure the story, character, visual-document, and page-directory paths.
3. Open that creative project in Codex and invoke `$comic-page-workflow`. If the Skill does not appear, restart Codex and check again. See the [official Skill documentation](https://learn.chatgpt.com/docs/build-skills) for repo-scoped discovery.
4. Begin with dialogue. Approve it before moving to visual design, drafts, panel selection, and asset generation. See the [Codex setup guide](docs/codex-setup.en.md) for the detailed configuration.

Ask your Agent to help with setup:

> From my downloaded PanelLoom directory, install skills/comic-page-workflow into .agents/skills/comic-page-workflow in this creative project. If the destination already exists, compare it and ask before updating. Read the project configuration example, help me prepare comic-project.json, verify the editor CLI and requirements paths, and confirm whether image generation is available in this environment.

Then start your first page:

> Use $comic-page-workflow. Read my project configuration, story, character references, and visual requirements. Draft this page's dialogue first, identifying the speaker, bubble type, reading order, and emotion. Wait for my approval before designing the artwork.

## From dialogue to a finished page

| Stage | Decisions | Saved output |
| --- | --- | --- |
| Dialogue | Exact wording, speaker, emotion, and text container | Approved dialogue design |
| Visual design | Camera, placement, expression, action, and negative space | Shot and panel guide |
| Draft exploration | Page proportions, reading order, and acting candidates | Independent layout drafts |
| Numbered selection | Framework source and content source for each panel | Framework and panel source map |
| Finished artwork | Redraw from selected crops, with room for reframing | Complete panel images |
| Lettered objects | Bubble contour, tail, and final text in one asset | Independently movable text objects |
| Local assembly | Agent initialization, followed by creator adjustments | Working project and high-resolution export |

The [illustrated P10 tutorial](case-studies/P10/guide/full-workflow.en.md) walks through these stages with real conversations in translation, Chinese originals, and saved outputs. The [panel design chapter](case-studies/P10/guide/storyboard-design.en.md) includes numbered candidates, separate framework and content selection, and six draft-to-finished comparisons. The [lettering chapter](case-studies/P10/guide/lettering-styles.en.md) explains how prompts control contours, tails, and typography. The [general page workflow](docs/page-workflow.en.md) provides a reusable checklist for your own work.

## Edit on the canvas

Double-click `ComicEditor.exe` in the release folder and open a version 2 JSON project. Keep the entire application folder. The portable package includes its runtime; existing-asset editing needs no .NET SDK, model API, or account.

The canvas wheel scales the selected image or bubble; Ctrl + wheel zooms the whole page. The sidebar wheel only scrolls its controls. Independent polygon panels support size, vertex, shape, and framing adjustments. Bubble controls support complete lettered PNGs, separate body and tail layers, rotation, opacity, clipping or cross-panel display, and front/back relationships to inset panels. Single-line borders retain independent panel shapes, and export renders from the source assets.

The application interface is currently in Chinese. The [English quick start](docs/quick-start.en.md) pairs visible control labels with English meanings. English interface switching is outside this documentation update.

## Try it with the practice pack

Get the Windows application and full practice pack from this repository's Releases when published; the [download guide](docs/downloads.en.md) lists the files and opening steps. P10 contains 6 panels and 10 lettered objects. P09 contains 9 panels and 13 historical blank bubbles. Start with the [case exercise](case-studies/guide/quick-start.en.md) and open `P10/projects/reference.json` from the extracted pack, then save a working copy before editing. `start.json` preserves the historical initial layout for comparison.

The artwork retains its Chinese lettering. English instructions explain the workflow and controls. Illustrated guides and their teaching images are included in this repository; the full practice ZIP is distributed as a Release attachment.

## Work with an Agent

`ComicEditor.Cli.exe` provides capability discovery, initialization, validation, asset replacement, rendering, and project bundling. Open editor windows support local sessions for the same user. When the creator enables **允许 Agent 修改本次会话** (Allow Agent to modify this session), an Agent can change specified objects while preserving the current manual state. A batch of edits can be undone together.

The general-purpose Skill is in [skills/comic-page-workflow](skills/comic-page-workflow/SKILL.md). It reads a project's own `comic-project.json` to locate story, character, and visual requirements. Examples, record templates, and stage references are included with the Skill.

## Version and development

The current local release candidate is `0.2.0-preview.4`, with behavior based on the accepted `0.2.0-preview.2`. This preparation work organizes the complete workflow, tutorials, licensing, and packaging. See [build and release](docs/build-and-release.md) and the [acceptance checklist](docs/acceptance-checklist.md), currently in Chinese.

Software, the general-purpose Skill, templates, and general documentation use **AGPL-3.0-only**. The embedded comics and case materials retain their separate [asset terms](ASSET-TERMS.en.md); bluecutt retains all legally held creative rights. See the [license scope](LICENSE-SCOPE.en.md), [full software license](LICENSE), and [asset guidance](docs/licensing.md).

Windows may display an unsigned-application warning on first launch. Verify the download source and checksums. Testing on a second Windows x64 computer was accepted on 2026-10-02: 10 automatic checks and 15 manual checks. Wider Windows compatibility remains subject to further testing.

Application binaries remain `0.2.0-preview.4`; this candidate reorganizes the homepage, illustrated guides, and asset terms. Local release preparation is in progress; the repository and Release attachments have not been published.

[Contributing](CONTRIBUTING.en.md) · [Credits](CREDITS.md)

Creator: bluecutt. AI collaborator: ChatGPT. The Chinese name is 格织. Pre-existing third-party character rights remain with their rights holders.

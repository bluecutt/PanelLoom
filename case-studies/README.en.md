# PanelLoom Case Practice Pack

[中文](README.md) | [English](README.en.md)

Comic Creation Workflow & Manual Page Assembler.

[Introduction and page examples](guide/product-introduction.en.md) · [Quick start](guide/quick-start.en.md) · [Asset terms](ASSET-TERMS.en.md)

Release-preparation candidate v09 is approved for everyone to download, with official public display of the currently reviewed teaching images. bluecutt retains the rights legally held in the comics and images. Local practice and GitHub's permitted platform uses follow the asset terms.

The application and full practice pack will be provided through the PanelLoom repository's Releases. Illustrated tutorials can also be read directly in the repository.

English entry pages retain the creator's approved P10 native-editor layout: clipping, front/back occlusion, independent panel shapes, and its 2× export setting. The historical initial layout is kept separately.

The pack contains complete original panels, text objects, draft references, editable projects, and native-editor reference exports. Layout practice with existing assets uses the Windows x64 portable editor and requires no SDK, image model, or author account. Projects use `ComicPanelEditorProject` version 2 and a native editor supporting API1.

Read the [asset terms](ASSET-TERMS.en.md), then open `P10/projects/reference.json` and save a working copy under `P10/work/` before adjusting the page.

| Case | Panels | Bubble/text objects | Historical final page | Native-editor reference |
| --- | --- | --- | --- | --- |
| P10 | 6 | 10 lettered objects | 2560×3840 | 2048×3072, single-line borders |
| P09 | 9 | 13 blank bubbles | 1024×1536 | 2048×3072, single-line borders |

`projects/reference.json` is the approved demonstration layout. `projects/start.json` preserves the historical initialization for comparison. Keep both as fixed baselines and save work separately. Complete images remain in `assets/`; polygon masks control what appears on the page without discarding content outside the frame.

`reference-output/historical-final.png` is a byte-identical copy of the original finished page. `editor-reference.png` is rendered from the native reference project. Historical manual layout and image repairs may not all be represented in the saved project, so both outputs are retained. Source-resolution notes are saved in `quality.json`.

The P10 chapters are available in English with Chinese counterparts: [full workflow](P10/guide/full-workflow.en.md), [numbered selection and redraw comparisons](P10/guide/storyboard-design.en.md), [lettered-object prompts and styles](P10/guide/lettering-styles.en.md), [object list](P10/guide/text-objects.en.md), [prompt appendix](P10/guide/prompt-notes.en.md), and [Agent operations](guide/agent-appendix.en.md). Historical conversations include their Chinese originals and English translations, with model-call text and archived prompts labeled separately. Artwork and lettering retain their original form. P09's advanced chapter remains in Chinese.

Creator: bluecutt. AI collaborator: ChatGPT. Illustrated tutorials and teaching images are included in the GitHub repository; the full practice ZIP is provided through its Releases. The creator has approved the platform content permissions described in the asset terms. The pack has not been published.

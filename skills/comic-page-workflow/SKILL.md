---
name: comic-page-workflow
description: Use when creating or revising manga pages with a configured ComicEditor native manual assembler, including panel source selection, independent lettered objects, page assembly, or localized updates; not video postproduction.
---

# Manga Page Workflow

Approved acting and original pixels remain the source of truth. Load the project's `comic-project.json`: `editorCliPath` resolves relative to that config, and `contracts` points to the user's own story, characters, visual agreement, dialogue and page-selection records. Missing approval or a choice that changes the story needs user direction; do not invent character canon from this portable skill.

Before assembly or modification, read [editor-agent-interface.md](references/editor-agent-interface.md) and query `capabilities` at the configured CLI. Require API1 and project version2; do not silently fall back to an incompatible editor.

## Reference routing

Read the reference for the active stage completely before acting:

- Dialogue, visual design and selection: [page-design.md](references/page-design.md).
- Line candidates, finished panel redraw and repairs: [image-production.md](references/image-production.md), plus the available image-generation skill.
- Movable lettering: [lettered-objects.md](references/lettered-objects.md).
- Assembly, current-state replacement and archive: [panel-assembly.md](references/panel-assembly.md), then the native adapter above.

Use the [page record](templates/page-record.md), [source map](templates/panel-source-map.md) and [text-object contract](templates/lettered-object-contract.md) for durable records. They are planning documents; create editor-ready JSON through the actual API1 init schema. The neutral init example carries its own images under `assets/example-page/`; it can run after this Skill is copied independently. Keep personal project rules in the configured contracts.

## Page stages

1. Read current page status and preceding final page. Continue the approved stage, rather than restarting completed work.
2. Agree page purpose, exact dialogue, speaker, reading order, container type and emotion. Connected bubbles are a single object only when the user wants them to move together.
3. Review detailed acting, gaze, poses and layout. Default priority is recognizable characters → expressive acting → hands/clothing/continuity → background. The project contract defines style and exceptions.
4. Generate independent structural-line candidates with the available image-generation tool; show promptly for user selection. Number panels and record content selections separately from the chosen frame.
5. Crop each selected reference, then independently redraw the complete panel without lettering/balloons. Preserve the chosen expression and pose. Overscan every edge; long panels require long compositions, not stretched portraits.
6. Generate each approved text and container together as an independently movable image object. Thought, caption, sound effect and unboxed text are also objects. Keep exact text, consistent normal lettering and stronger styling at the agreed emotional peaks. Generate transparency when needed using the image tool.
7. Initialize original panel images and accepted lettered objects in the editor, using explicit IDs, polygons, source paths, transforms, clipping and layers. The user then adjusts and saves the working project.
8. Export locally from original assets at the agreed resolution; report actual dimensions and quality warnings. After user confirmation, archive unchanged bytes and update page records.

## Local revisions

Use the newest user-saved project or live snapshot. Replace only the selected object, preserving other geometry, source references, transforms, clipping, layers and export settings. For changed asset dimensions choose preserve/refit explicitly. User-retouched accepted files supersede AI candidates. Whole-page Image lettering is an explicitly chosen legacy branch, not the default after local assembly.

Record file path, dimensions, hash, role and version instead of image Base64. Inspect references when needed; the user judges aesthetic acceptance. Repeated anatomy failures warrant a concrete alternative or manual repair, not unbounded retries.

Installing this skill, switching existing tools, choosing a source license or publishing to GitHub requires separate user authorization.

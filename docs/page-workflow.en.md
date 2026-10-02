# Single-page comic workflow

[English](page-workflow.en.md) | [中文](page-workflow.md)

Creators discuss and draw a page in stages with Codex, then assemble it in the local editor. New drawing requires available image-generation tools. Editing existing assets can be done independently.

| Stage | Decisions | Saved output |
| --- | --- | --- |
| Dialogue | Page purpose, exact text, speakers, emotion, reading order, bubbles and narration | Approved text |
| Visual design | Shots, positions, gaze, acting, hands, clothing, background and text space | Visual guide |
| Drafts | Independent layout candidates; compare framework and performances | Original drafts and numbered overview |
| Selection | Choose framework and each panel's content separately; identify repairs | Framework, source map and selected crops |
| Finished panels | Redraw selected crops independently, retaining complete canvases and adjustment margins | Original panel images without text or bubbles |
| Lettered objects | Generate contour, tail and final words together; split independently movable groups | PNGs and an object list |
| Assembly | Agent initializes framing, placement, text scale and occlusion; creator fine-tunes | Initial and manually adjusted projects |
| Output | Render from full assets at the agreed resolution | PNG, actual dimensions, provenance and archive |

## Describe specific changes

Provide dialogue IDs, exact replacement strings and emotional intent. Describe shot distance and direction, left/right positions, gaze, expression, action and text space. For interacting hands, identify whose left/right hand is involved, sleeve and wrist entry, contact and overlap.

Draft production can still revise text and layout. Select a framework version and a separate candidate ID for each panel. Keep a source map with the original draft and crop bounds so a framework choice retains your favorite acting from other drafts.

## Retain approved acting

Use each selected crop to anchor pose and expression. Use approved finished art for character and style continuity. Give the canvas adjustment margins while retaining the intended shot distance. Long panels require wide compositions. Keep complete originals; content hidden by polygon masks remains available for reframing.

Prioritize recognition and expressive acting, then hands, clothing and continuity, with background density guided by the visual contract. Repeated anatomy errors can be addressed through clearer action references or manual repair. Preserve creator-approved corrections.

## Keep lettering independently adjustable

Build a text-object list first: each approved string appears once, with the correct speaker, form and tail. Connected bubbles can move together; independently adjusted groups are separate assets. Captions, unboxed words and sound effects can also use object layers.

Keep ordinary text at a consistent optical size. At selected emotional peaks, use weight, stroke gesture, rhythm and overall tilt. Generate transparent exteriors with intact white bubble interiors and clear words. Review punctuation, contours, tails and transparency with the creator.

## Fine-tune and archive

Load full panel images and accepted lettered objects. Initialize placement and occlusion, then let the creator adjust directly on the canvas. Later replacements use the newest manual project and change only the specified asset, preserving unrelated framing, clipping, layers and export settings.

Export reads original assets. Record actual PNG dimensions and quality warnings, and retain the matching project. A manually retouched final image may differ from the project render; record that accurately. Archive unchanged accepted bytes and compare SHA-256.

Continue clothing, props, environment and emotion into the next page. Character introductions, color pages and localized updates follow their approved project-specific routes.

[Codex setup](codex-setup.en.md) · [Agent guide](agent-workflows.en.md) · [Quick start](quick-start.en.md)

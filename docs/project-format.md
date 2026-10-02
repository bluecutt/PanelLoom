# Portable projects

## Optional 0.2 metadata

Root `objectOrder` can contain plural keys `panels` and `balloons`; each enabled array contains all live IDs exactly once, last is front. Missing type arrays retain the original numeric/stable ordering below. Each balloon may contain `panelOcclusion` mapping live panel ID to `front` or `back`; inherit removes the mapping. Clipping remains independent. Rename/remove operations repair references atomically, respecting locked dependents. Unknown extension fields round-trip, but opening/saving in older editors does not promise preservation or rendering of new ordering/occlusion semantics. Keep the newer working project and a legacy copy separate.

Projects retain ComicPanelEditorProject version 2, complete original source files, independent polygons, initial/reset fields and unknown fields. Save-as rebases assetBase; it does not crop or re-encode images.

Initialization captures missing initial polygons and transforms. Raw read-only loading/saving does not invent fields: an old object lacking reset anchors captures its current geometry/transform on its first geometry edit. This freezes its image fitting rectangle before a frame move, preventing doubled translation, and makes reset usable without changing untouched objects. Existing reset anchors are preserved.

Panels paint by ascending zIndex (default 0), then readingOrder (default 0), then stable array order. Balloons paint by ascending zIndex (default 1000), then current-culture case-insensitive ID. Hit-testing reverses the same ordering, while selected editing handles retain priority. Reading order is independently editable for panels in the inspector or Agent API. Independent panel geometry remains independent; SingleLine does not enforce shared vertices.

`ComicEditor.Cli.exe bundle --project project.json --out-dir NEW_DIRECTORY` copies only explicitly referenced images into SHA256-addressed assets, deduplicating identical bytes rather than names. project.json and assets-manifest.json use relative paths inside the package. A sibling staging directory is promoted only after metadata, image decode and hashes pass. Existing directories are never merged. Failure removes only the newly owned staging folder.

Moving the entire bundle keeps its source mapping usable. Unknown custom metadata remains in project.json: review that metadata yourself before public release; bundle is not a personal-information scrubber. It never recursively copies your source folders or includes application settings.

# Local Agent sessions

0.2 adds per-operation `data.outcomes` to apply/dry-run. No-change operations keep the actual live revision; dry-run never advances it. A successful dispatch is not evidence that a snap moved: inspect Moved/AlreadyAligned/NoCandidate/Unsafe and its target data. Adjacent sorting and relative inset occlusion use the same Core registry as offline CLI and GUI. Existing same-user authorization, Busy, hash/revision and request-ID checks are unchanged.

The GUI registers a random session under user state/Sessions (or portable Data/Sessions). Both ends use Windows CurrentUserOnly named pipes on the local machine, not TCP, HTTP, or remote listeners. `session list` reads current-user registrations; `--registry-dir` explicitly selects another portable instance's Sessions folder. No arbitrary code evaluation exists.

`session snapshot --session UUID` returns live unsaved JSON, revision and authoritative hash. Apply patches use this hash (not the on-disk file hash) plus baseRevision. User changes, undo, redo, open, restore and save advance the revision. A stale patch is refused, never reloaded over user work. Writes start disabled and require the GUI checkbox; the Agent cannot enable it through the API. Pending property input, modal dialogs, drags and export are Busy boundaries, checked again on the WPF dispatcher.

`session apply --session UUID --patch patch.json [--dry-run]` commits all operations as one undo batch. Successful write receipts remain for the whole session: retrying identical request ID/payload returns the old receipt even after undo, without resurrecting the edit. Different payload with a used ID conflicts. Transport timeout is not part of the semantic payload. Capacity (10000 receipts / 128 MiB by default) rejects new writes without evicting old receipts. Non-committing failures and dry runs do not enter the write ledger.

`session save --session UUID --out NEW_FILE --revision N --base-hash HASH` is a separate explicit action. It checks live revision/hash and the original saved file's current hash. An existing different destination requires --if-output-hash; the currently open file uses the GUI's remembered disk hash. Local successful receipts never contain image bytes. A client timeout can leave the remote outcome unknown: fetch snapshot or retry the identical ID, rather than assuming rollback.

The CLI resolves --out against its own caller working directory before sending an absolute savePath over the pipe. A GUI launched from a different directory therefore cannot reinterpret the Agent's relative destination.

Same-user transport and actual GUI/CLI end-to-end are regression-tested. A separate-account/elevation-boundary deployment test remains part of release security acceptance; no system accounts or security policies are changed for tests.

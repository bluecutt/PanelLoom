using System.Text.Json.Nodes;
namespace ComicEditor.Cli;
public static class CliHelp
{
    public static JsonObject Data()=>new()
    {
        ["format"]="API1 JSON envelope; --help and help return this object without modifying a project",
        ["globalOptions"]="--request-id UUID --report NEW_JSON_PATH --timeout SECONDS (default 30)",
        ["usage"]=JsonOutput.Node(new[]{
            "capabilities | help | --help",
            "inspect --project PROJECT.json",
            "validate --project PROJECT.json",
            "init --manifest MANIFEST.json --out NEW_PROJECT.json",
            "apply --project PROJECT.json --patch PATCH.json [--dry-run] [--out OUTPUT.json --overwrite --if-output-hash SHA256]",
            "render --project PROJECT.json --out PAGE.png [--scale NUMBER --object UNIQUE_OBJECT_ID --padding NUMBER --memory-limit-mib NUMBER --overwrite --if-output-hash SHA256]",
            "bundle --project PROJECT.json --out-dir NEW_DIRECTORY",
            "open --project PROJECT.json [--portable-data]",
            "session list [--registry-dir DIRECTORY --portable-data]",
            "session status|snapshot --session UUID [--registry-dir DIRECTORY --portable-data]",
            "session apply --session UUID --patch PATCH.json [--dry-run --registry-dir DIRECTORY --portable-data]",
            "session save --session UUID --revision INTEGER --base-hash SHA256 --out OUTPUT.json [--if-output-hash SHA256 --registry-dir DIRECTORY --portable-data]"
        }),
        ["notes"]="Paths are resolved relative to the caller; existing outputs require matching hash and explicit overwrite as documented. GUI live writes additionally require user authorization. One patch is one undo; a no-op does not increment the live revision. readingOrder in object.layer is panel-only. Query capabilities for object.reorder, balloon.panelOcclusion and panel.snap; data.outcomes reports Moved/AlreadyAligned/NoCandidate/Unsafe/NoChange, not merely successful dispatch."
    };
}

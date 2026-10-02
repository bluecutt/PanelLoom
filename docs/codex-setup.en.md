# Use the workflow in Codex

[English](codex-setup.en.md) | [中文](codex-setup.md)

The package contains the complete `skills/comic-page-workflow` folder. Keep its references, templates and example assets together. The editor can run independently of Codex; generating new artwork requires image-generation tools available in your Codex environment.

## 1. Prepare your project

Extract the entire application folder. For the relative configuration below, place that folder at `tools/ComicEditor` inside your own creative project. Alternatively, configure the CLI's actual location. Create your own story, character and visual contracts.

```text
my-comic/
  .agents/skills/comic-page-workflow/
    SKILL.md
    references/  templates/  assets/
  comic-project.json
  contracts/story.md
  contracts/characters.md
  contracts/visual.md
  pages/
  tools/ComicEditor/ComicEditor.Cli.exe
  tools/ComicEditor/ComicEditor.exe
  tools/ComicEditor/...other application files
```

Copy the supplied Skill folder to `.agents/skills/comic-page-workflow`. Open this project in Codex. Codex discovers repository Skills under `.agents/skills`; restart Codex if a new Skill is not visible. See [the official Skill documentation](https://learn.chatgpt.com/docs/build-skills).

## 2. Set the project configuration

Copy `skills/comic-page-workflow/assets/project-config-example.json` to your project root as `comic-project.json`. Its default content is:

```json
{
  "editorCliPath": "tools/ComicEditor/ComicEditor.Cli.exe",
  "contracts": {
    "story": "contracts/story.md",
    "characters": "contracts/characters.md",
    "visual": "contracts/visual.md",
    "pages": "pages"
  }
}
```

`editorCliPath` resolves relative to this configuration file. Contract paths locate your own project requirements. Different creative projects can use different contracts and editor locations. Keep the complete runtime folder beside the executables.

## 3. Start a page

Select the Skill in Codex, or invoke `$comic-page-workflow` explicitly where Skill mentions are supported. For example:

> Use $comic-page-workflow. Read comic-project.json and my story, character and visual contracts. Prepare a dialogue draft for this page, listing the speaker, exact wording, emotion, reading order and bubble or caption form. Wait for my approval before designing shots.

The workflow progresses through dialogue, visual design, independent layout drafts, separate framework and panel selection, finished panel redraws, independent lettered objects, assembly and export. Confirm each stage with the creator. The creator adjusts framing and lettering in the editor and approves the final result.

## 4. Verify the editor connection

From your project directory, run:

```powershell
& '.\tools\ComicEditor\ComicEditor.Cli.exe' capabilities
& '.\tools\ComicEditor\ComicEditor.Cli.exe' validate --project '.\tools\ComicEditor\examples\minimal-page\project.json'
```

Successful JSON receipts should identify API version 1 and project version 2. Ask the Agent to read the Skill's native editor adapter before initialization or changes. The [Agent guide](agent-workflows.en.md) describes file-based and live-session operations.

If the Skill is missing, check the folder name, `SKILL.md` and the opened project directory. Avoid duplicate Skills with the same name. If the CLI fails, check its configured path and retained runtime files. Image generation availability is separate from the local editor connection. No model keys or accounts are shipped in this package.

[Quick start](quick-start.en.md) · [Page workflow](page-workflow.en.md) · [Agent guide](agent-workflows.en.md) · [Another Windows trial](new-windows-trial.en.md)

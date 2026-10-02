# Contributing

[English](CONTRIBUTING.en.md) | [中文](CONTRIBUTING.md)

We welcome reproducible bug reports, usability feedback, tests and improvements. Describe the creator's intended action, the observed result and the expected result. Include a minimal neutral project, Windows version, application version and reproduction steps. Use your own redistributable assets and omit private artwork or account information.

Before changing code, read the [Agent API](docs/agent-api.md) and [build instructions](docs/build-and-release.md), currently in Chinese. Preserve GUI/Agent feature parity, version 2 project compatibility and live write authorization. For a behavior fix, reproduce it with a failing test, implement the change and record a passing result. Public tests use neutral assets.

Contributions use **AGPL-3.0-only**. Submit work you are authorized to license under these terms. Retain third-party component notices. Case practice artwork has separate asset terms and is handled separately from software contributions.

A pull request should explain affected actions, test commands and results, compatibility impact and outstanding manual checks. For interface changes, attach screenshots of a neutral project. Describe human and AI participation accurately; the contributor remains responsible for checking the submitted work.

Useful starting points include the [English quick start](docs/quick-start.en.md), [Agent guide](docs/agent-workflows.en.md), `docs/function-coverage.csv` and the recipient [Windows trial](docs/new-windows-trial.en.md). Existing executables retain their `ComicEditor` names for compatibility; the product name is PanelLoom.

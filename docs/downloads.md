# 下载与练习

[中文](downloads.md) | [English](downloads.en.md)

打开 [0.2.0-preview.4 发行页](https://github.com/bluecutt/PanelLoom/releases/tag/v0.2.0-preview.4)，下载所需附件。所有介绍、教程、教学图片、源码、Skill 和下载入口均通过 GitHub 提供。

| 附件 | 用途 |
| --- | --- |
| [Windows x64 免安装 ZIP](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-0.2.0-preview.4-docs.3-win-x64.zip) | 解压后运行 `ComicEditor.exe`，保留整个目录及配套文件 |
| [对应版本源码 ZIP](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-source-0.2.0-preview.4-docs.3.zip) | 阅读代码、构建和参与开发；同时遵守软件与素材许可 |
| [完整练习包 v10](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/PanelLoom-case-practice-v10.zip) | P09/P10 原图、文字对象、起始与参考工程、完整中英教程 |
| [SHA-256 清单](https://github.com/bluecutt/PanelLoom/releases/download/v0.2.0-preview.4/SHA256SUMS.txt) | 核对版本、文件完整性与下载来源 |

下载并解压应用后，可以直接打开包内 `case-studies/P10/projects/reference.json`。单独下载练习包时，打开其中的 `P10/projects/reference.json`。使用“项目 → 另存为”建立工作副本。`reference.json` 为已确认排版，`start.json` 为历史初始化排版。操作步骤见[案例快速操作](../case-studies/guide/quick-start.md)。

手动调整已有素材无需安装 Codex、SDK 或模型接口。通过 Codex 开始新创作时，按[Skill 安装说明](codex-setup.md)配置自己的项目与契约。

开始练习前阅读[素材说明](../ASSET-TERMS.md)。软件使用 AGPL-3.0-only，案例素材遵守其独立条款与 GitHub 平台权限。

应用版本为 `0.2.0-preview.4`，包装订正版为 `docs.3`。这次订正仅接入发布链接及文档，已验收的 EXE、DLL 与工程布局保持不变。

使用 PowerShell 对下载文件运行 `Get-FileHash -Algorithm SHA256 -LiteralPath '文件的完整路径'`，将结果与同一发行页的 `SHA256SUMS.txt` 对照。首次运行可能出现未签名应用提示，请先核实下载来源和文件完整性。

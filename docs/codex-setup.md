# 在 Codex 中使用工作流

[English](codex-setup.en.md) | [中文](codex-setup.md)

发行包包含 `skills/comic-page-workflow`。将整个目录复制到自己的创作项目下的 `.agents/skills/comic-page-workflow`，Codex 可从项目目录发现 Skill。显式输入 `$comic-page-workflow` 开始使用；如果更新后尚未出现，重启 Codex。位置与发现规则见 [官方 Skill 文档](https://learn.chatgpt.com/docs/build-skills)。

准备自己的剧情、角色基准、画风约定和页面目录。参考 Skill 的 `assets/project-config-example.json`，填写本机编辑器 CLI 路径与项目文档路径。多个创作项目可以分别提供配置。

> 请使用 $comic-page-workflow，读取我的项目配置与角色画风约定。先为当前页整理台词草稿，注明说话人、气泡形态、情绪和阅读顺序。我确认后再设计画面。

Skill 引导台词、视觉设计、结构草稿、选格、逐格重绘、独立带字对象与本地组装。每次进入下一阶段先取得创作者确认。最终人工调整通过编辑器保存与导出。

应用包可单独运行。安装 Skill 是读者在自己的 Codex 环境中进行的操作；本项目的本地候选整理保持现有个人安装版不变。

[快速上手](quick-start.md) · [完整流程](page-workflow.md) · [Agent 入口](agent-workflows.md)

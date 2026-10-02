# 格织 · PanelLoom

[English](README.md) | [中文](README.zh-CN.md)

漫画创作工作流与手动组装器。

| 页面示例一 · P09 | 页面示例二 · P10 |
| --- | --- |
| [![P09 成品漫画](case-studies/P09/reference-output/historical-final.png)](case-studies/P09/reference-output/historical-final.png) | [![P10 成品漫画](case-studies/P10/reference-output/editor-reference.png)](case-studies/P10/reference-output/editor-reference.png) |

面向 Codex 的单页漫画工作流，配套 Windows x64 原生手动组装器和可独立使用的 Skill。创作者与 ChatGPT 讨论台词和画面，通过结构草稿选定框架与表演，逐格绘制画面、生成独立带字气泡，最后在编辑器中取景和排版。

创作者决定故事、选稿和最终效果；ChatGPT 整理设计约定、辅助生成素材、初始化工程并检查资源。点击上方预览可以查看完整图片，P10 使用当前确认的原生编辑器排版。

[下载与练习](docs/downloads.md) · [快速上手](docs/quick-start.md) · [P10 完整图文教程](case-studies/P10/guide/full-workflow.md) · [Codex 安装](docs/codex-setup.md) · [Agent 操作](docs/agent-workflows.md)

## 统一 → 拆卸 → 再统一

整页结构草稿先统一阅读路线、镜头和文字空间。选稿时分别选择页面框架与各格表演，再将选定裁片逐格绘制成完整正式图，带字气泡也独立生成。最后在本地组装器中取景、排版并高清导出。

这让重试落在单个对象上：手部或表情需要修改时，替换对应分镜；台词或字风需要调整时，替换对应气泡。已经认可的素材可以继续保留。减少整页反复生成，有助于避免文字和细节修改带来的累积模糊、噪点及无关区域漂移。

可替换素材和可撤销排版提高了试错容错率。创作者通过逐格挑选、打磨和人工精调掌握最终画面质量，导出时继续读取完整原图。

## 安装与开始使用

### 使用 Windows 编辑器

准备 Windows x64 电脑。已有素材的手动排版可直接使用免安装应用，无需安装 Codex、.NET SDK 或模型接口。

1. 正式发布后，从本仓库 **Releases** 下载 Windows x64 免安装 ZIP 和完整练习包。文件选择及校验方法见[下载说明](docs/downloads.md)。
2. 将应用 ZIP 完整解压到自己的工具目录，保留 EXE、DLL 和其他配套文件，双击 `ComicEditor.exe`。
3. 解压练习包，在应用中点击“打开工程”，选择 `P10/projects/reference.json`，查看已完成排版的示例。
4. 使用“项目 → 另存为”创建自己的工作副本，再拖动图片、调整气泡并高清导出。具体操作见[快速上手](docs/quick-start.md)。

### 在 Codex 中开始新创作

新创作需要当前 Codex 环境具备图像生成能力。先准备自己的创作项目目录，以及剧情、角色基准和画风约定。

1. 从下载的 PanelLoom 目录取出整个 `skills/comic-page-workflow` 文件夹，放入自己创作项目的 `.agents/skills/comic-page-workflow`。已有同名 Skill 时，先比较并确认更新方式，保留原有版本。
2. 参考[项目配置示例](skills/comic-page-workflow/assets/project-config-example.json)，在创作项目根目录准备 `comic-project.json`。填写 `editorCliPath`，使其指向本机 `ComicEditor.Cli.exe`，并填写剧情、角色、画风文档和页面目录路径。
3. 在 Codex 中打开该创作项目，输入 `$comic-page-workflow`。如果 Skill 尚未出现，重启 Codex 后检查。项目级位置与发现方式见[官方 Skill 文档](https://learn.chatgpt.com/docs/build-skills)。
4. 从台词讨论开始，确认后再进入画面设计、草稿、选格和素材生成。完整设置见[Codex 安装说明](docs/codex-setup.md)。

可以先把这段话交给 Agent 协助设置：

> 请从我已下载的 PanelLoom 目录，将 skills/comic-page-workflow 安装到当前创作项目的 .agents/skills/comic-page-workflow。若目标已存在，请先比较并询问是否更新。读取项目配置示例，协助我准备 comic-project.json，核对编辑器 CLI 与契约路径，并确认当前环境是否具备图像生成能力。

设置完成后，使用这段话开始第一页：

> 请使用 $comic-page-workflow，读取我的项目配置、剧情和角色画风约定。先整理当前页的台词初稿，标明说话人、气泡类型、阅读顺序和情绪。我确认后再设计画面。

## 从台词走到页面

| 步骤 | 本步确定什么 | 保存什么 |
| --- | --- | --- |
| 台词讨论 | 精确文本、说话人、情绪和容器 | 文字设计 |
| 画面设计 | 机位、站位、表情、动作和留白 | 画面说明 |
| 草稿探索 | 页面比例、阅读路线和表演候选 | 独立草稿 |
| 编号选稿 | 框架来源与各格内容来源 | 框架和逐格来源表 |
| 正式绘画 | 按选定草稿逐格细化，保留取景余量 | 完整分镜原图 |
| 带字对象 | 气泡轮廓、尾巴与最终文字一起完成 | 可独立移动的文字对象 |
| 本地装配 | Agent 初始化，创作者在画布精调 | 工作工程与高清输出 |

[P10 完整图文教程](case-studies/P10/guide/full-workflow.md)用真实对话和产物说明这些步骤。[分镜设计样章](case-studies/P10/guide/storyboard-design.md)展示编号总览、框架与内容的独立选择，以及六格线稿与正式原图对照。[带字气泡样式](case-studies/P10/guide/lettering-styles.md)解释提示词怎样控制轮廓、尾巴与字体。[通用单页流程](docs/page-workflow.md)提供可用于自己作品的步骤清单。

## 直接编辑画面

双击发行文件夹中的 `ComicEditor.exe`，打开 v2 JSON 工程。保留整个应用目录；免安装包包含运行时，使用时无需 .NET SDK、模型接口或账号。

画布普通滚轮缩放选中的图片或气泡，Ctrl＋滚轮缩放整页，侧栏滚轮只滚动。独立多边形可以调大小、顶点、形状和取景。气泡支持完整带字 PNG、主体与尾巴图层、旋转、透明度、限框或跨格，以及相对小格的前后遮挡。单线边界保持分镜形状独立，导出从原素材重新渲染。

界面当前使用中文。[英文快速上手](docs/quick-start.en.md)提供实际按钮名的英文对照。本轮只新增双语文档，软件界面的英文切换另行安排。

## 亲手试一次

正式发布后，从本仓库 Releases 下载 Windows 应用与完整练习包；[下载说明](docs/downloads.md)列出附件和打开步骤。P10 提供 6 个分镜和 10 个带字对象；P09 提供 9 个分镜和 13 个历史空白气泡。先阅读[案例快速操作](case-studies/guide/quick-start.md)，打开解压后练习包中的 `P10/projects/reference.json`，另存工作副本再调整。`start.json` 保留历史初始化布局，便于对照。

图文教程及教学图片随本仓库保存，完整练习 ZIP 作为 Releases 附件提供。新页面使用自己的故事、角色和画风约定。

## 与 Agent 配合

旁边的 `ComicEditor.Cli.exe` 提供能力查询、初始化、校验、素材替换、渲染与工程迁移。正在打开的窗口支持同用户本机会话；创作者勾选“允许 Agent 修改本次会话”后，Agent 可以在当前人工状态上修改指定对象，一批操作可整体撤销。

通用 Skill 位于 [skills/comic-page-workflow](skills/comic-page-workflow/SKILL.md)。它通过项目自己的 `comic-project.json` 查找剧情、角色和画风契约。示例、记录模板和阶段参考随 Skill 保存。

## 版本与开发

当前本地发行候选使用 `0.2.0-preview.4`，功能基于已验收的 `0.2.0-preview.2`；本轮整理完整工作流、教程、许可和打包内容。源码构建见 [构建与发行](docs/build-and-release.md)，操作验收见 [验收清单](docs/acceptance-checklist.md)。

软件、通用 Skill、模板和通用文档采用 **AGPL-3.0-only**。嵌入的漫画与案例素材适用独立的[素材使用说明](ASSET-TERMS.md)，bluecutt 保留依法享有的创作权益。[许可范围](LICENSE-SCOPE.md)、[完整软件许可](LICENSE)与[素材说明](docs/licensing.md)分别列出授权范围。

Windows 首次运行可能提示未签名应用。先核验下载来源和文件校验值。2026-10-02 已完成另一台 Windows x64 的试用验收：自动检查 10 项、人工检查 15 项通过。更广泛的 Windows 兼容性继续通过后续测试确认。

程序二进制仍为 `0.2.0-preview.4`。本候选整理首页、图文教程和素材授权；当前为本地发布准备状态，尚未建立远端仓库或上传 Releases 附件。

[参与开发](CONTRIBUTING.md) · [创作者清单](CREDITS.md)

作者：bluecutt。AI 协作助手：ChatGPT。第三方既有角色权益由相应权利人保留。

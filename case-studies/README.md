# 格织 · PanelLoom 案例练习包

[中文](README.md) | [English](README.en.md)

发布准备候选 v09。案例包面向所有人下载，当前已审核教学图片允许官方公开展示。bluecutt 保留其依法享有的漫画和图片权益；本地练习与 GitHub 平台允许的使用按素材说明分别处理。

软件与完整练习包在 PanelLoom GitHub 仓库的 Releases 中提供，图文教程也可直接在该仓库阅读。

P10 完整流程、分镜设计、气泡样式、文字对象清单和提示词附录均有英文版，历史对话附中文原文与英文译文，说明图增加英文标签。介绍、快速操作与素材说明继续提供双语入口。P10 的参考工程保留已确认的新原生排版及 2× 输出；起始工程单独保留历史初始化布局。

漫画创作工作流与手动组装器。

[成品与产品介绍](guide/product-introduction.md) · [快速操作](guide/quick-start.md) · [P10 完整创作教程](P10/guide/full-workflow.md) · [P09 进阶经验](P09/guide/advanced-workflow.md)

[分镜编号与逐格对照](P10/guide/storyboard-design.md) · [气泡提示词与样式](P10/guide/lettering-styles.md)

本包提供原始分镜、文字对象、草稿参考、可编辑工程和新版参考输出。现成素材练习使用 Windows x64 免安装编辑器，无需 SDK、图像模型或作者账号。配套工程为 ComicPanelEditorProject v2，使用支持 API1 的原生编辑器。

开始前阅读[素材使用说明](ASSET-TERMS.md)。先打开 `P10/projects/reference.json`，另存到 work/ 后进行本地调整。

| 案例 | 分镜 | 气泡/文字对象 | 历史正式成品 | 新版练习参考 |
| --- | --- | --- | --- | --- |
| P10 | 6 | 10 个带字对象 | 2560×3840 | 2048×3072，单线边界 |
| P09 | 9 | 13 个空白气泡 | 1024×1536 | 2048×3072，单线边界 |

`projects/reference.json` 是当前确认的完整演示布局；`projects/start.json` 保留历史初始化布局，适合比较 Agent 装载与人工精调的变化。二者作为只读基准，工作文件另存到同页 `work/`。原图放在 `assets/`，通过多边形遮罩显示；取景之外的内容仍然保留。

`reference-output/historical-final.png` 是历史正式成品的原字节副本；`editor-reference.png` 由新版参考工程渲染。历史人工调整与修图可能未全部写入已保存工程，因此分别保存两类参考。质量提示保存在 `quality.json`。

P10 的台词、画面、选格、重绘、气泡拆分与初始化章节展示实际历史对话节选。实际模型调用与归档提示词分别标注。正文聚焦制作方法，原始日志、账户信息与第三方参考图保留在作者本地。

作者：bluecutt。AI 协作助手：ChatGPT。图文教程与教学图片纳入 GitHub 仓库，完整练习包通过同仓库 Releases 提供；平台内容权限已获作者确认，具体条件见素材说明。当前尚未对外发布。

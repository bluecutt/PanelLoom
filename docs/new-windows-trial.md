# 另一台 Windows 试用清单

[English](new-windows-trial.en.md) | [中文](new-windows-trial.md)

当前停在这一发布门槛。本机解压验收另有记录；下表需要在另一台 Windows x64 电脑上实际填写。

## 准备

记录 Windows 版本/构建号、x64 架构、测试日期、ZIP 文件名及 SHA-256。先与交付清单核对，再将整个应用文件夹解压到可写目录，目录名可包含中文和空格。保留 EXE 旁的全部配套文件。自包含应用应在无 .NET SDK 的环境下运行；源码构建另行测试。

应用暂未签名。若系统安全提示阻止启动，保留安全软件，记录提示和目标文件，核对来源。未能启动的情况应记为待解决，不计作通过。这轮测试不要求关闭防护或添加全盘排除项。

## 必测人工操作

先打开 `examples/minimal-page/project.json`，工作成果另存到独立测试目录。案例包按附带使用条款仅在本地使用。

| 项目 | 预期结果 | 实测与证据 |
| --- | --- | --- |
| 启动 | `ComicEditor.exe` 创建可见、响应正常的窗口 | 待测 |
| 打开中性工程 | 原图和气泡完整加载 | 待测 |
| 选择对象 | 点击分镜/气泡模块切换到相应操作对象 | 待测 |
| 调整取景 | 拖动实时刷新；画布滚轮缩放选中图片 | 待测 |
| 查看整页 | Ctrl + 滚轮缩放整页；侧栏滚轮只滚动侧栏 | 待测 |
| 编辑框架 | 顶点/边修改仅作用于目标多边形，可撤销 | 待测 |
| 气泡变换 | 拖动、大小、旋转、不透明度作用于选中气泡 | 待测 |
| 限框与跨格 | 限框隐藏超出部分，整页自由允许跨格 | 待测 |
| 层级与小格遮挡 | 上移/下移一层、目标小格前后遮挡和撤销有效 | 待测 |
| 单线与吸附 | 下层气泡不细化边线；吸附回执准确区分各类结果 | 待测 |
| 属性控件 | 应用后折叠区保持状态；调色盘和固定倍率可用 | 待测 |
| 另存与重开 | 新 JSON 保持取景、框架、气泡、范围、层级 | 待测 |
| 导出 PNG | 中性页 2× 实际为 1280 × 1800，检查尺寸与清晰度 | 待测 |

中性示例只有一格和一个气泡。多格/嵌入小格测试可用自己的素材建立一次性测试工程，或在本地使用私有练习案例。另存工作副本，保留参考工程。

## 必测 CLI

在应用文件夹打开 PowerShell，先新建 `trial-output` 目录。重试时改用尚未存在的输出文件名。

```powershell
& '.\ComicEditor.Cli.exe' capabilities
& '.\ComicEditor.Cli.exe' validate --project '.\examples\minimal-page\project.json'
& '.\ComicEditor.Cli.exe' inspect --project '.\examples\minimal-page\project.json'
& '.\ComicEditor.Cli.exe' render --project '.\examples\minimal-page\project.json' --out '.\trial-output\neutral-2x.png' --scale 2
```

成功回执应为 API1/工程 v2，包含 `object.reorder`、`balloon.panelOcclusion`、`panel.snap`，导出尺寸 1280 × 1800。失败时记录退出码和原始 JSON 回执。

## 可选 Codex / Agent 检查

按[安装说明](codex-setup.md)在一次性创作项目中安装 Skill。检查发现与中性素材初始化，无须生成图片。实时会话只操作测试窗口：可读快照、未授权拒绝写入、授权后整批修改与一次撤销、取消授权后立即拒绝；旧 revision/hash 和 Busy 应拒绝不安全修改。详见[Agent 说明](agent-workflows.md)。

这些项目依赖测试者的 Codex 环境。跳过的项目独立记录；CLI 可运行不能代替 Skill 发现和图像生成能力的实测。

## 结果填写

请填写：测试者、日期、Windows 构建号/架构、应用版本 `0.2.0-preview.4`、包修订号 `docs.1`、ZIP 哈希、解压目录类型、是否预装 .NET SDK、每项“通过/失败/未测”及错误原文。对外反馈使用中性工程截图或复现文件。案例原图、截图和练习输出保留在本地。

当前状态：**PENDING_OTHER_WINDOWS_TRIAL**。开发电脑上的本机测试不关闭这一门槛。

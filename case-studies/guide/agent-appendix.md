# Agent 操作附录

[English](agent-appendix.en.md) | [中文](agent-appendix.md)

PanelLoom 为 Agent 保留离线 CLI、工程 JSON 和本机实时会话入口。以下操作使用正式应用里的 `ComicEditor.Cli.exe`。先查询 capabilities，按当前返回的协议填写请求。

```powershell
$cli = 'D:\PanelLoom\ComicEditor.Cli.exe'
$caseRoot = 'D:\PanelLoom-cases'
& $cli capabilities
& $cli inspect --project "$caseRoot\P10\projects\start.json"
& $cli validate --project "$caseRoot\P10\projects\start.json"
```

以上是可替换的读者路径。检查实际素材尺寸、对象数量和范围，再建立独立工作工程。

## 从已确认的布局初始化

init 的 manifest 含 apiVersion、完整 project 和 assetMap。每个对象使用 kind、id、sourceImage；只引用用户明确批准的素材。原图保持完整，分镜多边形负责取景。相对路径从 manifest 的 assetBase 解析。

```powershell
& $cli init --manifest manifest.json --out working.json
& $cli inspect --project working.json
& $cli validate --project working.json
& $cli render --project working.json --out practice.png --scale 2.5
```

编辑器应用中附带 schemas 和中性示例。用自己的创作素材建立 manifest 时，先按照这些文件检查结构。

## 修改、干跑与回执

inspect 返回当前工程 hash。patch 使用 apiVersion 1、全新的 UUID requestId、当前 baseProjectHash 和注册动作。先执行 dry-run，再输出到新工程。

```powershell
& $cli apply --project working.json --patch patch.json --out working-next.json --dry-run
& $cli apply --project working.json --patch patch.json --out working-next.json
```

气泡范围由 balloon.clip 管理；相对小格遮挡由 balloon.panelOcclusion 管理。需要自动将当前气泡放到最近合法层级时，明确传入 autoOrder:true。先检查 capabilities 是否支持此参数。读回 data.outcomes，确认实际调整与警告。

## 正在打开的人工工程

依次执行 session list、session snapshot，在当前未保存状态上构建请求。GUI 中的“允许 Agent 修改本次会话”由用户开启；默认只读。Agent 使用最新 revision/hash，先检查干跑结果，再整批修改。遇到 Busy 或结果未知时重新读取状态，保持相同请求的幂等重试。

保存是独立操作，人工布局确认前保留工作副本。所有图像通过本地路径传递，CLI 返回 JSON 回执。

本练习包中的 case 图像仅允许本地非破坏性排版。新的绘画、AI 重绘与公开输出使用创作者自己的素材。[使用说明](../ASSET-TERMS.md)。

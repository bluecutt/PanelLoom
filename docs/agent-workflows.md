# Agent 与人工协作

[English](agent-workflows.en.md) | [中文](agent-workflows.md)

入口是发行文件夹中的 `ComicEditor.Cli.exe`，不要只拷贝 EXE。`capabilities` 是动作和协议的权威清单；API v1，工程版本 2。所有工程修改使用同一套注册动作，GUI 对应入口见 function-coverage.csv。

0.2 的 `object.reorder` 是真实相邻一步；`balloon.panelOcclusion` 指定气泡与目标分镜的前后关系，不改变 `clipPanelId`。从 0.2.0-preview.2 起，添加 `autoOrder:true` 可在需要时自动把当前气泡移到最近的合法层级，保留其他对象相互顺序，不改变图片、气泡位置或裁切。GUI 默认启用。Agent 先查 capabilities，再明确传入该参数；省略或 false 保留旧版严格拒绝行为。互相矛盾的遮挡设置仍整批拒绝，不删除规则来换取成功。

`panel.snap` 的实际结果在 `data.outcomes`，不要把 success=true 当作“已吸上”。遮挡回执返回 Placed / OrderAdjusted / NoChange，`layerDelta` 正数为上移，负数为下移，零为层级不变。自动调层和设置遮挡只占一次撤销。使用最新人工保存文件或实时快照；完整图片和带字气泡仍通过 init/asset.replace 导入，只替换指定素材，其他几何与文字对象不动。

## 离线初始化与输出

1. 明确用户批准的框架、原始分镜、独立带字气泡和初始定位，填写 init manifest；完整原图不先裁切。
2. `ComicEditor.Cli.exe init --manifest manifest.json --out working.json`。
3. `ComicEditor.Cli.exe inspect --project working.json`；检查素材尺寸、hash、ID 与位置。
4. `ComicEditor.Cli.exe open --project working.json`；启动回执是 LaunchRequested，不能声称用户已看见窗口。
5. 用户精调后另存工程。Agent 再 inspect，使用当前文件 SHA256 构建 patch；先 dry-run，再 apply 到新副本。
6. `ComicEditor.Cli.exe render --project final.json --out final.png --scale 2.5` 从原素材重新渲染。不要放大低清预览或重新生成整张漫画。
7. `bundle --project final.json --out-dir portable-page` 仅复制工程明确引用的素材。分发前另行审核工程元数据与素材权限。

## 正在打开的工程

`session list` → `session snapshot --session UUID` → 根据返回的 revision 和 hash 制作 patch → `session apply --session UUID --patch patch.json`。

GUI 默认只读；用户勾选“允许 Agent 修改本次会话”后才可写。修改在当前未保存状态上执行，不用磁盘旧工程覆盖。一次批量操作可以整批 Undo。拖动、模态窗口和未提交输入会返回 Busy；关闭写入授权即时生效。

保存是单独的 `session save --session UUID --revision N --base-hash HASH --out new.json`。正式文件外部变化会拒绝保存。相同 requestId 和内容重试返回原回执，不再次移动；不同内容复用 ID 会拒绝。SESSION_RESULT_UNKNOWN 表示通信中断后结果未知，应重试同一请求 ID 或重新 snapshot，不要假定回滚。

## 安全边界

只允许当前 Windows 用户的本机命名管道，没有网络监听、模型调用或代码执行端点。离线文件接口始终可用。所有素材通过文件路径传递，不把图片 Base64 塞进工程、请求或对话。原工程、旧工具和已安装的个人 skill 不自动切换。

## 验证记录

本轮公开源码候选的 Public 套件为 189/189；独立 Skill 示例通过 init、validate 与 1280×1800 render。其他历史验收记录保留在对应版本资料中。另一台干净 Windows 和独立账户边界的结果另行记录。

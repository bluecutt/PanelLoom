# 构建与便携发行

候选版本为 `0.2.0-preview.4`。功能代码沿用 `0.2.0-preview.2`，本轮整理许可、完整通用 Skill、教程入口与发行打包。各版本独立输出，不覆盖旧包。

## 本地环境

使用稳定 .NET 10 SDK；当前核查为 10.0.401，项目通过 global.json 选择 10.0 系列兼容特性版本。仓库脚本默认使用 `.tools/dotnet/dotnet.exe`。已有 SDK 可在当前构建进程设置 `COMIC_EDITOR_DOTNET_PATH` 为其绝对路径，脚本核验实际版本。该本机路径不写进源码配置或包。

`scripts/Prepare-Sdk.ps1 -Download` 是明确请求下载官方 SDK 的入口，校验 SHA512 后解压到本地 `.tools`，保持系统 PATH 不变。离线工作不调用该入口。已有运行时缓存放 `.tools/nuget-packages`，SDK/cache 均不进入发行物。

## 测试

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test.ps1 -Suite Public
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/packaging/Packaging.Tests.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/packaging/ReleaseContents.Tests.ps1 -IncludeSourceExport
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/CasePracticePackage.Tests.ps1 -CliPath dist/ComicEditor-0.2.0-preview.4-win-x64/ComicEditor.Cli.exe
```

公共测试使用中性 fixtures，并拒绝空套件。完整 All/UiAll 中部分回归需要本地私有 fixtures；缺失时应报告未验证。通用 Skill 内的中性示例可在独立复制后通过 init、validate 和 render 验证。

## 打包

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Publish.ps1 -Version 0.2.0-preview.4 -Offline
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Export-Source.ps1 -Version 0.2.0-preview.4
```

`-Offline` 仅使用已有 SDK 和运行时缓存，缺依赖即失败。GUI/CLI 分别自包含发布，合并前比较同名文件 hash。保留整个 win-x64 文件夹；无安装器、驱动、后台服务或管理员步骤。源码包同时提供相同版本的自有源码、构建脚本和通用资料。

发行包包含 README、LICENSE、LICENSE-SCOPE、ASSET-TERMS、CREDITS、CONTRIBUTING、docs、schemas、中性示例与 Skill。存在 `case-studies/` 时，共用复制器把完整教学案例、原图、工程和双语教程一起带入应用及源码包，并要求素材条款和清单齐全。没有案例目录的中性项目仍可单独打包。源码包额外包含 src、tests 和 scripts。排除 .tools、artifacts、bin/obj、用户 Data、私有 fixture 与原始会话。

`ReleaseContents.Tests.ps1` 验证真实文件复制、案例全目录字节一致、中性打包兼容和缺少案例许可时的拒绝行为；`-IncludeSourceExport` 同时执行真实源码导出。常规导出与 GitHub 集中候选使用相同的许可及案例内容规则。

许可收集读取实际 runtimeconfig 中的 includedFrameworks，从对应缓存包取得许可证。核心运行时及 WindowsDesktop 按版本保存到 licenses/，核心 notices 与 SDK 补充 notices 分别保留；runtime-notices-manifest.json 记录相对位置和 hash。AGPL 标准原文单独为 LICENSE。release-manifest.json 为发行文件提供 hash 与大小清单。

## 验收

`Scan-Public-Package.ps1 -Directory <导出的源码或应用目录>` 检查实际可分发目录。构建工作目录包含私有缓存与测试产物，不作为可公开目录。

`Test-Package.ps1 -PackageDirectory <应用目录>` 复制包到中文空格路径，禁用 PATH 的 dotnet 查找，验证实际 EXE、只读会话和 1280×1800 中性输出。它使用独立测试窗口，测试结束关闭；保持已有窗口不变。本机结果与另一台干净 Windows 的验收分别记录。

软件候选本地通过后，由作者确认仓库名、对应源码提供方式与发行渠道，再执行发布。许可见 [licensing.md](licensing.md)。

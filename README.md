# ApkShellext2-Plus

[English](#english) | [中文](#中文)

<a name="english"></a>
## English

A Windows Explorer shell extension that shows **real app icons** (instead of generic file icons) for:

- `.apk` — Android package
- `.ipa` / `.tipa` — iOS / TrollStore package
- `.hap` — HarmonyOS Ability Package
- `.appx` / `.appxbundle` — Windows 8.1/10 UWP package

**ApkShellext2-Plus** is an enhanced fork of [ApkShellext2](https://github.com/kkguo/apkshellext) (MIT), hardened for real-world packages that break the original build.

### What's enhanced over upstream

| Area | Enhancement |
|---|---|
| Formats | New `.tipa` (TrollStore IPA) and `.hap` (HarmonyOS) support with a dedicated HAP reader |
| Obfuscated APKs | Ordinal (culture-invariant) zip entry lookup — fixes wrong/missing icons on packages whose resource names use Arabic-combining characters (MT Manager / NP Manager style obfuscation) |
| Split APKs | Graceful fallback when `AndroidManifest.xml` / `resources.arsc` are absent (ABI/language splits) |
| Hardened IPA/APK | Zip local-header size mismatch tolerated (central directory wins); illegal path characters in entry names sanitized — previously the whole package failed to open |
| Multi-bundle IPA | Picks the shortest `.app` path (main bundle) instead of the first match (Watch / extensions) |
| Icon quality | Candidate icon resources tried best-quality-first; adaptive/gradient/objectAnimator XML parse failures no longer kill the icon |
| UI | Context menu "Copy package name" (batch, multi-line); InfoTip cleans empty lines / empty label rows |
| Rendering | Aspect-ratio-preserving bitmap scaling; overlay-icon null guard |
| Build | Retargeted to .NET Framework 4.8, protobuf dependency removed, satellite assemblies generated via Roslyn (no `al.exe` needed) |

### Features (inherited)

- Best-resolution app icon in Explorer (Windows Explorer only, due to .NET restriction)
- Customizable InfoTip for package information
- Batch renaming via context menu with patterns
- Show overlay icon per app type
- Multi-language UI (crowdin contributors)

### Build

Requires Visual Studio 2022 (or Build Tools) with .NET Framework 4.8 targeting pack:

```
MSBuild.exe ApkShellext2\ApkShellext2.csproj -p:Configuration=Release -restore
```

Output: `ApkShellext2.dll` + `libwebp_x64/x86.dll` + language folders. Install with `install.bat` (from `Resources/`), then restart Explorer.

### Credits

- [ApkShellext2 / apkshellext](https://github.com/kkguo/apkshellext) by Jingang Guo — the original project (MIT)
- [SharpShell](https://github.com/dwmkerr/sharpshell), [SharpZipLib](https://github.com/icsharpcode/SharpZipLib), [PlistCS](https://github.com/animetrics/PlistCS), [PNGDecrush](https://github.com/MikeWeller/PNGDecrush), [Ionic.Zlib](https://github.com/jstedfast/Ionic.Zlib), [QRCoder](https://github.com/codebude/QRCoder), [SVG](https://github.com/vvvv/SVG), [WebP-Wrapper](https://github.com/JosePineiro/WebP-wrapper)

### License

MIT — see [LICENSE](LICENSE). Third-party libraries keep their own licenses in their folders.

---

<a name="中文"></a>
## 中文

一个 Windows 资源管理器 Shell 扩展，直接在资源管理器里显示应用包的**真实图标**，支持：

- `.apk` — 安卓安装包
- `.ipa` / `.tipa` — iOS / TrollStore 安装包
- `.hap` — 鸿蒙 Ability Package
- `.appx` / `.appxbundle` — Windows 8.1/10 UWP 应用包

本项目是 [ApkShellext2](https://github.com/kkguo/apkshellext)（MIT）的增强分支，重点修复了现实世界中各类"毒包"（加固壳、混淆包、split APK、非常规 IPA）导致原图标失效的问题。

### 相对上游的主要增强

| 领域 | 增强 |
|---|---|
| 格式 | 新增 `.tipa`（TrollStore IPA）与 `.hap`（鸿蒙）支持，含独立 HAP 读取器 |
| 混淆 APK | 条目查找改用 ordinal 比较，修复阿拉伯组合符号混淆包（MT管理器/NP管理器产物）图标错乱/丢失 |
| Split APK | 缺 AndroidManifest.xml / resources.arsc 时优雅回退，不再抛异常 |
| 加固/重打包包 | Zip 本地头 size=0 时以中央目录为准；条目名含 Windows 非法路径字符时防御处理 |
| 多 bundle IPA | 选取路径最短的主 `.app`，不再取第一个匹配（避免拿到 Watch/扩展的图标） |
| 图标质量 | 候选图标按期望质量降级尝试；adaptive-icon/gradient 等 XML 解析失败不再导致整体退回通用图标 |
| 界面 | 右键"复制包名"（支持多选批量）；悬浮提示自动清理空行与空值标签 |
| 渲染 | 非正方形源图等比缩放居中；角标叠加空引用防御 |
| 构建 | 目标框架升级 .NET Framework 4.8；移除 protobuf 依赖；Roslyn 直接生成多语言附属程序集（无需 al.exe） |

### 构建

需要 VS2022 / Build Tools 与 .NET Framework 4.8 目标包：

```
MSBuild.exe ApkShellext2\ApkShellext2.csproj -p:Configuration=Release -restore
```

产物为 `ApkShellext2.dll` + `libwebp_x64/x86.dll` + 各语言子目录，运行 `Resources/install.bat` 安装后重启资源管理器。

### 致谢与许可

基于 Jingang Guo 的 ApkShellext2（MIT），致谢与第三方库清单见上文。许可见 [LICENSE](LICENSE)。

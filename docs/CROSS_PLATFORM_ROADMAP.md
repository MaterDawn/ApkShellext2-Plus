# Cross-platform roadmap / 跨平台路线图

## 结论

ApkShellext2-Plus 可以覆盖 Windows、macOS、Linux、统信 UOS、麒麟等桌面系统，但不能直接复用 Windows COM Shell Extension。建议拆分为：

1. **PackageCore**：跨平台包解析和图标导出 CLI/库。
2. **WindowsAdapter**：保留现有 SharpShell COM 扩展。
3. **LinuxAdapter**：基于 freedesktop Thumbnail Managing Standard 的 `.thumbnailer` 集成。
4. **MacAdapter**：基于 Quick Look Thumbnail Extension；右键菜单按需增加 Finder 扩展。

## 当前代码的跨平台阻点

| 依赖 | 当前用途 | 处理方式 |
|---|---|---|
| `System.Drawing` / GDI+ | 解码、缩放、合成图标 | 核心层改用跨平台图像抽象；首选 ImageSharp（需单独核查许可证/费用适用范围）或 SkiaSharp |
| `System.Windows.Media` / WPF | Android VectorDrawable 渲染 | 抽成 renderer 接口；跨平台实现用 SkiaSharp SVG/path |
| `Microsoft.Win32.Registry` | 偏好设置、注册 | 移至 WindowsAdapter；核心用 JSON/环境变量 |
| `SharpShell.*` | Explorer COM 注册、日志、Shell API | 仅保留在 WindowsAdapter |
| `System.Windows.Forms` | 菜单、剪贴板、设置 UI | 仅保留 Windows；跨平台 UI 独立实现 |
| `.NET Framework 4.8` | Windows 构建目标 | PackageCore 升级为 `net8.0`；WindowsAdapter 可暂时保留 `net48` 或迁移 `net8.0-windows` |

## PackageCore API（建议）

```text
package-icon extract <input> --output <png> --size <px>
package-icon inspect <input> --json
```

标准 JSON：

```json
{
  "format": "apk",
  "packageId": "com.example.app",
  "appName": "Example",
  "versionName": "1.0.0",
  "versionCode": "1",
  "iconSource": "res/mipmap-xxxhdpi/icon.png",
  "iconWidth": 512,
  "iconHeight": 512
}
```

约束：

- 不使用 Registry、COM、WPF、WinForms。
- 输入只读；限制压缩条目数量、单条目尺寸、总解压量，防 ZIP bomb。
- 默认不执行包内代码、不加载包内 DLL。
- 缓存键使用 `SHA-256(file) + size + parserVersion`，禁止仅按路径或时间戳缓存。
- 所有解析器返回统一错误码，不把异常传播到文件管理器进程。

## Linux / UOS / 麒麟

优先实现，因为 freedesktop 标准可覆盖 Nautilus、Nemo、Caja、Thunar，并被 Dolphin/KIO 支持。

### MIME 类型

安装 shared-mime-info XML，为下列扩展注册 MIME：

- `application/vnd.android.package-archive`：`.apk`
- `application/x-ios-app`：`.ipa`、`.tipa`
- `application/x-harmonyos-hap`：`.hap`
- `application/vnd.ms-appx`：`.appx`
- `application/vnd.ms-appxbundle`：`.appxbundle`

### Thumbnailer

安装 `/usr/share/thumbnailers/apkshellext2-plus.thumbnailer`：

```ini
[Thumbnailer Entry]
TryExec=package-icon
Exec=package-icon extract %i --output %o --size %s
MimeType=application/vnd.android.package-archive;application/x-ios-app;application/x-harmonyos-hap;application/vnd.ms-appx;application/vnd.ms-appxbundle;
```

### 统信 UOS

- UOS 家庭版/专业版的 DDE 文件管理器版本差异较大，先验证其是否读取 freedesktop `.thumbnailer`。
- 若不读取，第二阶段对 DDE File Manager / `dde-file-manager` 的插件接口做定向适配。
- 同一 `.deb` 同时安装 MIME、CLI 和 thumbnailer；打包 `amd64`、`arm64`、`loong64`（按发行版支持情况）。

## macOS

### 图标预览

- 主路径：Quick Look Thumbnail Extension，接收文件 URL 和目标尺寸，调用 PackageCore，返回缩略图。
- 分发形态：包含扩展的签名 `.app`，需要 Apple Developer ID、公证和 hardened runtime。
- `.ipa/.tipa/.apk/.hap/.appx` 文件类型通过 UTType 声明。

### Finder 能力边界

- Quick Look 适合缩略图和预览，不等同于 Windows 的 `IIconHandler`。
- Finder Sync 官方定位主要是同步目录、徽标、上下文菜单，不宜滥用为全磁盘任意文件图标替换。
- 因此 macOS 第一版目标应是“图标缩略图 + Quick Look 信息页”，右键复制包名作为第二阶段。

## 分阶段交付

| 阶段 | 交付 | 验收 |
|---|---|---|
| P0 | `net8.0` PackageCore + CLI | Windows/Linux 对同一语料输出元数据一致；图标 SHA-256 或感知哈希可比较 |
| P1 | Linux `.thumbnailer` + MIME + `.deb` | Ubuntu/Debian、UOS、麒麟，Nautilus/DDE/Dolphin 显示真实图标 |
| P2 | macOS Quick Look App Extension | Intel + Apple Silicon；Finder 缩略图、空格预览、损坏包不崩溃 |
| P3 | 右键菜单/属性页增强 | Linux 文件管理器扩展、macOS Finder 菜单按平台能力实现 |
| P4 | CI 和发行 | Windows zip、Linux deb/rpm/AppImage、macOS dmg，自动化语料回归 |

## 工程目录建议

```text
src/
  PackageCore/                 # net8.0，无平台依赖
  PackageCore.Cli/             # package-icon
  PackageCore.Renderers.Skia/  # 跨平台图形渲染
  Windows.ShellExtension/      # 现有 SharpShell 代码
  Mac.QuickLook/               # Swift / Quick Look extension
packaging/
  linux/
    mime/
    thumbnailer/
    debian/
  macos/
tests/
  Corpus/
  PackageCore.Tests/
```

## 推荐先做什么

先完成 P0，不要并行写三个系统适配器。当前解析器与 Windows UI/GDI/WPF 耦合较深；若不先抽核心，后续每个平台都会复制并分叉解析逻辑，修复无法同步。

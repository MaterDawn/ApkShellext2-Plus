using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using ApkQuickReader;
using System.IO;

namespace ApkShellext2 {
    /// <summary>
    /// This is the base class for package reader
    /// defines common method a package reader should have
    /// for reusing and simplify in the code
    /// </summary>
    public class AppPackageReader : IDisposable{
        public const string extAPK = ".apk";
        // QQ/微信等传输工具可能把 APK 改名为 .1；必须嗅探内容，不能把所有 .1 盲目当 APK。
        public const string extTRANSFER = ".1";
        public const string extIPA = ".ipa";
        // .tipa = TrollStore IPA 包（iOS），容器结构与 .ipa 完全一致，按 iOSApp 处理
        public const string extTIPA = ".tipa";
        // .hap = HarmonyOS Ability Package（鸿蒙应用包）
        public const string extHAP = ".hap";
        public const string extAPPX = ".appx";
        public const string extAPPXBUNDLE = ".appxbundle";

        public enum AppType {
            AndroidApp,
            iOSApp,
            WindowsPhoneAppBundle,
            WindowsPhoneApp,
            HarmonyApp
        }
        private Dictionary<string, object> Flags;

        public virtual string FileName { get; protected set; }
        // App name 
        public virtual string AppName { get { return ""; } }
        // Package Name
        public virtual string PackageName { get { return ""; } }

        // App version
        public virtual string Version { get { return ""; } }
        // Sub version
        public virtual string Revision { get { return ""; } }

        public virtual CultureInfo Culture { get; set; }

        // Publisher
        public virtual string Publisher { get { return ""; } }

        public virtual Bitmap Icon { get { return null; } }

        /// <summary>
        /// 诊断：实际被选中的图标资源路径/键（子类覆写）。
        /// </summary>
        public virtual string IconPath { get { return ""; } }

        /// <summary>
        /// 诊断：源图标的像素尺寸（子类覆写）。Size.Empty 表示未知。
        /// </summary>
        public virtual Size IconSourceSize { get { return Size.Empty; } }

        /// <summary>
        /// 诊断：图标选择过程的追踪信息（子类覆写）。
        /// </summary>
        public virtual string IconTrace { get { return ""; } }

        public virtual string AppID { get { return ""; } }

        public virtual AppType Type { get { return AppType.AndroidApp; } }

        // use for other information, or file type specific info
        public virtual void setFlag(string flag, object value) {
            if (Flags == null)
                Flags = new Dictionary<string, object>();
            Flags[flag] = value;
        }

        public virtual object getFlag(string flag) {
            if (Flags != null && Flags.ContainsKey(flag))
                return Flags[flag];
            else
                return null;
        }

        public virtual Bitmap getIcon(Size size) {
            return Utility.ResizeBitmap(Icon, size);
        }

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed;
        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing) {
            if (disposed)
                return;
            if (Flags != null)
                Flags.Clear();
            Flags = null;
            disposed = true;
        }

        /// <summary>
        /// 检查 .1 是否真的是 APK。只读 ZIP 目录并要求 AndroidManifest.xml；
        /// classes.dex/resources.arsc 用作辅助信号。非 APK .1 不接管。
        /// </summary>
        public static bool IsTransferredApk(string path) {
            if (!File.Exists(path)) return false;
            try {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (ICSharpCode.SharpZipLib.Zip.ZipFile zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(stream)) {
                    bool manifest = false, dex = false, resources = false;
                    foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in zip) {
                        if (entry == null || entry.Name == null) continue;
                        if (string.Equals(entry.Name, "AndroidManifest.xml", StringComparison.OrdinalIgnoreCase))
                            manifest = true;
                        else if (string.Equals(entry.Name, "classes.dex", StringComparison.OrdinalIgnoreCase))
                            dex = true;
                        else if (string.Equals(entry.Name, "resources.arsc", StringComparison.OrdinalIgnoreCase))
                            resources = true;
                        if (manifest && (dex || resources)) return true;
                    }
                    return manifest;
                }
            } catch {
                return false;
            }
        }

        public static bool IsAndroidPackagePath(string path) {
            string suffix = Path.GetExtension(path).ToLowerInvariant();
            return suffix == extAPK || (suffix == extTRANSFER && IsTransferredApk(path));
        }

        public static AppType getAppType(string path) {
            // FIX: 扩展名必须大小写不敏感（Windows 上 .APK/.IPA 很常见），
            // 旧实现用 == 直接比较，遇到 .APK 直接抛 NotSupportedException，图标完全不显示。
            string suffix = Path.GetExtension(path).ToLowerInvariant();
            if (suffix == extAPK || (suffix == extTRANSFER && IsTransferredApk(path))) {
                return AppType.AndroidApp;
            } else if (suffix == extIPA || suffix == extTIPA) {
                return AppType.iOSApp;
            } else if (suffix == extHAP) {
                return AppType.HarmonyApp;
            } else if (suffix == extAPPXBUNDLE) {
                return AppType.WindowsPhoneAppBundle;
            } else if (suffix == extAPPX) {
                return AppType.WindowsPhoneApp;
            } else {
                throw new NotSupportedException(suffix + " type is not supported.");
            }
        }

        public static AppPackageReader Read(string path) {
            return Read(path, null);
        }

        /// <summary>
        /// 读取包；支持显式传入 culture。culture 为空时使用当前 UI 语言。
        /// </summary>
        public static AppPackageReader Read(string path, CultureInfo culture) {
            switch (getAppType(path)) {
                case AppType.AndroidApp: {
                        ApkQuickReader.ApkReader r = new ApkQuickReader.ApkReader(path);
                        if (culture != null) r.Culture = culture;
                        return r;
                    }
                case AppType.iOSApp: {
                        IpaReader r = new IpaReader(path);
                        if (culture != null) r.Culture = culture;
                        return r;
                    }
                case AppType.HarmonyApp: {
                        HapReader r = new HapReader(path);
                        if (culture != null) r.Culture = culture;
                        return r;
                    }
                case AppType.WindowsPhoneAppBundle:
                    return new AppxBundleReader(path);
                case AppType.WindowsPhoneApp:
                    return new AppxReader(path);
                default:
                    throw new NotSupportedException("File type is not supported.");
            }
        }

        protected void Log(string message) {
            Utility.Log(this, Path.GetFileName(FileName), message);
        }
    }
}

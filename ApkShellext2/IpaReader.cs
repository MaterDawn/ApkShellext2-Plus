using PlistCS;
using PNGDecrush;
using SharpShell.Attributes;
using SharpShell.Extensions;
using SharpShell.Diagnostics;
using SharpShell.Exceptions;
using SharpShell.ServerRegistration;
using SharpShell.SharpIconHandler;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Ionic.Zlib;

namespace ApkShellext2
{
    public class IpaReader : AppPackageReader
    {
        private string strAppRoot;
        private Dictionary<string, object> infoPlistDic;
        private Dictionary<string, object> itunesMetadataDic;
        private ZipFile zip;

        // 小写条目名 -> ZipEntry，用于大小写不敏感查找（iOS 包在 Windows 上大小写经常不一致）
        private Dictionary<string, ZipEntry> entryMap;

        // 诊断信息
        private string iconPath = "";
        private Size iconSourceSize = Size.Empty;
        private List<string> iconTrace = new List<string>();

        private const string iTunesMetadataPath = @"iTunesMetadata.plist";
        private const string infoPlistPath = @"(Payload/.*\.app/)Info\.plist";
        private const string CFBundleIcons = @"CFBundleIcons";
        private const string CFBundlePrimaryIcon = @"CFBundlePrimaryIcon";
        private const string CFBundleIconFile = @"CFBundleIconFile";
        private const string CFBundleIconFiles = @"CFBundleIconFiles";
        private const string CFBundleIconName = @"CFBundleIconName";
        private const string CFBundleIconsIpad = @"CFBundleIcons~ipad";
        private const string CFBundleDisplayName = @"CFBundleDisplayName";
        private const string FacebookDisplayName = @"FacebookDisplayName";
        private const string CFBundleIdentifier = @"CFBundleIdentifier";
        private const string CFBundleShortVersionString = @"CFBundleShortVersionString";
        private const string CFBundleVersion = @"CFBundleVersion";
        private const string CFBundleResourceSpecification = @"CFBundleResourceSpecification";

        public const string flagAppId = @"itemId";
        public const string flagCopyright = @"copyright";

        private static readonly byte[] PngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public IpaReader(string path) {
            FileName = path;
            openStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        public IpaReader(Stream stream) {
            openStream(stream);
        }

        private void openStream(Stream stream) {
            zip = new ZipFile(stream);
            ZipEntry infoPlist = null;
            foreach (ZipEntry en in zip) {
                Match m = Regex.Match(en.Name, infoPlistPath);
                if (m.Success) {
                    // FIX: 一个 IPA 里可能有多个 .app（Watch / App Extension / PlugIns），
                    // 旧实现取「第一个」匹配，可能拿到扩展的 Info.plist，图标就是错的。
                    // 这里取路径最短的那个，即主 App bundle。
                    string root = m.Groups[1].Value;
                    if (infoPlist == null || root.Length < strAppRoot.Length) {
                        strAppRoot = root;
                        infoPlist = en;
                    }
                }
            }

            if (infoPlist == null) {
                throw new EntryPointNotFoundException("cannot find info.plist");
            }

            byte[] infoBytes = new byte[infoPlist.Size];
            zip.GetInputStream(infoPlist).Read(infoBytes, 0, (int)infoPlist.Size);

            infoPlistDic = (Dictionary<string, object>)Plist.readPlist(infoBytes);
        }

        /// <summary>惰性构建小写条目索引。</summary>
        private Dictionary<string, ZipEntry> EntryMap {
            get {
                if (entryMap == null) {
                    entryMap = new Dictionary<string, ZipEntry>(StringComparer.Ordinal);
                    foreach (ZipEntry en in zip) {
                        string key = en.Name.ToLowerInvariant();
                        if (!entryMap.ContainsKey(key)) entryMap[key] = en;
                    }
                }
                return entryMap;
            }
        }

        /// <summary>大小写不敏感地取条目。</summary>
        private ZipEntry findEntry(string fullName) {
            ZipEntry e = zip.GetEntry(fullName);
            if (e != null) return e;
            ZipEntry v;
            if (EntryMap.TryGetValue(fullName.ToLowerInvariant(), out v)) return v;
            return null;
        }

        private static byte[] readEntry(ZipFile zip, ZipEntry en) {
            if (en == null || en.Size <= 0) return null;
            byte[] buf = new byte[en.Size];
            using (Stream s = zip.GetInputStream(en)) {
                int got = 0, r;
                while (got < buf.Length && (r = s.Read(buf, got, buf.Length - got)) > 0) got += r;
                if (got != buf.Length) Array.Resize(ref buf, got);
            }
            return buf;
        }

        public override string IconPath { get { return iconPath; } }
        public override Size IconSourceSize { get { return iconSourceSize; } }
        public override string IconTrace { get { return string.Join("; ", iconTrace.ToArray()); } }

        public string[] getStrings(Dictionary<string, object> dic, string[] keys) {
            for (int i = 0; i < keys.Length - 1; i++) {
                if (dic.ContainsKey(keys[i])) {
                    dic = (Dictionary<string, object>)dic[keys[i]];
                } else {
                    return new string[] { };
                    //throw new Exception("Given ID is not valid");
                }
            }
            if (dic.ContainsKey(keys[keys.Length - 1])) {
                if (dic[keys[keys.Length - 1]] is string) {
                    return new string[] { dic[keys[keys.Length - 1]] as string };
                }
                if (dic[keys[keys.Length - 1]] is int) {
                    return new string[] { dic[keys[keys.Length - 1]].ToString() };
                } else { // is list
                    object[] arr = ((List<object>)dic[keys[keys.Length - 1]]).ToArray();
                    return Array.ConvertAll<object, string>(arr, x => x.ToString());
                }
            } else {
                return new string[] { };
            }
        }

        public Bitmap getImage(string[] keys) {
            string[] images = getStrings(infoPlistDic, keys);
            if (images.Count() > 0) {
                return getImage(images[0]);
            }
            return null;
        }

        public Bitmap getImage(string name) {
            string used;
            return resolveIconName(name, out used);
        }

        /// <summary>
        /// 解析单个图标基名：自动补 @3x/@2x/@1x/.png 与 ~ipad 变体，大小写不敏感。
        /// FIX: 旧实现只试 @3x/@2x/.png 三种固定组合，且当名字本身已带 @2x（iOS 常见写法）
        /// 时会拼出 AppIcon60x60@2x@3x.png 这种不存在的名字，直接丢图标。
        /// </summary>
        private Bitmap resolveIconName(string name, out string usedEntry) {
            usedEntry = null;
            if (string.IsNullOrEmpty(name)) return null;
            name = name.Trim();
            if (name.Length == 0) return null;

            string lower = name.ToLowerInvariant();
            bool hasExt = lower.EndsWith(".png") || lower.EndsWith(".jpg") || lower.EndsWith(".jpeg");
            bool hasScale = Regex.IsMatch(lower, @"@[123]x(\.(png|jpg|jpeg))?$");

            List<string> tries = new List<string>();
            if (hasExt) {
                tries.Add(name);
            } else if (hasScale) {
                tries.Add(name + ".png");
            } else {
                tries.Add(name + "@3x.png");
                tries.Add(name + "@2x.png");
                tries.Add(name + "@1x.png");
                tries.Add(name + ".png");
            }

            // ~ipad 变体（后缀在比例之后、扩展名之前）
            int n0 = tries.Count;
            for (int i = 0; i < n0; i++) {
                string t = tries[i];
                int dot = t.LastIndexOf('.');
                if (dot > 0) tries.Add(t.Substring(0, dot) + "~ipad" + t.Substring(dot));
            }

            foreach (string t in tries) {
                ZipEntry en = findEntry(strAppRoot + t);
                if (en == null) continue;
                Bitmap b = decodeIpaImage(readEntry(zip, en));
                if (b != null) {
                    usedEntry = t;
                    return b;
                }
            }
            return null;
        }

        /// <summary>
        /// 解码 IPA 内的图片：先按普通 PNG/JPEG 解码，失败再尝试 CgBI（crunched）还原。
        /// FIX: 旧实现只在抛 InvalidDataException 时回退，而 GDI+ 对损坏/苹果专有 PNG
        /// 常抛 OutOfMemoryException/ArgumentException，于是 many crushed 图标直接丢失。
        /// </summary>
        private Bitmap decodeIpaImage(byte[] bytes) {
            if (bytes == null || bytes.Length < 8) return null;

            try {
                return new Bitmap(new MemoryStream(bytes));
            } catch { }

            // 苹果 CgBI（PNG crunched）需要先还原
            if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50) {
                try {
                    MemoryStream outp = new MemoryStream();
                    PNGDecrusher.Decrush(new MemoryStream(bytes), outp);
                    outp.Position = 0;
                    return new Bitmap(outp);
                } catch { }
            }
            return null;
        }

        /// <summary>收集 Info.plist 中所有图标候选基名（保持 plist 中的顺序，不排序）。</summary>
        private List<string> collectIconCandidates() {
            List<string> names = new List<string>();
            string[] scopes = new string[] { CFBundleIcons, CFBundleIconsIpad };
            foreach (string scope in scopes) {
                addStrings(names, new string[] { scope, CFBundlePrimaryIcon, CFBundleIconFiles });
                addStrings(names, new string[] { scope, CFBundlePrimaryIcon, CFBundleIconFile });
                addStrings(names, new string[] { scope, CFBundlePrimaryIcon, CFBundleIconName });
            }
            addStrings(names, new string[] { CFBundleIconFiles });
            addStrings(names, new string[] { CFBundleIconFile });
            addStrings(names, new string[] { CFBundleIconName });
            // 通用兜底名
            names.Add("AppIcon");
            names.Add("Icon");
            return names;
        }

        private void addStrings(List<string> target, string[] keys) {
            try {
                foreach (string s in getStrings(infoPlistDic, keys)) {
                    if (!string.IsNullOrEmpty(s) && !target.Contains(s)) target.Add(s);
                }
            } catch { }
        }

        /// <summary>
        /// 顶层 iTunesArtwork（旧版 IPA 的高清图标，512x512 甚至 1024x1024）。
        /// FIX: 旧实现完全没利用它，而它常常是整个包里最清晰的图标。
        /// </summary>
        private Bitmap loadITunesArtwork(out string used) {
            used = null;
            string[] names = new string[] {
                "iTunesArtwork@3x", "iTunesArtwork@2x", "iTunesArtwork"
            };
            foreach (string n in names) {
                ZipEntry en = findEntry(n);
                if (en == null) continue;
                Bitmap b = decodeIpaImage(readEntry(zip, en));
                if (b != null) {
                    used = n;
                    return b;
                }
            }
            return null;
        }

        /// <summary>
        /// 从 Assets.car（Asset Catalog）里提取图标。
        /// 这是被动启发式：在 BOM 归档中扫描内嵌的 PNG/JPEG 负载，
        /// 取面积最大的近似方形图（图标 rendition 通常就是最大的方形资源）。
        /// 仅在常规 PNG 路径全部失败时调用。
        /// </summary>
        private Bitmap loadFromAssetsCar(out string used) {
            used = null;
            ZipEntry en = findEntry(strAppRoot + "Assets.car");
            if (en == null || en.Size <= 0 || en.Size > 96L * 1024 * 1024) return null;
            byte[] car = readEntry(zip, en);
            if (car == null || car.Length < 64) return null;

            Bitmap best = null;
            int bestArea = -1;
            int candidates = 0;

            // ---- PNG 负载 ----
            int idx = 0;
            while (candidates < 600) {
                int p = indexOf(car, PngSignature, idx);
                if (p < 0) break;
                idx = p + PngSignature.Length;
                if (p + 24 > car.Length) break;
                int w = (car[p + 16] << 24) | (car[p + 17] << 16) | (car[p + 18] << 8) | car[p + 19];
                int h = (car[p + 20] << 24) | (car[p + 21] << 16) | (car[p + 22] << 8) | car[p + 23];
                if (w < 60 || h < 60) continue;
                if (w < h * 0.9 || h < w * 0.9) continue;   // 只考虑近似方形

                int end = findIend(car, p);
                if (end < 0) continue;
                candidates++;

                byte[] slice = new byte[end - p];
                Buffer.BlockCopy(car, p, slice, 0, slice.Length);
                Bitmap b = decodeIpaImage(slice);
                if (b == null) continue;
                if (b.Width * b.Height > bestArea) {
                    if (best != null) best.Dispose();
                    best = b;
                    bestArea = b.Width * b.Height;
                    used = "(Assets.car PNG " + b.Width + "x" + b.Height + ")";
                } else {
                    b.Dispose();
                }
            }

            iconTrace.Add("Assets.car scanned png candidates=" + candidates + " best=" + bestArea);
            return best;
        }

        private static int indexOf(byte[] haystack, byte[] needle, int start) {
            int last = haystack.Length - needle.Length;
            for (int i = Math.Max(0, start); i <= last; i++) {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j]) j++;
                if (j == needle.Length) return i;
            }
            return -1;
        }

        /// <summary>返回 IEND 之后的结束偏移（PNG 负载结尾），找不到返回 -1。</summary>
        private static int findIend(byte[] b, int start) {
            byte[] iend = new byte[] { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
            int p = indexOf(b, iend, start + 8);
            return (p < 0) ? -1 : p + iend.Length;
        }

        public override AppPackageReader.AppType Type {
            get {
                return AppType.iOSApp;
            }
        }

        public override string AppName {
            get {
                try {
                    string[] n = getStrings(infoPlistDic, new string[] {
                    CFBundleDisplayName});
                    if (n.Count() == 0) {
                        n = getStrings(infoPlistDic, new string[] {
                        FacebookDisplayName });
                    }
                    return n[0];
                } catch {
                    return "";
                }
            }
        }

        public override string Version {
            get {
                try {
                    return getStrings(infoPlistDic, new string[] {
                    CFBundleShortVersionString})[0];
                } catch {
                    return "";
                }
            }
        }

        public override string Revision {
            get {
                try {
                    return getStrings(infoPlistDic, new string[] {
                    CFBundleVersion})[0];
                } catch {
                    return "";
                }
            }
        }

        public override string PackageName {
            get {
                try {
                    return getStrings(infoPlistDic, new string[] {
                    CFBundleIdentifier})[0];
                } catch {
                    return "";
                }
            }
        }
        public override Bitmap Icon {
            get {
                try {
                    // FIX: 旧实现按 CFBundleIconFiles 的顺序取 images[0]，
                    // 而 Apple 的约定是「从小到大」排列（AppIcon20x20 → AppIcon60x60），
                    // 所以一直取到的是 60x60 这类最小图，拉到 256 尺寸非常糊。
                    // 现在解析全部候选，取真实像素面积最大的那个。
                    List<string> candidates = collectIconCandidates();
                    Bitmap best = null;
                    int bestArea = -1;
                    string bestName = null;

                    foreach (string name in candidates) {
                        string used;
                        Bitmap b = resolveIconName(name, out used);
                        if (b == null) continue;
                        int area = b.Width * b.Height;
                        if (area > bestArea) {
                            if (best != null) best.Dispose();
                            best = b;
                            bestArea = area;
                            bestName = used;
                        } else {
                            b.Dispose();
                        }
                        // 已经拿到 >=180x180，足够清晰，提前结束（避免无谓解压）
                        if (bestArea >= 180 * 180) break;
                    }

                    if (best == null) {
                        string used;
                        best = loadITunesArtwork(out used);
                        if (best != null) bestName = used;
                    }

                    if (best == null) {
                        string used;
                        best = loadFromAssetsCar(out used);
                        if (best != null) bestName = used;
                    }

                    if (best != null) {
                        iconPath = bestName;
                        iconSourceSize = new Size(best.Width, best.Height);
                        iconTrace.Add("picked " + bestName + " " + best.Width + "x" + best.Height);
                    } else {
                        iconTrace.Add("no icon found; candidates=" + candidates.Count);
                    }
                    return best;
                } catch (Exception ex) {
                    iconTrace.Add("Icon failed: " + ex.Message);
                    return null;
                }
            }
        }

        public override string Publisher {
            get {
                try {
                    ZipEntry itunesMetadata = zip.GetEntry(iTunesMetadataPath);
                    if (itunesMetadata == null)
                        return "";
                    byte[] itunesMetadataBytes = new byte[itunesMetadata.Size];
                    zip.GetInputStream(itunesMetadata).Read(itunesMetadataBytes, 0, (int)itunesMetadata.Size);
                    itunesMetadataDic = (Dictionary<string, object>)Plist.readPlist(itunesMetadataBytes);
                    return getStrings(itunesMetadataDic, new string[] { flagCopyright })[0];
                } catch {
                    return "";
                }
            }
        }

        public override string AppID {
            get {
                try {
                    ZipEntry itunesMetadata = zip.GetEntry(iTunesMetadataPath);
                    if (itunesMetadata == null)
                        return "";
                    byte[] itunesMetadataBytes = new byte[itunesMetadata.Size];
                    zip.GetInputStream(itunesMetadata).Read(itunesMetadataBytes, 0, (int)itunesMetadata.Size);
                    itunesMetadataDic = (Dictionary<string, object>)Plist.readPlist(itunesMetadataBytes);
                    return getStrings(itunesMetadataDic, new string[] { flagAppId })[0];
                } catch {
                    return "";
                }
            }
        }


        private bool disposed = false;
        protected override void Dispose(bool disposing) {
            if (disposed) return;
            if (disposing) {
                if (zip != null)
                    zip.Close();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        public void Close() {
            Dispose(true);
        }

        ~IpaReader() {
            Dispose(true);
        }
    }
}

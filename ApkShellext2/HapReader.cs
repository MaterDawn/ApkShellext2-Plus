using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ICSharpCode.SharpZipLib.Zip;
using WebPWrapper;

namespace ApkShellext2 {
    /// <summary>
    /// HarmonyOS Ability Package (.hap) 读取器。
    /// 结构：ZIP + 明文 JSON（module.json）；图标在 resources/base/media/ 下，
    /// 由 module.json 的 app.icon = "$media:<名字>" 引用。
    /// 注意：
    ///  - 分层图标 $media:layered_image 实际是 layered_image.json，其
    ///    foreground/background 可能是数字资源 ID，经 resources.index 的
    ///    id->路径 映射解析，失败再按启发式回退（foreground/icon/app_icon/starticon）。
    ///  - label 为 "$string:<名字>"，经 resources.index（二进制，见下方格式注释）
    ///    解析为实际应用名。
    /// </summary>
    public class HapReader : AppPackageReader {
        private ZipFile zip;
        private Dictionary<string, object> root;      // module.json 根
        private Dictionary<string, object> appNode;   // "app" 节点
        private List<object> abilities;               // "module"."abilities"
        // 小写媒体名（去扩展） -> zip 内完整条目名
        private Dictionary<string, string> mediaMap;

        private string iconPath = "";
        private Size iconSourceSize = Size.Empty;
        private Bitmap icon;

        // resources.index 解析产物（懒加载）
        private bool indexParsed;
        private Dictionary<string, string> stringMap;    // 资源名 -> 字符串值（resType=STRING）
        private Dictionary<uint, string> mediaPathById;  // 资源 ID -> 媒体文件路径（index 内，带模块前缀）

        private static readonly string[] imageExts = { ".png", ".webp", ".jpg", ".jpeg" };
        private static readonly string[] fallbackNames = { "foreground", "icon", "app_icon", "starticon" };

        public HapReader(string path) {
            FileName = path;
            open(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess));
        }

        public HapReader(Stream stream) {
            open(stream);
        }

        private void open(Stream stream) {
            try {
                zip = new ZipFile(stream);
                ZipEntry m = findEntry("module.json");
                if (m == null)
                    throw new EntryPointNotFoundException("module.json not found (not a .hap package?)");
                byte[] b = readEntry(zip, m);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                ser.MaxJsonLength = int.MaxValue;
                root = ser.Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(b));
                if (root == null || !root.ContainsKey("app"))
                    throw new InvalidDataException("module.json has no app node");
                appNode = root["app"] as Dictionary<string, object>;
                object mod;
                if (root.TryGetValue("module", out mod))
                    abilities = (mod as Dictionary<string, object>) != null &&
                                (mod as Dictionary<string, object>).ContainsKey("abilities")
                        ? (mod as Dictionary<string, object>)["abilities"] as List<object>
                        : null;
            } catch {
                if (zip != null) try { zip.Close(); } catch { }
                throw;
            }
        }

        public override string IconPath { get { return iconPath; } }
        public override Size IconSourceSize { get { return iconSourceSize; } }

        /// <summary>
        /// 应用名：module.json 的 label 是 "$string:资源名" 引用，
        /// 经 resources.index 解析为实际字符串；纯文本 label 直接返回。
        /// </summary>
        public override string AppName {
            get {
                string label = str(appNode, "label");
                if (string.IsNullOrEmpty(label) && abilities != null && abilities.Count > 0) {
                    Dictionary<string, object> ab = abilities[0] as Dictionary<string, object>;
                    if (ab != null) label = str(ab, "label");
                }
                return resolveStringRef(label);
            }
        }

        public override string PackageName {
            get { return str(appNode, "bundleName"); }
        }

        public override string Version {
            get { return str(appNode, "versionName"); }
        }

        public override string AppID {
            get { return str(appNode, "bundleName"); }
        }

        public override AppType Type { get { return AppType.HarmonyApp; } }

        public override Bitmap Icon {
            get {
                if (icon == null) {
                    try { icon = resolveIcon(); }
                    catch { icon = null; }
                }
                return icon;
            }
        }

        // ---------- resources.index 解析 ----------

        /*
         * resources.index（restool 生成）布局（小端）：
         *   IndexHeader: version[128] + fileSize(u32) + limitKeyConfigSize(u32) = 136 字节
         *   LimitKeyConfig × limitKeyConfigSize:
         *     'KEYS'(4) + idSetOffset(u32) + keyCount(u32) + keyCount × KeyParam(type u32 + value u32)
         *   IdSet（位于 idSetOffset）:
         *     'IDSS'(4) + idCount(u32) + idCount × (id u32 + dataOffset u32)
         *   IdItem（位于 dataOffset）:
         *     size(i32) + resType(i32) + id(i32) + valueLen(u16, 含结尾 NUL)
         *     + value[valueLen] + nameLen(u16, 含结尾 NUL) + name[nameLen]
         * resType=9 为 STRING；MEDIA 记录的 value 是 "entry/resources/base/media/xxx" 路径。
         */
        private void ensureIndex() {
            if (indexParsed) return;
            indexParsed = true;
            try { buildIndexMaps(); } catch { }
        }

        private void buildIndexMaps() {
            ZipEntry en = findEntry("resources.index");
            if (en == null) return;
            byte[] b = readEntry(zip, en);
            if (b == null || b.Length < 136) return;
            stringMap = new Dictionary<string, string>();
            mediaPathById = new Dictionary<uint, string>();
            int off = 128;                                   // version[128]
            uint keyCount = BitConverter.ToUInt32(b, off + 4); off += 8;
            for (uint g = 0; g < keyCount && off + 16 <= b.Length; g++) {
                off += 4;                                    // 'KEYS'
                uint idSetOff = BitConverter.ToUInt32(b, off); off += 4;
                uint keyParams = BitConverter.ToUInt32(b, off); off += 4;
                off += (int)keyParams * 8;                   // KeyParam = type(u32)+value(u32)
                if (idSetOff + 8 > b.Length) continue;
                int o = (int)idSetOff + 4;                   // 跳过 'IDSS'
                uint idCount = BitConverter.ToUInt32(b, o); o += 4;
                for (uint i = 0; i < idCount && o + 8 <= b.Length; i++) {
                    uint rid = BitConverter.ToUInt32(b, o);
                    uint doff = BitConverter.ToUInt32(b, o + 4);
                    o += 8;
                    parseIdItem(b, (int)doff, rid);
                }
            }
        }

        private void parseIdItem(byte[] b, int o, uint rid) {
            if (o + 14 > b.Length) return;
            int resType = BitConverter.ToInt32(b, o + 4);
            int valueLen = BitConverter.ToUInt16(b, o + 12);
            int p = o + 14;
            if (valueLen <= 1 || valueLen > 8192 || p + valueLen + 2 > b.Length) return;
            if (b[p + valueLen - 1] != 0) return;            // value 必须以 NUL 结尾
            string value = Encoding.UTF8.GetString(b, p, valueLen - 1);
            int nameLen = BitConverter.ToUInt16(b, p + valueLen);
            p += valueLen + 2;
            if (nameLen <= 1 || nameLen > 1024 || p + nameLen > b.Length) return;
            if (b[p + nameLen - 1] != 0) return;             // name 必须以 NUL 结尾
            string name = Encoding.UTF8.GetString(b, p, nameLen - 1);
            if (resType == 9 && !stringMap.ContainsKey(name)) stringMap[name] = value;
            if (!mediaPathById.ContainsKey(rid)) mediaPathById[rid] = value;
        }

        /// <summary>解析 label 引用："$string:名字" 查资源表；纯文本直接返回。</summary>
        private string resolveStringRef(string label) {
            if (string.IsNullOrEmpty(label)) return "";
            if (label.StartsWith("$string:")) {
                ensureIndex();
                if (stringMap == null) return "";
                string v;
                return stringMap.TryGetValue(label.Substring(8), out v) ? v : "";
            }
            return label.StartsWith("$") ? "" : label;   // 其余引用类型不支持
        }

        // ---------- 图标解析 ----------

        private Bitmap resolveIcon() {
            if (mediaMap == null) buildMediaMap();

            // 1) 按 module.json 声明：app.icon 优先，缺省回退 ability[0].icon
            string refName = str(appNode, "icon");
            if (string.IsNullOrEmpty(refName) && abilities != null && abilities.Count > 0) {
                Dictionary<string, object> ab = abilities[0] as Dictionary<string, object>;
                if (ab != null) refName = str(ab, "icon");
            }
            if (!string.IsNullOrEmpty(refName)) {
                string name = refName.StartsWith("$media:") ? refName.Substring(7) : null;
                if (!string.IsNullOrEmpty(name)) {
                    Bitmap b = resolveMediaName(name);
                    if (b != null) return b;
                }
            }

            // 2) 启发式回退（分层图标 ID、无声明等情况）
            foreach (string n in fallbackNames) {
                Bitmap b = decodeMedia(n);
                if (b != null) return b;
            }
            return null;
        }

        /// <summary>解析 $media: 引用；layered_image.json 递归一层；数字 ID 直接放弃走回退。</summary>
        private Bitmap resolveMediaName(string name) {
            if (string.IsNullOrEmpty(name)) return null;
            string low = name.Trim().ToLowerInvariant();
            if (low.Length == 0) return null;

            string entry;
            if (!mediaMap.TryGetValue(low, out entry)) return null;

            if (entry.ToLowerInvariant().EndsWith(".json")) {
                // layered_image: {"layered-image":{"background":"$media:x","foreground":"$media:y"}}
                byte[] b = readEntryByName(entry);
                if (b == null) return null;
                try {
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    ser.MaxJsonLength = int.MaxValue;
                    Dictionary<string, object> j = ser.Deserialize<Dictionary<string, object>>(
                        Encoding.UTF8.GetString(b));
                    if (j == null || !j.ContainsKey("layered-image")) return null;
                    Dictionary<string, object> li = j["layered-image"] as Dictionary<string, object>;
                    if (li == null) return null;
                    foreach (string k in new string[] { "foreground", "background" }) {
                        string v = str(li, k);
                        if (string.IsNullOrEmpty(v) || !v.StartsWith("$media:")) continue;
                        string sub = v.Substring(7);
                        Bitmap bm;
                        if (IsNumericId(sub)) {
                            // 子引用是数字资源 ID：经 resources.index 的 id->路径 映射定位
                            bm = decodeMediaById(sub);
                            if (bm != null) { iconPath = entry + " -> " + k + " -> id:" + sub; return bm; }
                            continue;
                        }
                        bm = resolveMediaName(sub);
                        if (bm != null) { iconPath = entry + " -> " + k + " -> " + sub; return bm; }
                    }
                } catch { }
                return null;
            }
            return decodeMedia(name);
        }

        private static bool IsNumericId(string s) {
            if (s == null || s.Length == 0) return false;
            foreach (char c in s) if (c < '0' || c > '9') return false;
            return true;
        }

        /// <summary>数字资源 ID -> resources.index 路径 -> zip 条目 -> 位图。</summary>
        private Bitmap decodeMediaById(string idText) {
            ensureIndex();
            if (mediaPathById == null) return null;
            uint id;
            if (!uint.TryParse(idText, out id)) return null;
            string indexPath;
            if (!mediaPathById.TryGetValue(id, out indexPath)) return null;
            return decodeMediaEntry(indexPath);
        }

        /// <summary>index 内路径形如 "entry/resources/base/media/x.webp"，映射回 zip 条目名。</summary>
        private Bitmap decodeMediaEntry(string indexPath) {
            if (string.IsNullOrEmpty(indexPath)) return null;
            string entryName = indexPath;
            int i = indexPath.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase);
            if (i >= 0) entryName = indexPath.Substring(i + 1);
            byte[] b = readEntryByName(entryName);
            if (b == null) {
                // 条目名不匹配时按后缀兜底
                foreach (ZipEntry en in zip) {
                    if (en.IsFile && en.Name.EndsWith(indexPath, StringComparison.OrdinalIgnoreCase)) {
                        b = readEntry(zip, en);
                        break;
                    }
                }
            }
            if (b == null) return null;
            Bitmap bmp = decodeImage(b);
            if (bmp != null) {
                iconPath = entryName;
                iconSourceSize = new Size(bmp.Width, bmp.Height);
            }
            return bmp;
        }

        private Bitmap decodeMedia(string name) {
            if (mediaMap == null) buildMediaMap();
            string entry;
            if (!mediaMap.TryGetValue(name.ToLowerInvariant(), out entry)) return null;
            if (entry.ToLowerInvariant().EndsWith(".json") || entry.ToLowerInvariant().EndsWith(".svg"))
                return null;
            byte[] b = readEntryByName(entry);
            if (b == null) return null;
            Bitmap bmp = decodeImage(b);
            if (bmp != null) {
                iconPath = entry;
                iconSourceSize = new Size(bmp.Width, bmp.Height);
            }
            return bmp;
        }

        /// <summary>PNG/JPEG/GIF/BMP 走 GDI+；WebP（RIFF....WEBP）走 WebPWrapper。</summary>
        private static Bitmap decodeImage(byte[] b) {
            if (b == null || b.Length < 12) return null;
            if (b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' &&
                b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P') {
                try { return new WebP().Decode(b); } catch { return null; }
            }
            try { return new Bitmap(new MemoryStream(b)); } catch { return null; }
        }

        private void buildMediaMap() {
            mediaMap = new Dictionary<string, string>(StringComparer.Ordinal);
            const string prefix = "resources/base/media/";
            foreach (ZipEntry en in zip) {
                if (!en.IsFile) continue;
                string n = en.Name;
                if (n == null || n.Length <= prefix.Length) continue;
                if (!n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string file = n.Substring(prefix.Length);
                int slash = file.IndexOf('/');
                if (slash >= 0) continue; // 只取 media 根
                int dot = file.LastIndexOf('.');
                string key = (dot > 0 ? file.Substring(0, dot) : file).ToLowerInvariant();
                if (!mediaMap.ContainsKey(key)) mediaMap[key] = n;
            }
        }

        // ---------- 基础工具 ----------

        private static string str(Dictionary<string, object> dic, string key) {
            if (dic == null || !dic.ContainsKey(key)) return "";
            object v = dic[key];
            return v is string ? (string)v : (v == null ? "" : v.ToString());
        }

        private ZipEntry findEntry(string name) {
            return zip.GetEntry(name);
        }

        private byte[] readEntryByName(string name) {
            ZipEntry en = findEntry(name);
            return readEntry(zip, en);
        }

        private static byte[] readEntry(ZipFile z, ZipEntry en) {
            if (en == null || en.Size <= 0) return null;
            byte[] buf = new byte[en.Size];
            using (Stream s = z.GetInputStream(en)) {
                int got = 0, r;
                while (got < buf.Length && (r = s.Read(buf, got, buf.Length - got)) > 0) got += r;
                if (got != buf.Length) Array.Resize(ref buf, got);
            }
            return buf;
        }

        protected override void Dispose(bool disposing) {
            if (zip != null) try { zip.Close(); } catch { }
            zip = null;
            if (icon != null) try { icon.Dispose(); } catch { }
            icon = null;
            base.Dispose(disposing);
        }
    }
}

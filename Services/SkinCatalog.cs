using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonettoMascot.Services;

/// <summary>一套皮肤的元信息。</summary>
public sealed class SkinEntry
{
    /// <summary>目录名，同时也是 .atlas / .skel 的文件名前缀。</summary>
    public string Id { get; init; } = "";

    /// <summary>界面上显示的名字（一定有值，不会为 null）。</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>显示名从哪来的：config / display.txt / dir。设置面板会把它写进提示，方便排查。</summary>
    public string NameSource { get; init; } = "dir";

    /// <summary>皮肤目录；资源缺失时为 null。</summary>
    public string? Dir { get; init; }

    /// <summary>.atlas 与 .skel 齐全，真的能加载。</summary>
    public bool IsUsable { get; init; }

    /// <summary>是否为默认皮肤（决定排序优先级）。</summary>
    public bool IsDefault { get; init; }
}

/// <summary>
/// 皮肤目录扫描。
///
/// 约定：Assets/characters/&lt;id&gt;/ 下放同名的 &lt;id&gt;.atlas、&lt;id&gt;.skel 和图集，
/// 该目录即为一套皮肤。
///
/// **没有任何硬编码的皮肤列表**：扫到哪个目录就有哪套皮肤，删掉目录就从列表里消失。
/// 想自定义显示名，两条路（优先级从高到低）：
///   1. config.json 里的 SkinNames —— 用户自己起的名，优先级最高，且不用动资源目录；
///   2. 皮肤目录里的 display.txt —— 跟着模型走，换机器 / 分享给别人时一起带过去。
///   都没写就用目录名。
/// </summary>
public static class SkinCatalog
{
    /// <summary>默认皮肤 id。只作为「首次启动优先选谁」的偏好，不参与列表构造。</summary>
    public const string DefaultSkinId = "302301";

    private static List<SkinEntry>? _cache;

    /// <summary>config.json 里配置的自定义名字（三级兜底的第一级）。</summary>
    private static IReadOnlyDictionary<string, string>? _configuredNames;

    /// <summary>当前可用的全部皮肤（默认皮肤在最前，其余按显示名排序）。</summary>
    public static IReadOnlyList<SkinEntry> All => _cache ??= Scan();

    /// <summary>注入配置里的自定义名。名字变了就清缓存，下次访问重新扫描。</summary>
    public static void SetConfiguredNames(IReadOnlyDictionary<string, string>? names)
    {
        _configuredNames = names;
        _cache = null;
    }

    /// <summary>丢弃缓存，下次访问 All 时重新扫描。</summary>
    public static void Invalidate() => _cache = null;

    /// <summary>皮肤显示名：配置名 → display.txt → 目录名。</summary>
    public static string DisplayNameOf(string id)
    {
        var hit = All.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
        if (hit != null) return hit.DisplayName;

        // 不在缓存里（比如配置里存了已删掉的皮肤），现场按同一套兜底链算一个
        string? dir = DirOf(id);
        return ResolveName(id, dir);
    }

    /// <summary>某套皮肤的目录；找不到返回 null。</summary>
    public static string? DirOf(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (string root in RootCandidates())
        {
            string dir = Path.Combine(root, id);
            if (Directory.Exists(dir)) return dir;
        }
        return null;
    }

    /// <summary>characters 根目录的候选路径（程序目录优先，其次是开发时的源码目录）。</summary>
    public static IEnumerable<string> RootCandidates()
    {
        string baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "Assets", "characters");
        yield return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Assets", "characters"));
    }

    /// <summary>首次启动时选谁：默认皮肤可用就用它，否则第一套可用的。</summary>
    public static string FirstUsableId()
    {
        var all = All;
        var def = all.FirstOrDefault(e => e.IsDefault && e.IsUsable);
        if (def != null) return def.Id;
        foreach (var e in all)
            if (e.IsUsable) return e.Id;
        return "";
    }

    private static List<SkinEntry> Scan()
    {
        // id → 目录。同名时以先找到的为准（程序目录优先于源码目录）
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string root in RootCandidates())
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string id = Path.GetFileName(dir);
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (!found.ContainsKey(id)) found[id] = dir;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Skin] 扫描目录失败 {root}", ex);
            }
        }

        var list = new List<SkinEntry>(found.Count);
        foreach (var kv in found)
        {
            string id = kv.Key;
            string dir = kv.Value;

            bool usable = false;
            try
            {
                usable = File.Exists(Path.Combine(dir, id + ".atlas"))
                      && File.Exists(Path.Combine(dir, id + ".skel"));
            }
            catch (Exception ex) { Logger.Error($"[Skin] 检查 {id} 资源失败", ex); }

            string source = "dir";
            string name = ResolveName(id, dir, ref source);

            list.Add(new SkinEntry
            {
                Id = id,
                DisplayName = name,
                NameSource = source,
                Dir = dir,
                IsUsable = usable,
                IsDefault = string.Equals(id, DefaultSkinId, StringComparison.OrdinalIgnoreCase),
            });
        }

        // 排序：
        //   1) 默认皮肤永远第一（用户的「主」形象，不该被别的目录挤走）
        //   2) 能用的排在前面，缺资源的沉底（一眼看出哪套坏了）
        //   3) 其余按显示名，名字相同再按 id，保证列表顺序稳定不跳动
        list.Sort((a, b) =>
        {
            if (a.IsDefault != b.IsDefault) return a.IsDefault ? -1 : 1;
            if (a.IsUsable != b.IsUsable) return a.IsUsable ? -1 : 1;
            int c = string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCulture);
            if (c != 0) return c;
            return string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
        });

        Logger.Log($"[Skin] 扫描到 {list.Count} 套皮肤（可用 {list.Count(e => e.IsUsable)} 套）："
            + string.Join(", ", list.Select(e => $"{e.Id}={e.DisplayName}[{e.NameSource}]{(e.IsUsable ? "" : "(缺失)")}")));
        return list;
    }

    /// <summary>三级兜底：配置名 → display.txt → 目录名。任何一级读失败都不抛异常。</summary>
    private static string ResolveName(string id, string? dir)
    {
        string source = "dir";
        return ResolveName(id, dir, ref source);
    }

    private static string ResolveName(string id, string? dir, ref string source)
    {
        // ① 配置名（config.json 的 SkinNames）—— 优先级最高
        if (_configuredNames != null
            && _configuredNames.TryGetValue(id, out string? configured)
            && !string.IsNullOrWhiteSpace(configured))
        {
            source = "config";
            return Clip(configured);
        }

        // ② display.txt —— 跟着皮肤目录走
        if (dir != null)
        {
            try
            {
                string txt = Path.Combine(dir, "display.txt");
                if (File.Exists(txt))
                {
                    string raw = File.ReadAllText(txt).Trim();
                    // 只取第一行，并限长，避免有人把整篇说明贴进去
                    int nl = raw.IndexOfAny(new[] { '\r', '\n' });
                    if (nl > 0) raw = raw.Substring(0, nl).Trim();
                    // 顺手吃掉 UTF-8 BOM 残留
                    raw = raw.Trim('\uFEFF', ' ', '\t');
                    if (raw.Length > 0)
                    {
                        source = "display.txt";
                        return Clip(raw);
                    }
                }
            }
            catch (Exception ex) { Logger.Error($"[Skin] 读取 {id}/display.txt 失败", ex); }
        }

        // ③ 目录名
        source = "dir";
        return Clip(id);
    }

    /// <summary>显示名限长：卡片一行放不下太长的名字，截断比撑破布局好。</summary>
    private static string Clip(string s) => s.Length > 24 ? s.Substring(0, 24) : s;
}

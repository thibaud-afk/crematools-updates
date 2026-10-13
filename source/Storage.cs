using System.IO;
using System.Text.Json;
namespace CremaTools;

public static class Storage
{
    public static readonly string Root = Environment.GetEnvironmentVariable("CREMATOOLS_TEST_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CremaTools");
    public static string FilePath(string file) { Directory.CreateDirectory(Root); return Path.Combine(Root, file); }
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static T Read<T>(string file, T fallback) { try { return JsonSerializer.Deserialize<T>(File.ReadAllText(file), Json) ?? fallback; } catch { return fallback; } }
    public static void Write<T>(string file, T value) { Directory.CreateDirectory(Path.GetDirectoryName(file)!); var tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp"; File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json)); File.Move(tmp, file, true); }
    public static string Unique(string folder, string filename) { Directory.CreateDirectory(folder); filename = Path.GetFileName(filename); var candidate = Path.Combine(folder, filename); int n = 2; while (File.Exists(candidate) || Directory.Exists(candidate)) candidate = Path.Combine(folder, Path.GetFileNameWithoutExtension(filename) + " (" + (n++) + ")" + Path.GetExtension(filename)); return candidate; }
    public static string Clean(string value) { foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); value = value.Trim().TrimEnd('.'); if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Indiquez un nom."); return value; }
    public static string Size(long n) => n >= 1048576 ? $"{n / 1048576d:0.0} Mo" : $"{n / 1024d:0} Ko";
}

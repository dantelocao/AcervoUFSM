using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class StorageBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void SetLocalStorage(string key, string json);
    [DllImport("__Internal")] private static extern string GetLocalStorage(string key);
    [DllImport("__Internal")] private static extern void DownloadText(string filename, string content);
#endif

    private static readonly Dictionary<string, string> _mem = new();

    // --- Salvar no localStorage ou em memória fallback ---
    public static void SaveLocal(string key, string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { SetLocalStorage(key, json ?? ""); }
        catch (System.Exception e) { Debug.LogError($"[StorageBridge] SetLocalStorage error: {e}"); }
#else
        _mem[key] = json ?? "";
        Debug.Log($"[StorageBridge] fallback SaveLocal: {key}");
#endif
    }

    // --- Carregar do localStorage ou memória fallback ---
    public static string LoadLocal(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return GetLocalStorage(key); }
        catch (System.Exception e)
        {
            Debug.LogError($"[StorageBridge] GetLocalStorage error: {e}");
            return null;
        }
#else
        return _mem.TryGetValue(key, out var v) ? v : null;
#endif
    }

    // --- Salvar como arquivo para download ---
    public static void SaveAsDownload(string filename, string content)
    {
        // sempre sanitiza o nome do arquivo
        var safeFileName = MakeSafeFileName(filename, "Scenario", ".json");

#if UNITY_WEBGL && !UNITY_EDITOR
        try { DownloadText(safeFileName, content ?? ""); }
        catch (System.Exception e) { Debug.LogError($"[StorageBridge] DownloadText error: {e}"); }
#else
        var dir = Path.Combine(Application.dataPath, "_ScenariosJson");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.Combine(dir, safeFileName);
        try
        {
            File.WriteAllText(path, content ?? "");
            Debug.Log($"[StorageBridge] fallback wrote: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StorageBridge] SaveAsDownload(File) error: {e}");
        }
#endif
    }

    // --- Helper: gera um nome seguro de arquivo ---
    private static string MakeSafeFileName(string raw, string defaultBase, string requiredExtension)
    {
        if (string.IsNullOrWhiteSpace(raw)) raw = defaultBase;

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            sb.Append(System.Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        var fileName = sb.ToString().Trim();
        if (string.IsNullOrEmpty(fileName)) fileName = defaultBase;

        // remove pontos/espacos do fim
        fileName = fileName.Trim().TrimEnd('.');

        // garante extensão
        if (!string.IsNullOrEmpty(requiredExtension) &&
            !fileName.EndsWith(requiredExtension, System.StringComparison.OrdinalIgnoreCase))
        {
            fileName += requiredExtension;
        }

        return fileName;
    }
}

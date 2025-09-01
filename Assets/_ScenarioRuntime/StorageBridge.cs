using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class StorageBridge
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void SetLocalStorage(string key, string json);
    [DllImport("__Internal")] private static extern string GetLocalStorage(string key);
    [DllImport("__Internal")] private static extern void DownloadText(string filename, string content);
#endif

    private static readonly Dictionary<string, string> _mem = new();

    public static void SaveLocal(string key, string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SetLocalStorage(key, json);
#else
        _mem[key] = json;
        Debug.Log($"[StorageBridge] fallback SaveLocal: {key}");
#endif
    }

    public static string LoadLocal(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return GetLocalStorage(key);
#else
        return _mem.TryGetValue(key, out var v) ? v : null;
#endif
    }

    public static void SaveAsDownload(string filename, string content)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadText(filename, content);
#else
        var dir = Path.Combine(Application.dataPath, "_ScenariosJson");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var path = Path.Combine(dir, filename);
        System.IO.File.WriteAllText(path, content);
        Debug.Log($"[StorageBridge] fallback wrote: {path}");
#endif
    }
}

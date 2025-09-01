using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;


public class ScenarioRuntimeUI : MonoBehaviour
{
    [Header("Refs")]
    public SceneStateManager manager;

    [Header("UI (opcional)")]
    public Button btnCaptureToLocal;
    public Button btnApplyFromLocal;
    public Button btnResetDefault;
    public Button btnDownloadJson;
    public TMP_InputField scenarioNameInput;

    [Header("LocalStorage")]
    public string localKey = "scenario_current";

    [Header("Viewer por URL")]
    public bool autoLoadFromUrlParam = true; // lê ?scenarioUrl=...

    void Awake()
    {
        if (btnCaptureToLocal) btnCaptureToLocal.onClick.AddListener(CaptureToLocal);
        if (btnApplyFromLocal) btnApplyFromLocal.onClick.AddListener(ApplyFromLocal);
        if (btnResetDefault) btnResetDefault.onClick.AddListener(manager.ResetToDefault);
        if (btnDownloadJson) btnDownloadJson.onClick.AddListener(DownloadJson);
    }

    void Start()
    {
#if UNITY_WEBGL
        if (autoLoadFromUrlParam)
        {
            var url = Application.absoluteURL;
            var sUrl = GetQuery(url, "scenarioUrl");
            if (!string.IsNullOrEmpty(sUrl))
                StartCoroutine(LoadFromRemoteUrl(sUrl));
        }
#endif
    }

    public void CaptureToLocal()
    {
        var name = scenarioNameInput && !string.IsNullOrEmpty(scenarioNameInput.text)
                   ? scenarioNameInput.text : "RuntimeCapture";
        var data = manager.Capture(name, includeMaterials: true);
        var json = manager.ToJson(data, true);
        StorageBridge.SaveLocal(localKey, json);
        Debug.Log($"[ScenarioRuntimeUI] Saved to localStorage '{localKey}'");
    }

    public void ApplyFromLocal()
    {
        var js = StorageBridge.LoadLocal(localKey);
        if (string.IsNullOrEmpty(js)) { Debug.LogWarning("localStorage vazio."); return; }
        var data = manager.FromJson(js);
        if (data == null) { Debug.LogError("JSON inválido."); return; }
        manager.Apply(data);
    }

    public void DownloadJson()
    {
        var name = scenarioNameInput && !string.IsNullOrEmpty(scenarioNameInput.text)
                   ? scenarioNameInput.text : "Scenario";
        var data = manager.Capture(name, includeMaterials: true);
        var json = manager.ToJson(data, true);
        StorageBridge.SaveAsDownload($"{name}.json", json);
    }

    private IEnumerator LoadFromRemoteUrl(string jsonUrl)
    {
        using var req = UnityWebRequest.Get(jsonUrl);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) { Debug.LogError(req.error); yield break; }
        var data = manager.FromJson(req.downloadHandler.text);
        if (data == null) { Debug.LogError("JSON remoto inválido."); yield break; }
        manager.Apply(data);
        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON remoto.");
    }


    private static string GetQuery(string url, string key)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key)) return null;

        var i = url.IndexOf('?');
        if (i < 0) return null;

        var query = url.Substring(i + 1);
        foreach (var kv in query.Split('&'))
        {
            var j = kv.IndexOf('=');
            if (j <= 0) continue;

            var k = UnityWebRequest.UnEscapeURL(kv.Substring(0, j));
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;

            return UnityWebRequest.UnEscapeURL(kv.Substring(j + 1));
        }

        return null;
    }

}

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScenarioRuntimeUI : MonoBehaviour
{
    [Header("Refs")]
    public SceneStateManager manager;
    public ScenarioIO io;

    [Header("UI")]
    public Button btnCaptureToLocal;
    public Button btnApplyFromLocal;
    public Button btnResetDefault;
    public Button btnDownloadJson;
    public Button btnApplyFromFile;
    public TMP_InputField scenarioNameInput;

    [Header("LocalStorage")]
    public string localKey = "scenario_current";

    [Header("Viewer por URL")]
    public bool autoLoadFromUrlParam = true;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void OpenFilePicker(string gameObjectName, string callbackName);
#endif

    private void Awake()
    {
        if (!manager)
            manager = FindObjectOfType<SceneStateManager>();

        if (!io)
            io = FindObjectOfType<ScenarioIO>();

        if (btnCaptureToLocal) btnCaptureToLocal.onClick.AddListener(CaptureToLocal);
        if (btnApplyFromLocal) btnApplyFromLocal.onClick.AddListener(ApplyFromLocal);
        if (btnResetDefault) btnResetDefault.onClick.AddListener(manager.ResetToDefault);
        if (btnDownloadJson) btnDownloadJson.onClick.AddListener(DownloadJson);
        if (btnApplyFromFile) btnApplyFromFile.onClick.AddListener(ApplyFromFile);
    }

    private void Start()
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

    // =====================================================================
    // CAPTURE  SALVAR EM LOCALSTORAGE
    // =====================================================================
    public void CaptureToLocal()
    {
        var name = scenarioNameInput && !string.IsNullOrEmpty(scenarioNameInput.text)
            ? scenarioNameInput.text
            : "RuntimeCapture";

        var data = io.Capture();
        var json = JsonUtility.ToJson(data, true);

        StorageBridge.SaveLocal(localKey, json);
        Debug.Log("[ScenarioRuntimeUI] Saved to localStorage '" + localKey + "'");
    }

    // =====================================================================
    // APPLY  CARREGAR DO LOCALSTORAGE
    // =====================================================================
    public void ApplyFromLocal()
    {
        var js = StorageBridge.LoadLocal(localKey);

        if (string.IsNullOrEmpty(js))
        {
            Debug.LogWarning("localStorage vazio.");
            return;
        }

        var data = JsonUtility.FromJson<ScenarioData>(js);
        if (data == null)
        {
            Debug.LogError("JSON inválido.");
            return;
        }

        io.Apply(data);

        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON do localStorage.");
    }

    // =====================================================================
    // DOWNLOAD JSON
    // =====================================================================
    public void DownloadJson()
    {
        var baseName = scenarioNameInput && !string.IsNullOrEmpty(scenarioNameInput.text)
            ? scenarioNameInput.text
            : "Scenario";

        var safeFileName = MakeSafeFileName(baseName, "Scenario", ".json");

        var data = io.Capture();
        var json = JsonUtility.ToJson(data, true);

        StorageBridge.SaveAsDownload(safeFileName, json);
    }

    // =====================================================================
    // APPLY FROM FILE (UPLOAD)
    // =====================================================================
    public void ApplyFromFile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        OpenFilePicker(gameObject.name, "OnFileJsonLoaded");
#else
#if UNITY_EDITOR
        var path = EditorUtility.OpenFilePanel("Selecione o JSON de cenário", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<ScenarioData>(json);

            if (data == null)
            {
                Debug.LogError("JSON inválido.");
                return;
            }

            io.Apply(data);
            Debug.Log("[ScenarioRuntimeUI] Aplicado JSON de arquivo (Editor).");
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro ao ler arquivo: " + ex.Message);
        }
#else
        Debug.LogWarning("Abrir arquivo local disponível somente no Editor ou WebGL via JS.");
#endif
#endif
    }

    public void OnFileJsonLoaded(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Nenhum conteúdo recebido do arquivo selecionado.");
            return;
        }

        var data = JsonUtility.FromJson<ScenarioData>(json);
        if (data == null)
        {
            Debug.LogError("JSON inválido recebido do arquivo.");
            return;
        }

        io.Apply(data);
        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON de upload (WebGL).");
    }

    // =====================================================================
    // REMOTE LOAD
    // =====================================================================
    private IEnumerator LoadFromRemoteUrl(string jsonUrl)
    {
        using var req = UnityWebRequest.Get(jsonUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        var data = JsonUtility.FromJson<ScenarioData>(req.downloadHandler.text);
        if (data == null)
        {
            Debug.LogError("JSON remoto inválido.");
            yield break;
        }

        io.Apply(data);

        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON remoto.");
    }

    // =====================================================================
    // HELPERS
    // =====================================================================
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

    private static string MakeSafeFileName(string raw, string defaultBase = "Scenario", string requiredExtension = ".json")
    {
        if (string.IsNullOrWhiteSpace(raw)) raw = defaultBase;

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(raw.Length);

        foreach (var ch in raw)
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);

        var fileName = sb.ToString().Trim();
        if (string.IsNullOrEmpty(fileName)) fileName = defaultBase;

        fileName = fileName.Trim().TrimEnd('.');

        if (!fileName.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            fileName += requiredExtension;

        return fileName;
    }
}

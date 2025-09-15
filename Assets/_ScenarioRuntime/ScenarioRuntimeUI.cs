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

    [Header("UI (opcional)")]
    public Button btnCaptureToLocal;
    public Button btnApplyFromLocal;
    public Button btnResetDefault;
    public Button btnDownloadJson;
    public Button btnApplyFromFile; // NOVO: bot�o para carregar .json do computador
    public TMP_InputField scenarioNameInput;

    [Header("LocalStorage")]
    public string localKey = "scenario_current";

    [Header("Viewer por URL")]
    public bool autoLoadFromUrlParam = true; // l� ?scenarioUrl=...

#if UNITY_WEBGL && !UNITY_EDITOR
    // Ponte para o plugin .jslib (WebGL)
    [DllImport("__Internal")] private static extern void OpenFilePicker(string gameObjectName, string callbackName);
#endif

    void Awake()
    {
        if (btnCaptureToLocal) btnCaptureToLocal.onClick.AddListener(CaptureToLocal);
        if (btnApplyFromLocal) btnApplyFromLocal.onClick.AddListener(ApplyFromLocal);
        if (btnResetDefault) btnResetDefault.onClick.AddListener(manager.ResetToDefault);
        if (btnDownloadJson) btnDownloadJson.onClick.AddListener(DownloadJson);
        if (btnApplyFromFile) btnApplyFromFile.onClick.AddListener(ApplyFromFile); // wire do bot�o novo
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
                   ? scenarioNameInput.text
                   : "RuntimeCapture";

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
        if (data == null) { Debug.LogError("JSON inv�lido."); return; }

        manager.Apply(data);
        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON do localStorage.");
    }

    public void DownloadJson()
    {
        var baseName = scenarioNameInput && !string.IsNullOrEmpty(scenarioNameInput.text)
                       ? scenarioNameInput.text
                       : "Scenario";

        // Gera um filename seguro para o SO/navegador
        var safeFileName = MakeSafeFileName(baseName, "Scenario", ".json");

        var data = manager.Capture(baseName, includeMaterials: true);
        var json = manager.ToJson(data, true);

        // Usa o nome sanitizado ao salvar/baixar
        StorageBridge.SaveAsDownload(safeFileName, json);
    }

    // ===== NOVO: carregar .json do computador =====
    public void ApplyFromFile()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // No WebGL: abre o seletor de arquivos; o JS chamar� OnFileJsonLoaded(string)
        OpenFilePicker(gameObject.name, "OnFileJsonLoaded");
#else
#if UNITY_EDITOR
        // Editor: abre um di�logo nativo para escolher o arquivo .json
        var path = EditorUtility.OpenFilePanel("Selecione o JSON de cen�rio", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            var data = manager.FromJson(json);
            if (data == null) { Debug.LogError("JSON inv�lido."); return; }
            manager.Apply(data);
            Debug.Log("[ScenarioRuntimeUI] Aplicado JSON de arquivo (Editor).");
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro ao ler arquivo: " + ex.Message);
        }
#else
        Debug.LogWarning("Abrir arquivo local est� implementado via JS para WebGL e via EditorUtility no Editor.");
#endif
#endif
    }

    // Callback chamado pelo .jslib no WebGL com o conte�do do arquivo selecionado
    public void OnFileJsonLoaded(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Nenhum conte�do recebido do arquivo selecionado.");
            return;
        }

        var data = manager.FromJson(json);
        if (data == null)
        {
            Debug.LogError("JSON inv�lido recebido do arquivo.");
            return;
        }

        manager.Apply(data);
        Debug.Log("[ScenarioRuntimeUI] Aplicado JSON de upload (WebGL).");
    }
    // ===== FIM da se��o NOVA =====

    private IEnumerator LoadFromRemoteUrl(string jsonUrl)
    {
        using var req = UnityWebRequest.Get(jsonUrl);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(req.error);
            yield break;
        }

        var data = manager.FromJson(req.downloadHandler.text);
        if (data == null)
        {
            Debug.LogError("JSON remoto inv�lido.");
            yield break;
        }

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

    // Helper para gerar nomes de arquivo seguros (evita caracteres ilegais e garante .json)
    private static string MakeSafeFileName(string raw, string defaultBase = "Scenario", string requiredExtension = ".json")
    {
        if (string.IsNullOrWhiteSpace(raw)) raw = defaultBase;

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
        {
            sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
        }

        var fileName = sb.ToString().Trim();
        if (string.IsNullOrEmpty(fileName)) fileName = defaultBase;

        // Evita terminar com ponto/espa�o
        fileName = fileName.Trim().TrimEnd('.');

        // Garante a extens�o
        if (!fileName.EndsWith(requiredExtension, StringComparison.OrdinalIgnoreCase))
            fileName += requiredExtension;

        return fileName;
    }
}

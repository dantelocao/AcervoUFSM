using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerScenarioLoader : MonoBehaviour
{
    [Header("Refs")]
    public ScenarioIO io;                     // arraste o ScenarioIO
    public SceneStateManager sceneState;      // arraste o SceneStateManager

    [Header("Config")]
    public bool autoLoadFromUrl = true;       // se deve buscar scenarioUrl automaticamente

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (autoLoadFromUrl)
        {
            string fullUrl = Application.absoluteURL;
            string jsonUrl = GetQuery(fullUrl, "scenarioUrl");

            if (!string.IsNullOrEmpty(jsonUrl))
            {
                Debug.Log("[PlayerScenarioLoader] URL recebida: " + jsonUrl);
                StartCoroutine(LoadAndApply(jsonUrl));
            }
            else
            {
                Debug.LogWarning("[PlayerScenarioLoader] Nenhum ?scenarioUrl= informado.");
            }
        }
#else
        Debug.Log("[PlayerScenarioLoader] Fora do WebGL — autoLoad ignorado.");
#endif
    }

    // ============================================================
    // CARREGAR JSON REMOTO
    // ============================================================
    private IEnumerator LoadAndApply(string url)
    {
        // ============================================================
        //  FIX: Converter URL RELATIVA → ABSOLUTA
        // ============================================================
        if (url.StartsWith("/"))
        {
            string appUrl = Application.absoluteURL;

            // extrai base: https://dominio.com
            Uri baseUri = new Uri(appUrl);
            string fixedUrl = baseUri.GetLeftPart(UriPartial.Authority) + url;

            Debug.Log("[PlayerScenarioLoader] URL relativa corrigida para: " + fixedUrl);
            url = fixedUrl;
        }

        using var req = UnityWebRequest.Get(url);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[PlayerScenarioLoader] Erro ao baixar JSON: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;

        ScenarioData data = null;

        try
        {
            Debug.Log("[PlayerScenarioLoader] JSON recebido (raw): " + json);
            data = JsonUtility.FromJson<ScenarioData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[PlayerScenarioLoader] JSON inválido: " + e.Message);
            yield break;
        }

        if (data == null)
        {
            Debug.LogError("[PlayerScenarioLoader] JSON nulo.");
            yield break;
        }

        Debug.Log("[PlayerScenarioLoader] Aplicando cenário...");
        io.Apply(data);

        sceneState.CurrentData = data;

        ApplySkyboxDirect(data);

        Debug.Log("[PlayerScenarioLoader] Cenário carregado!");
    }

    // ============================================================
    // APLICAR SKYBOX DIRETO DO JSON
    // ============================================================
    private void ApplySkyboxDirect(ScenarioData data)
    {
        string matName = data.GetSkyboxMaterialNameOrNull();

        if (string.IsNullOrEmpty(matName))
        {
            Debug.Log("[PlayerScenarioLoader] Nenhum skybox salvo no JSON.");
            return;
        }

        // procura material no Resources
        Material mat = Resources.Load<Material>(matName);

        if (!mat)
        {
            Debug.LogWarning($"[PlayerScenarioLoader] Skybox '{matName}' NÃO encontrado no Resources.");
            return;
        }

        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();

        Debug.Log("[PlayerScenarioLoader] Skybox aplicado: " + matName);
    }

    // ============================================================
    // PARSER DE QUERYSTRING
    // ============================================================
    private static string GetQuery(string fullUrl, string key)
    {
        if (string.IsNullOrEmpty(fullUrl) || string.IsNullOrEmpty(key))
            return null;

        int idx = fullUrl.IndexOf('?');
        if (idx < 0) return null;

        string query = fullUrl.Substring(idx + 1);
        foreach (var part in query.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq <= 0) continue;

            string k = UnityWebRequest.UnEscapeURL(part.Substring(0, eq));
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) continue;

            return UnityWebRequest.UnEscapeURL(part.Substring(eq + 1));
        }

        return null;
    }
}

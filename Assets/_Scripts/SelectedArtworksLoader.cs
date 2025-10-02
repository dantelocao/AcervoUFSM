using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Baixa e aplica as imagens de ScenarioData.artworks por objectId,
/// usando cache em sessão, MaterialPropertyBlock (sem instanciar materiais),
/// proxy no WebGL (CORS) e limite de concorrência.
/// </summary>
public class SelectedArtworksLoader : MonoBehaviour
{
    [Header("Concorrência")]
    [Range(1, 8)] public int maxConcurrentDownloads = 4;

    [Header("WebGL Proxy (somente WebGL)")]
    [Tooltip("Prefixo para contornar CORS no WebGL. Ex.: https://.../api/img?url=")]
    public string proxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

    // cache por sessão (URL -> Texture2D)
    private readonly Dictionary<string, Texture2D> _cache = new();
    private int _inFlight;

    // IDs de propriedades comuns (URP / Built-in)
    static readonly int BaseMapID = Shader.PropertyToID("_BaseMap"); // URP
    static readonly int MainTexID = Shader.PropertyToID("_MainTex"); // Built-in

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern IntPtr GetLocalStorage(string key);
#endif

    /// <summary>
    /// Aplica ScenarioData.artworks fazendo objectId -> EditableObject -> ArtworkSlot -> Renderer.
    /// </summary>
    public IEnumerator ApplyArtworksById(
        IEnumerable<ArtworkEntryById> entries,
        IReadOnlyDictionary<string, EditableObject> idToEditable)
    {
        if (entries == null) yield break;

        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.objectId) || string.IsNullOrWhiteSpace(e.imageUrl))
                continue;

            if (!idToEditable.TryGetValue(e.objectId, out var eo) || eo == null)
                continue;

            var slot = eo.GetComponentInChildren<ArtworkSlot>(true);
            if (slot == null || slot.TargetRenderer == null)
                continue;

            // Limita simultâneos
            while (_inFlight >= Mathf.Max(1, maxConcurrentDownloads))
                yield return null;

            _inFlight++;
            yield return StartCoroutine(DownloadAndApply(e.imageUrl, slot.TargetRenderer));
            _inFlight--;
        }
    }

    /// <summary>
    /// Limpa o cache de sessão (se quiser forçar re-download).
    /// </summary>
    public void ClearSessionCache()
    {
        _cache.Clear();
    }

    // ----------------- Internals -----------------

    private IEnumerator DownloadAndApply(string imageUrl, Renderer r)
    {
        if (!r) yield break;

        // cache de sessão
        if (_cache.TryGetValue(imageUrl, out var cached) && cached)
        {
            ApplyTexWithMPB(r, cached);
            yield break;
        }

        var finalUrl = Proxied(imageUrl);

        using (var req = UnityWebRequestTexture.GetTexture(finalUrl, true)) // nonReadable = true (economiza RAM)
        {
            req.timeout = 30;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[ArtworksLoader] Falha download: {req.error} | {finalUrl}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (!tex)
            {
                Debug.LogWarning($"[ArtworksLoader] Texture nula: {finalUrl}");
                yield break;
            }

            _cache[imageUrl] = tex;
            ApplyTexWithMPB(r, tex);
        }
    }

    private void ApplyTexWithMPB(Renderer r, Texture2D tex)
    {
        if (!r || !tex) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);

        // Tenta URP (_BaseMap); se não, Built-in (_MainTex)
        if (HasProperty(r, BaseMapID)) mpb.SetTexture(BaseMapID, tex);
        else if (HasProperty(r, MainTexID)) mpb.SetTexture(MainTexID, tex);
        else mpb.SetTexture(MainTexID, tex); // fallback

        r.SetPropertyBlock(mpb);
    }

    private static bool HasProperty(Renderer r, int propId)
    {
        var mat = r.sharedMaterial;
        return mat && mat.HasProperty(propId);
    }

    private string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(proxyBase))
            return proxyBase + Uri.EscapeDataString(remoteUrl);
#endif
        return remoteUrl;
    }
}

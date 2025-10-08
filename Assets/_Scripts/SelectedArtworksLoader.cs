using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class SelectedArtworksLoader : MonoBehaviour
{
    [Header("Planos que receber?o as texturas")]
    public Renderer[] planeRenderers;

    [Header("Chave do localStorage (React)")]
    public string localStorageKey = "selectedArtworks";

    // Base do proxy (usado somente no WebGL)
    private const string ProxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern IntPtr GetLocalStorage(string key);
    [DllImport("__Internal")] private static extern void   SetLocalStorage(string key, string json); // opcional
#endif

    private static string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // IMPORTANTE: respeitar o mesmo case de "ProxyBase"
        return ProxyBase + Uri.EscapeDataString(remoteUrl);
#else
        return remoteUrl;
#endif
    }

    private void Start()
    {
        StartCoroutine(LoadSelectedImages());
    }

    private IEnumerator LoadSelectedImages()
    {
        // Checagem b?sica
        if (planeRenderers == null || planeRenderers.Length == 0)
        {
            Debug.LogWarning("[SelectedArtworksLoader] Nenhum Renderer atribu?do no Inspector.");
            yield break;
        }

        string json = ReadSelectedJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"[SelectedArtworksLoader] Nada encontrado em '{localStorageKey}'.");
            yield break;
        }

        JArray arr;
        try
        {
            arr = JArray.Parse(json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SelectedArtworksLoader] JSON inv?lido em selectedArtworks: " + e.Message);
            yield break;
        }

        if (arr.Count == 0)
        {
            Debug.LogWarning("[SelectedArtworksLoader] Array de artworks vazio.");
            yield break;
        }

        int count = Mathf.Min(arr.Count, planeRenderers.Length);
        for (int i = 0; i < count; i++)
        {
            if (planeRenderers[i] == null)
            {
                Debug.LogWarning($"[SelectedArtworksLoader] Renderer nulo no ?ndice {i}.");
                continue;
            }

            var item = arr[i] as JObject;
            if (item == null)
            {
                Debug.LogWarning($"[SelectedArtworksLoader] Item {i} n?o ? um objeto JSON.");
                continue;
            }

            string imageUrl = item["imageUrl"]?.ToString();
            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogWarning($"[SelectedArtworksLoader] Sem imageUrl para item {i}.");
                continue;
            }

            yield return StartCoroutine(ApplyImageToPlane(imageUrl, planeRenderers[i]));
        }
    }

    private string ReadSelectedJson()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            IntPtr ptr = GetLocalStorage(localStorageKey);
            if (ptr == IntPtr.Zero) return null;

            // L? UTF-8; dispon?vel em Unity 2021+ / .NET 4.x
            string s = Marshal.PtrToStringUTF8(ptr);

            // Se quiser ser 100% correto com mem?ria, exponha um Free(ptr) no .jslib e chame aqui.
            return s;
        }
        catch (Exception e)
        {
            Debug.LogError("[SelectedArtworksLoader] Erro ao ler localStorage no WebGL: " + e.Message);
            return null;
        }
#else
        // Editor/Standalone: fallback opcional via PlayerPrefs
        return PlayerPrefs.HasKey(localStorageKey) ? PlayerPrefs.GetString(localStorageKey) : null;
#endif
    }

    private IEnumerator ApplyImageToPlane(string imageUrl, Renderer planeRenderer)
    {
        string finalUrl = Proxied(imageUrl);

        using (UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            imgRequest.timeout = 30; // evita travar
            yield return imgRequest.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (imgRequest.result != UnityWebRequest.Result.Success)
#else
            if (imgRequest.isNetworkError || imgRequest.isHttpError)
#endif
            {
                Debug.LogError("[SelectedArtworksLoader] Erro ao baixar imagem: " + imgRequest.error +
                               " | URL: " + finalUrl);
                yield break;
            }

            Texture2D tex = null;
            try
            {
                tex = DownloadHandlerTexture.GetContent(imgRequest);
            }
            catch (Exception e)
            {
                Debug.LogError("[SelectedArtworksLoader] Erro ao obter Texture2D: " + e.Message);
            }

            if (tex == null)
            {
                Debug.LogError("[SelectedArtworksLoader] Texture2D nula para URL: " + finalUrl);
                yield break;
            }

            planeRenderer.material.mainTexture = tex;
        }
    }
}
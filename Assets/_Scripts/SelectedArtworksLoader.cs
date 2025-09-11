using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class SelectedArtworksLoader : MonoBehaviour
{
    [Header("Planos que receberão as texturas")]
    public Renderer[] planeRenderers;

    [Header("Chave do localStorage (React)")]
    public string localStorageKey = "selectedArtworks";

    [Header("Proxy para WebGL (CORS)")]
    public string proxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern IntPtr GetLocalStorage(string key);
    [DllImport("__Internal")] private static extern void   SetLocalStorage(string key, string json); // opcional, caso queira escrever
#endif

    private string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return proxyBase + Uri.EscapeDataString(remoteUrl);
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
        string json = ReadSelectedJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"[SelectedArtworksLoader] Nada encontrado em '{localStorageKey}'.");
            yield break;
        }

        JArray arr;
        try { arr = JArray.Parse(json); }
        catch (Exception e)
        {
            Debug.LogError("[SelectedArtworksLoader] JSON inválido em selectedArtworks: " + e.Message);
            yield break;
        }

        int count = Mathf.Min(arr.Count, planeRenderers.Length);
        for (int i = 0; i < count; i++)
        {
            var item = arr[i] as JObject;
            string imageUrl = item?["imageUrl"]?.ToString();

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

            // Seu jslib escreve UTF-8, então use PtrToStringUTF8:
            string s = Marshal.PtrToStringUTF8(ptr);

            // OBS: o jslib aloca com _malloc mas não expõe _free.
            // Para poucas leituras isso é ok; se quiser rigor, adicione no jslib um wrapper Free(ptr) e chame aqui.

            return s;
        }
        catch (Exception e)
        {
            Debug.LogError("[SelectedArtworksLoader] Erro ao ler localStorage no WebGL: " + e.Message);
            return null;
        }
#else
        // Editor/Standalone: se quiser, carregue de PlayerPrefs ou de um TextAsset de teste
        return PlayerPrefs.HasKey(localStorageKey) ? PlayerPrefs.GetString(localStorageKey) : null;
#endif
    }

    private IEnumerator ApplyImageToPlane(string imageUrl, Renderer planeRenderer)
    {
        using (UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(Proxied(imageUrl)))
        {
            yield return imgRequest.SendWebRequest();

            if (imgRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SelectedArtworksLoader] Erro ao baixar imagem: " + imgRequest.error + " | URL: " + imageUrl);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(imgRequest);
            planeRenderer.material.mainTexture = tex;
        }
    }
}

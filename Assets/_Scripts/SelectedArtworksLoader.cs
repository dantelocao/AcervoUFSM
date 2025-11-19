using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class SelectedArtworksLoader : MonoBehaviour
{
    [Header("Prefab e Parent")]
    public GameObject obraPrefab;
    public Transform obraParent;

    [Header("Layout")]
    public Vector3 startPosition = Vector3.zero;
    public float offsetX = 1.0f;

    [Header("Chave do localStorage (React)")]
    public string localStorageKey = "selectedArtworks";

    private const string ProxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern IntPtr GetLocalStorage(string key);
#endif

    private static string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return ProxyBase + Uri.EscapeDataString(remoteUrl);
#else
        return remoteUrl;
#endif
    }

    private void Start()
    {
        if (obraPrefab == null)
        {
            Debug.LogError("[SelectedArtworksLoader] Nenhum prefab atribuído.");
            return;
        }

        if (obraParent == null)
        {
            Debug.LogError("[SelectedArtworksLoader] Nenhum parent atribuído.");
            return;
        }

        StartCoroutine(SpawnArtworksFromJson());
    }

    private IEnumerator SpawnArtworksFromJson()
    {
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
            Debug.LogError("[SelectedArtworksLoader] JSON inválido: " + e.Message);
            yield break;
        }

        if (arr.Count == 0)
        {
            Debug.LogWarning("[SelectedArtworksLoader] Nenhuma obra selecionada.");
            yield break;
        }

        for (int i = 0; i < arr.Count; i++)
        {
            JObject item = arr[i] as JObject;
            if (item == null)
            {
                Debug.LogWarning($"[SelectedArtworksLoader] Item {i} não é JSON válido.");
                continue;
            }

            string imageUrl = item["imageUrl"]?.ToString();
            if (string.IsNullOrEmpty(imageUrl))
            {
                Debug.LogWarning($"[SelectedArtworksLoader] Sem imageUrl em item {i}.");
                continue;
            }

            Vector3 pos = startPosition + new Vector3(offsetX * i, 0f, 0f);

            GameObject go = Instantiate(obraPrefab, pos, obraPrefab.transform.rotation, obraParent);

            ArtworkSlot slot = go.GetComponentInChildren<ArtworkSlot>();
            if (slot == null || !slot.IsValid)
            {
                Debug.LogError($"[SelectedArtworksLoader] Prefab sem ArtworkSlot. Index {i}");
                continue;
            }

            yield return StartCoroutine(ApplyImage(imageUrl, slot.TargetRenderer));

            ArtworkInfo info = go.GetComponent<ArtworkInfo>();
            if (info == null) info = go.AddComponent<ArtworkInfo>();
            info.imageUrl = imageUrl;

            EditableObject eo = go.GetComponent<EditableObject>();
            if (eo != null && SceneStateManager.Instance != null && SceneStateManager.Instance.CurrentData != null)
            {
                SceneStateManager.Instance.CurrentData.artworks.Add(new ArtworkEntryById
                {
                    objectId = eo.Id,
                    imageUrl = imageUrl
                });

                Debug.Log($"Registrada artwork: ID={eo.Id} URL={imageUrl}");
            }
            else
            {
                Debug.LogError("[SelectedArtworksLoader] NÃO conseguiu registrar artwork no CurrentData.");
            }
        }
    }

    private string ReadSelectedJson()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            IntPtr ptr = GetLocalStorage(localStorageKey);
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStringUTF8(ptr);
        }
        catch (Exception e)
        {
            Debug.LogError("[SelectedArtworksLoader] Erro ao ler localStorage WebGL: " + e.Message);
            return null;
        }
#else
        return PlayerPrefs.HasKey(localStorageKey) ? PlayerPrefs.GetString(localStorageKey) : null;
#endif
    }

    private IEnumerator ApplyImage(string imageUrl, Renderer renderer)
    {
        string finalUrl = Proxied(imageUrl);

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            req.timeout = 30;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("[SelectedArtworksLoader] Erro ao baixar imagem: " + req.error);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            renderer.material.mainTexture = tex;
        }
    }
}

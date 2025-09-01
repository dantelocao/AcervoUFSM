using System.Collections;
using System;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class TainacanImageLoader : MonoBehaviour
{
    // ? seu endpoint original
    public string apiUrl = "https://tainacan.ufsm.br/acervo-artistico/wp-json/tainacan/v2/collection/2174/items/";
    public Renderer[] planeRenderers; // arraste os planos aqui no Inspector

    // ? base do proxy (Vercel)
    const string ProxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

    // ? usa proxy só no WebGL (no Editor/Standalone acessa direto)
    static string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return ProxyBase + Uri.EscapeDataString(remoteUrl);
#else
        return remoteUrl;
#endif
    }

    void Start()
    {
        StartCoroutine(LoadImages());
    }

    IEnumerator LoadImages()
    {
        // IMPORTANTE: JSON também é cross-origin ? passe pelo proxy no WebGL
        using (UnityWebRequest uwr = UnityWebRequest.Get(Proxied(apiUrl)))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro na requisição: " + uwr.error);
                yield break;
            }

            string json = uwr.downloadHandler.text;
            Debug.Log("JSON recebido: " + json.Substring(0, Mathf.Min(200, json.Length)) + "...");

            JObject obj;
            try { obj = JObject.Parse(json); }
            catch (System.Exception e)
            {
                Debug.LogError("Erro ao parsear JSON: " + e.Message);
                yield break;
            }

            JArray items = (JArray)obj["items"];
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("Nenhum item encontrado na coleção!");
                yield break;
            }

            int count = Mathf.Min(items.Count, planeRenderers.Length);

            for (int i = 0; i < count; i++)
            {
                string html = items[i]["document_as_html"]?.ToString();
                if (string.IsNullOrEmpty(html))
                {
                    Debug.LogWarning($"document_as_html vazio no item {i}!");
                    continue;
                }

                // primeira <img src="...">
                var imgMatch = System.Text.RegularExpressions.Regex.Match(
                    html, "<img[^>]+src=\\\\?\\\"([^\\\"]+)"
                );

                if (!imgMatch.Success)
                {
                    Debug.LogWarning($"Nenhuma imagem encontrada no HTML do item {i}!");
                    continue;
                }

                string imageUrl = imgMatch.Groups[1].Value.Replace("\\/", "/");

                // BAIXA a imagem (via proxy no WebGL) e aplica no plano
                yield return StartCoroutine(ApplyImageToPlane(imageUrl, planeRenderers[i]));
            }
        }
    }

    IEnumerator ApplyImageToPlane(string url, Renderer planeRenderer)
    {
        using (UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(Proxied(url)))
        {
            yield return imgRequest.SendWebRequest();

            if (imgRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Erro ao baixar imagem: " + imgRequest.error);
                yield break;
            }

            try
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(imgRequest);
                planeRenderer.material.mainTexture = tex;
                Debug.Log("Imagem aplicada com sucesso!");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Erro ao aplicar imagem: " + e.Message);
            }
        }
    }
}

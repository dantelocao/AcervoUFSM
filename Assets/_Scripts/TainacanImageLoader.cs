using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public class TainacanImageLoader : MonoBehaviour
{
    // Endpoint da coleção no Tainacan
    [Header("Tainacan")]
    public string apiUrl = "https://tainacan.ufsm.br/acervo-artistico/wp-json/tainacan/v2/collection/2174/items/";

    // Planos onde as texturas serão aplicadas (arraste no Inspector)
    [Header("Destino das Imagens")]
    public Renderer[] planeRenderers;

    const string ProxyBase = "https://projetosoftware2-ufsm.vercel.app/api/img?url=";

    // Usa proxy SOMENTE no WebGL (no Editor/Standalone acessa direto o domínio original)
    static string Proxied(string remoteUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return ProxyBase + System.Uri.EscapeDataString(remoteUrl);
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
        using (UnityWebRequest uwr = UnityWebRequest.Get(Proxied(apiUrl)))
        {
            uwr.timeout = 20;
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Tainacan] Erro na requisição JSON: {uwr.error}\nURL: {uwr.url}");
                yield break;
            }

            string json = uwr.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError("[Tainacan] JSON vazio.");
                yield break;
            }

            Debug.Log($"[Tainacan] JSON recebido (prévia): {json.Substring(0, Mathf.Min(200, json.Length))}...");

            JObject obj;
            try
            {
                obj = JObject.Parse(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Tainacan] Erro ao parsear JSON: {e.Message}");
                yield break;
            }

            JArray items = (JArray)obj["items"];
            if (items == null || items.Count == 0)
            {
                Debug.LogWarning("[Tainacan] Nenhum item encontrado na coleção.");
                yield break;
            }

            int count = Mathf.Min(items.Count, planeRenderers.Length);
            if (count == 0)
            {
                Debug.LogWarning("[Tainacan] Não há Renderers atribuídos no Inspector.");
                yield break;
            }

            for (int i = 0; i < count; i++)
            {
                if (planeRenderers[i] == null)
                {
                    Debug.LogWarning($"[Tainacan] Renderer nulo no índice {i}.");
                    continue;
                }

                string html = items[i]["document_as_html"]?.ToString();
                if (string.IsNullOrEmpty(html))
                {
                    Debug.LogWarning($"[Tainacan] document_as_html vazio no item {i}.");
                    continue;
                }

                // Captura a primeira <img src="...">
                // Observação: o JSON pode escapar aspas, por isso o padrão aceita barra invertida opcional.
                var imgMatch = Regex.Match(html, "<img[^>]+src=\\\\?\\\"([^\\\"]+)");
                if (!imgMatch.Success)
                {
                    Debug.LogWarning($"[Tainacan] Nenhuma imagem encontrada no HTML do item {i}.");
                    continue;
                }

                string imageUrl = imgMatch.Groups[1].Value.Replace("\\/", "/");
                yield return StartCoroutine(ApplyImageToPlane(imageUrl, planeRenderers[i]));
            }
        }
    }

    IEnumerator ApplyImageToPlane(string url, Renderer planeRenderer)
    {
        string finalUrl = Proxied(url);
        using (UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(finalUrl))
        {
            imgRequest.timeout = 30;
            yield return imgRequest.SendWebRequest();

            if (imgRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Tainacan] Erro ao baixar imagem: {imgRequest.error}\nURL: {finalUrl}\n" +
                               "Se estiver no WebGL, isso normalmente é CORS — use o proxy (já habilitado neste script).");
                yield break;
            }

            try
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(imgRequest);
                if (tex == null)
                {
                    Debug.LogError("[Tainacan] Texture2D nula após download.");
                    yield break;
                }

                // Aplica textura
                planeRenderer.material.mainTexture = tex;
                Debug.Log($"[Tainacan] Imagem aplicada com sucesso no renderer '{planeRenderer.name}'.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Tainacan] Erro ao aplicar imagem: {e.Message}");
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class TainacanImageLoader : MonoBehaviour
{
    public string apiUrl = "https://tainacan.ufsm.br/acervo-artistico/wp-json/tainacan/v2/collection/2174/items/";
    public Renderer[] planeRenderers; // Coloque todos os planos na cena aqui

    void Start()
    {
        StartCoroutine(LoadImages());
    }

    IEnumerator LoadImages()
    {
        UnityWebRequest uwr = UnityWebRequest.Get(apiUrl);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Erro na requisição: " + uwr.error);
            yield break;
        }

        string json = uwr.downloadHandler.text;
        Debug.Log("JSON recebido: " + json.Substring(0, Mathf.Min(200, json.Length)) + "...");

        JObject obj;
        try
        {
            obj = JObject.Parse(json);
        }
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

            // Extrai a primeira imagem do HTML
            var imgMatch = System.Text.RegularExpressions.Regex.Match(
                html, "<img[^>]+src=\\\\?\\\"([^\\\"]+)"
            );

            if (!imgMatch.Success)
            {
                Debug.LogWarning($"Nenhuma imagem encontrada no HTML do item {i}!");
                continue;
            }

            string imageUrl = imgMatch.Groups[1].Value.Replace("\\/", "/");
            Debug.Log($"Imagem {i} encontrada: {imageUrl}");

            // Baixa e aplica a imagem no plano
            StartCoroutine(ApplyImageToPlane(imageUrl, planeRenderers[i]));
        }
    }

    IEnumerator ApplyImageToPlane(string url, Renderer planeRenderer)
    {
        UnityWebRequest imgRequest = UnityWebRequestTexture.GetTexture(url);
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

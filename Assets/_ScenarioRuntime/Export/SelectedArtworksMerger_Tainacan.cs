// Assets/_ScenarioRuntime/Export/SelectedArtworksMerger_Tainacan.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Metadados opcionais que podemos aproveitar para labels, UI, etc.
/// Não muda o schema do ScenarioData; é apenas um retorno auxiliar.
/// </summary>
[Serializable]
public class ArtworkMeta
{
    public string objectId;      // mapeado pelo resolver (índice -> objectId)
    public long sourceId;      // "id" do item vindo do seu JSON (ex.: 29463)
    public string title;         // "title"
    public string description;   // "description"
    public string imageUrl;      // "imageUrl" (igual ao que vai em ScenarioData.artworks)
}

public static class SelectedArtworksMerger_Tainacan
{
    /// <summary>
    /// Lê a lista do localStorage neste formato:
    ///   [{ id:number, title:string, description:string, imageUrl:string }, ...]
    /// Mapeia por índice -> objectId (via resolver) e injeta em data.artworks.
    /// Também devolve uma lista com metadados por objectId (opcional).
    /// </summary>
    /// <param name="data">ScenarioData que receberá data.artworks</param>
    /// <param name="localStorageKey">Chave onde está salvo o array mostrado por você</param>
    /// <param name="indexToObjectIdResolver">Função que recebe o índice do item e retorna o objectId do quadro correspondente</param>
    /// <param name="metasOut">Lista opcional a ser preenchida com metadados por objectId</param>
    /// <param name="clearExistingArtworks">Se true, limpa artworks antes de inserir</param>
    /// <returns>Quantidade de artworks inseridos/atualizados</returns>
    public static int MergeFromLocalStorageTainacan(ScenarioData data,
                                                    string localStorageKey,
                                                    Func<int, string> indexToObjectIdResolver,
                                                    List<ArtworkMeta> metasOut = null,
                                                    bool clearExistingArtworks = true)
    {
        if (data == null)
        {
            Debug.LogWarning("[TainacanMerger] ScenarioData nulo.");
            return 0;
        }
        if (indexToObjectIdResolver == null)
        {
            Debug.LogError("[TainacanMerger] É necessário um resolver (índice -> objectId).");
            return 0;
        }

        string raw = StorageBridge.LoadLocal(localStorageKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            Debug.Log($"[TainacanMerger] localStorage '{localStorageKey}' vazio.");
            return 0;
        }

        JArray arr;
        try { arr = JArray.Parse(raw); }
        catch (Exception e)
        {
            Debug.LogError("[TainacanMerger] JSON inválido no localStorage: " + e.Message);
            return 0;
        }

        if (clearExistingArtworks || data.artworks == null)
            data.artworks = new List<ArtworkEntryById>();
        if (data.artworks == null)
            data.artworks = new List<ArtworkEntryById>();

        metasOut?.Clear();
        var byObjectId = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < data.artworks.Count; i++)
        {
            var oid = data.artworks[i]?.objectId;
            if (!string.IsNullOrEmpty(oid) && !byObjectId.ContainsKey(oid))
                byObjectId[oid] = i;
        }

        int addedOrUpdated = 0;

        for (int i = 0; i < arr.Count; i++)
        {
            var obj = arr[i] as JObject;
            if (obj == null) continue;

            // Campos do seu JSON:
            long sourceId = obj["id"]?.ToObject<long>() ?? 0;
            string title = obj["title"]?.ToString();
            string description = obj["description"]?.ToString();
            string imageUrl = obj["imageUrl"]?.ToString();

            if (string.IsNullOrWhiteSpace(imageUrl)) continue;

            // Mapeia índice -> objectId do quadro na cena
            string objectId = indexToObjectIdResolver(i);
            if (string.IsNullOrWhiteSpace(objectId)) continue;

            // Insere ou atualiza em ScenarioData.artworks
            if (byObjectId.TryGetValue(objectId, out int idx))
            {
                // atualiza a URL
                data.artworks[idx].imageUrl = imageUrl;
            }
            else
            {
                data.artworks.Add(new ArtworkEntryById
                {
                    objectId = objectId,
                    imageUrl = imageUrl
                });
                byObjectId[objectId] = data.artworks.Count - 1;
            }

            // Metadados opcionais (para UI/placas etc.)
            metasOut?.Add(new ArtworkMeta
            {
                objectId = objectId,
                sourceId = sourceId,
                title = title,
                description = description,
                imageUrl = imageUrl
            });

            addedOrUpdated++;
        }

        if (data.schemaVersion < 2) data.schemaVersion = 2;
        return addedOrUpdated;
    }
}

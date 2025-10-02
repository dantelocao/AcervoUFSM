using UnityEngine;

/// <summary>
/// Aplica o skybox a partir dos campos do ScenarioData:
/// - skyboxMaterialName (via MaterialRegistry)
/// - skyboxPreset (material + rotação)
/// </summary>
public class SkyboxApplier : MonoBehaviour
{
    [Tooltip("Opcional: usar o registry para resolver skyboxMaterialName/preset.materialName.")]
    public MaterialRegistry materialRegistry;

    /// <summary>
    /// Aplica o skybox do ScenarioData (materialName e/ou preset).
    /// </summary>
    public void ApplyFromScenario(ScenarioData data)
    {
        if (data == null) return;

        // 1) Simples por materialName
        if (!string.IsNullOrEmpty(data.skyboxMaterialName))
        {
            var m = ResolveByName(data.skyboxMaterialName);
            if (m) ApplyMaterial(m, rotation: null);
        }

        // 2) Preset (opcional)
        var p = data.skyboxPreset;
        if (p != null)
        {
            if (!string.IsNullOrEmpty(p.materialName))
            {
                var m = ResolveByName(p.materialName);
                if (m) ApplyMaterial(m, p.rotation);
            }

            // exposureCompensation: se você controla via Volume (URP/HDRP), aplique fora daqui
        }
    }

    /// <summary>
    /// Aplica diretamente um Material de skybox (com rotação opcional).
    /// </summary>
    public void ApplyMaterial(Material mat, float? rotation)
    {
        if (!mat) return;

        RenderSettings.skybox = mat;

        if (rotation.HasValue && mat.HasProperty("_Rotation"))
            mat.SetFloat("_Rotation", rotation.Value);

        DynamicGI.UpdateEnvironment(); // atualiza ambient/reflections
    }

    private Material ResolveByName(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return null;
        if (materialRegistry != null)
        {
            var m = materialRegistry.Get(materialName);
            if (m) return m;
        }
        // fallback: tentar Resources (se você preferir organizar assim)
        // return Resources.Load<Material>(materialName);
        return null;
    }
}

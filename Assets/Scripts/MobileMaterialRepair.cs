using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MobileMaterialRepair : MonoBehaviour
{
    private static Material fallbackLit;
    private static Material fallbackUnlit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        // Disabled to prevent "shader thing" artifacts
        // var go = new GameObject(nameof(MobileMaterialRepair));
        // DontDestroyOnLoad(go);
        // go.AddComponent<MobileMaterialRepair>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(RepairNextFrame());
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RepairNextFrame());
    }

    private IEnumerator RepairNextFrame()
    {
        yield return null;
        RepairAllRenderers();
    }

    private static void RepairAllRenderers()
    {
        EnsureFallbackMaterials();

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (renderer == null) continue;

            var materials = renderer.sharedMaterials;
            var changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (!NeedsRepair(materials[i])) continue;

                Color color = ExtractColor(materials[i]);
                var replacement = new Material(fallbackLit);
                SetColor(replacement, color);
                materials[i] = replacement;
                changed = true;
            }

            if (changed) renderer.sharedMaterials = materials;
        }
    }

    private static bool NeedsRepair(Material material)
    {
        if (material == null || material.shader == null) return true;
        if (!material.shader.isSupported) return true;
        return material.shader.name.Contains("InternalErrorShader");
    }

    private static Color ExtractColor(Material material)
    {
        if (material == null) return Color.white;
        if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
        if (material.HasProperty("_Color")) return material.GetColor("_Color");
        return Color.white;
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
    }

    private static void EnsureFallbackMaterials()
    {
        if (fallbackLit != null && fallbackUnlit != null) return;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Shader sprite = Shader.Find("Sprites/Default");

        fallbackLit = new Material(lit != null ? lit : (unlit != null ? unlit : sprite));
        fallbackUnlit = new Material(unlit != null ? unlit : fallbackLit.shader);
    }
}

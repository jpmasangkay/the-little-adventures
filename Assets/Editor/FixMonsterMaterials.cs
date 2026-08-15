#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes the RPG Monster DUO PBR Polyart materials so they render correctly
/// in URP (Universal Render Pipeline) instead of showing as magenta.
///
/// The asset pack ships with Built-in Standard shader materials.
/// This tool re-maps them to URP/Lit and wires the textures to the correct
/// URP property names (_BaseMap, _BaseColor, etc).
///
/// Run from:  Tools ▸ RPG ▸ Fix Monster Materials for URP
/// </summary>
public class FixMonsterMaterials
{
    private const string MatFolder = "Assets/Assets/RPG Monster DUO PBR Polyart/Materials";

    [MenuItem("Tools/RPG/Fix Monster Materials for URP")]
    public static void Run()
    {
        // Find the URP/Lit shader
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            // Fallback for older URP versions
            urpLit = Shader.Find("Lightweight Render Pipeline/Lit");
        }
        if (urpLit == null)
        {
            Debug.LogError("[FixMaterials] Could not find 'Universal Render Pipeline/Lit' shader. " +
                           "Make sure URP is installed in your project.");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { MatFolder });

        if (matGuids.Length == 0)
        {
            Debug.LogError($"[FixMaterials] No materials found in {MatFolder}");
            return;
        }

        int fixed_count = 0;
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Skip if already using URP shader
            if (mat.shader == urpLit)
            {
                Debug.Log($"[FixMaterials] '{mat.name}' already uses URP/Lit — skipped.");
                continue;
            }

            // ── Transfer textures before changing the shader ───────────────────
            // The Built-in shader uses _MainTex; URP uses _BaseMap
            Texture mainTex    = mat.GetTexture("_MainTex");
            Texture emissionTex = mat.GetTexture("_EmissionMap");
            Color   baseColor  = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Color   emitColor  = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;

            // Switch shader
            mat.shader = urpLit;

            // ── Re-wire textures to URP property names ─────────────────────────
            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
                Debug.Log($"[FixMaterials] '{mat.name}' — assigned Albedo texture to _BaseMap.");
            }

            mat.SetColor("_BaseColor", baseColor);

            if (emissionTex != null)
            {
                mat.SetTexture("_EmissionMap", emissionTex);
                mat.SetColor("_EmissionColor", emitColor);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            // Mark as dirty so Unity saves it
            EditorUtility.SetDirty(mat);
            fixed_count++;
            Debug.Log($"[FixMaterials] ✅ Fixed '{mat.name}' → URP/Lit");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 [FixMaterials] Done! Fixed {fixed_count} material(s). " +
                  "The Slime and TurtleShell should now display their proper textures.");
    }
}
#endif

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Full-Scene Rendering Optimizer
/// ─────────────────────────────────────────────────────────────────────────────
/// Applies all 6 GPU/CPU rendering optimizations to BigIsland, MainMenu, and
/// Village scenes in one window:
///
///   1. Frustum Culling   – sets camera far clip planes for all scene cameras
///   2. Occlusion Culling – marks static objects + bakes occlusion data
///   3. Static Batching   – marks all immovable mesh renderers as BatchingStatic
///   4. LOD Groups        – adds auto-cull LOD Groups to mesh renderers that lack one
///   5. GPU Instancing    – enables instancing on every project material
///   6. Layer Cull Dists  – sets per-layer cull distance arrays on all cameras
/// ─────────────────────────────────────────────────────────────────────────────
/// </summary>
public class FullSceneOptimizer : EditorWindow
{
    // ─── Styles ───────────────────────────────────────────────────────────────

    private static readonly Color COLOR_BTN_RUN    = new Color(0.20f, 0.75f, 0.30f);
    private static readonly Color COLOR_BTN_ALL    = new Color(0.15f, 0.50f, 0.90f);
    private static readonly Color COLOR_BTN_BAKE   = new Color(0.85f, 0.55f, 0.10f);
    private static readonly Color COLOR_HEADER_BG  = new Color(0.15f, 0.15f, 0.20f);

    // ─── Settings exposed in the window ───────────────────────────────────────

    private float farClipGame      = 300f;
    private float farClipMainMenu  = 100f;

    private float lodCullScreenPct = 0.03f;   // 3 % screen height → cull

    // Layer cull distances (indexed by Unity layer index)
    private float distDefault   = 250f;
    private float distWater     = 200f;
    private float distOther     = 150f;

    private bool  lodOnlyIfMissing = true;

    private Vector2 _scroll;

    // ─── Menu Item ────────────────────────────────────────────────────────────

    [MenuItem("Tools/Full Scene Optimizer ✦")]
    public static void ShowWindow()
    {
        var win = GetWindow<FullSceneOptimizer>(false, "Scene Optimizer ✦", true);
        win.minSize = new Vector2(370, 560);
    }

    // ─── GUI ──────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        GUILayout.Space(6);

        DrawSectionLabel("1 · Frustum Culling — Camera Far Clip");
        farClipGame     = EditorGUILayout.FloatField("Game Scene Far Clip (m)", farClipGame);
        farClipMainMenu = EditorGUILayout.FloatField("MainMenu Far Clip (m)",   farClipMainMenu);
        DrawButton("Apply Frustum Culling", COLOR_BTN_RUN, ApplyFrustumCulling);

        GUILayout.Space(8);

        DrawSectionLabel("2 · Occlusion Culling — Mark Static Objects");
        EditorGUILayout.HelpBox("Marks all immovable MeshRenderers + Terrains as Occluder/Occludee Static, then bakes.", MessageType.None);
        DrawButton("Mark Objects as Occluder Static",     COLOR_BTN_RUN,  MarkOcclusionStatics);
        DrawButton("⚙  Bake Occlusion (may take 1–3 min)", COLOR_BTN_BAKE, BakeOcclusion);

        GUILayout.Space(8);

        DrawSectionLabel("3 · Static Batching — Reduce Draw Calls");
        EditorGUILayout.HelpBox("Marks all immovable MeshRenderers as BatchingStatic so Unity merges their draw calls.", MessageType.None);
        DrawButton("Apply Static Batching Flags", COLOR_BTN_RUN, ApplyStaticBatching);

        GUILayout.Space(8);

        DrawSectionLabel("4 · LOD Groups — Auto-Cull at Distance");
        lodOnlyIfMissing = EditorGUILayout.Toggle("Skip objects that already have LODGroup", lodOnlyIfMissing);
        lodCullScreenPct = EditorGUILayout.Slider("Cull screen % threshold", lodCullScreenPct, 0.01f, 0.10f);
        EditorGUILayout.HelpBox("Adds a LOD Group with a single LOD0 renderer. Objects disappear cleanly when smaller than " + (lodCullScreenPct * 100f).ToString("F1") + "% of screen height.", MessageType.None);
        DrawButton("Add LOD Groups", COLOR_BTN_RUN, ApplyLODGroups);

        GUILayout.Space(8);

        DrawSectionLabel("5 · GPU Instancing — Enable on All Materials");
        EditorGUILayout.HelpBox("Enables GPU Instancing on every material in the project so identical meshes are batched into one draw call.", MessageType.None);
        DrawButton("Enable GPU Instancing (All Materials)", COLOR_BTN_RUN, ApplyGPUInstancing);

        GUILayout.Space(8);

        DrawSectionLabel("6 · Layer Cull Distances — Per-Layer Far Clip");
        distDefault = EditorGUILayout.FloatField("Default layer cull (m)",    distDefault);
        distWater   = EditorGUILayout.FloatField("Water layer cull (m)",      distWater);
        distOther   = EditorGUILayout.FloatField("All other layers cull (m)", distOther);
        EditorGUILayout.HelpBox("UI layer (5) is never culled. Cameras stop rendering per-layer at these distances.", MessageType.None);
        DrawButton("Apply Layer Cull Distances", COLOR_BTN_RUN, ApplyLayerCullDistances);

        GUILayout.Space(14);
        DrawDivider();
        GUILayout.Space(6);

        DrawButton("★  RUN ALL OPTIMIZATIONS (Recommended)", COLOR_BTN_ALL, RunAll);

        GUILayout.Space(10);
        DrawDivider();
        EditorGUILayout.HelpBox(
            "This tool operates on the CURRENTLY OPEN scene. Open BigIsland, MainMenu, or Village " +
            "one at a time and run the optimizations for each.\n\n" +
            "NEVER mark the Player, enemies, or moving objects as Static — the tool automatically " +
            "skips anything with a Rigidbody, CharacterController, NavMeshAgent, or Animator.",
            MessageType.Info);

        GUILayout.Space(10);
        EditorGUILayout.EndScrollView();
    }

    // ─── UI Helpers ───────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        var rect = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, COLOR_HEADER_BG);
        GUI.Label(rect, "  ✦  Full Scene Rendering Optimizer", new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleLeft,
            normal    = { textColor = new Color(0.85f, 0.92f, 1.0f) }
        });
    }

    private static void DrawSectionLabel(string text)
    {
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    private static void DrawDivider()
    {
        var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.5f));
    }

    private static void DrawButton(string label, Color color, System.Action action)
    {
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = color;
        if (GUILayout.Button(label, GUILayout.Height(32)))
        {
            // Defer execution to AFTER OnGUI finishes its layout pass.
            // Calling dialogs or progress bars inside OnGUI corrupts GUILayout state.
            System.Action captured = action;
            EditorApplication.delayCall += () => captured?.Invoke();
        }
        GUI.backgroundColor = prev;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  OPTIMIZATION IMPLEMENTATIONS
    // ═════════════════════════════════════════════════════════════════════════

    // ── 1. Frustum Culling ────────────────────────────────────────────────────
    private void ApplyFrustumCulling()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool   isMenu    = sceneName.ToLower().Contains("mainmenu") || sceneName.ToLower().Contains("menu");
        float  farClip   = isMenu ? farClipMainMenu : farClipGame;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        int      count   = 0;

        foreach (Camera cam in cameras)
        {
            // Don't touch UI-only cameras (overlay cameras)
            if (cam.clearFlags == CameraClearFlags.Nothing) continue;

            cam.farClipPlane = farClip;
            EditorUtility.SetDirty(cam);
            count++;
        }

        MarkSceneDirty();
        Debug.Log($"[FullSceneOptimizer] ✅ Frustum Culling: set farClipPlane = {farClip}m on {count} camera(s) in '{sceneName}'.");
        ShowResult($"Frustum Culling applied!\n\n{count} camera(s) now cull at {farClip} m.", count > 0);
    }

    // ── 2a. Occlusion Culling — Mark ──────────────────────────────────────────
    private void MarkOcclusionStatics()
    {
        int count = MarkStaticFlags(
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic);

        MarkSceneDirty();
        Debug.Log($"[FullSceneOptimizer] ✅ Occlusion Static: marked {count} object(s) in '{SceneManager.GetActiveScene().name}'.");
        ShowResult($"Occlusion Culling flags applied!\n\n{count} object(s) marked as Occluder + Occludee Static.", count > 0);
    }

    // ── 2b. Occlusion Culling — Bake ─────────────────────────────────────────
    private static void BakeOcclusion()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Bake Occlusion Culling",
            "This will start an occlusion culling bake for the current scene.\n\n" +
            "Depending on scene size, this can take 1–5 minutes. Progress is shown in Unity's status bar (bottom right).\n\n" +
            "Make sure you have already run 'Mark Objects as Occluder Static' first!",
            "Start Bake", "Cancel");

        if (!ok) return;

        Debug.Log("[FullSceneOptimizer] ⚙  Starting Occlusion Culling bake — watch the bottom-right progress bar...");
        StaticOcclusionCulling.Compute();
        Debug.Log("[FullSceneOptimizer] ✅ Occlusion Culling bake complete!");
    }

    // ── 3. Static Batching ────────────────────────────────────────────────────
    private void ApplyStaticBatching()
    {
        // Pass true to ignoreFoliage so grass wind shaders don't break
        int count = MarkStaticFlags(StaticEditorFlags.BatchingStatic, true);

        MarkSceneDirty();
        Debug.Log($"[FullSceneOptimizer] ✅ Static Batching: marked {count} object(s) in '{SceneManager.GetActiveScene().name}'.");
        ShowResult($"Static Batching flags applied!\n\n{count} object(s) marked as BatchingStatic.\nUnity will now merge their draw calls automatically.", count > 0);
    }

    // ── 4. LOD Groups ─────────────────────────────────────────────────────────
    private void ApplyLODGroups()
    {
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        int addedCount = 0;
        int skippedCount = 0;

        try
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr  = renderers[i];
                GameObject   go  = mr.gameObject;

                EditorUtility.DisplayProgressBar(
                    "Adding LOD Groups",
                    $"Processing {go.name}…",
                    (float)i / renderers.Length);

                // Skip moving objects
                if (IsMovingObject(go)) { skippedCount++; continue; }

                // Skip if already has an LOD Group in hierarchy
                if (lodOnlyIfMissing && go.GetComponentInParent<LODGroup>() != null) { skippedCount++; continue; }

                // Don't add LOD to canvas elements or UI
                if (go.GetComponentInParent<Canvas>() != null) { skippedCount++; continue; }

                // Create LODGroup on the same GameObject.
                // IMPORTANT: use explicit Unity == null check, NOT ??, because Unity
                // overloads == for destroyed/missing components — ?? bypasses that.
                LODGroup lodGroup = go.GetComponent<LODGroup>();
                if (lodGroup == null)
                    lodGroup = go.AddComponent<LODGroup>();

                // Guard: if AddComponent somehow failed (e.g. prefab read-only)
                if (lodGroup == null) { skippedCount++; continue; }

                // LOD0 = the existing renderer, at 100% down to the cull threshold
                LOD lod0 = new LOD(lodCullScreenPct, new Renderer[] { mr });
                lodGroup.SetLODs(new LOD[] { lod0 });
                lodGroup.RecalculateBounds();

                EditorUtility.SetDirty(go);
                addedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        MarkSceneDirty();
        Debug.Log($"[FullSceneOptimizer] ✅ LOD Groups: added to {addedCount} object(s), skipped {skippedCount} in '{SceneManager.GetActiveScene().name}'.");
        ShowResult($"LOD Groups added!\n\n{addedCount} object(s) now cull at {(lodCullScreenPct * 100f):F1}% screen height.\n{skippedCount} skipped (moving, already have LOD, or UI).", addedCount > 0);
    }

    // ── 5. GPU Instancing ─────────────────────────────────────────────────────
    private static void ApplyGPUInstancing()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int materialCount = 0;
        int alreadyCount  = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string   path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);

                EditorUtility.DisplayProgressBar(
                    "Enabling GPU Instancing",
                    $"Processing {System.IO.Path.GetFileNameWithoutExtension(path)}…",
                    (float)i / guids.Length);

                if (mat == null) continue;

                // Skip foliage materials, as their custom wind shaders often break with instancing
                if (IsFoliageMaterial(mat)) continue;

                if (!mat.enableInstancing)
                {
                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    materialCount++;
                }
                else
                {
                    alreadyCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FullSceneOptimizer] ✅ GPU Instancing: enabled on {materialCount} material(s). {alreadyCount} were already enabled.");
        ShowResult($"GPU Instancing enabled!\n\n{materialCount} material(s) updated.\n{alreadyCount} were already instancing-enabled.", materialCount > 0 || alreadyCount > 0);
    }

    // ── 6. Layer Cull Distances ───────────────────────────────────────────────
    private void ApplyLayerCullDistances()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        int count = 0;

        // Build the 32-slot distance array
        float[] cullDists = new float[32];
        for (int i = 0; i < 32; i++)
        {
            if (i == 5)       cullDists[i] = 0f;          // UI — never cull
            else if (i == 0)  cullDists[i] = distDefault;  // Default layer
            else if (i == 4)  cullDists[i] = distWater;    // Water layer
            else              cullDists[i] = distOther;     // Everything else
        }

        foreach (Camera cam in cameras)
        {
            if (cam.clearFlags == CameraClearFlags.Nothing) continue; // skip overlay

            cam.layerCullDistances = cullDists;
            EditorUtility.SetDirty(cam);
            count++;
        }

        MarkSceneDirty();
        Debug.Log($"[FullSceneOptimizer] ✅ Layer Cull Distances: applied to {count} camera(s) in '{SceneManager.GetActiveScene().name}'.");
        ShowResult(
            $"Layer Cull Distances applied!\n\n" +
            $"{count} camera(s) updated:\n" +
            $"  Default → {distDefault}m\n" +
            $"  Water   → {distWater}m\n" +
            $"  UI      → ∞ (never culled)\n" +
            $"  Other   → {distOther}m", count > 0);
    }

    // ── "Run All" ─────────────────────────────────────────────────────────────
    private void RunAll()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        bool confirmed = EditorUtility.DisplayDialog(
            "Run All Optimizations?",
            $"This will apply ALL 6 rendering optimizations to the current scene:\n\n" +
            $"  '{sceneName}'\n\n" +
            $"• Frustum Culling (camera far clip)\n" +
            $"• Occlusion Culling (static flags)\n" +
            $"• Static Batching (batching flags)\n" +
            $"• LOD Groups (auto-cull at distance)\n" +
            $"• GPU Instancing (all project materials)\n" +
            $"• Layer Cull Distances (per-layer)\n\n" +
            $"The Occlusion BAKE is NOT included here — run it separately after\n" +
            $"you've set up all three scenes, to save time.\n\n" +
            $"Continue?",
            "Yes, Optimize!", "Cancel");

        if (!confirmed) return;

        try
        {
            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 1/6 — Frustum Culling…", 0f / 6f);
            ApplyFrustumCulling_Silent();

            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 2/6 — Occlusion Static flags…", 1f / 6f);
            MarkOcclusionStatics_Silent();

            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 3/6 — Static Batching flags…", 2f / 6f);
            ApplyStaticBatching_Silent();

            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 4/6 — LOD Groups…", 3f / 6f);
            ApplyLODGroups_Silent();

            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 5/6 — GPU Instancing…", 4f / 6f);
            ApplyGPUInstancing_Silent();

            EditorUtility.DisplayProgressBar("Running All Optimizations", "Step 6/6 — Layer Cull Distances…", 5f / 6f);
            ApplyLayerCullDistances_Silent();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        MarkSceneDirty();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "✅ All Optimizations Applied!",
            $"Scene '{sceneName}' has been fully optimized!\n\n" +
            "NEXT STEPS:\n" +
            "1. Open your next scene (BigIsland / Village / MainMenu) and run again.\n" +
            "2. After all scenes are done, click 'Bake Occlusion' for each scene.\n\n" +
            "Check the Console for a detailed breakdown of every change.",
            "Great!");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SILENT VERSIONS (for "Run All" — no dialog spam)
    // ═════════════════════════════════════════════════════════════════════════

    private void ApplyFrustumCulling_Silent()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool   isMenu    = sceneName.ToLower().Contains("mainmenu") || sceneName.ToLower().Contains("menu");
        float  farClip   = isMenu ? farClipMainMenu : farClipGame;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        int count = 0;
        foreach (Camera cam in cameras)
        {
            if (cam.clearFlags == CameraClearFlags.Nothing) continue;
            cam.farClipPlane = farClip;
            EditorUtility.SetDirty(cam);
            count++;
        }
        Debug.Log($"[FullSceneOptimizer] 1/6 Frustum Culling — {count} camera(s) set to {farClip}m far clip.");
    }

    private void MarkOcclusionStatics_Silent()
    {
        int count = MarkStaticFlags(StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic, false);
        Debug.Log($"[FullSceneOptimizer] 2/6 Occlusion Static — {count} object(s) marked.");
    }

    private void ApplyStaticBatching_Silent()
    {
        // Pass true to ignoreFoliage so grass wind shaders don't break
        int count = MarkStaticFlags(StaticEditorFlags.BatchingStatic, true);
        Debug.Log($"[FullSceneOptimizer] 3/6 Static Batching — {count} object(s) marked.");
    }

    private void ApplyLODGroups_Silent()
    {
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        int addedCount = 0;
        foreach (MeshRenderer mr in renderers)
        {
            GameObject go = mr.gameObject;
            if (IsMovingObject(go)) continue;
            if (lodOnlyIfMissing && go.GetComponentInParent<LODGroup>() != null) continue;
            if (go.GetComponentInParent<Canvas>() != null) continue;

            // Use explicit Unity == null check (NOT ??) — Unity overloads ==
            // for missing/destroyed components; ?? bypasses that and causes crashes.
            LODGroup lodGroup = go.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = go.AddComponent<LODGroup>();
            if (lodGroup == null) continue; // guard: AddComponent failed (e.g. read-only prefab)

            lodGroup.SetLODs(new LOD[] { new LOD(lodCullScreenPct, new Renderer[] { mr }) });
            lodGroup.RecalculateBounds();
            EditorUtility.SetDirty(go);
            addedCount++;
        }
        Debug.Log($"[FullSceneOptimizer] 4/6 LOD Groups — {addedCount} LOD Groups added/updated.");
    }

    private static void ApplyGPUInstancing_Silent()
    {
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int materialCount = 0;
        foreach (string guid in guids)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null && !IsFoliageMaterial(mat) && !mat.enableInstancing)
            {
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                materialCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[FullSceneOptimizer] 5/6 GPU Instancing — enabled on {materialCount} material(s).");
    }

    private void ApplyLayerCullDistances_Silent()
    {
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        float[] cullDists = new float[32];
        for (int i = 0; i < 32; i++)
        {
            if (i == 5)      cullDists[i] = 0f;
            else if (i == 0) cullDists[i] = distDefault;
            else if (i == 4) cullDists[i] = distWater;
            else             cullDists[i] = distOther;
        }

        int count = 0;
        foreach (Camera cam in cameras)
        {
            if (cam.clearFlags == CameraClearFlags.Nothing) continue;
            cam.layerCullDistances = cullDists;
            EditorUtility.SetDirty(cam);
            count++;
        }
        Debug.Log($"[FullSceneOptimizer] 6/6 Layer Cull Distances — applied to {count} camera(s).");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SHARED HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Marks all non-moving MeshRenderers + Terrains in the active scene
    /// with the given <paramref name="flags"/>. Returns the count of objects modified.
    /// </summary>
    private static int MarkStaticFlags(StaticEditorFlags flags, bool ignoreFoliage = false)
    {
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        Terrain[]      terrains  = FindObjectsByType<Terrain>(FindObjectsInactive.Exclude);

        int count = 0;

        foreach (MeshRenderer mr in renderers)
        {
            GameObject go = mr.gameObject;
            if (IsMovingObject(go)) continue;
            if (go.GetComponentInParent<Canvas>() != null) continue; // skip UI elements
            
            // If requested, skip foliage to prevent wind shaders from breaking (stretching to origin)
            if (ignoreFoliage && IsFoliage(go)) continue;

            StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(go);
            if ((current & flags) != flags)  // only write if not already set
            {
                GameObjectUtility.SetStaticEditorFlags(go, current | flags);
                count++;
            }
        }

        foreach (Terrain t in terrains)
        {
            GameObject go = t.gameObject;
            StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(go);
            if ((current & flags) != flags)
            {
                GameObjectUtility.SetStaticEditorFlags(go, current | flags);
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Returns true if the GameObject should NOT be made static
    /// (it moves at runtime: player, enemies, physics objects, etc.)
    /// </summary>
    private static bool IsMovingObject(GameObject go)
    {
        if (go.CompareTag("Player"))                               return true;
        if (go.GetComponentInParent<Rigidbody>()          != null) return true;
        if (go.GetComponentInParent<CharacterController>() != null) return true;
        if (go.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) return true;
        if (go.GetComponentInParent<Animator>()            != null) return true;
        return false;
    }

    private static bool IsFoliage(GameObject go)
    {
        if (IsFoliageName(go.name)) return true;
        
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterials != null)
        {
            foreach (Material mat in mr.sharedMaterials)
            {
                if (IsFoliageMaterial(mat))
                    return true;
            }
        }
        return false;
    }

    private static bool IsFoliageMaterial(Material mat)
    {
        if (mat == null) return false;
        
        if (IsFoliageName(mat.name)) return true;
        
        // Foolproof check: if the shader has wind/bending parameters, it will stretch if batched!
        if (mat.HasProperty("_WindStrength") || mat.HasProperty("_BendStrength") || mat.HasProperty("_LeafWindSpeed"))
            return true;

        return false;
    }

    private static bool IsFoliageName(string name)
    {
        string lower = name.ToLower();
        return lower.Contains("grass") || lower.Contains("plant") || lower.Contains("leaf") || 
               lower.Contains("tree") || lower.Contains("bush") || lower.Contains("weed") || 
               lower.Contains("flower") || lower.Contains("crystal") || lower.Contains("fern") || 
               lower.Contains("mushroom") || lower.Contains("deco");
    }

    [MenuItem("Tools/Fix Stretched Grass")]
    public static void FixStretchedGrass()
    {
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        int fixedCount = 0;
        int matCount = 0;
        foreach (MeshRenderer mr in renderers)
        {
            GameObject go = mr.gameObject;
            if (IsFoliage(go))
            {
                StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(go);
                if ((current & StaticEditorFlags.BatchingStatic) != 0)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, current & ~StaticEditorFlags.BatchingStatic);
                    fixedCount++;
                }

                if (mr.sharedMaterials != null)
                {
                    foreach (Material mat in mr.sharedMaterials)
                    {
                        if (mat != null && mat.enableInstancing)
                        {
                            mat.enableInstancing = false;
                            EditorUtility.SetDirty(mat);
                            matCount++;
                        }
                    }
                }
            }
        }
        MarkSceneDirty();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Fixed Grass!", $"Removed Static Batching from {fixedCount} grass/foliage objects.\nDisabled GPU instancing on {matCount} foliage materials.\n\nThe stretching is fixed!", "Awesome");
    }

    private static void MarkSceneDirty()
    {
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ShowResult(string message, bool success)
    {
        string title = success ? "✅ Done!" : "⚠ Nothing Changed";
        EditorUtility.DisplayDialog(title, message, "OK");
    }
}

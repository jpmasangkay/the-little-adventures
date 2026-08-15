using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-click editor tool that:
///   1. Registers the "Portal" tag in ProjectSettings/TagManager.asset (if missing).
///   2. Applies that tag to any GameObject in the active scene that has a ScenePortal component.
///
/// Run via: Tools → The Little Adventurers → Fix Portal Tag
/// </summary>
public static class PortalTagFixer
{
    private const string PORTAL_TAG = "Portal";

    [MenuItem("Tools/The Little Adventurers/Fix Portal Tag")]
    public static void FixPortalTag()
    {
        // ── Step 1: Register the tag ─────────────────────────────────────────────
        bool tagRegistered = EnsureTagExists(PORTAL_TAG);

        // ── Step 2: Apply tag to all ScenePortal objects in the active scene ─────
        ScenePortal[] portals = Object.FindObjectsByType<ScenePortal>(FindObjectsInactive.Include);
        int taggedCount = 0;

        foreach (ScenePortal portal in portals)
        {
            if (portal.gameObject.tag != PORTAL_TAG)
            {
                Undo.RecordObject(portal.gameObject, "Apply Portal Tag");
                portal.gameObject.tag = PORTAL_TAG;
                EditorUtility.SetDirty(portal.gameObject);
                taggedCount++;
                Debug.Log($"[PortalTagFixer] Applied '{PORTAL_TAG}' tag to: {portal.gameObject.name}");
            }
        }

        // ── Step 3: Also handle the specifically named portal in Village ──────────
        GameObject namedPortal = GameObject.Find("PortaltoBigIsland");
        if (namedPortal != null && namedPortal.tag != PORTAL_TAG)
        {
            Undo.RecordObject(namedPortal, "Apply Portal Tag");
            namedPortal.tag = PORTAL_TAG;
            EditorUtility.SetDirty(namedPortal);
            taggedCount++;
            Debug.Log($"[PortalTagFixer] Applied '{PORTAL_TAG}' tag to: PortaltoBigIsland");
        }

        // ── Step 4: Save the scene if anything changed ────────────────────────────
        if (taggedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        // ── Summary ───────────────────────────────────────────────────────────────
        string summary = $"Portal Tag Fix Complete:\n" +
                         $"  • Tag registered in TagManager: {(tagRegistered ? "YES (was missing)" : "already existed")}\n" +
                         $"  • GameObjects re-tagged: {taggedCount}";

        Debug.Log($"✅ [PortalTagFixer] {summary}");
        EditorUtility.DisplayDialog("Portal Tag Fix", summary, "OK");
    }

    /// <summary>
    /// Ensures <paramref name="tagName"/> exists in the project's TagManager.
    /// Returns <c>true</c> if the tag was newly added, <c>false</c> if it already existed.
    /// </summary>
    private static bool EnsureTagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                return false; // Already registered
        }

        // Append new tag
        int newIndex = tagsProp.arraySize;
        tagsProp.InsertArrayElementAtIndex(newIndex);
        tagsProp.GetArrayElementAtIndex(newIndex).stringValue = tagName;
        tagManager.ApplyModifiedProperties();

        Debug.Log($"[PortalTagFixer] Registered new tag: '{tagName}' in TagManager.");
        return true;
    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class FixUIStretchingEditor
{
    static FixUIStretchingEditor()
    {
        EditorApplication.delayCall += DoFix;
    }

    [MenuItem("Tools/Fix UI Stretching")]
    public static void DoFix()
    {
        bool changed = false;

        // Fix Canvas Scalers
        CanvasScaler[] scalers = Object.FindObjectsOfType<CanvasScaler>(true);
        foreach (var scaler in scalers)
        {
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.matchWidthOrHeight != 0.5f)
            {
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
                changed = true;
            }
        }

        // Fix Squashed Buttons
        Button[] buttons = Object.FindObjectsOfType<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.transform.localScale.x != 1f || btn.transform.localScale.y != 1f)
            {
                btn.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(btn.transform);
                changed = true;
            }
        }

        // Fix Image Aspect Ratios if they are simple
        Image[] images = Object.FindObjectsOfType<Image>(true);
        foreach (var img in images)
        {
            // If it's a background or icon that should preserve aspect
            if (img.type == Image.Type.Simple && !img.preserveAspect)
            {
                // We don't want to preserve aspect on EVERYTHING, but icons like coins/gems/potions
                if (img.gameObject.name.Contains("Icon") || img.gameObject.name.Contains("Coin") || img.gameObject.name.Contains("Gem") || img.gameObject.name.Contains("Potion"))
                {
                    img.preserveAspect = true;
                    EditorUtility.SetDirty(img);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            Debug.Log("UI Stretching fixed in the current scene!");
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class TextToTMPConverter : EditorWindow
{
    [MenuItem("Tools/Convert UI Text to TextMeshPro")]
    public static void ShowWindow()
    {
        GetWindow<TextToTMPConverter>("Convert Text to TMP");
    }

    private void OnGUI()
    {
        GUILayout.Label("Convert all UnityEngine.UI.Text to TextMeshProUGUI", EditorStyles.boldLabel);

        if (GUILayout.Button("Convert Selected Object(s)"))
        {
            ConvertSelected();
        }

        if (GUILayout.Button("Convert All in Active Scene"))
        {
            ConvertAllInScene();
        }
    }

    private void ConvertSelected()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            ConvertRecursively(obj);
        }
    }

    private void ConvertAllInScene()
    {
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            ConvertRecursively(root);
        }
    }

    private void ConvertRecursively(GameObject obj)
    {
        var textComponents = obj.GetComponentsInChildren<Text>(true);
        int count = 0;
        foreach (var oldText in textComponents)
        {
            GameObject textObj = oldText.gameObject;
            
            // Store properties
            string textValue = oldText.text;
            Color colorValue = oldText.color;
            int fontSize = oldText.fontSize;
            FontStyle fontStyle = oldText.fontStyle;
            TextAnchor alignment = oldText.alignment;
            bool raycastTarget = oldText.raycastTarget;
            
            // Layout properties
            var rectTransform = textObj.GetComponent<RectTransform>();
            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;

            Undo.DestroyObjectImmediate(oldText);

            var tmpText = Undo.AddComponent<TextMeshProUGUI>(textObj);
            
            // Restore properties
            tmpText.text = textValue;
            tmpText.color = colorValue;
            tmpText.fontSize = fontSize;
            tmpText.raycastTarget = raycastTarget;

            // Map alignment
            if (alignment == TextAnchor.UpperLeft) tmpText.alignment = TextAlignmentOptions.TopLeft;
            else if (alignment == TextAnchor.UpperCenter) tmpText.alignment = TextAlignmentOptions.Top;
            else if (alignment == TextAnchor.UpperRight) tmpText.alignment = TextAlignmentOptions.TopRight;
            else if (alignment == TextAnchor.MiddleLeft) tmpText.alignment = TextAlignmentOptions.Left;
            else if (alignment == TextAnchor.MiddleCenter) tmpText.alignment = TextAlignmentOptions.Center;
            else if (alignment == TextAnchor.MiddleRight) tmpText.alignment = TextAlignmentOptions.Right;
            else if (alignment == TextAnchor.LowerLeft) tmpText.alignment = TextAlignmentOptions.BottomLeft;
            else if (alignment == TextAnchor.LowerCenter) tmpText.alignment = TextAlignmentOptions.Bottom;
            else if (alignment == TextAnchor.LowerRight) tmpText.alignment = TextAlignmentOptions.BottomRight;

            // Map font style
            if (fontStyle == FontStyle.Bold) tmpText.fontStyle = FontStyles.Bold;
            else if (fontStyle == FontStyle.Italic) tmpText.fontStyle = FontStyles.Italic;
            else if (fontStyle == FontStyle.BoldAndItalic) tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            count++;
        }
        
        Debug.Log($"Converted {count} Text components to TextMeshProUGUI.");
    }
}

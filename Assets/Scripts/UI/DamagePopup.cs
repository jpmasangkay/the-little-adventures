using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Spawns a floating damage number above a world position, rises, then fades out.
/// Call DamagePopup.Create() from anywhere — no scene setup needed.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    private TextMeshPro _tmp;
    private float       _riseSpeed = 1.5f;
    private float       _lifetime  = 0.8f;
    private float       _timer;

    public static void Create(Vector3 worldPos, float amount, bool isPlayerDamage = false)
    {
        // Build the GameObject at runtime — no prefab required
        GameObject go = new GameObject("DamagePopup");
        go.transform.position = worldPos + Vector3.up * 0.5f;

        DamagePopup popup = go.AddComponent<DamagePopup>();

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text      = isPlayerDamage ? $"-{amount:0}" : $"-{amount:0}";
        tmp.fontSize  = isPlayerDamage ? 5f : 4f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = isPlayerDamage ? new Color(1f, 0.2f, 0.2f) // red for player damage
                                       : new Color(1f, 0.85f, 0f);  // yellow for enemy damage
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 10;

        popup._tmp = tmp;
        go.transform.localScale = Vector3.one * 0.4f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float t = _timer / _lifetime;

        // Rise upward
        transform.position += Vector3.up * (_riseSpeed * Time.deltaTime);

        // Face camera
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        // Fade out in the last 40% of lifetime
        if (_tmp != null && t > 0.6f)
        {
            Color c = _tmp.color;
            c.a = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);
            _tmp.color = c;
        }

        if (_timer >= _lifetime)
            Destroy(gameObject);
    }
}

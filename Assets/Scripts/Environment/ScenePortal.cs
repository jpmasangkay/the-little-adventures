using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScenePortal : MonoBehaviour
{
    [Tooltip("The exact name of the scene to load (e.g. 'BigIsland')")]
    public string targetSceneName = "BigIsland";

    [Tooltip("The tag that THIS collider (the portal trigger) must have to activate the scene load.\n" +
             "Add a small Box Collider (Is Trigger = true) tagged 'Portal' inside the portal ring,\n" +
             "and attach this script only to that object — NOT to the portal mesh/stairs.")]
    public string portalTag = "Portal";

    private bool isLoading = false;

    private void Awake()
    {
        // Warn if the tag is not set up correctly, without throwing a runtime exception.
        // gameObject.tag is a safe string read — unlike CompareTag(), it never throws
        // even when the tag hasn't been registered in Unity's TagManager.
        if (gameObject.tag != portalTag)
        {
            Debug.LogWarning(
                $"[ScenePortal] '{gameObject.name}' has tag '{gameObject.tag}' " +
                $"but expects '{portalTag}'. The portal will not activate until the tag matches. " +
                $"Fix: select this object → Inspector → Tag → '{portalTag}'.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't trigger multiple times
        if (isLoading) return;

        // ── Guard: only fire from the dedicated portal trigger, not from stairs/mesh colliders ──
        // Use gameObject.tag (string compare) instead of CompareTag() to avoid the
        // "Tag not defined" exception when the tag hasn't been registered in TagManager.
        if (gameObject.tag != portalTag) return;

        // Check if the colliding object is the player (has the 'PlayerMovement' component)
        if (other.GetComponent<PlayerMovement>() != null)
        {
            // ── Sync state with GameManager before leaving ────────────────────
            if (GameManager.Instance != null)
            {
                PlayerHealth ph  = other.GetComponent<PlayerHealth>();
                Inventory    inv = Inventory.Instance;
                PlayerStats  stats = other.GetComponent<PlayerStats>();
                GameManager.Instance.SavePlayerState(ph, inv, stats);
            }

            isLoading = true;
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadScene(targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
                Debug.LogWarning("[ScenePortal] LoadingScreenManager not found, falling back to direct load.");
            }
        }
    }
}

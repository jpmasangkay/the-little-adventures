using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to any NPC GameObject to give it interactive dialogue.
///
/// Setup:
///  1. Create a World-Space or Screen-Space Canvas with a TextMeshProUGUI component
///     and assign it to `dialoguePanel` / `dialogueText`.
///  2. Leave the canvas disabled in the Inspector — this script shows/hides it.
///  3. Assign dialogue lines in the Inspector.
///  4. Set `interactKey` (default E) or use an InputAction if desired.
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Lines")]
    [TextArea(2, 6)]
    public string[] lines;

    [Header("UI References")]
    [Tooltip("The parent Canvas / Panel to show/hide.")]
    public GameObject dialoguePanel;
    [Tooltip("The TextMeshProUGUI element that shows dialogue text.")]
    public TextMeshProUGUI dialogueText;
    [Tooltip("Optional: small prompt label like '[E] Talk'. Hidden when reading.")]
    public GameObject interactPrompt;

    [Header("Settings")]
    [Tooltip("Distance at which the interact prompt appears.")]
    public float interactDistance = 2.5f;
    [Tooltip("Seconds per character for typewriter effect (0 = instant).")]
    public float typewriterSpeed = 0.03f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private Transform _player;
    private int       _lineIndex   = 0;
    private bool      _isOpen      = false;
    private bool      _isTyping    = false;
    private Coroutine _typeCoroutine;

    private void Start()
    {
        // Find player
        GameObject pg = GameObject.FindGameObjectWithTag("Player");
        if (pg == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) pg = pm.gameObject;
        }
        if (pg != null) _player = pg.transform;

        // Hide everything initially
        if (dialoguePanel  != null) dialoguePanel.SetActive(false);
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= interactDistance;

        // ── Show/Hide prompt ─────────────────────────────────────────────────
        if (interactPrompt != null)
            interactPrompt.SetActive(inRange && !_isOpen);

        // ── Interact key ─────────────────────────────────────────────────────
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (inRange && !_isOpen)
            {
                OpenDialogue();
            }
            else if (_isOpen)
            {
                // If still typing, skip to end of this line
                if (_isTyping)
                {
                    if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
                    _isTyping = false;
                    if (lines != null && _lineIndex < lines.Length && dialogueText != null)
                        dialogueText.text = lines[_lineIndex];
                }
                else
                {
                    AdvanceLine();
                }
            }
        }

        // Face the player while talking
        if (_isOpen && _player != null)
        {
            Vector3 dir = (_player.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    5f * Time.deltaTime);
        }
    }

    // ── Dialogue Control ──────────────────────────────────────────────────────

    private void OpenDialogue()
    {
        if (lines == null || lines.Length == 0) return;
        _isOpen     = true;
        _lineIndex  = 0;
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ShowLine(_lineIndex);
    }

    private void AdvanceLine()
    {
        _lineIndex++;
        if (_lineIndex >= lines.Length)
        {
            CloseDialogue();
        }
        else
        {
            ShowLine(_lineIndex);
        }
    }

    private void CloseDialogue()
    {
        _isOpen = false;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        if (dialoguePanel  != null) dialoguePanel.SetActive(false);
    }

    private void ShowLine(int index)
    {
        if (dialogueText == null) return;
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);

        if (typewriterSpeed > 0f)
            _typeCoroutine = StartCoroutine(TypeLine(lines[index]));
        else
        {
            dialogueText.text = lines[index];
            _isTyping = false;
        }
    }

    private System.Collections.IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        dialogueText.text = string.Empty;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        _isTyping = false;
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}

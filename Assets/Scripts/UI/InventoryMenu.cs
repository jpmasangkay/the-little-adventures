using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InventoryMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI gemsText;
    public TextMeshProUGUI potionsText;
    public Button usePotionButton;
    public Button closeButton;

    private bool _isOpen = false;
    private InputReader _input;
    private static InventoryMenu _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void FixNonUniformScale(Transform t)
    {
        if (t == null) return;
        
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            float scaleX = rt.localScale.x;
            float scaleY = rt.localScale.y;
            if (Mathf.Abs(scaleX - scaleY) > 0.01f && scaleY != 0)
            {
                float ratio = scaleX / scaleY;
                rt.localScale = new Vector3(scaleY, scaleY, rt.localScale.z);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * ratio, rt.sizeDelta.y);
            }
        }

        foreach (Transform child in t)
        {
            FixNonUniformScale(child);
        }
    }

    private void Start()
    {
        // RUNTIME FIX: Fix Canvas Scaler stretching
        if (inventoryPanel != null)
        {
            FixNonUniformScale(inventoryPanel.transform);
            
            UnityEngine.UI.CanvasScaler scaler = inventoryPanel.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = 0.5f;

            // Auto-wire CloseButton if missing
            if (closeButton == null)
            {
                Transform cb = inventoryPanel.transform.Find("CloseButton");
                if (cb == null) cb = inventoryPanel.transform.Find("Close");
                if (cb != null) closeButton = cb.GetComponent<Button>();
            }
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // RUNTIME FIX: Fix Image aspect ratios so coins/gems aren't stretched
        UnityEngine.UI.Image[] images = GetComponentsInChildren<UnityEngine.UI.Image>(true);
        foreach (var img in images)
        {
            if (img.type == UnityEngine.UI.Image.Type.Simple)
            {
                if (img.gameObject.name.Contains("Icon") || img.gameObject.name.Contains("Coin") || img.gameObject.name.Contains("Gem") || img.gameObject.name.Contains("Potion"))
                {
                    img.preserveAspect = true;
                }
            }
        }

        if (usePotionButton != null) 
        {
            usePotionButton.onClick.RemoveAllListeners();
            usePotionButton.onClick.AddListener(OnUsePotionClicked);
        }
        
        if (closeButton != null) 
        {
            // Remove any broken CartoonUI.Close scripts if they exist
            foreach (var comp in closeButton.GetComponents<MonoBehaviour>())
            {
                if (comp != null && comp.GetType().Name.Contains("Close") && comp.GetType() != typeof(Button))
                {
                    Destroy(comp);
                }
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseInventory);
        }

        // Subscribe to inventory changes
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= UpdateUI;
            Inventory.Instance.OnInventoryChanged += UpdateUI;
            UpdateUI(); // Initial update
        }

        // Find Player InputReader
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _input = player.GetComponent<InputReader>();
        }
    }

    private void OnDestroy()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= UpdateUI;
        }
    }

    private bool _wasLeftClickDown = false;

    private void Update()
    {
        bool toggled = false;

        if (_input == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _input = player.GetComponent<InputReader>();
        }

        if (_input != null && _input.InventoryToggledThisFrame)
        {
            toggled = true;
        }
        else if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame || 
                UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                toggled = true;
            }
            
            // Allow closing with Escape
            if (_isOpen && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                toggled = true;
            }
        }

        if (toggled)
        {
            if (_isOpen) CloseInventory();
            else OpenInventory();
        }

        // Bulletproof manual click detection for Close Button when TimeScale is 0
        bool isLeftClick = false;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            // isPressed works even if wasPressedThisFrame fails due to FixedUpdate mode
            isLeftClick = UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
        }

        bool clickedThisFrame = isLeftClick && !_wasLeftClickDown;
        _wasLeftClickDown = isLeftClick;

        if (_isOpen && clickedThisFrame)
        {
            if (closeButton != null && closeButton.gameObject.activeInHierarchy)
            {
                RectTransform rt = closeButton.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, UnityEngine.InputSystem.Mouse.current.position.ReadValue(), null))
                {
                    CloseInventory();
                }
            }
        }
    }

    private Coroutine _animationCoroutine;

    private void OpenInventory()
    {
        _isOpen = true;
        if (inventoryPanel != null) 
        {
            inventoryPanel.SetActive(true);
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimatePanel(true));
        }

        Time.timeScale = 0f; // Pause game
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateUI();
    }

    private void CloseInventory()
    {
        _isOpen = false;
        Time.timeScale = 1f; // Resume game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (inventoryPanel != null)
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimatePanel(false));
        }
    }

    private IEnumerator AnimatePanel(bool opening)
    {
        CanvasGroup cg = inventoryPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = inventoryPanel.AddComponent<CanvasGroup>();

        float duration = 0.2f; // Fast, snappy animation
        float elapsed = 0f;

        Vector3 startScale = opening ? new Vector3(0.8f, 0.8f, 0.8f) : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : new Vector3(0.8f, 0.8f, 0.8f);

        float startAlpha = opening ? 0f : 1f;
        float endAlpha = opening ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Snappy ease-out curve
            float easeT = opening ? (1f - Mathf.Pow(1f - t, 3f)) : (t * t * t);

            inventoryPanel.transform.localScale = Vector3.Lerp(startScale, endScale, easeT);
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, easeT);

            yield return null;
        }

        inventoryPanel.transform.localScale = endScale;
        cg.alpha = endAlpha;

        if (!opening)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (Inventory.Instance == null) return;

        if (coinsText != null) coinsText.text = Inventory.Instance.coins.ToString();
        if (gemsText != null) gemsText.text = Inventory.Instance.gems.ToString();
        if (potionsText != null) potionsText.text = Inventory.Instance.potions.ToString();

        // Disable Use button if no potions or player is dead/missing
        if (usePotionButton != null)
        {
            bool canUse = Inventory.Instance.potions > 0;
            usePotionButton.interactable = canUse;
        }
    }

    private void OnUsePotionClicked()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TryUsePotion();
            }
        }
    }
}

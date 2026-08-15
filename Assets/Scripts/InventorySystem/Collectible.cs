using UnityEngine;

/// <summary>
/// Attach this to any world item (coin, gem, potion) to make it collectible.
/// When the player walks into its trigger collider the item is added to the Inventory
/// and the GameObject is destroyed.
/// </summary>
public class Collectible : MonoBehaviour
{
    public enum ItemType { Coin, Gem, Potion }

    [Header("Item")]
    public ItemType itemType = ItemType.Coin;
    [Tooltip("How many of this item the player receives.")]
    [Min(1)] public int amount = 1;

    [Header("Effects")]
    [Tooltip("Optional particle burst on pickup.")]
    public GameObject pickupVFXPrefab;
    [Tooltip("Optional audio clip played on pickup (uses AudioSource on Camera or AudioListener).")]
    public AudioClip  pickupSFX;

    [Header("Bob & Spin (optional)")]
    public bool enableBob  = true;
    public float bobHeight = 0.2f;
    public float bobSpeed  = 2f;
    public float spinSpeed = 90f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        if (enableBob)
        {
            float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() == null) return;

        // Add to inventory
        if (Inventory.Instance != null)
        {
            switch (itemType)
            {
                case ItemType.Coin:   Inventory.Instance.AddCoins(amount);   break;
                case ItemType.Gem:    Inventory.Instance.AddGems(amount);     break;
                case ItemType.Potion: Inventory.Instance.AddPotions(amount);  break;
            }
        }

        // Feedback
        if (pickupVFXPrefab != null)
            Instantiate(pickupVFXPrefab, transform.position, Quaternion.identity);

        if (pickupSFX != null)
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position);

        Destroy(gameObject);
    }
}

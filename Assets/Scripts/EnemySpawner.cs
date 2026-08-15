using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns enemies only inside designer-defined SpawnZones.
/// 
/// Setup:
///   1. Create empty GameObjects in open areas of your scene (e.g. "Zone_Field", "Zone_Path").
///   2. Assign them to the Spawn Zones list in the Inspector.
///   3. Set a Radius per zone — enemies spawn randomly within that circle.
///   4. Hit Play — enemies appear only inside your chosen zones.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ── Spawn Zone definition ─────────────────────────────────────────────────
    [System.Serializable]
    public class SpawnZone
    {
        [Tooltip("Place an empty GameObject in an open area of your scene and drag it here.")]
        public Transform center;

        [Tooltip("Enemies spawn within this radius around the zone center.")]
        [Min(1f)] public float radius = 10f;

        [Tooltip("Gizmo colour for this zone (helps tell zones apart in the Scene view).")]
        public Color gizmoColor = new Color(0f, 1f, 0.8f, 0.25f);
    }

    // ── Enemy prefab definition ───────────────────────────────────────────────
    [System.Serializable]
    public struct SpawnEntry
    {
        [Tooltip("Enemy prefab to spawn.")]
        public GameObject prefab;

        [Tooltip("Relative weight — higher = spawns more often.")]
        [Min(1)] public int weight;
    }

    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("Spawn Zones")]
    [Tooltip("Each zone is an empty GameObject placed in an open area. Enemies only spawn inside these.")]
    public SpawnZone[] spawnZones;

    [Header("Enemy Prefabs")]
    public SpawnEntry[] enemies;

    [Header("Spawn Settings")]
    [Tooltip("Height above zone center to start the downward ground raycast.")]
    public float raycastHeight = 50f;

    [Tooltip("Extra height above ground when placing the enemy so it drops cleanly onto the surface.")]
    public float spawnHeightOffset = 0.5f;

    [Tooltip("Which layers count as ground?")]
    public LayerMask groundMask;

    [Tooltip("Maximum surface slope — steeper points (rocks, cliffs) are rejected.")]
    [Range(0f, 60f)]
    public float maxSlopeAngle = 30f;

    [Header("Count")]
    [Tooltip("Maximum enemies alive at the same time.")]
    public int maxLiveEnemies = 6;

    [Tooltip("How many to spawn at game start.")]
    public int initialSpawnCount = 6;

    [Header("Proximity Checks")]
    [Tooltip("Enemies won't spawn this close to the player.")]
    public float minDistanceFromPlayer = 8f;

    [Tooltip("Minimum distance between enemies (spreads them out).")]
    public float minDistanceBetweenEnemies = 5f;

    [Header("Respawn")]
    [Tooltip("Seconds between respawn checks (randomised each cycle between RespawnMin and RespawnMax).")]
    [Range(60f, 600f)]
    public float respawnInterval = 240f;
    [Tooltip("Minimum seconds before the next respawn attempt (default 4 min).")]
    public float respawnMin = 240f;
    [Tooltip("Maximum seconds before the next respawn attempt (default 5 min).")]
    public float respawnMax = 300f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private List<GameObject> _alive = new List<GameObject>();
    private float            _respawnTimer;
    private int              _totalWeight;
    private Transform        _player;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Validate
        if (spawnZones == null || spawnZones.Length == 0)
        {
            Debug.LogError("[EnemySpawner] No Spawn Zones assigned! " +
                           "Create empty GameObjects in open areas and drag them into Spawn Zones.", this);
            return;
        }
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError("[EnemySpawner] No enemy prefabs assigned!", this);
            return;
        }

        // Auto-detect ground mask
        if (groundMask == 0)
            groundMask = ~0; // Everything — works for any scene

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
            if (pm != null) playerObj = pm.gameObject;
        }
        if (playerObj != null) _player = playerObj.transform;

        // Precompute weights
        _totalWeight = 0;
        foreach (var e in enemies) _totalWeight += e.weight;

        // Initial spawn
        int toSpawn = Mathf.Min(initialSpawnCount, maxLiveEnemies);
        int spawned = 0;
        for (int i = 0; i < toSpawn; i++)
            if (TrySpawnOne(i)) spawned++;

        Debug.Log($"[EnemySpawner] Spawned {spawned}/{toSpawn} enemies across {spawnZones.Length} zone(s).");
        _respawnTimer = respawnInterval;
    }

    private void Update()
    {
        _alive.RemoveAll(go => go == null);

        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f)
        {
            // Reset to a new random interval in the 4–5 min range each cycle
            _respawnTimer = Random.Range(respawnMin, respawnMax);
            while (_alive.Count < maxLiveEnemies)
                if (!TrySpawnOne(_alive.Count)) break;
        }
    }

    // ── Core spawn logic ──────────────────────────────────────────────────────

    private bool TrySpawnOne(int index)
    {
        if (enemies == null || enemies.Length == 0) return false;

        GameObject prefab = PickPrefab();
        if (prefab == null) return false;

        // Try each zone in random order so we don't always exhaust zone 0 first
        int[] zoneOrder = RandomZoneOrder();

        foreach (int z in zoneOrder)
        {
            SpawnZone zone = spawnZones[z];
            if (zone == null || zone.center == null) continue;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // Pick random point inside this zone's circle
                Vector2 circle  = Random.insideUnitCircle * zone.radius;
                Vector3 origin  = zone.center.position + new Vector3(circle.x, raycastHeight, circle.y);

                // Raycast ALL hits downward, sorted highest-Y first.
                // This lets the slope check fall through tree canopy meshes
                // (curved normals → steep angle → rejected) down to the flat
                // terrain surface underneath, which passes the slope check.
                RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down,
                                                       raycastHeight * 2f, groundMask);
                if (hits.Length == 0) continue;

                // Sort descending by Y — topmost surface first
                System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));

                // Find the first hit that is flat enough to stand on
                RaycastHit validHit = default;
                bool foundFlat = false;
                foreach (RaycastHit h in hits)
                {
                    float s = Vector3.Angle(h.normal, Vector3.up);
                    if (s <= maxSlopeAngle) { validHit = h; foundFlat = true; break; }
                }
                if (!foundFlat) continue;

                Vector3 spawnPos = validHit.point;

                // Player proximity check
                if (_player != null &&
                    Vector3.Distance(spawnPos, _player.position) < minDistanceFromPlayer)
                    continue;

                // Enemy spread check
                bool tooClose = false;
                foreach (GameObject alive in _alive)
                {
                    if (alive == null) continue;
                    if (Vector3.Distance(spawnPos, alive.transform.position) < minDistanceBetweenEnemies)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // All checks passed — spawn slightly above ground
                Vector3 finalPos = spawnPos + Vector3.up * spawnHeightOffset;
                GameObject go = Instantiate(prefab, finalPos,
                                            Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                _alive.Add(go);
                return true;
            }
        }

        Debug.LogWarning("[EnemySpawner] Could not find a valid spawn point. " +
                         "Make sure your Spawn Zone centers are placed in open areas.");
        return false;
    }

    /// <summary>Returns zone indices in a shuffled order so all zones get equal priority.</summary>
    private int[] RandomZoneOrder()
    {
        int[] order = new int[spawnZones.Length];
        for (int i = 0; i < order.Length; i++) order[i] = i;

        // Fisher-Yates shuffle
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    private GameObject PickPrefab()
    {
        if (_totalWeight <= 0) return null;

        int roll = Random.Range(0, _totalWeight);
        int cumulative = 0;
        foreach (var e in enemies)
        {
            cumulative += e.weight;
            if (roll < cumulative) return e.prefab;
        }
        return enemies[enemies.Length - 1].prefab;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (spawnZones == null) return;

        foreach (SpawnZone zone in spawnZones)
        {
            if (zone == null || zone.center == null) continue;

            // Filled disc
            Gizmos.color = zone.gizmoColor;
            Gizmos.DrawSphere(zone.center.position, zone.radius);

            // Wire outline
            Color outline = zone.gizmoColor;
            outline.a = 1f;
            Gizmos.color = outline;
            Gizmos.DrawWireSphere(zone.center.position, zone.radius);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages zombie spawn points and handles spawning zombies during waves
/// Controls spawn positions, spacing, cooldowns, and player proximity detection
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    /// <summary>
    /// Represents a location where zombies can spawn in the game world
    /// Tracks cooldown times and manages position offsets for multiple zombies
    /// </summary>
    [System.Serializable]
    public class SpawnPoint
    {
        /// <summary>
        /// The physical location in the scene where zombies will spawn
        /// </summary>
        public Transform location;
        
        /// <summary>
        /// Time when a zombie last spawned from this point
        /// </summary>
        public float lastSpawnTime = -3f; // Initialize to allow immediate spawn
        
        /// <summary>
        /// Whether this spawn point is ready to spawn another zombie based on cooldown time
        /// </summary>
        public bool CanSpawn => Time.time - lastSpawnTime >= cooldownTime;
        
        /// <summary>
        /// Time in seconds between spawns from this specific point
        /// </summary>
        public float cooldownTime = 3f;
        
        /// <summary>
        /// List of currently active zombies that originated from this spawn point
        /// </summary>
        public List<GameObject> activeZombies = new List<GameObject>(); // Track zombies at this spawn
    }

    /// <summary>
    /// The prefab of the zombie to spawn
    /// </summary>
    [Tooltip("The prefab of the zombie to spawn")]
    public GameObject zombiePrefab;
    
    /// <summary>
    /// List of all possible spawn points in the level
    /// </summary>
    [Tooltip("List of spawn points")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    
    /// <summary>
    /// Distance in meters within which the player must be for spawning to occur at a point
    /// </summary>
    [Tooltip("Distance within which player must be for spawning to occur")]
    public float playerDetectionRange = 50f;
    
    /// <summary>
    /// Maximum radius in meters for zombie offset around spawn point to prevent overlapping
    /// </summary>
    [Tooltip("Maximum radius for zombie offset around spawn point")]
    public float maxSpawnOffset = 3f;
    
    /// <summary>
    /// Whether to show visual indicators of spawn points in the editor
    /// </summary>
    [Tooltip("Toggle to show/hide gizmos in the editor")]
    public bool showGizmos = true;
    
    /// <summary>
    /// Reference to the player's transform for distance calculations
    /// </summary>
    private Transform playerTransform;
    
    /// <summary>
    /// Reference to the wave management system for zombie registration
    /// </summary>
    private WaveManagement waveManager;
    
    /// <summary>
    /// Initializes the spawner by finding player and wave manager references
    /// </summary>
    void Start()
    {
        // Find the player by tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
        }
        
        // Find wave manager reference
        waveManager = FindFirstObjectByType<WaveManagement>();
        if (waveManager == null)
        {
            Debug.LogWarning("WaveManagement script not found in scene!");
        }
    }
    
    /// <summary>
    /// Attempts to spawn zombies at valid spawn points
    /// </summary>
    /// <param name="count">Number of zombies to spawn</param>
    /// <returns>Actual number of zombies spawned</returns>
    public int SpawnZombies(int count)
    {
        // Don't spawn anything when paused
        if (PauseManager.IsPaused())
            return 0;
        
        if (playerTransform == null || zombiePrefab == null)
            return 0;
        
        int spawned = 0;
        List<SpawnPoint> validSpawnPoints = new List<SpawnPoint>();
        
        // Clean up any destroyed zombies from the tracking lists
        foreach (SpawnPoint point in spawnPoints)
        {
            point.activeZombies.RemoveAll(z => z == null);
        }
        
        // Find valid spawn points (those within range and off cooldown)
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.location == null)
                continue;
                
            float distanceToPlayer = Vector3.Distance(point.location.position, playerTransform.position);
            
            if (distanceToPlayer <= playerDetectionRange && point.CanSpawn)
            {
                validSpawnPoints.Add(point);
            }
        }
        
        // No valid spawn points found
        if (validSpawnPoints.Count == 0)
            return 0;
        
        // Try to spawn the requested number of zombies
        for (int i = 0; i < count; i++)
        {
            // If we've used all valid points, break out of the loop
            if (validSpawnPoints.Count == 0)
                break;
                
            // Select a random spawn point
            int index = Random.Range(0, validSpawnPoints.Count);
            SpawnPoint selectedPoint = validSpawnPoints[index];
            
            // Calculate spawn position with offset if there are already zombies at this location
            Vector3 spawnPosition = GetOffsetSpawnPosition(selectedPoint);
            
            // Spawn zombie at the calculated position but maintain original rotation
            GameObject zombie = Instantiate(zombiePrefab, spawnPosition, selectedPoint.location.rotation);
            
            // Add zombie to the tracking list for this spawn point
            selectedPoint.activeZombies.Add(zombie);
            
            // Register with wave manager if available
            if (waveManager != null)
            {
                waveManager.RegisterZombie(zombie);
            }
            
            // Update last spawn time and remove from valid points
            selectedPoint.lastSpawnTime = Time.time;
            validSpawnPoints.RemoveAt(index);
            
            spawned++;
        }
        
        return spawned;
    }
    
    /// <summary>
    /// Calculates an offset position for spawning if the spawn point already has zombies
    /// </summary>
    /// <param name="spawnPoint">The spawn point to calculate an offset from</param>
    /// <returns>A position near the spawn point that doesn't overlap existing zombies</returns>
    private Vector3 GetOffsetSpawnPosition(SpawnPoint spawnPoint)
    {
        // If no zombies at this spawn yet, use the exact location
        if (spawnPoint.activeZombies.Count == 0)
        {
            return spawnPoint.location.position;
        }
        
        // Otherwise, find an available position nearby
        Vector3 basePosition = spawnPoint.location.position;
        
        // Try up to 10 times to find a position that doesn't overlap with existing zombies
        for (int attempt = 0; attempt < 10; attempt++)
        {
            // Random offset within a circle around the spawn point
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(1f, maxSpawnOffset);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f, // Keep on the same Y level
                Mathf.Sin(angle) * distance
            );
            
            Vector3 testPosition = basePosition + offset;
            
            // Check if this position is far enough from existing zombies
            bool positionValid = true;
            foreach (GameObject existingZombie in spawnPoint.activeZombies)
            {
                if (existingZombie != null)
                {
                    float distToExisting = Vector3.Distance(testPosition, existingZombie.transform.position);
                    if (distToExisting < 1f) // Minimum spacing between zombies
                    {
                        positionValid = false;
                        break;
                    }
                }
            }
            
            // If we found a valid position, use it
            if (positionValid)
            {
                return testPosition;
            }
        }
        
        // If all attempts failed, just use a random offset
        float fallbackAngle = Random.Range(0f, Mathf.PI * 2f);
        float fallbackDistance = Random.Range(1f, maxSpawnOffset);
        Vector3 fallbackOffset = new Vector3(
            Mathf.Cos(fallbackAngle) * fallbackDistance,
            0f,
            Mathf.Sin(fallbackAngle) * fallbackDistance
        );
        
        return basePosition + fallbackOffset;
    }
    
    /// <summary>
    /// Visualizes spawn points, player detection range, and maximum offset in the editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Don't draw anything if gizmos are disabled
        if (!showGizmos)
            return;
            
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.location != null)
            {
                // Draw spawn point
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(point.location.position, 1f);
                
                // Draw player detection range
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f); // Semi-transparent orange
                Gizmos.DrawSphere(point.location.position, playerDetectionRange);
                
                // Draw maximum spawn offset range
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // Semi-transparent green
                Gizmos.DrawWireSphere(point.location.position, maxSpawnOffset);
            }
        }
    }
    
    /// <summary>
    /// Enables or disables gizmo visualization
    /// </summary>
    /// <param name="visible">Whether gizmos should be visible</param>
    public void SetGizmosVisibility(bool visible)
    {
        showGizmos = visible;
    }
    
    /// <summary>
    /// Retrieves the total number of active zombies across all spawn points
    /// </summary>
    /// <returns>Total number of active zombies in the scene</returns>
    public int GetTotalActiveZombies()
    {
        int total = 0;
        foreach (SpawnPoint point in spawnPoints)
        {
            // First clean up any null references
            point.activeZombies.RemoveAll(z => z == null);
            // Then count the remaining zombies
            total += point.activeZombies.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Gets the number of valid spawn points that are currently available
    /// </summary>
    /// <returns>Count of spawn points that are off cooldown and have valid locations</returns>
    public int GetAvailableSpawnPointCount()
    {
        int count = 0;
        foreach (SpawnPoint point in spawnPoints)
        {
            if (point.location != null && point.CanSpawn)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Resets cooldowns on all spawn points to allow immediate spawning
    /// </summary>
    public void ResetAllCooldowns()
    {
        foreach (SpawnPoint point in spawnPoints)
        {
            point.lastSpawnTime = -3f;
        }
    }
}
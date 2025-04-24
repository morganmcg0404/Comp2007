using System.Collections.Generic;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPoint
    {
        public Transform location;
        public float lastSpawnTime = -3f; // Initialize to allow immediate spawn
        public bool CanSpawn => Time.time - lastSpawnTime >= cooldownTime;
        public float cooldownTime = 3f;
        public List<GameObject> activeZombies = new List<GameObject>(); // Track zombies at this spawn
    }

    [Tooltip("The prefab of the zombie to spawn")]
    public GameObject zombiePrefab;
    
    [Tooltip("List of spawn points")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    
    [Tooltip("Distance within which player must be for spawning to occur")]
    public float playerDetectionRange = 50f;
    
    [Tooltip("Maximum radius for zombie offset around spawn point")]
    public float maxSpawnOffset = 3f;
    
    [Tooltip("Toggle to show/hide gizmos in the editor")]
    public bool showGizmos = true;
    
    private Transform playerTransform;
    private WaveManagement waveManager;
    
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
    
    // For debugging: visualize spawn radius in Scene view
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
    public void SetGizmosVisibility(bool visible)
    {
        showGizmos = visible;
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages zombie wave spawning, progression, and difficulty scaling
/// Controls zombie health, damage, movement types, and spawn rates based on wave number
/// </summary>
public class WaveManagement : MonoBehaviour
{
    [Header("Wave Settings")]
    /// <summary>
    /// Current wave number, increments after each wave is completed
    /// </summary>
    [SerializeField] private int currentWave = 0;
    
    /// <summary>
    /// Number of zombies in the first wave
    /// </summary>
    [SerializeField] private int zombiesPerWave = 4;          // Starting zombies
    
    /// <summary>
    /// Additional health points zombies gain per wave
    /// </summary>
    [SerializeField] private float zombieHealthIncrease = 5f; // Health increase per round
    
    /// <summary>
    /// Starting health for zombies in the first wave
    /// </summary>
    [SerializeField] private float baseZombieHealth = 100f;   // Starting zombie health
    
    /// <summary>
    /// Maximum number of zombies that can spawn in a single wave
    /// </summary>
    [SerializeField] private int maxZombiesPerWave = 500;     // Maximum zombies in any wave
    
    /// <summary>
    /// Wave number at which the maximum zombie count per wave is reached
    /// </summary>
    [SerializeField] private int waveToReachMaxZombies = 100; // Wave at which max zombies is reached
    
    /// <summary>
    /// Maximum number of zombies that can be alive simultaneously
    /// </summary>
    [SerializeField] private int maxZombiesAlive = 25;        // Maximum zombies alive at once
    
    [Header("Time Settings")]
    /// <summary>
    /// Delay in seconds between completing a wave and starting the next one
    /// </summary>
    [SerializeField] private float timeBetweenWaves = 10f;    // Time between waves in seconds
    
    /// <summary>
    /// Time in seconds between individual zombie spawns during a wave
    /// </summary>
    [SerializeField] private float zombieSpawnInterval = 0.5f; // Time between zombie spawns in seconds
    
    [Header("References")]
    /// <summary>
    /// Reference to the ZombieSpawner component that handles actual zombie instantiation
    /// </summary>
    [SerializeField] private ZombieSpawner zombieSpawner;
    
    /// <summary>
    /// UI text element displaying the current wave number
    /// </summary>
    [SerializeField] private TextMeshProUGUI waveText;
    
    /// <summary>
    /// UI text element displaying the number of remaining zombies
    /// </summary>
    [SerializeField] private TextMeshProUGUI zombieCountText;

    [Header("Zombie Type Distribution")]
    /// <summary>
    /// Wave at which 100% of zombies will be sprinters (fastest type)
    /// </summary>
    [SerializeField] private int waveForAllSprinters = 45;    // Wave at which 100% of zombies will sprint
    
    /// <summary>
    /// Wave at which sprinters begin to appear in the zombie population
    /// </summary>
    [SerializeField] private int waveForSprintersStart = 15;  // Wave at which sprinters start appearing
    
    /// <summary>
    /// Wave at which joggers begin to appear in the zombie population
    /// </summary>
    [SerializeField] private int waveForJoggersStart = 5;     // Wave at which joggers start appearing
    
    /// <summary>
    /// Wave at which 100% of zombies will be at least joggers
    /// </summary>
    [SerializeField] private int waveForAllJoggers = 15;      // Wave at which 100% of zombies will be joggers or better

    [Header("Zombie Behavior")]
    /// <summary>
    /// Base damage dealt by zombies in the first wave
    /// </summary>
    [SerializeField] private float baseZombieDamage = 10f; // Base damage for wave 1
    
    /// <summary>
    /// Percentage damage increase per wave (0.01 = 1% increase per wave)
    /// </summary>
    [SerializeField] private float zombieDamageIncreasePerWave = 0.01f; // 1% increase per wave
    
    // Wave state tracking
    /// <summary>
    /// Number of zombies still to be defeated in the current wave
    /// </summary>
    private int zombiesRemainingInWave = 0;
    
    /// <summary>
    /// Number of zombies currently active in the scene
    /// </summary>
    private int zombiesAlive = 0;
    
    /// <summary>
    /// Whether a wave is currently in progress
    /// </summary>
    private bool waveInProgress = false;
    
    /// <summary>
    /// Whether the game is currently active (false when game over)
    /// </summary>
    private bool gameActive = true;
    
    /// <summary>
    /// Initializes the wave management system and starts the first wave
    /// </summary>
    private void Start()
    {
        // Find references if not set in inspector
        if (zombieSpawner == null)
        {
            zombieSpawner = FindFirstObjectByType<ZombieSpawner>();
        }
        
        // Start first wave
        StartCoroutine(StartNextWave());
    }
    
    /// <summary>
    /// Checks for wave completion and updates UI elements
    /// </summary>
    private void Update()
    {
        // Skip all processing when game is paused
        if (PauseManager.IsPaused() || !gameActive) return;
        
        UpdateUI();
        
        // Check if wave is complete
        if (waveInProgress && zombiesRemainingInWave <= 0 && zombiesAlive <= 0)
        {
            waveInProgress = false;
            StartCoroutine(StartNextWave());
        }
    }
    
    /// <summary>
    /// Starts the next wave after a delay, incrementing difficulty and zombie count
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator StartNextWave()
    {
        // Increment wave counter immediately
        currentWave++;
        
        // Calculate zombies for this wave
        int zombiesThisWave = CalculateZombiesForWave(currentWave);
        
        // Update UI immediately to show the new wave and zombie count
        zombiesRemainingInWave = zombiesThisWave;
        
        // Update UI to show the current wave and zombies
        if (waveText != null)
        {
            waveText.text = $"Wave {currentWave}";
        }
        
        // Update zombie counter to show upcoming zombies
        if (zombieCountText != null)
        {
            zombieCountText.text = $"Zombies Remaining: {zombiesRemainingInWave}";
        }
        
        // Only add delay after the first wave (not at the start of the game)
        if (currentWave > 1) 
        {
            // Wait for the configured time between waves
            yield return new WaitForSeconds(timeBetweenWaves);
        }
        
        // Now actually start the wave after the delay
        waveInProgress = true;
        
        // Start spawning zombies
        StartCoroutine(SpawnZombiesForWave(zombiesThisWave));
    }
    
    /// <summary>
    /// Spawns zombies over time until the wave quota is reached, respecting the maximum alive zombies limit
    /// </summary>
    /// <param name="totalZombies">Total number of zombies to spawn in this wave</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator SpawnZombiesForWave(int totalZombies)
    {
        int zombiesLeftToSpawn = totalZombies;
        
        while (zombiesLeftToSpawn > 0 && gameActive)
        {
            // Skip processing if game is paused
            if (PauseManager.IsPaused())
            {
                yield return null;
                continue;
            }
            
            // Only spawn if we haven't reached max alive zombies
            if (zombiesAlive < maxZombiesAlive)
            {
                int zombiesToSpawn = Mathf.Min(maxZombiesAlive - zombiesAlive, zombiesLeftToSpawn);
                
                // Try to spawn zombies using the zombie spawner
                int zombiesSpawned = zombieSpawner.SpawnZombies(zombiesToSpawn);
                zombiesLeftToSpawn -= zombiesSpawned;
                zombiesAlive += zombiesSpawned;
            }
            
            yield return new WaitForSeconds(zombieSpawnInterval);
        }
    }
    
    /// <summary>
    /// Calculates the number of zombies to spawn for a specific wave
    /// Scales linearly from initial zombiesPerWave to maxZombiesPerWave based on wave number
    /// </summary>
    /// <param name="wave">The wave number to calculate for</param>
    /// <returns>The number of zombies to spawn for the specified wave</returns>
    private int CalculateZombiesForWave(int wave)
    {
        if (wave == 1)
        {
            // First wave should have exactly zombiesPerWave zombies
            return zombiesPerWave;
        }
        else if (wave >= waveToReachMaxZombies)
        {
            // After reaching the target wave, stay at max zombies
            return maxZombiesPerWave;
        }
        
        // For waves 2 through waveToReachMaxZombies-1, use linear scaling
        float progress = (float)(wave - 1) / (waveToReachMaxZombies - 1);
        return Mathf.RoundToInt(Mathf.Lerp(zombiesPerWave, maxZombiesPerWave, progress));
    }
    
    /// <summary>
    /// Calculates zombie health for the current wave based on base health and per-wave increase
    /// </summary>
    /// <returns>The health amount for zombies in the current wave</returns>
    private float CalculateZombieHealth()
    {
        return baseZombieHealth + (currentWave - 1) * zombieHealthIncrease;
    }

    /// <summary>
    /// Calculates the chance for a zombie to be a sprinter based on wave number
    /// </summary>
    /// <param name="wave">Current wave number</param>
    /// <returns>Probability (0.0-1.0) that a zombie will be a sprinter</returns>
    private float CalculateSprintChance(int wave)
    {
        // No sprinters before the starting wave
        if (wave < waveForSprintersStart)
            return 0f;
        
        // All zombies are sprinters after the maximum wave
        if (wave >= waveForAllSprinters)
            return 1f;
        
        // Linear interpolation between starting wave and all-sprinters wave
        float progress = (float)(wave - waveForSprintersStart) / (waveForAllSprinters - waveForSprintersStart);
        return Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// Calculates the chance for a zombie to be at least a jogger based on wave number
    /// </summary>
    /// <param name="wave">Current wave number</param>
    /// <returns>Probability (0.0-1.0) that a zombie will be a jogger or sprinter</returns>
    private float CalculateJoggerChance(int wave)
    {
        // No joggers before the starting wave
        if (wave < waveForJoggersStart)
            return 0f;
        
        // All zombies are at least joggers after the maximum wave
        if (wave >= waveForAllJoggers)
            return 1f;
        
        // Linear interpolation between starting wave and all-joggers wave
        float progress = (float)(wave - waveForJoggersStart) / (waveForAllJoggers - waveForJoggersStart);
        return Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Calculates the damage multiplier for zombies in the current wave
    /// </summary>
    /// <returns>Damage multiplier that increases with wave number</returns>
    private float CalculateZombieDamageMultiplier()
    {
        // Start at 100% on wave 1, then increase by 1% per wave
        return 1f + (currentWave - 1) * zombieDamageIncreasePerWave;
    }

    /// <summary>
    /// Calculates the actual damage value for zombies in the current wave
    /// </summary>
    /// <returns>The damage amount zombies will deal in the current wave</returns>
    private float CalculateZombieDamage()
    {
        float rawDamage = baseZombieDamage * CalculateZombieDamageMultiplier();
        return Mathf.Round(rawDamage); // Round to nearest whole number
    }
    
    /// <summary>
    /// Called when a zombie dies to update counters and check for wave completion
    /// </summary>
    public void ZombieDied()
    {
        zombiesAlive--;
        zombiesRemainingInWave--;
        UpdateUI();
    }
    
    /// <summary>
    /// Updates UI elements with current wave information and zombie counts
    /// </summary>
    private void UpdateUI()
    {
        if (waveText != null)
        {
            waveText.text = $"{currentWave}";
        }
        
        if (zombieCountText != null)
        {
            zombieCountText.text = $"Zombies Remaining: {zombiesRemainingInWave}";
        }
    }
    
    /// <summary>
    /// Registers a newly spawned zombie with the wave manager
    /// Sets health, speed type, and damage values based on current wave difficulty
    /// </summary>
    /// <param name="zombie">The GameObject of the spawned zombie</param>
    public void RegisterZombie(GameObject zombie)
    {
        // Set the zombie's health based on the current wave
        HealthSystem healthSystem = zombie.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            // Keep existing health setup...
            float zombieHealth = CalculateZombieHealth();
            
            var healthField = healthSystem.GetType().GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (healthField != null)
            {
                healthField.SetValue(healthSystem, zombieHealth);
            }
            
            // Force heal to full health
            healthSystem.Heal(zombieHealth * 2);
            
            // Keep death event subscription...
            UnityEngine.Events.UnityAction deathAction = () => ZombieDied();
            
            var onDeathField = healthSystem.GetType().GetField("onDeath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (onDeathField != null)
            {
                var onDeathEvent = onDeathField.GetValue(healthSystem) as UnityEngine.Events.UnityEvent;
                if (onDeathEvent != null)
                {
                    onDeathEvent.AddListener(deathAction);
                }
            }
        }
    
        // Determine if this zombie should be a sprinter based on current wave
        ZombieNavigation zombieNav = zombie.GetComponent<ZombieNavigation>();
        if (zombieNav != null)
        {
            ZombieNavigation.ZombieType zombieType = DetermineZombieType(currentWave);
            zombieNav.SetZombieType(zombieType);
        }

        ZombieAttack zombieAttack = zombie.GetComponent<ZombieAttack>();
        if (zombieAttack != null)
        {
            // Calculate the damage multiplier for this wave
            float damageMultiplier = CalculateZombieDamageMultiplier();
            
            // Get the base damage value from the zombie attack component
            float baseDamage = 0f;
            var damageField = zombieAttack.GetType().GetField("attackDamage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (damageField != null)
            {
                baseDamage = (float)damageField.GetValue(zombieAttack);
            }
            else
            {
                // Fallback to our base damage value if we can't read the field
                baseDamage = baseZombieDamage;
            }
            
            // Calculate new damage with the multiplier
            float scaledDamage = baseDamage * damageMultiplier;
            
            // Set the newly scaled damage - this will be rounded when actually applied
            zombieAttack.SetDamageMultiplier(damageMultiplier);
        }
    }

    /// <summary>
    /// Determines the zombie type based on the current wave number and probability distributions
    /// </summary>
    /// <param name="wave">Current wave number</param>
    /// <returns>The determined zombie type (Walker, Jogger, or Sprinter)</returns>
    private ZombieNavigation.ZombieType DetermineZombieType(int wave)
    {
        // Get the chance for this zombie to be a sprinter or jogger
        float sprintChance = CalculateSprintChance(wave);
        float joggerChance = CalculateJoggerChance(wave);
        
        // Random roll to determine type
        float randomValue = Random.value; // 0.0 to 1.0
        
        // Check for sprinter first (highest tier)
        if (randomValue < sprintChance)
        {
            return ZombieNavigation.ZombieType.Sprinter;
        }
        // Then check for jogger (middle tier)
        else if (randomValue < joggerChance)
        {
            return ZombieNavigation.ZombieType.Jogger;
        }
        // Otherwise default to walker (lowest tier)
        else
        {
            return ZombieNavigation.ZombieType.Walker;
        }
    }
    
    /// <summary>
    /// Gets the proportion of each zombie type for the current wave
    /// Used for UI display and statistics
    /// </summary>
    /// <returns>Array with percentages: [Walker%, Jogger%, Sprinter%]</returns>
    public float[] GetZombieTypeDistribution()
    {
        float sprintChance = CalculateSprintChance(currentWave);
        float joggerChance = CalculateJoggerChance(currentWave);
        
        // Calculate the actual percentages of each type
        float sprintPercent = sprintChance;
        float joggerPercent = joggerChance - sprintChance; // Joggers are the portion between sprint chance and jogger chance
        float walkerPercent = 1f - joggerChance; // Walkers are the remaining percentage
        
        // Ensure all percentages are valid (between 0 and 1)
        sprintPercent = Mathf.Clamp01(sprintPercent);
        joggerPercent = Mathf.Clamp01(joggerPercent);
        walkerPercent = Mathf.Clamp01(walkerPercent);
        
        // Return as array: [Walker%, Jogger%, Sprinter%]
        return new float[] { walkerPercent, joggerPercent, sprintPercent };
    }
    
    /// <summary>
    /// Gets the current wave number
    /// </summary>
    /// <returns>The current wave number</returns>
    public int GetCurrentWave()
    {
        return currentWave;
    }
    
    /// <summary>
    /// Gets the number of zombies remaining to be defeated in the current wave
    /// </summary>
    /// <returns>Number of zombies remaining</returns>
    public int GetRemainingZombies()
    {
        return zombiesRemainingInWave;
    }
    
    /// <summary>
    /// Sets the game active state to control wave spawning
    /// </summary>
    /// <param name="active">Whether the game is active (false stops wave spawning)</param>
    public void SetGameActive(bool active)
    {
        gameActive = active;
    }
}
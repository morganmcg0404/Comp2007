using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveManagement : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private int zombiesPerWave = 4;          // Starting zombies
    [SerializeField] private float zombieHealthIncrease = 5f; // Health increase per round
    [SerializeField] private float baseZombieHealth = 100f;   // Starting zombie health
    [SerializeField] private int maxZombiesPerWave = 500;     // Maximum zombies in any wave
    [SerializeField] private int waveToReachMaxZombies = 100; // Wave at which max zombies is reached
    [SerializeField] private int maxZombiesAlive = 25;        // Maximum zombies alive at once
    
    [Header("Time Settings")]
    [SerializeField] private float timeBetweenWaves = 10f;    // Time between waves in seconds
    [SerializeField] private float zombieSpawnInterval = 0.5f; // Time between zombie spawns in seconds
    
    [Header("References")]
    [SerializeField] private ZombieSpawner zombieSpawner;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI zombieCountText;

    [Header("Zombie Behavior")]
    [SerializeField] private int waveForAllSprinters = 30; // Wave at which 100% of zombies will sprint
    [SerializeField] private float baseZombieDamage = 10f; // Base damage for wave 1
    [SerializeField] private float zombieDamageIncreasePerWave = 0.01f; // 1% increase per wave
    
    // Wave state tracking
    private int zombiesRemainingInWave = 0;
    private int zombiesAlive = 0;
    private bool waveInProgress = false;
    private bool gameActive = true;
    
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
    
    // Update the Update method to check for pause state
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
    
    // Update coroutines to use unscaled time when paused
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
    
    // Calculate number of zombies for wave (scales from initial zombiesPerWave to maxZombiesPerWave)
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
    
    // Calculate health for zombies at current wave
    private float CalculateZombieHealth()
    {
        return baseZombieHealth + (currentWave - 1) * zombieHealthIncrease;
    }

    private float CalculateSprintChance(int wave)
    {
        // Linear progression from 0% at wave 1 to 100% at waveForAllSprinters
        return Mathf.Clamp01((float)(wave - 1) / (waveForAllSprinters - 1));
    }

    private float CalculateZombieDamageMultiplier()
    {
        // Start at 100% on wave 1, then increase by 1% per wave
        return 1f + (currentWave - 1) * zombieDamageIncreasePerWave;
    }

    private float CalculateZombieDamage()
    {
        float rawDamage = baseZombieDamage * CalculateZombieDamageMultiplier();
        return Mathf.Round(rawDamage); // Round to nearest whole number
    }
    
    // Call this when a zombie dies
    public void ZombieDied()
    {
        zombiesAlive--;
        zombiesRemainingInWave--;
        UpdateUI();
    }
    
    // Update UI elements
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
    
    // This method is called by the HealthSystem of each zombie when they die
    // Make sure to call this method in your zombie's health system's death event
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
            float sprintChance = CalculateSprintChance(currentWave);
            bool isSprinter = Random.value < sprintChance;
            zombieNav.SetSprinter(isSprinter);
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
}
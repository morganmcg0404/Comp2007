using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles basic health functionality for any entity in the game
/// including damage, healing, death, and related visual effects
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isDead = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;
    
    [Header("Audio Feedback")]
    /// <summary>
    /// Sound name for when this entity takes damage
    /// </summary>
    [SerializeField] private string damageSoundName = "ZombieDamage";
    
    /// <summary>
    /// Whether to play damage sounds for this entity
    /// </summary>
    [SerializeField] private bool playDamageSound = true;
    
    /// <summary>
    /// Sound name for when this entity dies
    /// </summary>
    [SerializeField] private string deathSoundName = "";
    
    /// <summary>
    /// Cooldown time between damage sounds to prevent sound spam
    /// </summary>
    [SerializeField] private float damageSoundCooldown = 0.3f;
    
    /// <summary>
    /// Audio mixer group for health-related sounds
    /// </summary>
    [SerializeField] private string audioMixerGroup = "SFX";
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent onDamage;
    
    // Track last damage sound time to prevent spam
    private float lastDamageSoundTime = -1f;

    /// <summary>
    /// Initializes the health system by setting current health to maximum
    /// </summary>
    void Start()
    {
        // Initialize health to max at start
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Applies damage to the entity and handles death if health reaches zero
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply</param>
    public void TakeDamage(float damageAmount)
    {
        // If already dead, don't process damage again
        if (isDead)
            return;

        currentHealth -= damageAmount;
        
        // Play damage sound if enabled and not on cooldown
        if (playDamageSound && !string.IsNullOrEmpty(damageSoundName) && 
            Time.time > lastDamageSoundTime + damageSoundCooldown)
        {
            // Play damage sound
            PlayDamageSound();
            lastDamageSoundTime = Time.time;
        }
        
        // Invoke damage event
        onDamage?.Invoke();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Plays the damage sound using SoundManager with mixer support
    /// </summary>
    private void PlayDamageSound()
    {
        if (string.IsNullOrEmpty(damageSoundName)) return;
    
        SoundManager soundManager = SoundManager.GetInstance();
        if (soundManager == null || soundManager.GetSoundLibrary() == null) 
        {
            Debug.LogWarning("SoundManager or SoundLibrary not available");
            return;
        }
    
        AudioClip clip = soundManager.GetSoundLibrary().GetClipFromName(damageSoundName);
        if (clip == null) return;
    
        // Create the audio source as child of the damaged entity
        GameObject audioObj = new GameObject(damageSoundName + "_Sound");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
    
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = 0.8f;
        audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
        audioSource.spatialBlend = 1.0f; // Full 3D sound
    
        // Set audio mixer group if SoundManager provides it
        if (soundManager.GetAudioMixerGroup(audioMixerGroup) != null) 
        {
            audioSource.outputAudioMixerGroup = soundManager.GetAudioMixerGroup(audioMixerGroup);
        }
    
        audioSource.Play();
    
        // Clean up after playing
        Destroy(audioObj, clip.length + 0.1f);
    }
    
    /// <summary>
    /// Plays the death sound using SoundManager with mixer support
    /// </summary>
    private void PlayDeathSound()
    {
        if (string.IsNullOrEmpty(deathSoundName)) return;
        
        SoundManager soundManager = SoundManager.GetInstance();
        if (soundManager == null) return;
        
        // Use direct SoundManager method for death sound
        soundManager.PlaySound3DWithMixer(deathSoundName, transform.position, 1.0f, audioMixerGroup);
    }
    
    /// <summary>
    /// Restores health to the entity, not exceeding maximum health
    /// </summary>
    /// <param name="healAmount">Amount of health to restore</param>
    public void Heal(float healAmount)
    {
        if (!isDead)
        {
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        }
    }
    
    /// <summary>
    /// Handles entity death, including effects, points, and object cleanup
    /// </summary>
    void Die()
    {
        isDead = true;
        currentHealth = 0;
    
        // Play death sound if assigned
        if (!string.IsNullOrEmpty(deathSoundName))
        {
            PlayDeathSound();
        }
        
        // Invoke death event
        onDeath?.Invoke();
    
        // Award points if this is an enemy (check tag)
        if (gameObject.CompareTag("Enemy"))
        {
            if (PointSystem.Instance != null)
            {
                PointSystem.Instance.EnemyKilled();
            }
        }
    
        // Spawn death effect if assigned
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
    
        // Destroy the game object if set to destroy on death
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            // If not destroying, you might want to disable components like
            // colliders, renderers, etc.
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
        }
    }
    
    /// <summary>
    /// Gets the current health value
    /// </summary>
    /// <returns>The current health amount</returns>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Gets the maximum possible health value
    /// </summary>
    /// <returns>The maximum health amount</returns>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Checks if the entity is dead
    /// </summary>
    /// <returns>True if entity is dead, false otherwise</returns>
    public bool IsDead()
    {
        return isDead;
    }
}
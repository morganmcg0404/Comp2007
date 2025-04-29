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
    
    [Header("Events")]
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private UnityEvent onDamage;

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
        
        // Invoke damage event
        onDamage?.Invoke();
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
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
using UnityEngine;
using UnityEngine.Events;

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

    void Start()
    {
        // Initialize health to max at start
        currentHealth = maxHealth;
    }

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
    
    public void Heal(float healAmount)
    {
        if (!isDead)
        {
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        }
    }
    
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
    
    // Public getter for current health (used by your shooting script)
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    public bool IsDead()
    {
        return isDead;
    }
}
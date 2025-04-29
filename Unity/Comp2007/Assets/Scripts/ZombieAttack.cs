using System.Collections;
using UnityEngine;

public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.8f;          // Distance at which zombie can attack
    [SerializeField] private float attackCooldown = 1.5f;       // Time between attacks
    [SerializeField] private float attackDamage = 10f;          // Base damage per attack
    [SerializeField] private float attackWindupTime = 0.6f;     // Time between attack start and damage
    [SerializeField] private float attackRecoveryTime = 0.4f;   // Time after damage before zombie can move again
    
    [Header("Attack Variations")]
    [SerializeField] private float sprintDamageMultiplier = 1.5f;  // Sprinters do more damage
    [SerializeField] private float minDamageVariation = 0.8f;      // Minimum damage multiplier (80% of base)
    [SerializeField] private float maxDamageVariation = 1.2f;      // Maximum damage multiplier (120% of base)
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem bloodSplatterEffect;
    [SerializeField] private AudioSource attackSound;
    [SerializeField] private AudioSource attackHitSound;
    [SerializeField] private AudioSource attackMissSound;
    
    // Wave-based damage scaling
    private float waveDamageMultiplier = 1.0f;
    
    // Component references
    private ZombieNavigation zombieNav;
    private Animator animator;
    private Transform playerTransform;
    private HealthArmourSystem playerHealth;
    
    // State tracking
    private bool canAttack = true;
    private bool isAttacking = false;
    private float lastAttackTime = -10f;
    private Collider playerCollider;
    private Coroutine currentAttackCoroutine = null;

    private void Awake()
    {
        // Get references
        zombieNav = GetComponent<ZombieNavigation>();
        animator = GetComponent<Animator>();
        
        // Find player references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<HealthArmourSystem>();
            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInChildren<HealthArmourSystem>();
            }
            
            playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                playerCollider = player.GetComponentInChildren<Collider>();
            }
        }
    }

    private void Update()
    {
        // Skip all processing when game is paused
        if (PauseManager.IsPaused())
            return;
            
        // Skip if already attacking or on cooldown or no player
        if (isAttacking || !canAttack || playerTransform == null) 
            return;
        
        // Check if player is in attack range
        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer <= attackRange)
        {
            TriggerAttack();
        }
    }
    
    private float GetDistanceToPlayer()
    {
        if (playerCollider != null)
        {
            // Use closest point on player's collider for more accurate distance
            Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closestPoint);
        }
        else if (playerTransform != null)
        {
            return Vector3.Distance(transform.position, playerTransform.position);
        }
        
        return float.MaxValue; // No player found
    }
    
    private void TriggerAttack()
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        
        currentAttackCoroutine = StartCoroutine(AttackSequence());
    }
    
    private IEnumerator AttackSequence()
    {
        // Set attack state
        isAttacking = true;
        canAttack = false;
        lastAttackTime = Time.time;
        
        // Tell navigation to stop moving
        if (zombieNav != null)
        {
            zombieNav.StopMoving();
        }
        
        // Play attack animation and sound
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        if (attackSound != null && !attackSound.isPlaying)
        {
            attackSound.Play();
        }
        
        // Wait for attack windup with pause support
        float elapsedTime = 0f;
        while (elapsedTime < attackWindupTime)
        {
            if (!PauseManager.IsPaused())
            {
                elapsedTime += Time.deltaTime;
            }
            yield return null;
        }
        
        // Check if player is still in range at the moment of damage
        bool playerInRange = GetDistanceToPlayer() <= attackRange * 1.2f;
        
        if (playerInRange && playerHealth != null)
        {
            // Calculate and apply damage
            float finalDamage = CalculateDamage();
            playerHealth.TakeDamage(finalDamage);
            
            // Visual and audio feedback for hit
            if (attackHitSound != null)
            {
                attackHitSound.Play();
            }
            
            if (bloodSplatterEffect != null)
            {
                bloodSplatterEffect.Play();
            }
        }
        else if (attackMissSound != null)
        {
            // Player dodged the attack
            attackMissSound.Play();
        }
        
        // Wait for attack recovery with pause support
        elapsedTime = 0f;
        while (elapsedTime < attackRecoveryTime)
        {
            if (!PauseManager.IsPaused())
            {
                elapsedTime += Time.deltaTime;
            }
            yield return null;
        }
        
        // End attack state
        isAttacking = false;
        
        // Resume movement
        if (zombieNav != null)
        {
            zombieNav.StartMoving();
        }
        
        // Start cooldown period with pause support
        elapsedTime = 0f;
        while (elapsedTime < attackCooldown)
        {
            if (!PauseManager.IsPaused())
            {
                elapsedTime += Time.deltaTime;
            }
            yield return null;
        }
        
        // Ready for next attack
        canAttack = true;
        currentAttackCoroutine = null;
    }
    
    private float CalculateDamage()
    {
        // Use the IsSprinter method directly rather than reflection
        bool isSprinter = zombieNav != null && zombieNav.IsSprinter();
        
        // Base damage with wave multiplier
        float damage = attackDamage * waveDamageMultiplier;
        
        // Apply sprinter bonus
        if (isSprinter)
        {
            damage *= sprintDamageMultiplier;
        }
        
        // Apply random variation
        float variation = Random.Range(minDamageVariation, maxDamageVariation);
        damage *= variation;
        
        return Mathf.Round(damage);
    }
    
    // Public API methods for wave scaling
    public void SetAttackDamage(float newDamage)
    {
        attackDamage = newDamage;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        waveDamageMultiplier = multiplier;
    }
    
    // Visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
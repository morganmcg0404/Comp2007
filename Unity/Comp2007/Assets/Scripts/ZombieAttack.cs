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
    private float waveDamageMultiplier = 1.0f; // Starts at 1x (100%)
    
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
                // Try to find health system in children
                playerHealth = player.GetComponentInChildren<HealthArmourSystem>();
            }
            
            // Get player collider for distance checking
            playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null)
            {
                playerCollider = player.GetComponentInChildren<Collider>();
            }
        }
    }

    // Update the Update method to check for pause state
    private void Update()
    {
        // Skip all processing when game is paused
        if (PauseManager.IsPaused())
            return;
            
        // Skip if already attacking or on cooldown
        if (isAttacking || !canAttack || playerTransform == null) return;
        
        // Check if player is in attack range
        float distanceToPlayer = GetDistanceToPlayer();
        
        if (distanceToPlayer <= attackRange)
        {
            // Start attack sequence
            TriggerAttack();
        }
    }
    
    // Calculate actual distance to player, using collider if available
    private float GetDistanceToPlayer()
    {
        if (playerCollider != null)
        {
            // Use the closest point on player's collider for more accurate distance
            Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
            return Vector3.Distance(transform.position, closestPoint);
        }
        else if (playerTransform != null)
        {
            // Fall back to transform position if no collider
            return Vector3.Distance(transform.position, playerTransform.position);
        }
        
        return float.MaxValue; // No player found
    }
    
    // Start the attack sequence
    private void TriggerAttack()
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        
        currentAttackCoroutine = StartCoroutine(AttackSequence());
    }
    
    // Update the AttackSequence coroutine for pause support
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
        
        if (attackSound != null && !PauseManager.IsPaused())
        {
            attackSound.Play();
        }
        
        // Wait for attack windup (during animation) with pause support
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
        float damageDistance = GetDistanceToPlayer();
        bool playerInRange = damageDistance <= attackRange * 1.2f; // Slight buffer for fairness
        
        if (playerInRange && playerHealth != null && !PauseManager.IsPaused())
        {
            // Calculate final damage with variations
            float finalDamage = CalculateDamage();
            
            // Apply damage to player
            playerHealth.TakeDamage(finalDamage);
            
            // Visual and audio feedback for hit
            if (attackHitSound != null && !PauseManager.IsPaused())
            {
                attackHitSound.Play();
            }
            
            if (bloodSplatterEffect != null && !PauseManager.IsPaused())
            {
                bloodSplatterEffect.Play();
            }
            
        }
        else if (!PauseManager.IsPaused())
        {
            // Player dodged the attack
            if (attackMissSound != null)
            {
                attackMissSound.Play();
            }
            
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
    
    // Calculate damage with variations based on zombie type and randomness
    private float CalculateDamage()
    {
        // Check if this is a sprinter zombie for damage bonus
        bool isSprinter = false;
        if (zombieNav != null)
        {
            // Use reflection to check the isSprinter field (since it's private)
            var sprintField = zombieNav.GetType().GetField("isSprinter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sprintField != null)
            {
                isSprinter = (bool)sprintField.GetValue(zombieNav);
            }
        }
        
        // Base damage calculation with wave multiplier
        float damage = attackDamage * waveDamageMultiplier;
        
        // Apply sprinter bonus if applicable
        if (isSprinter)
        {
            damage *= sprintDamageMultiplier;
        }
        
        // Apply random variation
        float variation = Random.Range(minDamageVariation, maxDamageVariation);
        damage *= variation;
        
        // Round to nearest whole number for display/application
        return Mathf.Round(damage);
    }
    
    // Allow external components to adjust damage (e.g., for wave scaling)
    public void SetAttackDamage(float newDamage)
    {
        attackDamage = newDamage;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        waveDamageMultiplier = multiplier;
    }

    public float GetCurrentDamage()
    {
        return CalculateDamage();
    }
    
    // Visualize attack range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
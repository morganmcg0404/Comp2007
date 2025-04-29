using System.Collections;
using UnityEngine;

/// <summary>
/// Controls zombie melee attack behavior, including damage calculation, timing, and player detection
/// Provides visual and audio feedback for attacks and handles attack animations
/// </summary>
public class ZombieAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    /// <summary>
    /// Distance in meters at which zombie can attack the player
    /// </summary>
    [SerializeField] private float attackRange = 1.8f;          // Distance at which zombie can attack
    
    /// <summary>
    /// Time in seconds between attack attempts
    /// </summary>
    [SerializeField] private float attackCooldown = 1.5f;       // Time between attacks
    
    /// <summary>
    /// Base damage per attack before modifiers
    /// </summary>
    [SerializeField] private float attackDamage = 10f;          // Base damage per attack
    
    /// <summary>
    /// Time in seconds between attack initiation and damage application
    /// </summary>
    [SerializeField] private float attackWindupTime = 0.6f;     // Time between attack start and damage
    
    /// <summary>
    /// Time in seconds after damage application before zombie can move again
    /// </summary>
    [SerializeField] private float attackRecoveryTime = 0.4f;   // Time after damage before zombie can move again
    
    [Header("Attack Variations")]
    /// <summary>
    /// Additional damage multiplier applied to sprinting zombies (faster zombies)
    /// </summary>
    [SerializeField] private float sprintDamageMultiplier = 1.5f;  // Sprinters do more damage
    
    /// <summary>
    /// Minimum random damage variation (percentage of base damage)
    /// </summary>
    [SerializeField] private float minDamageVariation = 0.8f;      // Minimum damage multiplier (80% of base)
    
    /// <summary>
    /// Maximum random damage variation (percentage of base damage)
    /// </summary>
    [SerializeField] private float maxDamageVariation = 1.2f;      // Maximum damage multiplier (120% of base)
    
    [Header("Visual Feedback")]
    /// <summary>
    /// Particle effect played when successfully hitting the player
    /// </summary>
    [SerializeField] private ParticleSystem bloodSplatterEffect;
    
    /// <summary>
    /// Sound played when initiating an attack
    /// </summary>
    [SerializeField] private AudioSource attackSound;
    
    /// <summary>
    /// Sound played when attack successfully hits the player
    /// </summary>
    [SerializeField] private AudioSource attackHitSound;
    
    /// <summary>
    /// Sound played when attack misses the player
    /// </summary>
    [SerializeField] private AudioSource attackMissSound;
    
    // Wave-based damage scaling
    /// <summary>
    /// Damage multiplier set by the wave manager for progressive difficulty scaling
    /// </summary>
    private float waveDamageMultiplier = 1.0f;
    
    // Component references
    /// <summary>
    /// Reference to the zombie's navigation component
    /// </summary>
    private ZombieNavigation zombieNav;
    
    /// <summary>
    /// Reference to the zombie's animator for attack animations
    /// </summary>
    private Animator animator;
    
    /// <summary>
    /// Reference to the player's transform for distance calculations
    /// </summary>
    private Transform playerTransform;
    
    /// <summary>
    /// Reference to the player's health system for applying damage
    /// </summary>
    private HealthArmourSystem playerHealth;
    
    // State tracking
    /// <summary>
    /// Whether the zombie is allowed to start a new attack
    /// </summary>
    private bool canAttack = true;
    
    /// <summary>
    /// Whether an attack is currently in progress
    /// </summary>
    private bool isAttacking = false;
    
    /// <summary>
    /// Time of the last attack for cooldown calculation
    /// </summary>
    private float lastAttackTime = -10f;
    
    /// <summary>
    /// Reference to the player's collider for more accurate distance calculation
    /// </summary>
    private Collider playerCollider;
    
    /// <summary>
    /// Reference to the currently executing attack coroutine
    /// </summary>
    private Coroutine currentAttackCoroutine = null;

    /// <summary>
    /// Initializes component references and finds the player in the scene
    /// </summary>
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

    /// <summary>
    /// Checks player distance each frame and initiates attacks when in range
    /// </summary>
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
    
    /// <summary>
    /// Calculates the distance between the zombie and the player, using collider if available
    /// </summary>
    /// <returns>Distance to the player in meters, or float.MaxValue if player not found</returns>
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
    
    /// <summary>
    /// Initiates an attack sequence, stopping any existing attack in progress
    /// </summary>
    private void TriggerAttack()
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        
        currentAttackCoroutine = StartCoroutine(AttackSequence());
    }
    
    /// <summary>
    /// Coroutine that manages the full attack sequence including windup, damage, and recovery
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
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
    
    /// <summary>
    /// Calculates the final damage amount based on zombie type, wave scaling, and random variation
    /// </summary>
    /// <returns>The final calculated damage amount (rounded to nearest integer)</returns>
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
    
    /// <summary>
    /// Sets the base attack damage value
    /// </summary>
    /// <param name="newDamage">New base damage value</param>
    public void SetAttackDamage(float newDamage)
    {
        attackDamage = newDamage;
    }

    /// <summary>
    /// Sets the wave-based damage multiplier for progressive difficulty scaling
    /// </summary>
    /// <param name="multiplier">Damage multiplier (1.0 = 100% of base damage)</param>
    public void SetDamageMultiplier(float multiplier)
    {
        waveDamageMultiplier = multiplier;
    }
    
    /// <summary>
    /// Visualizes the attack range in the Unity editor as a wire sphere
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    /// <summary>
    /// Checks if the zombie is currently in attack state
    /// </summary>
    /// <returns>True if the zombie is in the process of attacking</returns>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    /// <summary>
    /// Gets the base attack damage before modifiers
    /// </summary>
    /// <returns>Base attack damage value</returns>
    public float GetBaseAttackDamage()
    {
        return attackDamage;
    }

    /// <summary>
    /// Gets the current effective attack damage after all multipliers
    /// </summary>
    /// <returns>Current effective attack damage (average, without random variation)</returns>
    public float GetEffectiveAttackDamage()
    {
        float avgVariation = (minDamageVariation + maxDamageVariation) / 2f;
        bool isSprinter = zombieNav != null && zombieNav.IsSprinter();
        
        float damage = attackDamage * waveDamageMultiplier;
        if (isSprinter)
        {
            damage *= sprintDamageMultiplier;
        }
        
        return damage * avgVariation;
    }
}
using UnityEngine;
using System.Collections;

/// <summary>
/// Handles melee weapon attacks such as knife slashes
/// Implements both primary (stab) and secondary (swing) attack types with different damage profiles
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    [Header("Melee Attack Settings")]
    /// <summary>Base damage amount for the melee weapon</summary>
    [SerializeField] private float damage = 50f;
    
    /// <summary>Maximum distance the attack can reach</summary>
    [SerializeField] private float attackRange = 3f;
    
    /// <summary>How many seconds between attacks</summary>
    [SerializeField] private float attackRate = 0.8f;
    
    /// <summary>Width of the attack arc in degrees for swing attacks</summary>
    [SerializeField] private float attackAngle = 60f; // Attack arc in degrees
    
    /// <summary>Layers that can be hit by the melee attack</summary>
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Attack Types")]
    /// <summary>Whether the primary attack (stab) is enabled</summary>
    [SerializeField] private bool primaryStab = true;
    
    /// <summary>Whether the secondary attack (swing) is enabled</summary>
    [SerializeField] private bool secondarySwing = true;
    
    /// <summary>Damage multiplier for the stab attack</summary>
    [SerializeField] private float stabDamageMultiplier = 1.5f;
    
    /// <summary>Rate multiplier for stab attacks (values below 1 make stabs faster)</summary>
    [SerializeField] private float stabRateMultiplier = 0.7f;
    
    [Header("Effects")]
    /// <summary>Audio source for the primary (stab) attack sound</summary>
    [SerializeField] private AudioSource primaryAttackSound;
    
    /// <summary>Audio source for the secondary (swing) attack sound</summary>
    [SerializeField] private AudioSource secondaryAttackSound;
    
    /// <summary>Particle system for the swing attack visual effect</summary>
    [SerializeField] private ParticleSystem slashEffect;
    
    /// <summary>Effect spawned when hitting non-organic surfaces</summary>
    [SerializeField] private GameObject hitEffect;
    
    /// <summary>Effect spawned when hitting organic targets like enemies</summary>
    [SerializeField] private GameObject bloodEffect;
    
    [Header("Animation")]
    /// <summary>Animator component for playing melee attack animations</summary>
    [SerializeField] private Animator meleeAnimator;
    
    /// <summary>Animator trigger parameter name for the primary attack</summary>
    [SerializeField] private string primaryAttackTrigger = "Stab";
    
    /// <summary>Animator trigger parameter name for the secondary attack</summary>
    [SerializeField] private string secondaryAttackTrigger = "Swing";
    
    // Internal variables
    /// <summary>Time when the next attack can be performed</summary>
    private float nextAttackTime = 0f;
    
    /// <summary>Reference to the player camera for raycasting</summary>
    private Camera playerCamera;
    
    /// <summary>Whether an attack is currently in progress</summary>
    private bool isAttacking = false;
    
    /// <summary>
    /// Initializes the melee attack component
    /// Sets up camera reference and animator if not assigned
    /// </summary>
    void Start()
    {
        playerCamera = Camera.main;
        
        // If animator reference is missing, try to get it from this gameObject
        if (meleeAnimator == null)
        {
            meleeAnimator = GetComponent<Animator>();
        }
    }
    
    /// <summary>
    /// Handles input for triggering melee attacks
    /// Checks for attack cooldowns and input conditions
    /// </summary>
    void Update()
    {
        // Skip all input processing if game is paused
        if (PauseManager.IsPaused())
            return;
        
        // Check if we can attack
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            // Primary attack (left click / stab)
            if (Input.GetButtonDown("Fire1") && primaryStab)
            {
                PerformStabAttack();
            }
            // Secondary attack (right click / swing)
            else if (Input.GetButtonDown("Fire2") && secondarySwing)
            {
                PerformSwingAttack();
            }
        }
    }
    
    /// <summary>
    /// Performs a stab attack (primary attack)
    /// Deals higher damage in a focused point but has a more limited hit area
    /// </summary>
    void PerformStabAttack()
    {
        // Set next attack time accounting for stab rate
        nextAttackTime = Time.time + (attackRate * stabRateMultiplier);
        
        // Set attacking flag
        isAttacking = true;
        
        // Play animation if animator is assigned
        if (meleeAnimator != null)
        {
            meleeAnimator.SetTrigger(primaryAttackTrigger);
        }
        
        // Play sound effect
        if (primaryAttackSound != null)
        {
            primaryAttackSound.Play();
        }
        
        // Attack happens after a small delay (animation sync)
        StartCoroutine(DelayedStabDamage(0.2f));
    }
    
    /// <summary>
    /// Performs a swing attack (secondary attack)
    /// Deals standard damage but in a wider arc in front of the player
    /// </summary>
    void PerformSwingAttack()
    {
        // Set next attack time
        nextAttackTime = Time.time + attackRate;
        
        // Set attacking flag
        isAttacking = true;
        
        // Play animation if animator is assigned
        if (meleeAnimator != null)
        {
            meleeAnimator.SetTrigger(secondaryAttackTrigger);
        }
        
        // Play sound effect
        if (secondaryAttackSound != null)
        {
            secondaryAttackSound.Play();
        }
        
        // Play slash effect if available
        if (slashEffect != null)
        {
            slashEffect.Play();
        }
        
        // Attack happens after a small delay (animation sync)
        StartCoroutine(DelayedSwingDamage(0.2f));
    }
    
    /// <summary>
    /// Applies stab damage after a short delay to sync with the animation
    /// Uses a direct forward raycast for precise targeting
    /// </summary>
    /// <param name="delay">Time in seconds to wait before applying damage</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator DelayedStabDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Stab is a forward-only attack with more focused range
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, attackRange, hitLayers))
        {
            // Apply damage with multiplier
            ApplyDamage(hit, damage * stabDamageMultiplier);
            
            // Create hit effect at hit point
            CreateHitEffect(hit);
        }
        
        // Reset attacking flag after a short cooldown
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }
    
    /// <summary>
    /// Applies swing damage after a short delay to sync with the animation
    /// Uses an arc attack that can hit multiple targets
    /// </summary>
    /// <param name="delay">Time in seconds to wait before applying damage</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator DelayedSwingDamage(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Swing is an arc attack that can hit multiple targets
        PerformArcAttack();
        
        // Reset attacking flag after a short cooldown
        yield return new WaitForSeconds(0.3f);
        isAttacking = false;
    }
    
    /// <summary>
    /// Performs an arc attack that can hit multiple targets in front of the player
    /// Uses overlap sphere and angle checking to determine valid targets
    /// </summary>
    private void PerformArcAttack()
    {
        // Get all colliders within attack range
        Collider[] hitColliders = Physics.OverlapSphere(playerCamera.transform.position, attackRange, hitLayers);
        
        foreach (Collider hitCollider in hitColliders)
        {
            // Get direction to the hit object
            Vector3 directionToTarget = (hitCollider.transform.position - playerCamera.transform.position).normalized;
            
            // Calculate angle between forward direction and target direction
            float angle = Vector3.Angle(playerCamera.transform.forward, directionToTarget);
            
            // If within our attack arc/angle
            if (angle < attackAngle / 2)
            {
                // Do a raycast to confirm we have line of sight
                if (Physics.Raycast(playerCamera.transform.position, directionToTarget, out RaycastHit hit, attackRange, hitLayers))
                {
                    if (hit.collider == hitCollider)
                    {
                        // Apply damage
                        ApplyDamage(hit, damage);
                        
                        // Create hit effect
                        CreateHitEffect(hit);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Applies damage to a hit object if it has a health component
    /// Also handles point scoring and kill detection
    /// </summary>
    /// <param name="hit">The raycast hit information</param>
    /// <param name="damageAmount">Amount of damage to apply</param>
    private void ApplyDamage(RaycastHit hit, float damageAmount)
    {
        // Apply damage if target has health component
        HealthSystem health = hit.collider.GetComponent<HealthSystem>();
        if (health != null)
        {
            // Add points for hit if this is an enemy
            if (hit.collider.CompareTag("Enemy") && PointSystem.Instance != null)
            {
                PointSystem.Instance.EnemyHit();
            }
            
            // Track health before applying damage to detect kills
            float healthBefore = health.GetCurrentHealth();
            
            // Apply damage to the health system
            health.TakeDamage(damageAmount);
            
            // Check if the enemy was killed by this attack (wasn't dead before, is dead now)
            if (hit.collider.CompareTag("Enemy") && healthBefore > 0 && health.IsDead() && PointSystem.Instance != null)
            {
                // Award additional points for knife kill (on top of regular kill points)
                // The HealthSystem already awards normal kill points, so we add extra points here
                PointSystem.Instance.AddPoints(100); // Extra 100 points for melee kills (200 total with regular kill points)
            }
        
            // Basic hit information
            string attackType = damageAmount > damage ? "Stab" : "Swing";
        
            // Create detailed debug message
            if (hit.collider.CompareTag("Enemy"))
            {
                float remainingHealth = health.GetCurrentHealth();
                bool enemyKilled = remainingHealth <= 0;
            }
        }
    }
    
    /// <summary>
    /// Creates an appropriate hit effect at the point of impact
    /// Uses different effects for organic vs non-organic targets
    /// </summary>
    /// <param name="hit">The raycast hit information containing the hit point and normal</param>
    private void CreateHitEffect(RaycastHit hit)
    {
        // Determine which effect to show
        GameObject effectToInstantiate = hitEffect;
        
        // Use blood effect for enemies or specific tags
        if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
        {
            effectToInstantiate = bloodEffect;
        }
        
        // Create effect if assigned
        if (effectToInstantiate != null)
        {
            GameObject impact = Instantiate(
                effectToInstantiate, 
                hit.point, 
                Quaternion.LookRotation(hit.normal)
            );
            
            Destroy(impact, 2f); // Clean up effect after 2 seconds
        }
    }
    
    /// <summary>
    /// Visualizes the attack range and arc in the Unity editor for debugging
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerCamera.transform.position, attackRange);
            
            // Draw attack arc
            Vector3 rightDir = Quaternion.AngleAxis(attackAngle / 2, Vector3.up) * playerCamera.transform.forward;
            Vector3 leftDir = Quaternion.AngleAxis(-attackAngle / 2, Vector3.up) * playerCamera.transform.forward;
            
            Gizmos.DrawRay(playerCamera.transform.position, rightDir * attackRange);
            Gizmos.DrawRay(playerCamera.transform.position, leftDir * attackRange);
        }
    }
    
    /// <summary>
    /// Checks if the melee weapon is currently performing an attack
    /// </summary>
    /// <returns>True if an attack is in progress, false otherwise</returns>
    public bool IsAttacking()
    {
        return isAttacking;
    }
}
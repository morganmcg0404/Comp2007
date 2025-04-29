using UnityEngine;
using System.Collections;

/// <summary>
/// Handles melee weapon attacks such as knife slashes
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    [Header("Melee Attack Settings")]
    [SerializeField] private float damage = 50f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackRate = 0.8f;
    [SerializeField] private float attackAngle = 60f; // Attack arc in degrees
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Attack Types")]
    [SerializeField] private bool primaryStab = true;
    [SerializeField] private bool secondarySwing = true;
    [SerializeField] private float stabDamageMultiplier = 1.5f;
    [SerializeField] private float stabRateMultiplier = 0.7f;
    
    [Header("Effects")]
    [SerializeField] private AudioSource primaryAttackSound;
    [SerializeField] private AudioSource secondaryAttackSound;
    [SerializeField] private ParticleSystem slashEffect;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject bloodEffect;
    
    [Header("Animation")]
    [SerializeField] private Animator meleeAnimator;
    [SerializeField] private string primaryAttackTrigger = "Stab";
    [SerializeField] private string secondaryAttackTrigger = "Swing";
    
    // Internal variables
    private float nextAttackTime = 0f;
    private Camera playerCamera;
    private bool isAttacking = false;
    
    // Start is called before the first frame update
    void Start()
    {
        playerCamera = Camera.main;
        
        // If animator reference is missing, try to get it from this gameObject
        if (meleeAnimator == null)
        {
            meleeAnimator = GetComponent<Animator>();
        }
    }
    
    // Update is called once per frame
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
    /// Apply stab damage after animation delay
    /// </summary>
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
    /// Apply swing damage after animation delay
    /// </summary>
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
    /// Apply damage to hit object if it has health
    /// </summary>
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
    /// Create appropriate hit effect based on what was hit
    /// </summary>
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
    /// Visualize the attack arc in the editor
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
    /// Public method to check if the melee weapon is in the middle of an attack
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }
}
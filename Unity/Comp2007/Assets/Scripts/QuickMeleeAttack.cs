using UnityEngine;
using System.Collections;

/// <summary>
/// Allows quick melee attacks while using primary or secondary weapons
/// </summary>
public class QuickMeleeAttack : MonoBehaviour
{
    [Header("Quick Melee Settings")]
    [SerializeField] private KeyCode quickMeleeKey = KeyCode.V;
    [SerializeField] private float damage = 40f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float cooldown = 1.0f;
    [SerializeField] private float attackDelay = 0.2f; // Time between press and damage
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Effects")]
    [SerializeField] private AudioSource quickMeleeSound;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject bloodEffect;
    [SerializeField] private Animator viewModelAnimator;
    [SerializeField] private string quickMeleeAnimTrigger = "QuickMelee";
    
    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    
    // Internal variables
    private Camera playerCamera;
    private float nextAttackTime = 0f;
    private bool isQuickAttacking = false;
    
    private void Start()
    {
        playerCamera = Camera.main;
        
        // Try to find weapon manager if not assigned
        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
            
            if (weaponManager == null)
            {
                weaponManager = GetComponentInParent<WeaponManager>();
                
                if (weaponManager == null)
                {
                    Debug.LogWarning("WeaponManager not found for QuickMeleeAttack");
                }
            }
        }
    }
    
    private void Update()
    {
        // Skip all input processing if game is paused
        if (PauseManager.IsPaused())
            return;
        
        // Check for quick melee input
        if (Input.GetKeyDown(quickMeleeKey) && Time.time >= nextAttackTime && !isQuickAttacking)
        {
            // Only allow when using primary or secondary (not when knife is already equipped)
            if (weaponManager != null && 
                (weaponManager.GetCurrentWeaponSlot() == WeaponManager.WeaponSlot.Primary || 
                 weaponManager.GetCurrentWeaponSlot() == WeaponManager.WeaponSlot.Secondary))
            {
                StartCoroutine(PerformQuickMeleeAttack());
            }
        }
    }
    
    /// <summary>
    /// Performs the quick melee attack sequence
    /// </summary>
    private IEnumerator PerformQuickMeleeAttack()
    {
        // Set cooldown and state
        nextAttackTime = Time.time + cooldown;
        isQuickAttacking = true;
        
        // Play animation if available
        if (viewModelAnimator != null)
        {
            viewModelAnimator.SetTrigger(quickMeleeAnimTrigger);
        }
        
        // Play sound if available
        if (quickMeleeSound != null)
        {
            quickMeleeSound.Play();
        }
        
        // Wait for the attack delay (animation timing)
        yield return new WaitForSeconds(attackDelay);
        
        // Perform the actual damage
        PerformQuickMeleeDamage();
        
        // Reset state after a short delay
        yield return new WaitForSeconds(0.3f);
        isQuickAttacking = false;
    }
    
    /// <summary>
    /// Applies damage for the quick melee attack
    /// </summary>
    private void PerformQuickMeleeDamage()
    {
        // Direct forward attack
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, attackRange, hitLayers))
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
                
                // Apply damage
                health.TakeDamage(damage);
                
                // Check if the enemy was killed by this attack (wasn't dead before, is dead now)
                if (hit.collider.CompareTag("Enemy") && healthBefore > 0 && health.IsDead() && PointSystem.Instance != null)
                {
                    // Award additional points for quick melee kill (on top of regular kill points)
                    // The HealthSystem already awards normal kill points, so we add extra points here
                    PointSystem.Instance.AddPoints(100); // Extra 100 points for melee kills (200 total with regular kill points)
                }
                
                // Debug info
                if (hit.collider.CompareTag("Enemy"))
                {
                    float remainingHealth = health.GetCurrentHealth();
                    bool enemyKilled = remainingHealth <= 0;
                }
            }
            
            // Show hit effect
            CreateHitEffect(hit);
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
    /// Returns whether a quick melee attack is in progress
    /// </summary>
    public bool IsQuickAttacking()
    {
        return isQuickAttacking;
    }
}
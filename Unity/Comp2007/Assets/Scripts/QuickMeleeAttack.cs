using UnityEngine;
using System.Collections;

/// <summary>
/// Allows quick melee attacks while using primary or secondary weapons
/// Handles damage application, effects, and interaction with the point system
/// </summary>
public class QuickMeleeAttack : MonoBehaviour
{
    [Header("Quick Melee Settings")]
    /// <summary>
    /// Key used to trigger quick melee attacks
    /// </summary>
    [SerializeField] private KeyCode quickMeleeKey = KeyCode.V;
    
    /// <summary>
    /// Damage dealt by quick melee attacks
    /// </summary>
    [SerializeField] private float damage = 40f;
    
    /// <summary>
    /// Maximum distance the quick melee attack can reach
    /// </summary>
    [SerializeField] private float attackRange = 2f;
    
    /// <summary>
    /// Time in seconds before another quick melee attack can be performed
    /// </summary>
    [SerializeField] private float cooldown = 1.0f;
    
    /// <summary>
    /// Delay in seconds between button press and damage application
    /// </summary>
    [SerializeField] private float attackDelay = 0.2f; // Time between press and damage
    
    /// <summary>
    /// Layers that can be hit by quick melee attacks
    /// </summary>
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Effects")]
    /// <summary>
    /// Sound name for the quick melee attack sound
    /// </summary>
    [SerializeField] private string meleeSoundName = "QuickMelee";
    
    /// <summary>
    /// Effect spawned when hitting non-organic surfaces
    /// </summary>
    [SerializeField] private GameObject hitEffect;
    
    /// <summary>
    /// Effect spawned when hitting organic targets like enemies
    /// </summary>
    [SerializeField] private GameObject bloodEffect;
    
    /// <summary>
    /// Animator component for the weapon view model
    /// </summary>
    [SerializeField] private Animator viewModelAnimator;
    
    /// <summary>
    /// Animation trigger parameter name for quick melee animations
    /// </summary>
    [SerializeField] private string quickMeleeAnimTrigger = "QuickMelee";
    
    [Header("References")]
    /// <summary>
    /// Reference to the weapon manager for checking current weapon
    /// </summary>
    [SerializeField] private WeaponManager weaponManager;
    
    // Internal variables
    /// <summary>
    /// Reference to the main camera for raycasting
    /// </summary>
    private Camera playerCamera;
    
    /// <summary>
    /// Time when the next attack can be performed
    /// </summary>
    private float nextAttackTime = 0f;
    
    /// <summary>
    /// Whether a quick melee attack is currently in progress
    /// </summary>
    private bool isQuickAttacking = false;
    
    /// <summary>
    /// Initializes references and attempts to find weapon manager if not assigned
    /// </summary>
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
    
    /// <summary>
    /// Checks for quick melee input and initiates attack when appropriate
    /// </summary>
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
    /// Performs the quick melee attack sequence including animation and damage
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
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
        
        // Play melee sound as child of player
        PlayMeleeSound(meleeSoundName);
        
        // Wait for the attack delay (animation timing)
        yield return new WaitForSeconds(attackDelay);
        
        // Perform the actual damage
        PerformQuickMeleeDamage();
        
        // Reset state after a short delay
        yield return new WaitForSeconds(0.3f);
        isQuickAttacking = false;
    }
    
    /// <summary>
    /// Applies damage for the quick melee attack using a forward raycast
    /// Also handles point scoring and effect creation
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
            
            // Show hit effect and play appropriate sound
            CreateHitEffect(hit);
        }
    }
    
    /// <summary>
    /// Creates an appropriate hit effect at the point of impact
    /// Uses different effects for organic vs non-organic targets
    /// </summary>
    /// <param name="hit">The raycast hit information containing the hit point and normal</param>
    private void CreateHitEffect(RaycastHit hit)
    {
        // Determine which effect to show and sound to play
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
    /// Returns whether a quick melee attack is currently in progress
    /// </summary>
    /// <returns>True if an attack is in progress, false otherwise</returns>
    public bool IsQuickAttacking()
    {
        return isQuickAttacking;
    }
    
    /// <summary>
    /// Plays melee sounds as a child of the player for better spatial audio while moving
    /// </summary>
    /// <param name="soundName">Name of the sound in SoundLibrary</param>
    /// <param name="volume">Volume level (default 1.0)</param>
    private void PlayMeleeSound(string soundName, float volume = 1.0f, string mixerGroup = "SFX")
    {
        if (string.IsNullOrEmpty(soundName)) return;
    
        SoundManager soundManager = SoundManager.GetInstance();
        if (soundManager == null || soundManager.GetSoundLibrary() == null) 
        {
            Debug.LogWarning("SoundManager or SoundLibrary not available");
            return;
        }
    
        AudioClip clip = soundManager.GetSoundLibrary().GetClipFromName(soundName);
        if (clip == null) return;
    
        // Find the player transform - could be parent or grandparent of weapon
        Transform playerTransform = transform;
        while (playerTransform.parent != null)
        {
            if (playerTransform.CompareTag("Player"))
                break;
            playerTransform = playerTransform.parent;
        }
    
        // Create the audio source as child of player
        GameObject audioObj = new GameObject(soundName + "_Sound");
        audioObj.transform.SetParent(playerTransform);
        audioObj.transform.localPosition = Vector3.zero;
    
        AudioSource audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f; // Full 3D sound
    
        // Set audio mixer group if SoundManager provides it
        if (soundManager.GetAudioMixerGroup(mixerGroup) != null) 
        {
            audioSource.outputAudioMixerGroup = soundManager.GetAudioMixerGroup(mixerGroup);
        }
    
        audioSource.Play();
    
        // Clean up after playing
        Destroy(audioObj, clip.length + 0.1f);
    }
}
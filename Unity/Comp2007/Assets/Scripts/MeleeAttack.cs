using UnityEngine;
using System.Collections;

/// <summary>
/// Handles melee weapon attacks such as knife slashes
/// Implements swing attack mechanics with damage, effects, and sound
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    [Header("Melee Attack Settings")]
    /// <summary>Damage amount for the melee weapon</summary>
    [SerializeField] private float damage = 50f;
    
    /// <summary>Maximum distance the attack can reach</summary>
    [SerializeField] private float attackRange = 3f;
    
    /// <summary>How many seconds between attacks</summary>
    [SerializeField] private float attackRate = 0.8f;
    
    /// <summary>Width of the attack arc in degrees for swing attacks</summary>
    [SerializeField] private float attackAngle = 60f; // Attack arc in degrees
    
    /// <summary>Layers that can be hit by the melee attack</summary>
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Effects")]
    /// <summary>Sound name for the swing attack</summary>
    [SerializeField] private string swingSoundName = "KnifeSwing";
    
    /// <summary>Particle system for the swing attack visual effect</summary>
    [SerializeField] private ParticleSystem slashEffect;
    
    /// <summary>Effect spawned when hitting non-organic surfaces</summary>
    [SerializeField] private GameObject hitEffect;
    
    /// <summary>Effect spawned when hitting organic targets like enemies</summary>
    [SerializeField] private GameObject bloodEffect;
    
    [Header("Animation")]
    /// <summary>Animator component for playing melee attack animations</summary>
    [SerializeField] private Animator meleeAnimator;
    
    /// <summary>Animator trigger parameter name for the swing attack</summary>
    [SerializeField] private string swingAnimTrigger = "Swing";
    
    /// <summary>Whether the attack timing should match the animation length</summary>
    [SerializeField] private bool matchAttackTimeToAnimation = true;
    
    /// <summary>Animation clip for the swing attack if not using animator controller</summary>
    [SerializeField] private AnimationClip swingAnimClip;
    
    /// <summary>How far into the animation the damage should be applied (0-1 range)</summary>
    [SerializeField] private float damageDelayPercent = 0.5f;
    
    [Header("References")]
    /// <summary>Reference to the weapon manager for checking current weapon selection</summary>
    [SerializeField] private WeaponManager weaponManager;
    
    // Internal variables
    /// <summary>Time when the next attack can be performed</summary>
    private float nextAttackTime = 0f;
    
    /// <summary>Reference to the player camera for raycasting</summary>
    private Camera playerCamera;
    
    /// <summary>Whether an attack is currently in progress</summary>
    private bool isAttacking = false;
    
    /// <summary>
    /// Initializes the melee attack component
    /// Sets up camera reference, animator, and weapon manager references
    /// </summary>
    void Start()
    {
        playerCamera = Camera.main;
        
        // Set up the animator
        SetupAnimator();
        
        // Try to find weapon manager if not assigned
        if (weaponManager == null)
        {
            // Try to find in parent objects
            weaponManager = GetComponentInParent<WeaponManager>();
            
            if (weaponManager == null)
            {
                // Try to find in the scene
                weaponManager = FindFirstObjectByType<WeaponManager>();
                
                if (weaponManager == null)
                {
                    Debug.LogWarning("WeaponManager reference not found for MeleeAttack script.");
                }
            }
        }
    }
    
    /// <summary>
    /// Sets up the animator component if not already assigned
    /// </summary>
    private void SetupAnimator()
    {
        // If animator not assigned, try to find one
        if (meleeAnimator == null)
        {
            meleeAnimator = GetComponent<Animator>();
            
            // If still not found, try to find on child objects
            if (meleeAnimator == null)
            {
                meleeAnimator = GetComponentInChildren<Animator>();
            }
        }
        
        // Validate the animation clip if we're using it
        if (matchAttackTimeToAnimation && swingAnimClip == null && meleeAnimator != null)
        {
            // Try to get the clip from the animator if possible
            if (meleeAnimator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = meleeAnimator.runtimeAnimatorController.animationClips;
                foreach (AnimationClip clip in clips)
                {
                    if (clip.name.Contains("swing") || clip.name.Contains("attack") || 
                        clip.name.Contains("slash") || clip.name.Contains("melee"))
                    {
                        swingAnimClip = clip;
                        Debug.Log($"Auto-assigned swing animation clip: {clip.name}");
                        break;
                    }
                }
            }
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
        
        // Only process input if this weapon is the current active one
        if (!IsMeleeWeaponActive())
            return;
        
        // Check if we can attack with Mouse1 (left click)
        if (Time.time >= nextAttackTime && !isAttacking && Input.GetButtonDown("Fire1"))
        {
            PerformSwingAttack();
        }
    }
    
    /// <summary>
    /// Performs a swing attack
    /// Deals standard damage in a wide arc in front of the player
    /// </summary>
    void PerformSwingAttack()
    {
        // Set next attack time
        nextAttackTime = Time.time + attackRate;
        
        // Set attacking flag
        isAttacking = true;
        
        // Play animation first if animator is assigned
        float animationDuration = 0.5f; // Default animation duration if not matching to clip
        if (meleeAnimator != null)
        {
            meleeAnimator.SetTrigger(swingAnimTrigger);
            
            // Get animation length if we should match timing
            if (matchAttackTimeToAnimation && swingAnimClip != null)
            {
                animationDuration = swingAnimClip.length;
            }
        }
        
        // Play swing sound after animation starts
        PlayMeleeSound(swingSoundName);
        
        // Play slash effect if available
        if (slashEffect != null)
        {
            slashEffect.Play();
        }
        
        // Calculate when damage should occur based on the animation
        float damageDelay = matchAttackTimeToAnimation && swingAnimClip != null ?
            swingAnimClip.length * damageDelayPercent : 0.2f;
        
        // Attack happens after calculated delay (animation sync)
        StartCoroutine(DelayedSwingDamage(damageDelay, animationDuration));
    }
    
    /// <summary>
    /// Applies swing damage after a short delay to sync with the animation
    /// Uses an arc attack that can hit multiple targets
    /// </summary>
    /// <param name="damageDelay">Time in seconds to wait before applying damage</param>
    /// <param name="animationDuration">Total duration of the animation</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator DelayedSwingDamage(float damageDelay, float animationDuration)
    {
        // Wait until the point in the animation where damage should occur
        yield return new WaitForSeconds(damageDelay);
        
        // Swing is an arc attack that can hit multiple targets
        PerformArcAttack();
        
        // Wait until animation is fully complete before allowing next attack
        float remainingAnimTime = animationDuration - damageDelay;
        if (remainingAnimTime > 0)
        {
            yield return new WaitForSeconds(remainingAnimTime);
        }
        
        // Reset attacking flag
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
    
    /// <summary>
    /// Checks if the melee weapon is currently active in the weapon manager
    /// </summary>
    /// <returns>True if melee weapon is active, false otherwise</returns>
    private bool IsMeleeWeaponActive()
    {
        if (weaponManager == null)
            return false;
            
        return weaponManager.GetCurrentWeaponSlot() == WeaponManager.WeaponSlot.Melee;
    }
    
    /// <summary>
    /// Plays melee sounds as a child of the player for better spatial audio while moving
    /// </summary>
    /// <param name="soundName">Name of the sound in SoundLibrary</param>
    /// <param name="volume">Volume level (default 1.0)</param>
    /// <param name="mixerGroup">Audio mixer group to use (default "SFX")</param>
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
    
    /// <summary>
    /// Gets the current swing animation clip
    /// </summary>
    /// <returns>Current swing animation clip or null if not assigned</returns>
    public AnimationClip GetSwingAnimClip()
    {
        return swingAnimClip;
    }
    
    /// <summary>
    /// Sets a new swing animation clip
    /// </summary>
    /// <param name="newClip">New animation clip to use for swinging</param>
    public void SetSwingAnimClip(AnimationClip newClip)
    {
        if (newClip != null)
        {
            swingAnimClip = newClip;
            
            // Update attack timing if we're matching to animation
            if (matchAttackTimeToAnimation)
            {
                attackRate = newClip.length;
            }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles weapon shooting mechanics including hitscan firing, ammo management,
/// fire modes, recoil, bullet penetration, and visual effects
/// </summary>
public class Shoot : MonoBehaviour
{
    [Header("Shooting Properties")]
    /// <summary>
    /// Transform marking the position and direction where bullets originate
    /// </summary>
    [SerializeField] private Transform firePoint;
    
    /// <summary>
    /// Maximum distance in units that bullets can travel
    /// </summary>
    [SerializeField] private float range = 100f;
    
    /// <summary>
    /// Base damage dealt by each bullet hit
    /// </summary>
    [SerializeField] private float damage = 10f;
    
    /// <summary>
    /// Rate of fire measured in rounds per minute
    /// </summary>
    [SerializeField] private int roundsPerMinute = 600; // Changed from fireRate to roundsPerMinute
    
    /// <summary>
    /// Layers that bullets can collide with
    /// </summary>
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Fire Mode Settings")]
    /// <summary>
    /// Default firing mode when the weapon is first equipped
    /// </summary>
    [SerializeField] private FireMode defaultFireMode = FireMode.SemiAuto;
    
    /// <summary>
    /// Whether the player can toggle between fire modes
    /// </summary>
    [SerializeField] private bool canToggleFireMode = true;  // Enable/disable fire mode toggling
    
    /// <summary>
    /// Key used to switch between fire modes
    /// </summary>
    [SerializeField] private KeyCode toggleFireModeKey = KeyCode.B;
    
    [Header("Ammunition Settings")]
    /// <summary>
    /// Number of rounds in a full magazine
    /// </summary>
    [SerializeField] private int magazineSize = 30;
    
    /// <summary>
    /// Maximum total ammunition that can be carried including current magazine
    /// </summary>
    [SerializeField] private int maxAmmo = 150;
    
    /// <summary>
    /// Time in seconds it takes to complete a reload
    /// </summary>
    [SerializeField] private float reloadTime = 2.0f;
    
    /// <summary>
    /// Key used to manually initiate a reload
    /// </summary>
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    
    /// <summary>
    /// Whether the weapon automatically reloads when the magazine is empty
    /// </summary>
    [SerializeField] private bool autoReloadWhenEmpty = true;
    
    [Header("Effects")]
    /// <summary>
    /// Sound name for firing the weapon
    /// </summary>
    [SerializeField] private string shootSoundName = "Shoot";

    /// <summary>
    /// Visual particle effect for the muzzle flash when firing
    /// </summary>
    [SerializeField] private ParticleSystem muzzleFlash;

    /// <summary>
    /// Effect spawned at bullet impact points
    /// </summary>
    [SerializeField] private GameObject hitEffect;

    /// <summary>
    /// Line renderer used to draw bullet trails
    /// </summary>
    [SerializeField] private LineRenderer bulletTrail;

    /// <summary>
    /// How long bullet trails remain visible in seconds
    /// </summary>
    [SerializeField] private float trailDuration = 0.05f;

    /// <summary>
    /// Sound name for toggling between fire modes
    /// </summary>
    [SerializeField] private string fireModeToggleSoundName = "FireModeToggle";

    /// <summary>
    /// Sound name for reloading the weapon
    /// </summary>
    [SerializeField] private string reloadSoundName = "Reload";

    /// <summary>
    /// Sound name for bullet impact
    /// </summary>
    [SerializeField] private string bulletImpactSoundName = "BulletImpact";
    
    [Header("Recoil Settings")]
    /// <summary>
    /// Maximum recoil angle in degrees applied when firing
    /// </summary>
    [SerializeField] private float recoilAmount = 1.0f;     // Maximum recoil angle in degrees
    
    /// <summary>
    /// Speed at which recoil returns to neutral position
    /// </summary>
    [SerializeField] private float recoilRecoverySpeed = 5.0f;  // How quickly recoil recovers
    
    /// <summary>
    /// Speed at which recoil is applied when firing
    /// </summary>
    [SerializeField] private float recoilRotationSpeed = 10.0f; // How quickly recoil is applied
    
    /// <summary>
    /// Maximum accumulated recoil angle in degrees
    /// </summary>
    [SerializeField] private float maxRecoilAngle = 3.0f;   // Maximum accumulated recoil
    
    /// <summary>
    /// Transform to which recoil rotation is applied (typically camera)
    /// </summary>
    [SerializeField] private Transform recoilTarget; // Assign this in inspector to camera or a parent object

    [Header("Penetration Settings")]
    /// <summary>
    /// Whether bullets can pass through multiple targets
    /// </summary>
    [SerializeField] private bool enablePenetration = true;
    
    /// <summary>
    /// Maximum number of targets a bullet can penetrate
    /// </summary>
    [SerializeField] private int maxPenetration = 3; // Maximum number of enemies a bullet can penetrate
    
    /// <summary>
    /// Percentage damage reduction (0-1) after each penetration
    /// </summary>
    [SerializeField] private float damageReductionPerHit = 0.3f; // Damage reduction after each penetration (30%)
    
    /// <summary>
    /// Layers that bullets can penetrate (typically enemies)
    /// </summary>
    [SerializeField] private LayerMask penetrableLayers; // Layers that can be penetrated (typically enemies)
    
    [Header("Animations")]
    /// <summary>
    /// Animator component for weapon animations
    /// </summary>
    [SerializeField] private Animator weaponAnimator;
    
    /// <summary>
    /// Name of the reload animation trigger parameter in the Animator
    /// </summary>
    [SerializeField] private string reloadAnimTrigger = "Reload";
    
    /// <summary>
    /// Whether the reload time should match the animation length
    /// </summary>
    [SerializeField] private bool matchReloadTimeToAnimation = true;
    
    /// <summary>
    /// Animator controller for the reload animation
    /// </summary>
    [SerializeField] private RuntimeAnimatorController reloadAnimController;
    
    /// <summary>
    /// Optional reload animation clip if not using an animator controller
    /// </summary>
    [SerializeField] private AnimationClip reloadAnimClip;
    
    /// <summary>
    /// Defines the available automatic firing modes
    /// </summary>
    public enum FireMode
    {
        /// <summary>One shot per trigger pull</summary>
        SemiAuto,
        
        /// <summary>Continuous fire while trigger is held</summary>
        FullAuto
    }
    
    /// <summary>
    /// Time when the weapon can fire next
    /// </summary>
    private float nextFireTime = 0f;
    
    /// <summary>
    /// Internal calculation of time between shots in seconds
    /// </summary>
    private float fireRate; // Keep this as an internal variable for calculations
    
    /// <summary>
    /// Reference to the player's camera
    /// </summary>
    private Camera playerCamera;
    
    /// <summary>
    /// Current rotation applied by recoil
    /// </summary>
    private Vector3 currentRotation;
    
    /// <summary>
    /// Target rotation for recoil interpolation
    /// </summary>
    private Vector3 targetRotation;
    
    /// <summary>
    /// Current firing mode of the weapon
    /// </summary>
    private FireMode currentFireMode;
    
    // Ammo variables
    /// <summary>
    /// Current rounds in the loaded magazine
    /// </summary>
    private int currentAmmoInMag;
    
    /// <summary>
    /// Current total reserve ammunition
    /// </summary>
    private int currentTotalAmmo;
    
    /// <summary>
    /// Whether the weapon is currently in the reload animation
    /// </summary>
    private bool isReloading = false;

    /// <summary>
    /// Whether this weapon is currently the active weapon
    /// </summary>
    private bool isActiveWeapon = false;
    
    /// <summary>
    /// Reference to the weapon management system
    /// </summary>
    private WeaponManager weaponManager;

    /// <summary>
    /// Initializes weapon references, ammo counts, and firing parameters
    /// </summary>
    void Start()
    {
        // If no fire point is assigned, use this object's position
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // Find the camera (typically the main camera or a weapon camera)
        playerCamera = Camera.main;
        
        // Set line renderer if it exists
        if (bulletTrail != null)
        {
            bulletTrail.enabled = false;
        }
        
        // Initialize recoil variables
        currentRotation = Vector3.zero;
        targetRotation = Vector3.zero;
        
        // Set initial fire mode
        currentFireMode = defaultFireMode;
        
        // Initialize ammo
        currentAmmoInMag = magazineSize;
        currentTotalAmmo = maxAmmo - magazineSize;
        
        // Find the weapon manager to track active state
        weaponManager = FindAnyObjectByType<WeaponManager>();
        if (weaponManager == null)
        {
            weaponManager = GetComponentInParent<WeaponManager>();
        }
        
        // Convert rounds per minute to fire rate in seconds
        UpdateFireRateFromRPM();
        
        // Initialize weapon animator if needed
        SetupAnimator();
    }
    
    /// <summary>
    /// Converts rounds per minute to a fire rate in seconds between shots
    /// </summary>
    private void UpdateFireRateFromRPM()
    {
        // Calculate seconds per round from rounds per minute
        // RPM / 60 = Rounds Per Second, 1 / RPS = Seconds Per Round
        if (roundsPerMinute <= 0)
            roundsPerMinute = 1; // Prevent division by zero
            
        fireRate = 60f / roundsPerMinute;
    }
    
    /// <summary>
    /// Sets the weapon's fire rate in rounds per minute
    /// </summary>
    /// <param name="rpm">New rate of fire in rounds per minute</param>
    public void SetRoundsPerMinute(int rpm)
    {
        roundsPerMinute = Mathf.Max(1, rpm); // Ensure RPM is at least 1
        UpdateFireRateFromRPM();
    }
    
    /// <summary>
    /// Handles weapon input, firing, reloading, and recoil every frame
    /// </summary>
    void Update()
    {
        // Skip all processing if game is paused
        if (PauseManager.IsPaused())
            return;
        
        // First, check if this weapon is currently active
        UpdateActiveState();
        
        // Only process input if this is the active weapon
        if (!isActiveWeapon)
            return;
            
        // Skip shooting logic if reloading
        if (isReloading)
            return;
        
        // Check for reload input
        if (Input.GetKeyDown(reloadKey) && currentAmmoInMag < magazineSize && currentTotalAmmo > 0)
        {
            StartCoroutine(Reload());
            return;
        }
        
        // Check for fire mode toggle input
        if (canToggleFireMode && Input.GetKeyDown(toggleFireModeKey))
        {
            ToggleFireMode();
        }
        
        // Handle firing based on current fire mode
        if (currentFireMode == FireMode.FullAuto)
        {
            // For full auto, check if button is being held
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
            {
                TryToFire();
            }
        }
        else // Semi-Auto
        {
            // For semi-auto, only fire on button press
            if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
            {
                TryToFire();
            }
        }
        
        // Update recoil effect
        UpdateRecoil();
    }
    
    /// <summary>
    /// Attempts to fire the weapon, checking ammo and handling empty magazine cases
    /// </summary>
    void TryToFire()
    {
        // Check if we have ammo
        if (currentAmmoInMag <= 0)
        {            
            // Auto reload if enabled and we have reserve ammo
            if (autoReloadWhenEmpty && currentTotalAmmo > 0)
            {
                StartCoroutine(Reload());
            }
            
            return;
        }
        
        // We have ammo, fire the weapon
        FireHitscan();
        
        // Decrease ammo in magazine
        currentAmmoInMag--;
    }
    
    /// <summary>
    /// Coroutine that handles the reload sequence including timing and ammo transfer
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    IEnumerator Reload()
    {
        // Start reload sequence
        isReloading = true;
        
        float animationDuration = reloadTime;
        if (weaponAnimator != null)
        {
            // Trigger the reload animation immediately
            weaponAnimator.SetTrigger(reloadAnimTrigger);
            
            // Get animation length if we should match timing to animation
            if (matchReloadTimeToAnimation && reloadAnimClip != null)
            {
                animationDuration = reloadAnimClip.length;
                
                // Log mismatch warning if there's a significant difference
                if (Mathf.Abs(reloadTime - animationDuration) > 0.25f)
                {
                    Debug.LogWarning($"Reload time ({reloadTime}s) doesn't match animation length ({animationDuration}s)");
                }
            }
        }
        
        // Play reload sound AFTER starting animation to ensure sync
        PlayWeaponSound(reloadSoundName);
        
        // Wait for reload time (use animation length if matching to animation)
        if (matchReloadTimeToAnimation && reloadAnimClip != null)
        {
            yield return new WaitForSeconds(animationDuration);
        }
        else
        {
            yield return new WaitForSeconds(reloadTime);
        }
        
        // Calculate ammo to add to magazine
        int ammoToAdd = magazineSize - currentAmmoInMag;
        
        // Make sure we don't exceed total ammo
        if (ammoToAdd > currentTotalAmmo)
        {
            ammoToAdd = currentTotalAmmo;
        }
        
        // Update ammo counts
        currentAmmoInMag += ammoToAdd;
        currentTotalAmmo -= ammoToAdd;
        
        // End reload sequence
        isReloading = false;
    }
    
    /// <summary>
    /// Toggles between available fire modes and plays feedback sound
    /// </summary>
    void ToggleFireMode()
    {
        // Switch between fire modes
        currentFireMode = (currentFireMode == FireMode.SemiAuto) ? 
            FireMode.FullAuto : FireMode.SemiAuto;
        
        // Play toggle sound with mixer support
        PlayWeaponSound(fireModeToggleSoundName);
        
        // You could add UI feedback here, like displaying the current fire mode on screen
    }
    
    /// <summary>
    /// Fires a hitscan ray, handles bullet penetration, damage, and visual effects
    /// </summary>
    void FireHitscan()
    {
        // Set the next time we can fire
        nextFireTime = Time.time + fireRate;

        // Play muzzle flash
        PlayMuzzleFlash();

        // Play shoot sound attached to player
        PlayWeaponSound(shootSoundName);

        // Apply recoil
        ApplyRecoil();

        // Define start point for raycast
        Vector3 rayOrigin = firePoint.position;
        // For better accuracy when using a camera, use its position
        if (playerCamera != null)
        {
            rayOrigin = playerCamera.transform.position;
        }

        // Direction of the ray
        Vector3 rayDirection = firePoint.forward;
        if (playerCamera != null)
        {
            rayDirection = playerCamera.transform.forward;
        }

        // Variables to track penetration
        int penetrationsRemaining = enablePenetration ? maxPenetration : 1;
        float currentDamage = damage;
        Vector3 currentOrigin = rayOrigin;
        RaycastHit hit;
        Vector3 lastHitPoint = rayOrigin;

        // Track all hit points for bullet trail
        List<Vector3> hitPoints = new List<Vector3>() { firePoint.position };

        // Continue tracing the bullet path until we run out of penetrations
        while (penetrationsRemaining > 0 && 
               Physics.Raycast(currentOrigin, rayDirection, out hit, range, hitLayers))
        {
            // Add hit point to our trail
            hitPoints.Add(hit.point);
            lastHitPoint = hit.point;
        
            // Process the hit
            bool isEnemy = hit.collider.CompareTag("Enemy");
            HealthSystem health = hit.collider.GetComponent<HealthSystem>();

            // Apply damage if it has health and isn't already dead
            if (health != null && !health.IsDead())
            {
                health.TakeDamage(currentDamage);
            
                // Award points for hitting a living enemy
                if (isEnemy && PointSystem.Instance != null)
                {
                    PointSystem.Instance.EnemyHit();
                }
            }

            // Spawn hit effect at impact point
            if (hitEffect != null)
            {
                GameObject impact = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
    
                // Play impact sound at hit location using SoundManager with mixer support
                if (SoundManager.GetInstance() != null) 
                {
                    SoundManager.GetInstance().PlaySound3DWithMixer(bulletImpactSoundName, hit.point, 0.7f, "SFX");
                }
            }

            // Check if this object can be penetrated (typically only enemies)
            bool canPenetrate = enablePenetration && 
                               ((1 << hit.collider.gameObject.layer) & penetrableLayers) != 0;
        
            if (canPenetrate && penetrationsRemaining > 1)
            {
                // Reduce damage for subsequent hits
                currentDamage *= (1f - damageReductionPerHit);
            
                // Move the ray origin slightly past the hit point to prevent hitting the same object
                currentOrigin = hit.point + rayDirection * 0.1f;
            
                // Reduce penetrations remaining
                penetrationsRemaining--;
            }
            else
            {
                // Can't penetrate further
                penetrationsRemaining = 0;
            }
        }

        // If we didn't hit anything, add the maximum range point for the trail
        if (hitPoints.Count == 1)
        {
            hitPoints.Add(rayOrigin + rayDirection * range);
        }

        // Draw bullet trail from gun barrel to final impact point
        if (bulletTrail != null)
        {
            StartCoroutine(ShowBulletTrail(hitPoints));
        }
    }
    
    /// <summary>
    /// Coroutine that displays bullet trails between all hit points for the specified duration
    /// </summary>
    /// <param name="points">List of points in the bullet's path</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    IEnumerator ShowBulletTrail(List<Vector3> points)
    {
        if (bulletTrail != null)
        {
            bulletTrail.enabled = true;
            bulletTrail.positionCount = points.Count;
        
            for (int i = 0; i < points.Count; i++)
            {
                bulletTrail.SetPosition(i, points[i]);
            }
        
            yield return new WaitForSeconds(trailDuration);
        
            bulletTrail.enabled = false;
        }
    }

    /// <summary>
    /// Adds ammo to the weapon's reserve supply
    /// </summary>
    /// <param name="amount">Amount of ammo to add</param>
    /// <returns>The actual amount of ammo added</returns>
    public int AddAmmo(int amount)
    {
        // Store original amount to calculate how much was added
        int originalAmmo = currentTotalAmmo;
    
        // If amount is very large (int.MaxValue), restore to max
        if (amount >= int.MaxValue/2)
        {
            // Just set to max
            currentTotalAmmo = maxAmmo - magazineSize;
        }
        else
        {
            // Add the specified amount, but cap at maximum
            currentTotalAmmo = Mathf.Min(currentTotalAmmo + amount, maxAmmo - magazineSize);
        }
    
        // Calculate how much was actually added
        int ammoAdded = currentTotalAmmo - originalAmmo;
    
        return ammoAdded;
    }
    
    /// <summary>
    /// Applies recoil effect when firing the weapon
    /// </summary>
    void ApplyRecoil()
    {
        // Add vertical recoil (kick upward)
        targetRotation.x -= Random.Range(recoilAmount * 0.7f, recoilAmount);
        
        // Add a bit of horizontal recoil (slight random left/right shake)
        targetRotation.y += Random.Range(-recoilAmount * 0.3f, recoilAmount * 0.3f);
        
        // Clamp the maximum recoil
        targetRotation.x = Mathf.Clamp(targetRotation.x, -maxRecoilAngle, 0);
        targetRotation.y = Mathf.Clamp(targetRotation.y, -maxRecoilAngle, maxRecoilAngle);
    }
    
    /// <summary>
    /// Updates recoil recovery and applies current recoil to the camera
    /// </summary>
    void UpdateRecoil()
    {
        // Only proceed if we have a valid target
        if (recoilTarget == null) return;
        
        // Smoothly interpolate current rotation towards target rotation
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, recoilRecoverySpeed * Time.deltaTime);
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, recoilRotationSpeed * Time.deltaTime);
        
        // Apply rotation directly to the recoil transform's local rotation
        // Don't try to preserve existing angles - completely set the rotation based on recoil
        recoilTarget.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
    }

    /// <summary>
    /// Updates whether this weapon is currently the active weapon in the weapon manager
    /// </summary>
    private void UpdateActiveState()
    {
        if (weaponManager != null)
        {
            // Get the component's WeaponSlot through GunPositioner
            GunPositioner gunPositioner = GetComponent<GunPositioner>();
            if (gunPositioner != null)
            {
                WeaponManager.WeaponSlot mySlot = gunPositioner.GetWeaponSlot();
                isActiveWeapon = (weaponManager.GetCurrentWeaponSlot() == mySlot);
            }
            else
            {
                // Fallback: check if this weapon is the current weapon object
                isActiveWeapon = (weaponManager.GetCurrentWeaponObject() == gameObject);
            }
        }
    }

    /// <summary>
    /// Plays weapon sounds as a child of the player for better spatial audio while moving
    /// </summary>
    /// <param name="soundName">Name of the sound in SoundLibrary</param>
    /// <param name="volume">Volume level (default 1.0)</param>
    private void PlayWeaponSound(string soundName, float volume = 1.0f, string mixerGroup = "SFX")
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
    /// Gets the current fire mode as a string for UI display
    /// </summary>
    /// <returns>String representation of current fire mode</returns>
    public string GetCurrentFireMode()
    {
        return currentFireMode.ToString();
    }
    
    /// <summary>
    /// Gets the current ammunition in the loaded magazine
    /// </summary>
    /// <returns>Number of rounds in current magazine</returns>
    public int GetCurrentAmmoInMag()
    {
        return currentAmmoInMag;
    }
    
    /// <summary>
    /// Gets the current total reserve ammunition
    /// </summary>
    /// <returns>Number of remaining bullets in reserve</returns>
    public int GetCurrentTotalAmmo()
    {
        return currentTotalAmmo;
    }
    
    /// <summary>
    /// Checks if the weapon is currently reloading
    /// </summary>
    /// <returns>True if currently in reload animation, false otherwise</returns>
    public bool IsReloading()
    {
        return isReloading;
    }

    /// <summary>
    /// Gets the maximum ammunition this weapon can hold
    /// </summary>
    /// <returns>Maximum total ammo capacity</returns>
    public int GetMaxAmmo()
    {
        return maxAmmo;
    }

    /// <summary>
    /// Gets the magazine capacity of this weapon
    /// </summary>
    /// <returns>Number of rounds in a full magazine</returns>
    public int GetMagazineSize()
    {
        return magazineSize;
    }

    /// <summary>
    /// Gets the weapon's rate of fire in rounds per minute
    /// </summary>
    /// <returns>Fire rate in rounds per minute</returns>
    public int GetRoundsPerMinute()
    {
        return roundsPerMinute;
    }
    
    /// <summary>
    /// Sets up the animator component if not already assigned
    /// </summary>
    private void SetupAnimator()
    {
        // If animator not assigned, try to find one
        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponent<Animator>();
            
            // If still not found, try to find on child objects
            if (weaponAnimator == null)
            {
                weaponAnimator = GetComponentInChildren<Animator>();
            }
        }
        
        // If we have an animator but no controller assigned and we have a reload controller, assign it
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController == null && reloadAnimController != null)
        {
            weaponAnimator.runtimeAnimatorController = reloadAnimController;
        }
        
        // If using animation clip directly without animator controller
        if (weaponAnimator != null && reloadAnimClip != null && weaponAnimator.runtimeAnimatorController == null)
        {
            // Create a simple controller at runtime
            AnimatorOverrideController overrideController = new AnimatorOverrideController();
            
            // We need a base controller to override
            // In this case, you would need a simple base controller in your project
            // You could also create one programmatically but that's more complex
            
            // Just log warning for now if no controller assigned
            Debug.LogWarning("Direct animation clip assignment requires a base RuntimeAnimatorController");
        }
    }
    
    /// <summary>
    /// Gets the current reload animation clip
    /// </summary>
    /// <returns>Current reload animation clip or null if not assigned</returns>
    public AnimationClip GetReloadAnimClip()
    {
        return reloadAnimClip;
    }
    
    /// <summary>
    /// Sets a new reload animation clip
    /// </summary>
    /// <param name="newClip">New animation clip to use for reloading</param>
    public void SetReloadAnimClip(AnimationClip newClip)
    {
        if (newClip != null)
        {
            reloadAnimClip = newClip;
            
            // Update reload time if we're matching to animation
            if (matchReloadTimeToAnimation)
            {
                reloadTime = newClip.length;
            }
        }
    }

    /// <summary>
    /// Creates and plays a muzzle flash particle effect at the weapon's fire point
    /// </summary>
    private void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            // Option 1: Play the attached particle system directly if it's assigned
            muzzleFlash.Play();
        }
        else
        {
            // Option 2: Instantiate a new particle system at the fire point
            GameObject muzzleFlashPrefab = Resources.Load<GameObject>("Effects/MuzzleFlash");
            if (muzzleFlashPrefab != null)
            {
                // Instantiate at the fire point position and rotation
                GameObject muzzleFlashInstance = Instantiate(
                    muzzleFlashPrefab, 
                    firePoint.position, 
                    firePoint.rotation
                );
                
                // Get the particle system from the instantiated object
                ParticleSystem particleSystem = muzzleFlashInstance.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    // Configure the particle system if needed
                    var main = particleSystem.main;
                    main.stopAction = ParticleSystemStopAction.Destroy; // Auto-destroy when done
                    
                    // Start playing the effect
                    particleSystem.Play();
                    
                    // Alternatively, destroy after fixed time (useful if stop action doesn't work)
                    float duration = main.duration + main.startLifetime.constantMax;
                    Destroy(muzzleFlashInstance, duration);
                }
                else
                {
                    // If no particle system found, destroy after a fixed time
                    Destroy(muzzleFlashInstance, 2f);
                }
            }
        }
    }
}
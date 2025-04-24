using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shoot : MonoBehaviour
{
    [Header("Shooting Properties")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float range = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private int roundsPerMinute = 600; // Changed from fireRate to roundsPerMinute
    [SerializeField] private LayerMask hitLayers;
    
    [Header("Fire Mode Settings")]
    [SerializeField] private FireMode defaultFireMode = FireMode.SemiAuto;
    [SerializeField] private bool canToggleFireMode = true;  // Enable/disable fire mode toggling
    [SerializeField] private KeyCode toggleFireModeKey = KeyCode.B;
    
    [Header("Ammunition Settings")]
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int maxAmmo = 150;
    [SerializeField] private float reloadTime = 2.0f;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    [SerializeField] private bool autoReloadWhenEmpty = true;
    
    [Header("Effects")]
    [SerializeField] private AudioSource shootSound;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private LineRenderer bulletTrail;
    [SerializeField] private float trailDuration = 0.05f;
    [SerializeField] private AudioSource fireModeToggleSound; // Optional sound for mode switching
    [SerializeField] private AudioSource reloadSound; // Optional sound for reloading
    [SerializeField] private AudioSource emptyClipSound; // Sound when trying to shoot with empty magazine
    
    [Header("Recoil Settings")]
    [SerializeField] private float recoilAmount = 1.0f;     // Maximum recoil angle in degrees
    [SerializeField] private float recoilRecoverySpeed = 5.0f;  // How quickly recoil recovers
    [SerializeField] private float recoilRotationSpeed = 10.0f; // How quickly recoil is applied
    [SerializeField] private float maxRecoilAngle = 3.0f;   // Maximum accumulated recoil
    [SerializeField] private Transform recoilTarget; // Assign this in inspector to camera or a parent object

    [Header("Penetration Settings")]
    [SerializeField] private bool enablePenetration = true;
    [SerializeField] private int maxPenetration = 3; // Maximum number of enemies a bullet can penetrate
    [SerializeField] private float damageReductionPerHit = 0.3f; // Damage reduction after each penetration (30%)
    [SerializeField] private LayerMask penetrableLayers; // Layers that can be penetrated (typically enemies)
    
    // Define fire modes
    public enum FireMode
    {
        SemiAuto,
        FullAuto
    }
    
    private float nextFireTime = 0f;
    private float fireRate; // Keep this as an internal variable for calculations
    private Camera playerCamera;
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private FireMode currentFireMode;
    
    // Ammo variables
    private int currentAmmoInMag;
    private int currentTotalAmmo;
    private bool isReloading = false;

    // Add new field to track if this weapon is active
    private bool isActiveWeapon = false;
    private WeaponManager weaponManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    }
    
    // Added method to convert RPM to fire rate in seconds
    private void UpdateFireRateFromRPM()
    {
        // Calculate seconds per round from rounds per minute
        // RPM / 60 = Rounds Per Second, 1 / RPS = Seconds Per Round
        if (roundsPerMinute <= 0)
            roundsPerMinute = 1; // Prevent division by zero
            
        fireRate = 60f / roundsPerMinute;
    }
    
    // Method to set RPM at runtime (for weapon upgrades, attachments, etc.)
    public void SetRoundsPerMinute(int rpm)
    {
        roundsPerMinute = Mathf.Max(1, rpm); // Ensure RPM is at least 1
        UpdateFireRateFromRPM();
    }
    
    // Update is called once per frame
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
    
    void TryToFire()
    {
        // Check if we have ammo
        if (currentAmmoInMag <= 0)
        {
            // Play empty clip sound
            if (emptyClipSound != null)
            {
                emptyClipSound.Play();
            }
            
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
    
    IEnumerator Reload()
    {
        // Start reload sequence
        isReloading = true;
        
        // Play reload sound if available
        if (reloadSound != null)
        {
            reloadSound.Play();
        }
        
        // Wait for reload time
        yield return new WaitForSeconds(reloadTime);
        
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
    
    void ToggleFireMode()
    {
        // Switch between fire modes
        currentFireMode = (currentFireMode == FireMode.SemiAuto) ? 
            FireMode.FullAuto : FireMode.SemiAuto;
        
        // Play toggle sound if assigned
        if (fireModeToggleSound != null)
        {
            fireModeToggleSound.Play();
        }
        
        // You could add UI feedback here, like displaying the current fire mode on screen
    }
    
    void FireHitscan()
    {
        // Set the next time we can fire
        nextFireTime = Time.time + fireRate;

        // Play effects
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (shootSound != null)
        {
            shootSound.Play();
        }

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
            // Add hit point to our trail - Fix: 'add' to 'Add' (proper capitalization)
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

    // Add ammo to the weapon's reserve
    // Returns the amount of ammo actually added
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

    // Update whether this weapon is currently active
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
    
    // For UI display or debugging
    public string GetCurrentFireMode()
    {
        return currentFireMode.ToString();
    }
    
    // Public getters for UI display
    public int GetCurrentAmmoInMag()
    {
        return currentAmmoInMag;
    }
    
    public int GetCurrentTotalAmmo()
    {
        return currentTotalAmmo;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }

    // Get the maximum ammo this weapon can hold
    public int GetMaxAmmo()
    {
        return maxAmmo;
    }

    // Add a new public getter for magazine size
    public int GetMagazineSize()
    {
        return magazineSize;
    }

    // Get rounds per minute for UI display
    public int GetRoundsPerMinute()
    {
        return roundsPerMinute;
    }
}
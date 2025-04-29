using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages player health and Armour visualization and gameplay mechanics
/// </summary>
public class HealthArmourSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float _baseMaxHealth = 100f;
    [SerializeField] private float _currentHealth;
    
    [Header("Health UI")]
    [SerializeField] private TextMeshProUGUI _currentHealthText;  // Text display for current health
    [SerializeField] private TextMeshProUGUI _maxHealthText;      // Text display for maximum health
    [SerializeField] private string _healthNumberFormat = "0";    // Format for health numbers (0 = no decimals)
    
    [Header("Armour Settings")]
    [SerializeField] private List<Image> _ArmourPlateImages = new List<Image>(3); // 3 Armour plate UI elements
    [SerializeField] private float _maxArmourPlateHealth = 50f;                  // Health per Armour plate
    [SerializeField] private float _ArmourDamageReduction = 0.15f;               // 15% damage reduction when wearing Armour
    [SerializeField] private float _ArmourDamageSplit = 0.50f;                   // 50% of damage goes to Armour
    [SerializeField] private Color _fullArmourColor = Color.white;               // Color for full Armour plates
    [SerializeField] private Color _lowArmourColor = Color.red;                  // Color for nearly broken Armour plates
    [SerializeField] private Color _emptyArmourColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Color for empty armor plates
    
    [Header("UI Settings")]
    [SerializeField] private float _ArmourSlideAnimTime = 0.5f;                 // How quickly Armour plates visually deplete
    [SerializeField] private float _healthCounterAnimTime = 0.5f;               // How quickly health counter animates
    [SerializeField] private bool _showEmptyArmourPlates = true;                // Whether to show empty armor plate outlines
    
    [Header("Armour Plate Animation")]
    [SerializeField] private float _plateSlideAnimTime = 0.4f;                  // How long the slide animation takes
    
    [Header("Death Sequence")]
    [SerializeField] private bool _isPlayerCharacter = false;        // Is this the main player?
    [SerializeField] private float _returnToMenuDelay = 7f;          // How long before returning to main menu
    [SerializeField] private string _mainMenuSceneName = "MainMenu"; // Scene name to load
    [SerializeField] private GameObject _weaponHolder;               // Reference to the weapons container

    [Header("Death Camera Settings")]
    [SerializeField] private Transform _deathCameraTarget; // Optional target for camera to orbit
    [SerializeField] private float _deathCameraRotationSpeed = 15f; // How fast the camera rotates
    [SerializeField] private float _deathCameraDistance = 3.5f; // Distance from center point
    [SerializeField] private float _deathCameraHeightOffset = 1f; // Height offset
    [SerializeField] private Vector3 _deathCameraOffset = new Vector3(0f, 1f, 0f); // Offset from player position
    
    // Internal variables
    private float[] _ArmourPlateHealths = new float[3];
    private int _activeArmourPlates = 0;
    private Vector2[] _originalPlatePositions = new Vector2[3];                 // Store original positions for reset
    private int _displayedCurrentHealth;
    private int _displayedMaxHealth;
    private bool _isDead = false;
    
    // Properties
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _baseMaxHealth;
    public int ArmourPlatesRemaining => _activeArmourPlates;
    public float CurrentTotalArmour => GetTotalRemainingArmour();

    /// <summary>
    /// Initializes health and Armour system on startup
    /// </summary>
    private void Awake()
    {
        // Initialize health
        _currentHealth = _baseMaxHealth;
        _displayedCurrentHealth = Mathf.RoundToInt(_currentHealth);
        _displayedMaxHealth = Mathf.RoundToInt(_baseMaxHealth);
        UpdateHealthDisplay(true); // Force immediate update
        
        // Initialize Armour plates to empty
        InitializeArmourPlates();
    }

    // Add this to the Update method, or create it if it doesn't exist
    private void Update()
    {
        // Check for death in case health was set to 0 by another means
        if (_currentHealth <= 0 && !_isDead)
        {
            HandleDeath();
        }
        
        // For testing only - press K to trigger instant death
        if (Input.GetKeyDown(KeyCode.K))
        {
            _currentHealth = 0;  // Simply set health to 0 to trigger death check above
        }
    }

    /// <summary>
    /// Initialize the Armour plate UI elements
    /// </summary>
    private void InitializeArmourPlates()
    {
        _activeArmourPlates = 0;
        
        for (int i = 0; i < 3; i++)
        {
            // Reset internal tracking
            _ArmourPlateHealths[i] = 0f;
            
            // Make sure we have valid UI references
            if (i < _ArmourPlateImages.Count && _ArmourPlateImages[i] != null)
            {
                // Store the original position for reset
                _originalPlatePositions[i] = _ArmourPlateImages[i].rectTransform.anchoredPosition;
                
                // Set initial fill and color immediately (no animation for initialization)
                if (_showEmptyArmourPlates)
                {
                    // Show empty plate outlines
                    _ArmourPlateImages[i].fillAmount = 1f;
                    _ArmourPlateImages[i].color = _emptyArmourColor;
                }
                else
                {
                    // Hide empty plates completely
                    _ArmourPlateImages[i].fillAmount = 0f;
                    _ArmourPlateImages[i].color = _fullArmourColor;
                }
            }
        }
    }

    /// <summary>
    /// Processes damage to the player, including Armour calculations
    /// </summary>
    /// <param name="incomingDamage">Base damage amount before reductions</param>
    public void TakeDamage(float incomingDamage)
    {   
        // Don't process damage if already dead
        if (_isDead) 
        {
            return;
        }
        
        float effectiveDamage = incomingDamage;
        float healthDamage = incomingDamage;
        
        // Apply Armour damage reduction and splitting if player has Armour
        if (_activeArmourPlates > 0)
        {
            // Reduce total damage due to wearing Armour
            effectiveDamage *= (1f - _ArmourDamageReduction);
            
            // Calculate portion of damage going to Armour vs health
            float ArmourDamage = effectiveDamage * _ArmourDamageSplit;
            healthDamage = effectiveDamage * (1f - _ArmourDamageSplit);
            
            // Apply damage to Armour plates
            DamageArmourPlates(ArmourDamage);
        }
        
        // Apply damage to health
        _currentHealth = Mathf.Max(0f, _currentHealth - healthDamage);
        
        // Update UI displays
        UpdateHealthDisplay();
        
        // Check for death
        if (_currentHealth <= 0 && !_isDead)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Handles player death sequence
    /// </summary>
    private void HandleDeath()
    {
        // Only process death once
        if (_isDead)
        {
            return;
        }
        
        // Set isDead flag first thing to prevent multiple death sequences
        _isDead = true;
        
        // Ensure health is exactly zero
        if (_currentHealth > 0)
        {
            _currentHealth = 0;
            UpdateHealthDisplay(true);  // Force immediate UI update
        }
        
        // Only proceed with special death sequence if this is the player character
        if (!_isPlayerCharacter)
        {
            return;
        }
        
        // Set up camera rotation around player
        SetupDeathCamera();
        
        // Unlock the mouse cursor so player can interact with the UI
        UnlockMouseCursor();
        
        // Disable player controls (this also disables HUD and weapons)
        DisablePlayerControls();
        
        // Force the tab menu to open
        ForceTabMenuOpen();
        
        // Set up return to main menu
        Invoke("ReturnToMainMenu", _returnToMenuDelay);
    }

    /// <summary>
    /// Sets up the death camera to rotate around a configurable point
    /// </summary>
    private void SetupDeathCamera()
    {
        // Find main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }
        
        // First, disable the MouseLook script
        MouseLook mouseLookScript = mainCamera.GetComponentInParent<MouseLook>();
        if (mouseLookScript != null)
        {
            mouseLookScript.enabled = false;
        }
        else
        {
            // Try finding it on the player if not on camera
            mouseLookScript = GetComponentInParent<MouseLook>();
            if (mouseLookScript != null)
            {
                mouseLookScript.enabled = false;
            }
            else
            {
                // Try one more approach - find it anywhere in the scene using the newer API
                mouseLookScript = FindAnyObjectByType<MouseLook>();
                if (mouseLookScript != null)
                {
                    mouseLookScript.enabled = false;
                }
            }
        }

        // Ensure we disable all look scripts
        DisableLookScripts();

        // Check if we already have a CameraRotate component on the camera
        CameraRotate cameraRotate = mainCamera.GetComponent<CameraRotate>();
        if (cameraRotate == null)
        {
            // Add CameraRotate script if not present
            cameraRotate = mainCamera.gameObject.AddComponent<CameraRotate>();
        }
        
        // Determine the target for camera rotation
        Transform deathTarget;
        
        // If a custom target is specified in the inspector, use that
        if (_deathCameraTarget != null)
        {
            deathTarget = _deathCameraTarget;
        }
        else
        {
            // Otherwise create a new target at an offset from the player
            GameObject targetObj = new GameObject("DeathCameraTarget");
            targetObj.transform.position = transform.position + _deathCameraOffset;
            deathTarget = targetObj.transform;
        }
        
        // Configure the camera rotation
        cameraRotate.ConfigureRotation(
            deathTarget,                 // Target to orbit around
            _deathCameraRotationSpeed,   // Rotation speed
            _deathCameraDistance,        // Distance from target
            _deathCameraHeightOffset,    // Height offset
            true                         // Start rotating immediately
        );
    }
    
    /// <summary>
    /// Specifically searches for and disables all look scripts
    /// </summary>
    private void DisableLookScripts()
    {
        // Get all MouseLook scripts in the scene and disable them
        MouseLook[] allMouseLooks = FindObjectsByType<MouseLook>(FindObjectsSortMode.None);
        foreach (MouseLook look in allMouseLooks)
        {
            if (look != null && look.enabled)
            {
                look.enabled = false;
            }
        }
        
        // Look for scripts with "look" in the name as backup
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != null && script.enabled && 
                (script.GetType().Name.ToLower().Contains("look") || 
                 script.GetType().Name.ToLower().Contains("camera")) &&
                !(script is CameraRotate)) // Don't disable our orbit script
            {
                script.enabled = false;
            }
        }
    }

    /// <summary>
    /// Adds health to the player, not exceeding maximum
    /// </summary>
    /// <param name="amount">Amount of health to add</param>
    /// <returns>Amount of health actually added</returns>
    public float AddHealth(float amount)
    {
        float originalHealth = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, _baseMaxHealth);
        UpdateHealthDisplay();
        
        // Return how much health was actually added
        return _currentHealth - originalHealth;
    }

    /// <summary>
    /// Increases the player's maximum health
    /// </summary>
    /// <param name="amount">Amount to increase max health by</param>
    /// <param name="healToNewMax">If true, also increases current health by the same amount</param>
    public void IncreaseMaxHealth(float amount, bool healToNewMax = false)
    {
        _baseMaxHealth += amount;
        
        // Optionally, also heal the player by the same amount
        if (healToNewMax)
        {
            _currentHealth += amount;
        }
        
        UpdateHealthDisplay();
    }

    /// <summary>
    /// Adds an Armour plate to the player
    /// </summary>
    /// <returns>True if successful, false if already at max plates</returns>
    public bool AddArmourPlate()
    {
        // Find the first empty plate slot
        for (int i = 0; i < 3; i++)
        {
            if (_ArmourPlateHealths[i] <= 0)
            {
                // Add a new full plate
                _ArmourPlateHealths[i] = _maxArmourPlateHealth;
                _activeArmourPlates++;
                
                // Animate the plate sliding in from right
                AnimatePlateSlide(i, true);
                
                // Update UI
                UpdateArmourPlateUI(i);
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Repairs a damaged armor plate to full health
    /// </summary>
    /// <returns>True if successful, false if no damaged plate was found</returns>
    public bool RepairArmourPlate()
    {
        // First check if we have any damaged plates (not empty, not full)
        for (int i = 0; i < 3; i++)
        {
            // Look for plates that exist but are damaged
            if (_ArmourPlateHealths[i] > 0 && _ArmourPlateHealths[i] < _maxArmourPlateHealth)
            {
                float previousHealth = _ArmourPlateHealths[i];
                
                // Repair to full health
                _ArmourPlateHealths[i] = _maxArmourPlateHealth;
                
                // Animate the plate repair
                AnimatePlateSlide(i, true, 0.5f);
                
                // Update UI
                UpdateArmourPlateUI(i);

                return true;
            }
        }
        
        // No damaged plates found, try adding a new one instead
        return AddArmourPlate();
    }
    
    /// <summary>
    /// Gets the total remaining Armour across all plates
    /// </summary>
    private float GetTotalRemainingArmour()
    {
        float total = 0f;
        for (int i = 0; i < 3; i++)
        {
            total += _ArmourPlateHealths[i];
        }
        return total;
    }
    
    /// <summary>
    /// Gets the current value of a specific Armour plate
    /// </summary>
    /// <param name="plateIndex">Index of the Armour plate (0-2)</param>
    public float GetArmourPlateHealth(int plateIndex)
    {
        if (plateIndex >= 0 && plateIndex < 3)
        {
            return _ArmourPlateHealths[plateIndex];
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the maximum health of a single Armour plate
    /// </summary>
    public float GetMaxArmourPlateHealth()
    {
        return _maxArmourPlateHealth;
    }

    /// <summary>
    /// Handles armor purchase or repair
    /// </summary>
    /// <param name="cost">Money cost for the operation</param>
    /// <param name="playerMoney">Reference to player's current money</param>
    /// <returns>True if purchase was successful, false if not enough money or already at max armor</returns>
    public bool PurchaseOrRepairArmour(int cost, ref int playerMoney)
    {
        // Check if player has enough money
        if (playerMoney < cost)
        {
            return false;
        }
        
        // Try to repair any damaged plates first, or add a new one if none are damaged
        bool success = RepairArmourPlate();
        
        // If successful, deduct cost
        if (success)
        {
            playerMoney -= cost;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Forces the tab menu to open on death
    /// </summary>
    private void ForceTabMenuOpen()
    {
        // Try to find the tab menu component using the newer API
        TabMenu tabMenu = FindAnyObjectByType<TabMenu>();
        if (tabMenu != null)
        {
            // Call the appropriate method to open the tab menu
            tabMenu.ForceMenuOpen();
        }
        else
        {
            // Alternative approach: find a GameObject with TabMenu in the name
            GameObject tabMenuObject = GameObject.Find("TabMenu");
            if (tabMenuObject != null)
            {
                tabMenuObject.SetActive(true);
            }
            else
            {
                // Try one more approach - find any UI element that might be the tab menu
                // Using the newer FindObjectsByType API
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (Canvas canvas in canvases)
                {
                    if (canvas.gameObject.name.Contains("Tab") || 
                        canvas.gameObject.name.Contains("Menu") ||
                        canvas.gameObject.name.Contains("Pause"))
                    {
                        canvas.gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns to the main menu after death
    /// </summary>
    private void ReturnToMainMenu()
    {
        // Store information that we're coming from a death sequence
        PlayerPrefs.SetInt("ComingFromDeath", 1);
        PlayerPrefs.Save();
        
        // Find and destroy all objects that might be marked with DontDestroyOnLoad
        DestroyPersistentObjects();
        
        // Load the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
    }
    
    /// <summary>
    /// Destroys all objects that might be marked with DontDestroyOnLoad
    /// </summary>
    private void DestroyPersistentObjects()
    {
        // 1. Find and destroy the player root object
        GameObject playerObject = this.gameObject;
        Transform playerRoot = playerObject.transform;
        
        // Find the actual root parent that might have DontDestroyOnLoad
        while (playerRoot.parent != null)
        {
            playerRoot = playerRoot.parent;
        }
        
        // Log the player object we found
        GameObject playerRootObject = playerRoot.gameObject;
        
        // 2. Find and handle the main camera separately
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Transform cameraRoot = mainCamera.transform;
            
            // Find the root of the camera hierarchy
            while (cameraRoot.parent != null && cameraRoot.parent != playerRoot)
            {
                cameraRoot = cameraRoot.parent;
            }
            
            // If the camera is not a child of the player (which would be destroyed with the player)
            if (cameraRoot.parent != playerRoot)
            {
                GameObject cameraRootObject = cameraRoot.gameObject;
                
                // Destroy the camera root object if it's not part of the player
                if (cameraRootObject != playerRootObject)
                {
                    Destroy(cameraRootObject);
                }
            }
        }
        
        // 3. Check for any objects with "_DontDestroy" in their name or specific DontDestroy tag
        GameObject[] allRootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in allRootObjects)
        {
            // Check if this is a persistent object by name or tag
            if (obj.name.Contains("DontDestroy") || obj.CompareTag("DontDestroy") || 
                obj.name.Contains("Persistent") || obj.name.Contains("Player"))
            {
                // Skip if it's the player object we already found
                if (obj != playerRootObject)
                {
                    Destroy(obj);
                }
            }
        }
        
        // 4. Finally destroy the player root object
        Destroy(playerRootObject);
    }

    /// <summary>
    /// Disables player controls and related systems upon death
    /// </summary>
    private void DisablePlayerControls()
    {
        // Disable weapon holder if referenced
        if (_weaponHolder != null)
        {
            _weaponHolder.SetActive(false);
        }
        
        // Find and disable player input system
        PlayerInput playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
        
        // Find and disable character controller
        CharacterController characterController = GetComponentInParent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Find and disable first-person movement script
        MonoBehaviour[] scripts = GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && (
                script.GetType().Name.Contains("Movement") ||
                script.GetType().Name.Contains("Controller") ||
                script.GetType().Name.Contains("PlayerControls") ||
                script.GetType().Name.Contains("FirstPerson")))
            {
                script.enabled = false;
            }
        }
        
        // Disable any HUD elements that should be hidden during death
        // This assumes there might be a HUD manager or canvas we can find
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && (
                canvas.name.Contains("HUD") ||
                canvas.name.Contains("Crosshair") ||
                canvas.name.Contains("Weapon") ||
                canvas.name.Contains("Ammo")))
            {
                canvas.gameObject.SetActive(false);
            }
        }
        
        // Try to find and disable any action maps related to gameplay
        PlayerInput[] allPlayerInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (PlayerInput input in allPlayerInputs)
        {
            if (input != null)
            {
                try
                {
                    // Try to disable the gameplay action map (common naming pattern)
                    input.DeactivateInput();
                }
                catch (System.Exception)
                {
                    // Ignore exceptions here
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the health display UI with current values
    /// </summary>
    /// <param name="immediate">If true, updates instantly without animation</param>
    private void UpdateHealthDisplay(bool immediate = false)
    {
        // Target values from current state
        int targetCurrentHealth = Mathf.RoundToInt(_currentHealth);
        int targetMaxHealth = Mathf.RoundToInt(_baseMaxHealth);
        
        if (immediate)
        {
            // Immediate update without animation
            _displayedCurrentHealth = targetCurrentHealth;
            _displayedMaxHealth = targetMaxHealth;
            
            // Update UI text if available
            if (_currentHealthText != null)
            {
                _currentHealthText.text = targetCurrentHealth.ToString(_healthNumberFormat);
            }
            
            if (_maxHealthText != null)
            {
                _maxHealthText.text = targetMaxHealth.ToString(_healthNumberFormat);
            }
            
            return;
        }
        
        // Animate current health value changing
        if (_currentHealthText != null)
        {
            DOTween.Kill(_currentHealthText);
            DOTween.To(() => _displayedCurrentHealth, x => {
                _displayedCurrentHealth = x;
                _currentHealthText.text = Mathf.RoundToInt(x).ToString(_healthNumberFormat);
            }, targetCurrentHealth, _healthCounterAnimTime);
        }
        
        // Animate max health value changing (usually only happens with upgrades)
        if (_maxHealthText != null && _displayedMaxHealth != targetMaxHealth)
        {
            DOTween.Kill(_maxHealthText);
            DOTween.To(() => _displayedMaxHealth, x => {
                _displayedMaxHealth = x;
                _maxHealthText.text = Mathf.RoundToInt(x).ToString(_healthNumberFormat);
            }, targetMaxHealth, _healthCounterAnimTime);
        }
    }
    
    /// <summary>
    /// Applies damage to armor plates, starting with rightmost plate
    /// </summary>
    /// <param name="damageAmount">Amount of damage to apply to armor</param>
    private void DamageArmourPlates(float damageAmount)
    {
        float remainingDamage = damageAmount;
        
        // Start with the highest index (rightmost) plate and work backwards
        for (int i = 2; i >= 0 && remainingDamage > 0; i--)
        {
            // Skip empty plates
            if (_ArmourPlateHealths[i] <= 0) 
                continue;
            
            // Calculate damage to this plate
            float damageTaken = Mathf.Min(_ArmourPlateHealths[i], remainingDamage);
            _ArmourPlateHealths[i] -= damageTaken;
            remainingDamage -= damageTaken;
            
            // Update UI for this plate
            UpdateArmourPlateUI(i);
            
            // If plate was destroyed, reduce active count
            if (_ArmourPlateHealths[i] <= 0)
            {
                _activeArmourPlates--;
                
                // Animate plate sliding away
                AnimatePlateSlide(i, false);
            }
        }
    }
    
    /// <summary>
    /// Updates the visual representation of an armor plate based on its current health
    /// </summary>
    /// <param name="plateIndex">Index of the plate to update (0-2)</param>
    private void UpdateArmourPlateUI(int plateIndex)
    {
        if (plateIndex < 0 || plateIndex >= _ArmourPlateImages.Count || _ArmourPlateImages[plateIndex] == null)
            return;
            
        Image plateImage = _ArmourPlateImages[plateIndex];
        float plateHealth = _ArmourPlateHealths[plateIndex];
        float healthPercent = plateHealth / _maxArmourPlateHealth;
        
        // Determine if this plate is empty, low, or normal
        if (plateHealth <= 0)
        {
            if (_showEmptyArmourPlates)
            {
                // Show as empty outline
                DOTween.To(() => plateImage.fillAmount, x => plateImage.fillAmount = x, 1f, _ArmourSlideAnimTime);
                DOTween.To(() => plateImage.color, x => plateImage.color = x, _emptyArmourColor, _ArmourSlideAnimTime);
            }
            else
            {
                // Hide completely
                DOTween.To(() => plateImage.fillAmount, x => plateImage.fillAmount = x, 0f, _ArmourSlideAnimTime);
            }
        }
        else
        {
            // Animate the fill amount to match current health
            DOTween.To(() => plateImage.fillAmount, x => plateImage.fillAmount = x, healthPercent, _ArmourSlideAnimTime);
            
            // Set color based on health percentage
            Color targetColor = Color.Lerp(_lowArmourColor, _fullArmourColor, healthPercent);
            DOTween.To(() => plateImage.color, x => plateImage.color = x, targetColor, _ArmourSlideAnimTime);
        }
    }
    
    /// <summary>
    /// Animates an armor plate sliding in or out
    /// </summary>
    /// <param name="plateIndex">Index of the plate to animate</param>
    /// <param name="isAddingPlate">True if adding plate, false if removing</param>
    /// <param name="delay">Optional delay before animation starts</param>
    private void AnimatePlateSlide(int plateIndex, bool isAddingPlate, float delay = 0f)
    {
        // Make sure we have a valid plate at this index
        if (plateIndex < 0 || plateIndex >= _ArmourPlateImages.Count || _ArmourPlateImages[plateIndex] == null)
            return;
            
        Image plateImage = _ArmourPlateImages[plateIndex];
        RectTransform rectTransform = plateImage.rectTransform;
        
        // Get original and offset positions
        Vector2 originalPos = _originalPlatePositions[plateIndex];
        Vector2 offsetPos = originalPos + new Vector2(100f, 0); // 100 units to the right
        
        if (isAddingPlate)
        {
            // Reset to starting position (off-screen)
            rectTransform.anchoredPosition = offsetPos;
            
            // Set initial state
            if (_showEmptyArmourPlates)
            {
                plateImage.fillAmount = 1.0f;
                plateImage.color = _fullArmourColor;
            }
            else
            {
                plateImage.fillAmount = _ArmourPlateHealths[plateIndex] / _maxArmourPlateHealth;
                plateImage.color = _fullArmourColor;
            }
            
            // Animate sliding in
            rectTransform.DOAnchorPos(originalPos, _plateSlideAnimTime).SetDelay(delay);
        }
        else
        {
            // Animate sliding out
            rectTransform.DOAnchorPos(offsetPos, _plateSlideAnimTime).SetDelay(delay);
        }
    }
    
    /// <summary>
    /// Unlocks the mouse cursor for menu interaction
    /// </summary>
    private void UnlockMouseCursor()
    {
        // Make the cursor visible and unlock it
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
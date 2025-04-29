using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the crosshair UI element with dynamic sizing and target detection
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform crosshairCanvas;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Camera playerCamera;

    [Header("Crosshair Settings")]
    [SerializeField] private float baseSize = 10f;
    [SerializeField] private float minSize = 10f;
    [SerializeField] private float maxSize = 50f;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color targetColor = Color.red;
    
    [Header("Dynamic Resizing")]
    [SerializeField] private bool dynamicCrosshair = true;
    [SerializeField] private float movementSizeMultiplier = 1.5f;
    [SerializeField] private float sizeSmoothTime = 0.1f;
    
    [Header("Target Detection")]
    [SerializeField] private bool showTargetColor = true;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private string enemyTag = "Enemy";

    // Private variables
    private float currentSize;
    private float sizeVelocity;
    private CharacterController playerController;
    private bool isTargetDetected = false;
    
    /// <summary>
    /// Initializes crosshair settings, references, and cursor state
    /// </summary>
    void Start()
    {
        // Initialize crosshair
        currentSize = baseSize;
        
        // Hide default cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // If no camera assigned, use main camera
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        // Try to find player controller to detect movement
        playerController = FindFirstObjectByType<CharacterController>();
        
        // If no crosshair canvas is assigned, use this GameObject's RectTransform
        if (crosshairCanvas == null)
            crosshairCanvas = GetComponent<RectTransform>();
            
        // If no crosshair image is assigned, try to find one in children
        if (crosshairImage == null)
            crosshairImage = GetComponentInChildren<Image>();
            
        if (crosshairImage == null)
            Debug.LogWarning("No crosshair image assigned or found in children!");
            
        // Set initial size
        UpdateCrosshairSize(baseSize);
    }

    /// <summary>
    /// Updates crosshair size and color each frame based on player movement and target detection
    /// </summary>
    void Update()
    {
        // Calculate target size based on player state
        float targetSize = CalculateTargetSize();
        
        // Smooth transition to target size
        if (dynamicCrosshair)
        {
            currentSize = Mathf.SmoothDamp(currentSize, targetSize, ref sizeVelocity, sizeSmoothTime);
            UpdateCrosshairSize(currentSize);
        }
        
        // Check for target under crosshair
        DetectTarget();
        
        // Update crosshair color based on target detection
        UpdateCrosshairColor();
    }
    
    /// <summary>
    /// Calculates the target crosshair size based on player movement
    /// </summary>
    /// <returns>The calculated target size clamped between minimum and maximum values</returns>
    private float CalculateTargetSize()
    {
        float targetSize = baseSize;
        
        // Adjust size based on movement if we have access to the player controller
        if (playerController != null && dynamicCrosshair)
        {
            // Get horizontal velocity (ignoring vertical movement)
            Vector3 horizontalVelocity = playerController.velocity;
            horizontalVelocity.y = 0;
            
            // Increase size based on speed
            float speedFactor = horizontalVelocity.magnitude / 5.0f; // Normalize by dividing by expected max speed
            targetSize += baseSize * speedFactor * movementSizeMultiplier;
        }
        
        // Clamp size between min and max
        return Mathf.Clamp(targetSize, minSize, maxSize);
    }
    
    /// <summary>
    /// Updates the visual size of the crosshair UI element
    /// </summary>
    /// <param name="size">The new size to apply to the crosshair</param>
    private void UpdateCrosshairSize(float size)
    {
        if (crosshairCanvas != null)
        {
            crosshairCanvas.sizeDelta = new Vector2(size, size);
        }
    }
    
    /// <summary>
    /// Checks if the player is looking at an enemy target
    /// Updates isTargetDetected flag based on raycast results
    /// </summary>
    private void DetectTarget()
    {
        isTargetDetected = false;
        
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, maxDistance, targetLayers))
            {
                // Check if the hit object is an enemy
                if (hit.collider.CompareTag(enemyTag))
                {
                    isTargetDetected = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Updates the crosshair color based on target detection status
    /// Changes to targetColor when looking at an enemy, otherwise uses defaultColor
    /// </summary>
    private void UpdateCrosshairColor()
    {
        if (crosshairImage != null && showTargetColor)
        {
            crosshairImage.color = isTargetDetected ? targetColor : defaultColor;
        }
    }
    
    /// <summary>
    /// Temporarily increases the crosshair size for visual feedback (e.g., when firing)
    /// </summary>
    /// <param name="amount">Amount to increase the crosshair size by</param>
    /// <param name="duration">Duration in seconds before returning to normal size</param>
    public void TemporarilyExpandCrosshair(float amount, float duration)
    {
        // This could be called when firing a weapon
        StartCoroutine(ExpandCrosshairRoutine(amount, duration));
    }
    
    /// <summary>
    /// Coroutine that handles temporary expansion of the crosshair
    /// </summary>
    /// <param name="amount">Amount to increase the crosshair size by</param>
    /// <param name="duration">Duration in seconds before returning to normal size</param>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ExpandCrosshairRoutine(float amount, float duration)
    {
        float originalBaseSize = baseSize;
        baseSize += amount;
        
        yield return new WaitForSeconds(duration);
        
        baseSize = originalBaseSize;
    }
    
    /// <summary>
    /// Gets whether the player is currently looking at an enemy target
    /// </summary>
    /// <returns>True if an enemy is detected in crosshair, false otherwise</returns>
    public bool IsTargetDetected()
    {
        return isTargetDetected;
    }
}

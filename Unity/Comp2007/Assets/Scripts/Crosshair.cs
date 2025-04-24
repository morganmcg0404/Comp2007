using UnityEngine;
using UnityEngine.UI;

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
    
    private void UpdateCrosshairSize(float size)
    {
        if (crosshairCanvas != null)
        {
            crosshairCanvas.sizeDelta = new Vector2(size, size);
        }
    }
    
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
    
    private void UpdateCrosshairColor()
    {
        if (crosshairImage != null && showTargetColor)
        {
            crosshairImage.color = isTargetDetected ? targetColor : defaultColor;
        }
    }
    
    // Public methods to be called by other scripts
    
    public void TemporarilyExpandCrosshair(float amount, float duration)
    {
        // This could be called when firing a weapon
        StartCoroutine(ExpandCrosshairRoutine(amount, duration));
    }
    
    private System.Collections.IEnumerator ExpandCrosshairRoutine(float amount, float duration)
    {
        float originalBaseSize = baseSize;
        baseSize += amount;
        
        yield return new WaitForSeconds(duration);
        
        baseSize = originalBaseSize;
    }
    
    // Helper to get if target is detected
    public bool IsTargetDetected()
    {
        return isTargetDetected;
    }
}

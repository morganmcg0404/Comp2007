using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic player movement controller for first person games with sliding
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Component References")]
    public CharacterController controller;
    public Transform cameraTransform; // Reference to camera to adjust height

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 2.5f;
    public float speedTransitionRate = 5f; // Controls how quickly speed changes
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Directional Speed Modifiers")]
    [Range(0.1f, 1.0f)] public float backwardSpeedMultiplier = 0.5f; // 50% speed when moving backward
    [Range(0.1f, 1.0f)] public float sidewaysSpeedMultiplier = 0.75f; // 75% speed when strafing

    [Header("Crouch Settings")]
    public float standingHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    public Vector3 standingCameraPosition = new Vector3(0, 0.8f, 0);
    public Vector3 crouchingCameraPosition = new Vector3(0, 0.4f, 0);

    [Header("Slide Settings")]
    public float slideForce = 5f; // Initial force of the slide
    public float slideDuration = 1.0f; // Maximum slide duration in seconds
    public float slideSpeedThreshold = 7f; // Minimum speed required to slide
    public float slideControlReduction = 0.5f; // How much control player has while sliding (0-1)
    public float slideCooldown = 2.0f; // Cooldown period between slides in seconds
    
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f; // Stamina recovered per second
    public float sprintStaminaDrain = 15f; // Stamina drained per second when sprinting
    public float slideStaminaCost = 20f; // Stamina cost per slide
    public float staminaRegenDelay = 1.5f; // Delay before stamina starts regenerating
    [SerializeField] private Image staminaBar; // Optional UI element
    [SerializeField] private Color normalStaminaColor = Color.green;
    [SerializeField] private Color lowStaminaColor = Color.red;
    [SerializeField] private float lowStaminaThreshold = 30f;

    [Header("Stamina UI Settings")]
    [SerializeField] private float staminaBarAnimDuration = 0.3f;
    [SerializeField] private Ease staminaBarEase = Ease.OutQuad;
    [SerializeField] private Image staminaBarBackground;
    private Coroutine backgroundBarCoroutine;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("DOTween Settings")]
    [SerializeField] private int initialTweenCapacity = 1500;
    [SerializeField] private int maxTweenCapacity = 2000;

    // Private variables
    private Vector3 velocity;
    private bool isGrounded;
    private float currentSpeed;
    private float targetSpeed;
    private bool isSprinting;
    private bool isCrouching;
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;
    private float targetHeight;
    private Vector3 targetCameraPosition;
    private float originalControllerY;
    private float lastSlideTime = -10f;
    private KeyCode sprintKey = KeyCode.LeftShift;
    private float currentDirectionModifier = 1.0f; // Add this line to fix the error
    
    // Stamina variables
    private float currentStamina;
    private float lastStaminaUseTime;
    private bool staminaDepleted = false;
    private float previousStaminaFill = 1f;
    private Tweener staminaBarTween;

    // Debug UI
    public bool showDebugUI = true;
    private GUIStyle debugTextStyle;

    // Speed modifier for ADS
    private float externalSpeedMultiplier = 1.0f;

    private void Awake()
    {
        // Initialize DOTween with higher capacity
        DOTween.SetTweensCapacity(initialTweenCapacity, maxTweenCapacity);
    }
    
    private void Start()
    {
        // Setup debug text style
        debugTextStyle = new GUIStyle();
        debugTextStyle.fontSize = 20;
        debugTextStyle.normal.textColor = Color.white;
        
        // Store original controller center Y
        originalControllerY = controller.center.y;
        
        // Initialize crouch variables
        targetHeight = standingHeight;
        targetCameraPosition = standingCameraPosition;
        
        // Initialize speed
        currentSpeed = walkSpeed;
        targetSpeed = walkSpeed;
        
        // Make sure controller is correct height at start
        controller.height = standingHeight;
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = standingCameraPosition;
        }
        
        // Initialize stamina
        currentStamina = maxStamina;
        previousStaminaFill = 1f;
        UpdateStaminaBar(false);
    }

    private void Update()
    {
        // Ground check - adjust position based on current controller height
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep player grounded
        }

        // Get movement input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Create movement vector relative to player orientation
        Vector3 move = transform.right * x + transform.forward * z;

        // Check if player has stopped moving (near-zero input)
        bool isMoving = move.magnitude > 0.1f;
        
        // NEW: Check if player is moving forward (needed for sprint restrictions)
        bool isMovingForward = z > 0.1f;

        // Auto-disable sprint when player stops moving OR is no longer moving forward
        if (isSprinting && (!isMoving || !isMovingForward))
        {
            isSprinting = false;
        }

        // Normalize if magnitude > 1 to prevent faster diagonal movement
        if (move.magnitude > 1f)
            move.Normalize();

        // Calculate direction-based speed modifiers
        float directionModifier = 1.0f;

        // Check if moving backward (negative Z input)
        if (z < -0.1f)
        {
            // Moving primarily backward
            directionModifier = backwardSpeedMultiplier; // Use the inspector value
        }
        // Check if moving sideways (X input) but not primarily forward
        else if (Mathf.Abs(x) > 0.1f && z < 0.5f)
        {
            // Moving primarily sideways
            directionModifier = sidewaysSpeedMultiplier; // Use the inspector value
        }

        // Store the direction modifier for later reference
        currentDirectionModifier = directionModifier;
    
        // Determine current target speed based on stance (unless sliding)
        if (!isSliding)
        {
            // Set base target speed based on movement type
            if (isCrouching)
                targetSpeed = crouchSpeed;
            else if (isSprinting)
                targetSpeed = sprintSpeed;
            else
                targetSpeed = walkSpeed;
        
            // Only apply direction modifier to target speed when actually moving
            float effectiveTargetSpeed = isMoving ? targetSpeed * directionModifier : targetSpeed;
        
            // Smoothly transition current speed to target speed
            currentSpeed = Mathf.Lerp(currentSpeed, effectiveTargetSpeed * externalSpeedMultiplier, speedTransitionRate * Time.deltaTime);
        }
    
        // Apply movement using the current calculated speed (no need to multiply by directionModifier again)
        // as it's already factored into currentSpeed
        Vector3 moveAmount = move * currentSpeed * Time.deltaTime;
        controller.Move(moveAmount);

        // Handle sliding state and transitions
        HandleSliding(move);
    
        // Check if slide cooldown has elapsed
        float timeSinceLastSlide = Time.time - lastSlideTime;
        bool canSlide = timeSinceLastSlide >= slideCooldown;
    
        // Check if we have enough stamina to slide
        bool hasStaminaForSlide = currentStamina >= slideStaminaCost;
    
        // Handle crouching (when not sliding)
        if (!isSliding && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)))
        {
            // If we're sprinting, speed is above threshold, cooldown elapsed, and we have stamina
            if (isSprinting && currentSpeed >= slideSpeedThreshold && canSlide && hasStaminaForSlide)
            {
                StartSlide(move);
                UseStamina(slideStaminaCost); // Consume stamina for slide
            }
            else
            {
                // Otherwise just toggle crouch
                ToggleCrouch();
            }
        }
    
        // Handle sprint toggle with key press
        bool hasStaminaToSprint = currentStamina > 0 && !staminaDepleted;
        if (Input.GetKeyDown(sprintKey) && !isSliding && isGrounded)
        {
            // Only allow sprinting if we have stamina AND we're actually moving FORWARD
            if (!isSprinting && hasStaminaToSprint && isMovingForward)
            {
                isSprinting = true;
            
                // If we were crouching and now sprinting, uncrouch
                if (isCrouching)
                {
                    isCrouching = false;
                    targetHeight = standingHeight;
                    targetCameraPosition = standingCameraPosition;
                }
            }
            else if (isSprinting)
            {
                // Always allow stopping sprint
                isSprinting = false;
            }
        }
    
        // Handle stamina depletion for sprinting - only when actually moving
        if (isSprinting && isMoving)
        {
            UseStamina(sprintStaminaDrain * Time.deltaTime);
        
            // If we run out of stamina, stop sprinting
            if (currentStamina <= 0)
            {
                isSprinting = false;
                staminaDepleted = true;
            }
        }
        
        // Handle stamina regeneration
        HandleStaminaRegen();
        
        // Update character controller height and camera position for crouch/slide
        UpdateCrouchState();
        
        // Handle jumping (can't jump while crouching or sliding)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            if (isCrouching || isSliding)
            {
                // Exit crouching/sliding state when trying to jump
                EndSlide();
                isCrouching = false;
                targetHeight = standingHeight;
                targetCameraPosition = standingCameraPosition;
            }
            else
            {
                // Perform normal jump
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                // Disable sprinting when jumping
                isSprinting = false;
            }
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Calculate current horizontal velocity for UI
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float displaySpeed = horizontalVelocity.magnitude;
        
        // Pass both the raw current speed and the actual measured speed to the debug UI
        UpdateDebugUI(displaySpeed, timeSinceLastSlide);
    }
    
    // Handle stamina regeneration with proper UI updates
    private void HandleStaminaRegen()
    {
        // Only regenerate if we haven't used stamina recently
        if (Time.time > lastStaminaUseTime + staminaRegenDelay)
        {
            // Store old stamina value
            float oldStamina = currentStamina;
        
            // Regenerate stamina
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
        
            // Reset depleted flag once we have enough stamina back
            if (currentStamina > maxStamina * 0.2f && staminaDepleted)
            {
                staminaDepleted = false;
                UpdateStaminaBar();
            }
        
            // KEY FIX: Always start the repeating update when regenerating stamina
            else if (currentStamina < maxStamina && !IsInvoking("UpdateStaminaBarForRegen"))
            {
                // Initial update to start the visual feedback
                UpdateStaminaBar();
                // Schedule regular updates during regeneration
                InvokeRepeating("UpdateStaminaBarForRegen", 0.1f, 0.2f);
            }
            // If we've reached max stamina, ensure we stop updates and show full bar
            else if (currentStamina >= maxStamina)
            {
                if (IsInvoking("UpdateStaminaBarForRegen"))
                {
                    CancelInvoke("UpdateStaminaBarForRegen");
                    // One final update to ensure the bar is full
                    UpdateStaminaBar();
                }
                else if (staminaBar != null && staminaBar.fillAmount < 1.0f)
                {
                    // Make sure bar shows full even if we weren't actively updating
                    UpdateStaminaBar();
                }
            }
        }
    }

    // Update method called regularly during regeneration
    private void UpdateStaminaBarForRegen()
    {
        // No need to check for significant changes - just update regularly
        UpdateStaminaBar(true);
    
        // If we've reached full stamina or started using stamina again, cancel repeating updates
        if (currentStamina >= maxStamina || Time.time <= lastStaminaUseTime + staminaRegenDelay)
        {
            CancelInvoke("UpdateStaminaBarForRegen");
        
            // One final update to ensure the bar is correct
            UpdateStaminaBar(true);
        }
    }
    
    // Update the UI stamina bar if assigned
    private void UpdateStaminaBar(bool animate = true)
    {
        if (staminaBar == null)
            return;
    
        float targetFill = currentStamina / maxStamina;

        if (staminaBarBackground != null)
        {
            // For decreasing stamina, animate the background with delay
            if (targetFill < previousStaminaFill)
            {
                // Use the coroutine-based animation instead of DOTween
                AnimateStaminaBackground(previousStaminaFill, targetFill, staminaBarAnimDuration * 1.5f);
            }
            else
            {
                // For increasing stamina, just match the foreground without animation
                if (backgroundBarCoroutine != null)
                {
                    StopCoroutine(backgroundBarCoroutine);
                    backgroundBarCoroutine = null;
                }
                staminaBarBackground.fillAmount = targetFill;
            }
        }
    
        // Apply immediate update if not animating
        if (!animate)
        {
            // Kill any existing animations first
            DOTween.Kill(staminaBar);
            DOTween.Kill(staminaBarBackground);
        
            // Set values directly
            staminaBar.fillAmount = targetFill;
            previousStaminaFill = targetFill;
        
            // Set color based on stamina level
            staminaBar.color = (currentStamina <= lowStaminaThreshold) ? lowStaminaColor : normalStaminaColor;
        
            // Also update background
            if (staminaBarBackground != null)
            {
                staminaBarBackground.fillAmount = targetFill;
            }
        
            return;
        }
    
        // ANIMATION HANDLING - PREVENT STUTTERING
        // Only create a new animation if the change is significant or if no animation is running
        bool significantChange = Mathf.Abs(targetFill - previousStaminaFill) > 0.01f;
    
        // For the foreground bar (primary stamina indicator)
        if (significantChange || (staminaBarTween == null || !staminaBarTween.IsActive()))
        {
            // Kill any existing animation
            if (staminaBarTween != null && staminaBarTween.IsActive())
            {
                staminaBarTween.Kill();
            }
        
            // Create new animation with proper ID
            staminaBarTween = staminaBar.DOFillAmount(targetFill, staminaBarAnimDuration)
                .SetId("StaminaBarFill")
                .SetEase(staminaBarEase)
                .OnComplete(() => {
                    previousStaminaFill = targetFill;
                    staminaBarTween = null;
                });
        }
    
        // Handle color change - do it instantly to avoid color animation issues
        Color targetColor = (currentStamina <= lowStaminaThreshold) ? lowStaminaColor : normalStaminaColor;
        staminaBar.color = targetColor;
    
        // Handle background bar (for trail effect)
        if (staminaBarBackground != null)
        {
            // For decreasing stamina, animate the background with delay
            if (targetFill < previousStaminaFill)
            {
                // Kill any existing background animations
                DOTween.Kill(staminaBarBackground);
            
                // Set initial state
                staminaBarBackground.fillAmount = previousStaminaFill;
            
                // Create new animation with proper ID and longer duration
                staminaBarBackground.DOFillAmount(targetFill, staminaBarAnimDuration * 1.5f)
                    .SetId("StaminaBarBG")
                    .SetDelay(0.1f)
                    .SetEase(Ease.OutCubic);
            }
            else
            {
                // For increasing stamina, just match the foreground without animation
                DOTween.Kill(staminaBarBackground);
                staminaBarBackground.fillAmount = targetFill;
            }
        }
    }

    private void AnimateStaminaBackground(float startFill, float endFill, float duration)
    {
        // Stop any existing background animation
        if (backgroundBarCoroutine != null)
        {
            StopCoroutine(backgroundBarCoroutine);
        }
    
        // Start a new animation
        backgroundBarCoroutine = StartCoroutine(AnimateBackgroundFill(startFill, endFill, duration));
    }

    private IEnumerator AnimateBackgroundFill(float startFill, float endFill, float duration)
    {
        if (staminaBarBackground == null)
            yield break;
    
        staminaBarBackground.fillAmount = startFill;
    
        float elapsedTime = 0;
    
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            // Use smoothstep interpolation for smoother animation
            float smoothT = t * t * (3f - 2f * t);
            staminaBarBackground.fillAmount = Mathf.Lerp(startFill, endFill, smoothT);
        
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    
        // Ensure we end at exactly the target value
        staminaBarBackground.fillAmount = endFill;
        backgroundBarCoroutine = null;
    }

    private void OnDisable()
    {
        // Kill any active tweens associated with this component
        if (staminaBarTween != null && staminaBarTween.IsActive())
        {
            staminaBarTween.Kill();
            staminaBarTween = null;
        }
        
        if (staminaBar != null)
        {
            DOTween.Kill(staminaBar);
        }
        
        if (staminaBarBackground != null)
        {
            DOTween.Kill(staminaBarBackground);
        }
        
        // Cancel any invokes
        CancelInvoke("UpdateStaminaBarForRegen");
    }

    private void OnDestroy()
    {
        // Clean up any remaining tweens
        if (staminaBarTween != null)
        {
            staminaBarTween.Kill();
        }
        
        if (staminaBar != null)
        {
            DOTween.Kill(staminaBar);
        }
        
        if (staminaBarBackground != null)
        {
            DOTween.Kill(staminaBar);
        }
    }
    
    // Use stamina and track when it was last used
    private void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        lastStaminaUseTime = Time.time;
        UpdateStaminaBar();
    }
    
    private void StartSlide(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.1f)
            moveDirection = transform.forward; // Use forward direction if no input
        
        isSliding = true;
        isCrouching = true;
        slideTimer = slideDuration;
    
        // Store slide direction (normalized)
        slideDirection = moveDirection.normalized;
    
        // Set slide speed based on current speed + additional force
        currentSpeed = Mathf.Max(currentSpeed, sprintSpeed) + slideForce;
    
        // Set the crouch height
        targetHeight = crouchHeight;
        targetCameraPosition = crouchingCameraPosition;
    
        // Untoggle sprint when sliding
        isSprinting = false;
    }
    
    private void HandleSliding(Vector3 moveDirection)
    {
        if (!isSliding)
            return;
            
        // Decrement slide timer
        slideTimer -= Time.deltaTime;
        
        // Gradually reduce slide speed
        currentSpeed = Mathf.Lerp(currentSpeed, crouchSpeed, (1 - (slideTimer / slideDuration)) * speedTransitionRate * Time.deltaTime);
        
        // End slide if timer runs out or player slows down significantly
        if (slideTimer <= 0 || currentSpeed < crouchSpeed * 1.2f || !isGrounded)
        {
            EndSlide();
        }
        
        // Allow player to end slide early by standing
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl))
        {
            EndSlide();
        }
    }
    
    private void EndSlide()
    {
        if (!isSliding)
            return;
            
        isSliding = false;
        
        // Record the time when slide ended for cooldown
        lastSlideTime = Time.time;
        
        // Maintain crouch if control is still held
        isCrouching = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        
        // Set appropriate height
        targetHeight = isCrouching ? crouchHeight : standingHeight;
        targetCameraPosition = isCrouching ? crouchingCameraPosition : standingCameraPosition;
        
        // Reset speed to appropriate value
        targetSpeed = isCrouching ? crouchSpeed : walkSpeed;
    }
    
    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        
        // Set target values based on crouch state
        targetHeight = isCrouching ? crouchHeight : standingHeight;
        targetCameraPosition = isCrouching ? crouchingCameraPosition : standingCameraPosition;
    }
    
    private void UpdateCrouchState()
    {
        // Smoothly adjust controller height
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
    
        // Calculate new center Y position based on height change
        Vector3 newCenter = controller.center;
        newCenter.y = originalControllerY - ((standingHeight - controller.height) / 2f);
        controller.center = newCenter;
    
        // Adjust camera position for crouch/slide
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, 
                targetCameraPosition, 
                crouchTransitionSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateDebugUI(float displaySpeed, float timeSinceLastSlide)
    {
        if (!showDebugUI)
            return;
        
        // Calculate direction modifier for debug display
        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");
        float directionModifier = 1.0f;
    
        if (z < -0.1f)
        {
            directionModifier = backwardSpeedMultiplier;
        }
        else if (Mathf.Abs(x) > 0.1f && z < 0.5f)
        {
            directionModifier = sidewaysSpeedMultiplier;
        }
        
        // Calculate the effective speed (what the player would actually move at)
        float effectiveSpeed = currentSpeed * directionModifier;
        
        // Calculate slide cooldown status for debug display
        bool canSlide = timeSinceLastSlide >= slideCooldown;
        float cooldownRemaining = Mathf.Max(0, slideCooldown - timeSinceLastSlide);
        
        // Update debug information
        debugInfo = new DebugInfo
        {
            speed = displaySpeed, // Actual measured speed
            targetSpeed = targetSpeed,
            baseSpeed = currentSpeed, // Rename to make it clearer this is pre-modifier
            effectiveSpeed = effectiveSpeed, // New field showing speed with modifier applied
            directionModifier = directionModifier,
            isCrouching = isCrouching,
            isSprinting = isSprinting,
            isSliding = isSliding,
            slideTimeRemaining = slideTimer,
            slideCooldownRemaining = cooldownRemaining,
            canSlideNow = canSlide,
            isGrounded = isGrounded,
            height = controller.height,
            centerY = controller.center.y,
            yPos = transform.position.y,
            groundCheckPos = groundCheck.position.y,
            currentStamina = currentStamina,
            maxStamina = maxStamina,
            staminaDepleted = staminaDepleted,
            staminaRegenDelay = staminaRegenDelay,
            timeSinceStaminaUse = Time.time - lastStaminaUseTime
        };
    }
    
    private struct DebugInfo
    {
        public float speed;
        public float targetSpeed;
        public float baseSpeed; // Rename to make it clearer this is pre-modifier
        public float effectiveSpeed; // New field showing speed with modifier applied
        public float directionModifier; // New field
        public bool isCrouching;
        public bool isSprinting;
        public bool isSliding;
        public float slideTimeRemaining;
        public float slideCooldownRemaining;
        public bool canSlideNow;
        public bool isGrounded;
        public float height;
        public float centerY;
        public float yPos;
        public float groundCheckPos;
        public float currentStamina;
        public float maxStamina;
        public bool staminaDepleted;
        public float staminaRegenDelay;
        public float timeSinceStaminaUse;
    }
    
    private DebugInfo debugInfo;

    // Debug UI to show current speed and state
    private void OnGUI()
    {
        if (showDebugUI)
        {
            string stateText;
            if (debugInfo.isSliding)
                stateText = "Sliding";
            else if (debugInfo.isCrouching)
                stateText = "Crouching";
            else if (debugInfo.isSprinting)
                stateText = "Sprinting";
            else if (!debugInfo.isGrounded)
                stateText = "Jumping";
            else
                stateText = "Walking";
                
            string cooldownText = debugInfo.canSlideNow ? "Ready" : $"{debugInfo.slideCooldownRemaining:F1}s";
            string speedText = $"Speed: {debugInfo.speed:F2} units/s ({stateText})";
            string transitionText = $"Current/Target Speed: {debugInfo.baseSpeed:F2}/{debugInfo.targetSpeed:F2}";
            string effectiveSpeedText = $"Effective Speed: {debugInfo.effectiveSpeed:F2} units/s"; // New line
            string stateDetails = debugInfo.isSliding 
                ? $" | Slide Time: {debugInfo.slideTimeRemaining:F2}s" 
                : $" | Slide Cooldown: {cooldownText}";
                
            string posText = $"Y Pos: {debugInfo.yPos:F2} | GroundCheck: {debugInfo.groundCheckPos:F2}";
            string heightText = $"Controller Height: {debugInfo.height:F2} | Center Y: {debugInfo.centerY:F2}";
            
            // Stamina info
            string staminaText = $"Stamina: {debugInfo.currentStamina:F1}/{debugInfo.maxStamina}";
            if (debugInfo.staminaDepleted)
                staminaText += " (DEPLETED)";
                
            string staminaRegenText = $"Regen: {(debugInfo.timeSinceStaminaUse >= debugInfo.staminaRegenDelay ? "Active" : $"In {debugInfo.staminaRegenDelay - debugInfo.timeSinceStaminaUse:F1}s")}";
                
            GUI.Label(new Rect(10, 10, 300, 30), speedText, debugTextStyle);
            GUI.Label(new Rect(10, 40, 300, 30), transitionText, debugTextStyle);
            GUI.Label(new Rect(10, 70, 350, 30), stateDetails, debugTextStyle);
            GUI.Label(new Rect(10, 100, 300, 30), posText, debugTextStyle);
            GUI.Label(new Rect(10, 130, 300, 30), heightText, debugTextStyle);
            GUI.Label(new Rect(10, 160, 300, 30), staminaText, debugTextStyle);
            GUI.Label(new Rect(10, 190, 300, 30), staminaRegenText, debugTextStyle);
            // Add direction modifier display
            string directionText = $"Direction Modifier: {debugInfo.directionModifier:F2}x";
            GUI.Label(new Rect(10, 220, 300, 30), directionText, debugTextStyle);
            // Add effective speed display
            GUI.Label(new Rect(10, 250, 300, 30), effectiveSpeedText, debugTextStyle);
        }
    }
    
    // For any script that needs to check stamina status
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
    
    public float GetMaxStamina()
    {
        return maxStamina;
    }
    
    public bool IsStaminaDepleted()
    {
        return staminaDepleted;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp01(multiplier);
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
}
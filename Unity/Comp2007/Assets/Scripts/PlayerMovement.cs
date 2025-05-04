using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Basic player movement controller for first person games with sliding
/// Includes walking, sprinting, crouching, sliding, and stamina management
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Component References")]
    /// <summary>
    /// Character controller component used for player movement
    /// </summary>
    public CharacterController controller;
    
    /// <summary>
    /// Reference to camera transform to adjust height during crouching/standing
    /// </summary>
    public Transform cameraTransform;

    [Header("Movement Settings")]
    /// <summary>
    /// Base movement speed when walking
    /// </summary>
    public float walkSpeed = 5f;
    
    /// <summary>
    /// Movement speed when sprinting (consumes stamina)
    /// </summary>
    public float sprintSpeed = 10f;
    
    /// <summary>
    /// Movement speed when crouching
    /// </summary>
    public float crouchSpeed = 2.5f;
    
    /// <summary>
    /// Controls how quickly speed changes between states
    /// </summary>
    public float speedTransitionRate = 5f;
    
    /// <summary>
    /// How high the player jumps in world units
    /// </summary>
    public float jumpHeight = 2f;
    
    /// <summary>
    /// Gravity force applied to player (negative value)
    /// </summary>
    public float gravity = -9.81f;

    [Header("Directional Speed Modifiers")]
    /// <summary>
    /// Speed multiplier when moving backward (0.1-1.0)
    /// </summary>
    [Range(0.1f, 1.0f)] public float backwardSpeedMultiplier = 0.5f;
    
    /// <summary>
    /// Speed multiplier when strafing sideways (0.1-1.0)
    /// </summary>
    [Range(0.1f, 1.0f)] public float sidewaysSpeedMultiplier = 0.75f;

    [Header("Crouch Settings")]
    /// <summary>
    /// Character controller height when standing
    /// </summary>
    public float standingHeight = 2f;
    
    /// <summary>
    /// Character controller height when crouching
    /// </summary>
    public float crouchHeight = 1f;
    
    /// <summary>
    /// How quickly the player transitions between standing and crouching
    /// </summary>
    public float crouchTransitionSpeed = 10f;
    
    /// <summary>
    /// Camera position when standing (local space)
    /// </summary>
    public Vector3 standingCameraPosition = new Vector3(0, 0.8f, 0);
    
    /// <summary>
    /// Camera position when crouching (local space)
    /// </summary>
    public Vector3 crouchingCameraPosition = new Vector3(0, 0.4f, 0);

    [Header("Slide Settings")]
    /// <summary>
    /// Initial force applied when starting a slide
    /// </summary>
    public float slideForce = 5f;
    
    /// <summary>
    /// Maximum slide duration in seconds
    /// </summary>
    public float slideDuration = 1.0f;
    
    /// <summary>
    /// Minimum speed required to initiate a slide
    /// </summary>
    public float slideSpeedThreshold = 7f;
    
    /// <summary>
    /// How much control player has while sliding (0-1)
    /// </summary>
    public float slideControlReduction = 0.5f;
    
    /// <summary>
    /// Cooldown period between slides in seconds
    /// </summary>
    public float slideCooldown = 2.0f;
    
    [Header("Stamina Settings")]
    /// <summary>
    /// Maximum stamina points
    /// </summary>
    public float maxStamina = 100f;
    
    /// <summary>
    /// Rate at which stamina regenerates per second
    /// </summary>
    public float staminaRegenRate = 10f;
    
    /// <summary>
    /// Amount of stamina consumed per second while sprinting
    /// </summary>
    public float sprintStaminaDrain = 15f;
    
    /// <summary>
    /// One-time stamina cost for performing a slide
    /// </summary>
    public float slideStaminaCost = 20f;
    
    /// <summary>
    /// Delay in seconds before stamina begins regenerating after use
    /// </summary>
    public float staminaRegenDelay = 1.5f;
    
    /// <summary>
    /// UI element for displaying stamina bar
    /// </summary>
    [SerializeField] private Image staminaBar;
    
    /// <summary>
    /// Color for normal stamina levels
    /// </summary>
    [SerializeField] private Color normalStaminaColor = Color.green;
    
    /// <summary>
    /// Color for low stamina warning
    /// </summary>
    [SerializeField] private Color lowStaminaColor = Color.red;
    
    /// <summary>
    /// Threshold below which stamina is considered low (displays warning color)
    /// </summary>
    [SerializeField] private float lowStaminaThreshold = 30f;

    [Header("Stamina UI Settings")]
    /// <summary>
    /// Duration of stamina bar fill animations
    /// </summary>
    [SerializeField] private float staminaBarAnimDuration = 0.3f;
    
    /// <summary>
    /// Easing function for stamina bar animations
    /// </summary>
    [SerializeField] private Ease staminaBarEase = Ease.OutQuad;
    
    /// <summary>
    /// Background element of the stamina bar for delayed animation effect
    /// </summary>
    [SerializeField] private Image staminaBarBackground;

    /// <summary>
    /// Reference to the active coroutine for background bar animation
    /// </summary>
    private Coroutine backgroundBarCoroutine;

    [Header("Ground Check")]
    /// <summary>
    /// Transform position used for ground detection
    /// </summary>
    public Transform groundCheck;
    
    /// <summary>
    /// Radius of the ground check sphere cast
    /// </summary>
    public float groundDistance = 0.4f;
    
    /// <summary>
    /// Layers that count as ground for jumping and movement
    /// </summary>
    public LayerMask groundMask;

    [Header("DOTween Settings")]
    /// <summary>
    /// Initial capacity for DOTween animations
    /// </summary>
    [SerializeField] private int initialTweenCapacity = 1500;
    
    /// <summary>
    /// Maximum capacity for DOTween animations
    /// </summary>
    [SerializeField] private int maxTweenCapacity = 2000;

    [Header("Audio Settings")]
    /// <summary>
    /// Sound name for footsteps while walking
    /// </summary>
    [SerializeField] private string walkFootstepSoundName = "WalkFootstep";

    /// <summary>
    /// Sound name for footsteps while running
    /// </summary>
    [SerializeField] private string sprintFootstepSoundName = "SprintFootstep";

    /// <summary>
    /// Sound name for footsteps while crouching
    /// </summary>
    [SerializeField] private string crouchFootstepSoundName = "CrouchFootstep";

    /// <summary>
    /// Sound name for jumping
    /// </summary>
    [SerializeField] private string jumpSoundName = "Jump";

    /// <summary>
    /// Time between footstep sounds when walking
    /// </summary>
    [SerializeField] private float walkFootstepInterval = 0.5f;

    /// <summary>
    /// Time between footstep sounds when sprinting
    /// </summary>
    [SerializeField] private float sprintFootstepInterval = 0.3f;

    /// <summary>
    /// Time between footstep sounds when crouching
    /// </summary>
    [SerializeField] private float crouchFootstepInterval = 0.7f;

    /// <summary>
    /// Minimum velocity required to make footstep sounds
    /// </summary>
    [SerializeField] private float footstepVelocityThreshold = 0.1f;

    /// <summary>
    /// Maximum velocity to scale footstep volume from
    /// </summary>
    [SerializeField] private float maxFootstepVelocity = 10f;

    // Private variables
    /// <summary>
    /// Current vertical velocity vector
    /// </summary>
    private Vector3 velocity;
    
    /// <summary>
    /// Whether the player is currently touching the ground
    /// </summary>
    private bool isGrounded;
    
    /// <summary>
    /// Current movement speed after all modifiers
    /// </summary>
    private float currentSpeed;
    
    /// <summary>
    /// Target speed the player is transitioning toward
    /// </summary>
    private float targetSpeed;
    
    /// <summary>
    /// Whether the player is currently sprinting
    /// </summary>
    private bool isSprinting;
    
    /// <summary>
    /// Whether the player is currently crouching
    /// </summary>
    private bool isCrouching;
    
    /// <summary>
    /// Whether the player is currently sliding
    /// </summary>
    private bool isSliding;
    
    /// <summary>
    /// Time remaining in the current slide
    /// </summary>
    private float slideTimer;
    
    /// <summary>
    /// Direction vector for the current slide
    /// </summary>
    private Vector3 slideDirection;
    
    /// <summary>
    /// Target height the player is transitioning toward
    /// </summary>
    private float targetHeight;
    
    /// <summary>
    /// Target camera position the player is transitioning toward
    /// </summary>
    private Vector3 targetCameraPosition;
    
    /// <summary>
    /// Original Y position of the character controller center
    /// </summary>
    private float originalControllerY;
    
    /// <summary>
    /// Time when the last slide was performed
    /// </summary>
    private float lastSlideTime = -10f;
    
    /// <summary>
    /// Key used for sprint activation
    /// </summary>
    private KeyCode sprintKey = KeyCode.LeftShift;
    
    /// <summary>
    /// Current movement direction modifier based on forward/sideways/backward movement
    /// </summary>
    private float currentDirectionModifier = 1.0f;
    
    // Stamina variables
    /// <summary>
    /// Current stamina value
    /// </summary>
    private float currentStamina;
    
    /// <summary>
    /// Time when stamina was last used
    /// </summary>
    private float lastStaminaUseTime;
    
    /// <summary>
    /// Whether stamina is completely depleted, preventing sprinting until threshold is reached
    /// </summary>
    private bool staminaDepleted = false;
    
    /// <summary>
    /// Previous fill amount of the stamina bar for animation
    /// </summary>
    private float previousStaminaFill = 1f;
    
    /// <summary>
    /// Reference to the active stamina bar animation tween
    /// </summary>
    private Tweener staminaBarTween;

    /// <summary>
    /// Whether to show debug UI with movement information
    /// </summary>
    public bool showDebugUI = true;
    
    /// <summary>
    /// Style for debug text display
    /// </summary>
    private GUIStyle debugTextStyle;

    /// <summary>
    /// Speed modifier applied from external sources like aiming
    /// </summary>
    private float externalSpeedMultiplier = 1.0f;

    // Sound timing variables
    private float lastFootstepTime = 0f;
    private float currentFootstepInterval = 0.5f;

    /// <summary>
    /// Initializes DOTween with appropriate capacity settings
    /// </summary>
    private void Awake()
    {
        // Initialize DOTween with higher capacity
        DOTween.SetTweensCapacity(initialTweenCapacity, maxTweenCapacity);
    }
    
    /// <summary>
    /// Sets up initial movement settings, debug display, and character height
    /// </summary>
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

    /// <summary>
    /// Handles player movement, jumping, crouching, sliding and stamina management each frame
    /// </summary>
    private void Update()
    {
        // Check ground state before updating
        bool wasGroundedBefore = isGrounded;
        
        // Ground check - adjust position based on current controller height
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep player grounded
        }

        // Keep track of grounded state for next frame
        wasGroundedBefore = isGrounded;

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
    
        // Apply movement using the current calculated speed
        Vector3 moveAmount = move * currentSpeed * Time.deltaTime;
        controller.Move(moveAmount);

        // HANDLE FOOTSTEPS
        HandleFootstepSounds(isMoving);
    
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
                // Perform normal jump and play sound
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                PlayMovementSound(jumpSoundName, 0.8f);
                
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
    
    /// <summary>
    /// Handles stamina regeneration with proper UI updates
    /// </summary>
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

    /// <summary>
    /// Update method called regularly during stamina regeneration to update UI
    /// </summary>
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
    
    /// <summary>
    /// Updates the UI stamina bar with proper animation and visual feedback
    /// </summary>
    /// <param name="animate">Whether to animate the changes or update instantly</param>
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

    /// <summary>
    /// Animates the stamina bar background element for smooth transitions
    /// </summary>
    /// <param name="startFill">Starting fill amount</param>
    /// <param name="endFill">Target fill amount</param>
    /// <param name="duration">Duration of animation in seconds</param>
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

    /// <summary>
    /// Coroutine that handles the animation of background fill amount
    /// </summary>
    /// <param name="startFill">Starting fill amount</param>
    /// <param name="endFill">Target fill amount</param>
    /// <param name="duration">Duration of animation in seconds</param>
    /// <returns>IEnumerator for coroutine execution</returns>
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

    /// <summary>
    /// Cleans up DOTween animations when component is disabled
    /// </summary>
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

    /// <summary>
    /// Ensures proper cleanup of DOTween animations when object is destroyed
    /// </summary>
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
    
    /// <summary>
    /// Consumes stamina and updates the timestamp of last usage
    /// </summary>
    /// <param name="amount">Amount of stamina to consume</param>
    private void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        lastStaminaUseTime = Time.time;
        UpdateStaminaBar();
    }
    
    /// <summary>
    /// Initiates a slide in the specified movement direction
    /// </summary>
    /// <param name="moveDirection">Direction vector for the slide</param>
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
    
    /// <summary>
    /// Manages active sliding state including speed reduction over time
    /// </summary>
    /// <param name="moveDirection">Current movement input direction</param>
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
    
    /// <summary>
    /// Ends the sliding state and applies cooldown
    /// </summary>
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
    
    /// <summary>
    /// Toggles between crouching and standing states
    /// </summary>
    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        
        // Set target values based on crouch state
        targetHeight = isCrouching ? crouchHeight : standingHeight;
        targetCameraPosition = isCrouching ? crouchingCameraPosition : standingCameraPosition;
    }
    
    /// <summary>
    /// Updates the character controller height and camera position for smooth crouching transitions
    /// </summary>
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

    /// <summary>
    /// Updates the debug information displayed on screen
    /// </summary>
    /// <param name="displaySpeed">Current measured movement speed</param>
    /// <param name="timeSinceLastSlide">Time elapsed since last slide</param>
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
    
    /// <summary>
    /// Structure to hold debug information for display
    /// </summary>
    private struct DebugInfo
    {
        public float speed;
        public float targetSpeed;
        public float baseSpeed;
        public float effectiveSpeed;
        public float directionModifier;
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
    
    /// <summary>
    /// Current debug information for display
    /// </summary>
    private DebugInfo debugInfo;

    /// <summary>
    /// Displays debug information on screen when enabled
    /// </summary>
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
            string effectiveSpeedText = $"Effective Speed: {debugInfo.effectiveSpeed:F2} units/s";
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
    
    /// <summary>
    /// Returns the current stamina value
    /// </summary>
    /// <returns>Current stamina points</returns>
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
    
    /// <summary>
    /// Returns the maximum stamina value
    /// </summary>
    /// <returns>Maximum stamina points</returns>
    public float GetMaxStamina()
    {
        return maxStamina;
    }
    
    /// <summary>
    /// Checks if player's stamina is fully depleted
    /// </summary>
    /// <returns>True if stamina is depleted, false otherwise</returns>
    public bool IsStaminaDepleted()
    {
        return staminaDepleted;
    }

    /// <summary>
    /// Sets an external speed multiplier (used for aiming down sights, etc.)
    /// </summary>
    /// <param name="multiplier">Speed multiplier between 0 and 1</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp01(multiplier);
    }

    /// <summary>
    /// Checks if the player is currently sprinting
    /// </summary>
    /// <returns>True if sprinting, false otherwise</returns>
    public bool IsSprinting()
    {
        return isSprinting;
    }

    /// <summary>
    /// Checks if the player is currently on the ground
    /// </summary>
    /// <returns>True if grounded, false otherwise</returns>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Plays movement sounds as a child of the player for better spatial audio
    /// </summary>
    /// <param name="soundName">Name of the sound in SoundLibrary</param>
    /// <param name="volume">Volume level (default 1.0)</param>
    /// <param name="mixerGroup">Audio mixer group to use (default "SFX")</param>
    /// <param name="destroyAfterPlaying">Whether to destroy the audio source after playing</param>
    /// <returns>The created audio object if destroyAfterPlaying is false, otherwise null</returns>
    private GameObject PlayMovementSound(string soundName, float volume = 1.0f, string mixerGroup = "SFX", bool destroyAfterPlaying = true)
    {
        if (string.IsNullOrEmpty(soundName)) return null;

        SoundManager soundManager = SoundManager.GetInstance();
        if (soundManager == null || soundManager.GetSoundLibrary() == null) 
        {
            Debug.LogWarning("SoundManager or SoundLibrary not available");
            return null;
        }

        AudioClip clip = soundManager.GetSoundLibrary().GetClipFromName(soundName);
        if (clip == null) return null;

        // Create the audio source as child of player
        GameObject audioObj = new GameObject(soundName + "_Sound");
        audioObj.transform.SetParent(transform);
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

        // Clean up after playing if needed
        if (destroyAfterPlaying)
        {
            Destroy(audioObj, clip.length + 0.1f);
            return null;
        }
        
        return audioObj;
    }

    /// <summary>
    /// Handles footstep sounds based on player state and movement
    /// </summary>
    /// <param name="isMoving">Whether the player is currently moving</param>
    private void HandleFootstepSounds(bool isMoving)
    {
        // Only play footsteps when grounded and moving
        if (!isGrounded || !isMoving)
        {
            return;
        }

        // Calculate horizontal velocity for footstep timing/volume
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        float velocityMagnitude = horizontalVelocity.magnitude;
        
        // Don't play footsteps if moving too slowly
        if (velocityMagnitude < footstepVelocityThreshold)
        {
            return;
        }

        // Set the appropriate footstep interval based on movement state
        if (isSliding)
        {
            // Sliding uses a different sound system
            return;
        }
        else if (isCrouching)
        {
            currentFootstepInterval = crouchFootstepInterval;
        }
        else if (isSprinting)
        {
            currentFootstepInterval = sprintFootstepInterval;
        }
        else
        {
            currentFootstepInterval = walkFootstepInterval;
        }

        // Check if it's time to play a footstep
        if (Time.time >= lastFootstepTime + currentFootstepInterval)
        {
            // Determine which footstep sound to use
            string footstepSound = walkFootstepSoundName;
            
            if (isCrouching)
            {
                footstepSound = crouchFootstepSoundName;
            }
            else if (isSprinting)
            {
                footstepSound = sprintFootstepSoundName;
            }
            
            // Scale volume based on velocity (faster = louder, up to a point)
            float volume = Mathf.Clamp01(velocityMagnitude / maxFootstepVelocity) * 0.7f + 0.3f;
            
            // Vary pitch slightly for more natural sound
            float pitch = Random.Range(0.95f, 1.05f);
            
            // Play the footstep sound
            PlayMovementSound(footstepSound, volume);
            
            // Update the last footstep time
            lastFootstepTime = Time.time;
        }
    }

    /// <summary>
    /// Fades out an audio source and destroys its game object
    /// </summary>
    /// <param name="audioSource">The audio source to fade</param>
    /// <param name="fadeDuration">Duration of the fade in seconds</param>
    private IEnumerator FadeOutAndDestroy(AudioSource audioSource, float fadeDuration)
    {
        if (audioSource != null)
        {
            float startVolume = audioSource.volume;
            float timer = 0;
            
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeDuration);
                yield return null;
            }
            
            if (audioSource != null && audioSource.gameObject != null)
            {
                Destroy(audioSource.gameObject);
            }
        }
    }
}
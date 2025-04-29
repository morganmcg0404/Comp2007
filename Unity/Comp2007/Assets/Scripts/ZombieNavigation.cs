using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls zombie movement behavior using Unity's NavMesh system
/// Handles different zombie movement types, target following, and animation integration
/// </summary>
public class ZombieNavigation : MonoBehaviour
{
    /// <summary>
    /// Defines the different speed categories of zombies
    /// </summary>
    public enum ZombieType
    {
        /// <summary>Slow zombie with basic walking speed</summary>
        Walker,
        
        /// <summary>Medium-speed zombie that jogs toward targets</summary>
        Jogger,
        
        /// <summary>Fast zombie that runs at full speed</summary>
        Sprinter
    }

    [Header("Navigation Settings")]
    /// <summary>
    /// Time in seconds between path recalculations
    /// </summary>
    [SerializeField] private float updatePathInterval = 0.5f;
    
    /// <summary>
    /// Distance in meters at which the zombie will stop approaching the player
    /// </summary>
    [SerializeField] private float stoppingDistance = 1.5f;
    
    /// <summary>
    /// Speed at which the zombie rotates to face its movement direction
    /// </summary>
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Movement Settings")]
    /// <summary>
    /// Movement speed in meters per second for walker zombies
    /// </summary>
    [SerializeField] private float walkSpeed = 3f;    // Normal walking speed
    
    /// <summary>
    /// Movement speed in meters per second for jogger zombies
    /// </summary>
    [SerializeField] private float jogSpeed = 5f;     // Medium jogger speed
    
    /// <summary>
    /// Movement speed in meters per second for sprinter zombies
    /// </summary>
    [SerializeField] private float sprintSpeed = 8f;  // Sprint speed for faster zombies
    
    /// <summary>
    /// Movement speed in meters per second when critically injured
    /// </summary>
    [SerializeField] private float injuredSpeed = 1.5f; // Speed when critically injured
    
    [Header("Zombie Type")]
    /// <summary>
    /// The movement type of this zombie instance
    /// </summary>
    [SerializeField] private ZombieType zombieType = ZombieType.Walker; // Default to walker
    
    /// <summary>
    /// Health percentage threshold below which the zombie is considered critically injured
    /// </summary>
    [SerializeField] private float criticalHealthPercent = 0.1f; // 10% health threshold for slowing down
    
    [Header("Grounding Settings")]
    /// <summary>
    /// Whether to apply gravity and ground checking to the zombie
    /// </summary>
    [SerializeField] private bool applyGravity = true;
    
    /// <summary>
    /// Strength of gravity applied to the zombie when not on NavMesh
    /// </summary>
    [SerializeField] private float gravityMultiplier = 9.81f; // Standard gravity
    
    /// <summary>
    /// Distance to check below the zombie for ground detection
    /// </summary>
    [SerializeField] private float groundCheckDistance = 0.2f; // How far to check for ground
    
    /// <summary>
    /// Layers that are considered as ground for height adjustment
    /// </summary>
    [SerializeField] private LayerMask groundLayer; // Set this in the inspector to your ground layers
    
    /// <summary>
    /// Transform positioned at the zombie's feet for accurate ground checking
    /// </summary>
    [SerializeField] private Transform groundCheckPoint; // Reference to a child GameObject at the zombie's feet
    
    /// <summary>
    /// Height in meters to maintain above the ground surface
    /// </summary>
    [SerializeField] private float heightOffset = 0.1f; // How high above the ground to maintain the zombie

    [Header("References")]
    /// <summary>
    /// Reference to the zombie's animator for controlling movement animations
    /// </summary>
    [SerializeField] private Animator animator;
    
    // Component references
    /// <summary>
    /// Reference to the NavMeshAgent component that handles pathfinding
    /// </summary>
    private NavMeshAgent navMeshAgent;
    
    /// <summary>
    /// Reference to the player's transform for targeting
    /// </summary>
    private Transform playerTransform;
    
    /// <summary>
    /// Reference to the zombie's health system for damage response
    /// </summary>
    private HealthSystem healthSystem;
    
    // State tracking
    /// <summary>
    /// Whether the zombie is actively pursuing the player
    /// </summary>
    private bool isChasing = false;
    
    /// <summary>
    /// Whether the zombie's health is below the critical threshold
    /// </summary>
    private bool isCriticallyInjured = false;
    
    /// <summary>
    /// Time when the path was last updated
    /// </summary>
    private float lastPathUpdateTime = 0f;
    
    /// <summary>
    /// Current movement speed based on zombie type and health status
    /// </summary>
    private float currentSpeed;

    /// <summary>
    /// Reference to the player's collider for accurate distance calculations
    /// </summary>
    private Collider playerCollider;
    
    /// <summary>
    /// Whether to target the closest point on player collider vs. player transform
    /// </summary>
    private bool useClosestPoint = true;
    
    /// <summary>
    /// Whether the zombie is currently on the ground
    /// </summary>
    private bool isGrounded = true;
    
    /// <summary>
    /// Current vertical velocity when applying gravity
    /// </summary>
    private float verticalVelocity = 0f;
    
    /// <summary>
    /// Initializes components, finds the player, and configures the NavMeshAgent
    /// </summary>
    private void Awake()
    {
        // Get references to components
        navMeshAgent = GetComponent<NavMeshAgent>();
        healthSystem = GetComponent<HealthSystem>();
        
        // Find the player by tag if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                
                // Get the player's collider
                playerCollider = player.GetComponent<Collider>();
                if (playerCollider == null)
                {
                    // Try to find collider in children if not on the player GameObject
                    playerCollider = player.GetComponentInChildren<Collider>();
                }
                
                if (playerCollider == null)
                {
                    Debug.LogWarning("Player doesn't have a collider. Falling back to transform position targeting.");
                    useClosestPoint = false;
                }
            }
        }
        
        // Set initial speed based on sprinter status
        currentSpeed = sprintSpeed;
        
        // Configure NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.speed = currentSpeed;
            navMeshAgent.updateRotation = false; // We'll handle rotation manually for smoother turns
            
            if (applyGravity)
            {
                // Keep the actual NavMesh handling normal
                navMeshAgent.autoTraverseOffMeshLink = true;
                
                // Very important: don't set baseOffset to 0, let the NavMeshAgent use its configured value
                // This helps prevent sinking into the ground
                
                // Additional settings that might help
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                navMeshAgent.avoidancePriority = 50;
            }
        }
        
        // Set isChasing to true immediately
        isChasing = true;
    }
    
    /// <summary>
    /// Updates zombie movement, rotation, and animation each frame
    /// Handles pause detection and death state
    /// </summary>
    private void Update()
    {
        // Skip all processing when game is paused
        if (PauseManager.IsPaused())
            return;
            
        // Don't navigate if dead or player not found
        if ((healthSystem != null && healthSystem.IsDead()) || playerTransform == null)
        {
            StopMoving();
            return;
        }
        
        // Check if health is critically low
        CheckHealthStatus();
        
        // Apply gravity and check grounding
        ApplyGravity();
        
        UpdatePath();
        UpdateRotation();
        UpdateAnimations();
    }
    
    /// <summary>
    /// Checks the zombie's current health and updates movement speed if critically injured
    /// </summary>
    private void CheckHealthStatus()
    {
        if (healthSystem != null)
        {
            float healthPercent = healthSystem.GetCurrentHealth() / healthSystem.GetMaxHealth();
            
            // Check if zombie is critically injured (below 10% health)
            bool wasInjured = isCriticallyInjured;
            isCriticallyInjured = healthPercent <= criticalHealthPercent;
            
            // If injury status changed, update speed
            if (wasInjured != isCriticallyInjured)
            {
                UpdateMovementSpeed();
            }
        }
    }
    
    /// <summary>
    /// Updates the zombie's movement speed based on type and injury status
    /// </summary>
    private void UpdateMovementSpeed()
    {
        // If critically injured, use injured speed regardless of zombie type
        if (isCriticallyInjured)
        {
            currentSpeed = injuredSpeed;
        }
        else
        {
            // Otherwise use appropriate speed based on zombie type
            switch (zombieType)
            {
                case ZombieType.Walker:
                    currentSpeed = walkSpeed;
                    break;
                case ZombieType.Jogger:
                    currentSpeed = jogSpeed;
                    break;
                case ZombieType.Sprinter:
                    currentSpeed = sprintSpeed;
                    break;
                default:
                    currentSpeed = walkSpeed; // Fallback
                    break;
            }
        }
        
        // Apply to NavMeshAgent if available
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.speed = currentSpeed;
        }
    }
    
    /// <summary>
    /// Updates the navigation path to target the player at regular intervals
    /// </summary>
    public void UpdatePath()
    {
        // Skip during pause
        if (PauseManager.IsPaused())
            return;
            
        if (Time.time - lastPathUpdateTime < updatePathInterval)
            return;
            
        lastPathUpdateTime = Time.time;
        
        // Target position - either closest point on collider or transform position
        Vector3 targetPosition = GetTargetPosition();
        
        // Always chase the player - removed distance check
        navMeshAgent.SetDestination(targetPosition);
    }
    
    /// <summary>
    /// Updates the zombie's rotation to face movement direction or player
    /// </summary>
    public void UpdateRotation()
    {
        // Skip during pause
        if (PauseManager.IsPaused())
            return;
            
        if (navMeshAgent.velocity.sqrMagnitude > 0.1f)
        {
            // Smoothly rotate towards movement direction
            Quaternion targetRotation = Quaternion.LookRotation(navMeshAgent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else if (isChasing && playerTransform != null)
        {
            // If stopped but chasing, look at player or closest point
            Vector3 targetPos = useClosestPoint && playerCollider != null ? 
                playerCollider.ClosestPoint(transform.position) : 
                playerTransform.position;
                
            Vector3 direction = targetPos - transform.position;
            direction.y = 0; // Keep rotation level with ground
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// Updates zombie animation parameters based on movement and state
    /// </summary>
    public void UpdateAnimations()
    {
        // Skip during pause
        if (PauseManager.IsPaused())
            return;
            
        if (animator != null)
        {
            // Set animation parameters based on movement
            float speed = navMeshAgent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsChasing", isChasing);
            
            // Set animation states based on zombie type and health
            bool isJogging = zombieType == ZombieType.Jogger && !isCriticallyInjured;
            bool isSprinting = zombieType == ZombieType.Sprinter && !isCriticallyInjured;
            
            animator.SetBool("IsJogging", isJogging);
            animator.SetBool("IsSprinting", isSprinting);
            animator.SetBool("IsInjured", isCriticallyInjured);
            
            if (healthSystem != null)
            {
                animator.SetBool("IsDead", healthSystem.IsDead());
            }
        }
    }

    /// <summary>
    /// Determines the optimal target position on the player to navigate towards
    /// </summary>
    /// <returns>The position in world space to target for navigation</returns>
    private Vector3 GetTargetPosition()
    {
        // If we can use closest point targeting and player collider exists
        if (useClosestPoint && playerCollider != null)
        {
            // Get the closest point on the player's collider
            Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
            
            // Check if the point is valid (not at origin if no closest point found)
            if (closestPoint != Vector3.zero || 
                Vector3.Distance(closestPoint, playerCollider.transform.position) < 1f)
            {
                // Sample the NavMesh to ensure we're getting a valid destination
                NavMeshHit hit;
                if (NavMesh.SamplePosition(closestPoint, out hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
            }
        }
        
        // Fallback to player's transform position
        return playerTransform.position;
    }
    
    /// <summary>
    /// Stops the zombie's movement and clears its navigation path
    /// </summary>
    public void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
        
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
        }
    }
    
    /// <summary>
    /// Resumes the zombie's movement after being stopped
    /// </summary>
    public void StartMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = false;
            
            // Make sure speed is updated when starting to move again
            navMeshAgent.speed = currentSpeed;
        }
    }
    
    /// <summary>
    /// Sets the type of this zombie (Walker, Jogger, Sprinter)
    /// </summary>
    /// <param name="type">The zombie type to set</param>
    public void SetZombieType(ZombieType type)
    {
        zombieType = type;
        UpdateMovementSpeed();
    }
    
    /// <summary>
    /// Called in OnDrawGizmosSelected to visualize the zombie's movement capabilities
    /// </summary>
    private void DrawSpeedGizmos()
    {
        // Draw colored lines based on zombie type
        if (zombieType == ZombieType.Sprinter && !isCriticallyInjured)
        {
            // Red for sprinters
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
        }
        else if (zombieType == ZombieType.Jogger && !isCriticallyInjured)
        {
            // Yellow for joggers
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
        else
        {
            // Green for walkers
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1);
        }
        
        // Draw magenta line for injured zombies
        if (isCriticallyInjured)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1);
        }
    }
    
    /// <summary>
    /// Checks if the zombie is a sprinter
    /// </summary>
    /// <returns>True if the zombie is a sprinter and not critically injured</returns>
    public bool IsSprinter()
    {
        return zombieType == ZombieType.Sprinter && !isCriticallyInjured;
    }
    
    /// <summary>
    /// Checks if the zombie is a jogger
    /// </summary>
    /// <returns>True if the zombie is a jogger and not critically injured</returns>
    public bool IsJogger()
    {
        return zombieType == ZombieType.Jogger && !isCriticallyInjured;
    }
    
    /// <summary>
    /// Checks if the zombie is critically injured and should move slowly
    /// </summary>
    /// <returns>True if critically injured, false otherwise</returns>
    public bool IsCriticallyInjured()
    {
        return isCriticallyInjured;
    }
    
    /// <summary>
    /// Visualizes zombie movement type and stopping distance in the editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Visual indicator for sprinting zombies
        if (zombieType == ZombieType.Sprinter && !isCriticallyInjured)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
        }
        
        // Visual indicator for injured zombies
        if (isCriticallyInjured)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1);
        }
    }

    /// <summary>
    /// Visualizes the current navigation target and path during gameplay
    /// </summary>
    private void OnDrawGizmos()
    {
        // Only show in play mode and when selected
        if (!Application.isPlaying || !isActiveAndEnabled)
            return;
            
        // Show the exact target point
        if (playerTransform != null && isChasing)
        {
            Vector3 targetPos = GetTargetPosition();
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPos, 0.3f);
            
            // Draw line from zombie to target
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }

    /// <summary>
    /// Applies gravity and handles ground detection for the zombie
    /// </summary>
    private void ApplyGravity()
    {
        if (!applyGravity || navMeshAgent == null || !navMeshAgent.isActiveAndEnabled)
            return;
            
        // Only apply custom gravity if we're not on a NavMesh
        if (!navMeshAgent.isOnNavMesh)
        {
            // Apply standard gravity
            verticalVelocity -= gravityMultiplier * Time.deltaTime;
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
            return;
        }
        
        // Check grounding less frequently to reduce NavMesh interference
        // Only check every 0.5 seconds or when significantly off the ground
        if (Time.frameCount % 30 == 0 || !isGrounded) // Roughly every 0.5 sec at 60fps
        {
            isGrounded = IsGrounded();
        }
        
        // Reset vertical velocity when on NavMesh
        if (isGrounded)
        {
            verticalVelocity = 0f;
        }
    }

    /// <summary>
    /// Checks if the zombie is on the ground and adjusts its height if necessary
    /// </summary>
    /// <returns>True if the zombie is on the ground, false otherwise</returns>
    private bool IsGrounded()
    {
        // Use the ground check point if available, otherwise use transform position
        Vector3 origin = groundCheckPoint != null ? 
            groundCheckPoint.position : 
            transform.position + Vector3.up * 0.5f;
        
        Debug.DrawRay(origin, Vector3.down * (groundCheckDistance + 1f), Color.red); // Visualization
        
        // Cast a ray downward to check for ground
        RaycastHit hit;
        if (Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance + 1f, groundLayer))
        {
            // Calculate desired position
            float desiredHeightAboveGround = heightOffset;
            float targetY = hit.point.y + desiredHeightAboveGround;
            
            // Only make significant adjustments if really needed (zombie is floating or sinking badly)
            float heightDifference = Mathf.Abs(transform.position.y - targetY);
            
            if (heightDifference > 0.5f) // Only fix severe discrepancies
            {
                // Create the new position, maintaining X and Z
                Vector3 newPosition = new Vector3(
                    transform.position.x,
                    targetY,
                    transform.position.z
                );
                
                // Apply the new position
                transform.position = newPosition;
                
                // Update the NavMeshAgent
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.Warp(newPosition); // Force immediate NavMesh position update
                }
            }
            // For minor discrepancies, make very gentle adjustments
            else if (heightDifference > 0.05f) 
            {
                // Smoothly adjust height while preserving movement
                float newY = Mathf.Lerp(transform.position.y, targetY, 0.1f); // Very gradual adjustment
                
                Vector3 newPosition = new Vector3(
                    transform.position.x,
                    newY,
                    transform.position.z
                );
                
                // Apply the new position
                transform.position = newPosition;
                
                // Let the NavMeshAgent catch up naturally
                if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.nextPosition = transform.position;
                }
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the current movement speed of the zombie
    /// </summary>
    /// <returns>Current speed in meters per second</returns>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    /// <summary>
    /// Gets the current zombie type (Walker, Jogger, Sprinter)
    /// </summary>
    /// <returns>The zombie's type as an enum value</returns>
    public ZombieType GetZombieType()
    {
        return zombieType;
    }
    
    /// <summary>
    /// Checks if the zombie is currently chasing the player
    /// </summary>
    /// <returns>True if the zombie is in chase mode</returns>
    public bool IsChasing()
    {
        return isChasing;
    }
}
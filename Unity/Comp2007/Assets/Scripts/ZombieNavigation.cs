using UnityEngine;
using UnityEngine.AI;

public class ZombieNavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private float updatePathInterval = 0.5f;
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;    // Normal walking speed
    [SerializeField] private float sprintSpeed = 8f;  // Sprint speed for faster zombies
    [SerializeField] private float injuredSpeed = 1.5f; // Speed when critically injured
    
    [Header("Sprint Behavior")]
    [SerializeField] private bool isSprinter = false; // Whether this zombie is a sprinter
    [SerializeField] private float criticalHealthPercent = 0.1f; // 10% health threshold for slowing down
    
    [Header("References")]
    [SerializeField] private Animator animator;
    
    // Component references
    private NavMeshAgent navMeshAgent;
    private Transform playerTransform;
    private HealthSystem healthSystem;
    
    // State tracking
    private bool isChasing = false;
    private bool isCriticallyInjured = false;
    private float lastPathUpdateTime = 0f;
    private float currentSpeed;

    private Collider playerCollider;
    private bool useClosestPoint = true;
    
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
        currentSpeed = isSprinter ? sprintSpeed : walkSpeed;
        
        // Configure NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.speed = currentSpeed;
            navMeshAgent.updateRotation = false; // We'll handle rotation manually for smoother turns
        }
        
        // Set isChasing to true immediately
        isChasing = true;
    }
    
    // Update the Update method to check for pause state
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
        
        UpdatePath();
        UpdateRotation();
        UpdateAnimations();
    }
    
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
    
    private void UpdateMovementSpeed()
    {
        // If critically injured, use injured speed regardless of sprinter status
        if (isCriticallyInjured)
        {
            currentSpeed = injuredSpeed;
        }
        else
        {
            // Otherwise use normal or sprint speed based on sprinter status
            currentSpeed = isSprinter ? sprintSpeed : walkSpeed;
        }
        
        // Apply to NavMeshAgent if available
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.speed = currentSpeed;
        }
    }
    
    // Update any additional methods that might be called during paused state
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
    
    // Update any additional methods that might be called during paused state
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
    
    // Update any additional methods that might be called during paused state
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
            
            // Effectively sprinting only if both designated as sprinter AND not critically injured
            bool effectivelySprinting = isSprinter && !isCriticallyInjured;
            animator.SetBool("IsSprinting", effectivelySprinting);
            
            // Add injured animation state if you have one
            animator.SetBool("IsInjured", isCriticallyInjured);
            
            if (healthSystem != null)
            {
                animator.SetBool("IsDead", healthSystem.IsDead());
            }
        }
    }

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
    
    public void StartMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = false;
            
            // Make sure speed is updated when starting to move again
            navMeshAgent.speed = currentSpeed;
        }
    }
    
    // Method to set this zombie as a sprinter - used by WaveManagement
    public void SetSprinter(bool sprinter)
    {
        isSprinter = sprinter;
        
        // Update speed based on both sprinter status and injury status
        UpdateMovementSpeed();
    }
    
    // Optional: Add this to visualize chase distances
    private void OnDrawGizmosSelected()
    {
        // Removed pursuit distance visualization since zombies always chase
        
        // Visual indicator for sprinting zombies
        if (isSprinter && !isCriticallyInjured)
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
}
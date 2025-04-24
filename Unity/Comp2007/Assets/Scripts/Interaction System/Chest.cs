using UnityEngine;

/// <summary>
/// Represents an interactive chest that can be opened and closed
/// Implements IInteractable for interaction system integration
/// Costs health to open and plays animations
/// </summary>
public class Chest : MonoBehaviour, IInteractable
{
    [SerializeField] private string _openPrompt = "Press E to close";      // Text shown when chest is open
    [SerializeField] private string _closedPrompt = "Press E to open (Costs 25 HP)"; // Text shown when chest is closed
    [SerializeField] private Animator _animator;                           // Reference to chest's animator component
    [SerializeField] private float _healthCost = 25f;                     // Health cost to open chest

    private bool _isOpen;                                                 // Current state of the chest
    private HealthArmourSystem _healthArmourSystem;                            // Reference to player's health system

    /// <summary>
    /// Called when the script instance is being loaded
    /// Finds and stores reference to the HealtharmourSystem in the scene
    /// </summary>
    private void Start()
    {
        _healthArmourSystem = FindAnyObjectByType<HealthArmourSystem>();
    }
    
    /// <summary>
    /// Property required by IInteractable interface
    /// Returns different prompts based on chest state
    /// </summary>
    public string InteractionPrompt => _isOpen ? _openPrompt : _closedPrompt;

    /// <summary>
    /// Called when player interacts with the chest
    /// Handles health cost check, damage application, and animation
    /// </summary>
    public bool Interact(Interactor interactor)
    {
        // If no health system was found, try to find it on the interactor
        if (_healthArmourSystem == null)
        {
            _healthArmourSystem = interactor.GetComponent<HealthArmourSystem>();
            
            if (_healthArmourSystem == null)
            {
                _healthArmourSystem = interactor.GetComponentInParent<HealthArmourSystem>();
            }
        }
        
        // Check health cost when opening
        if (!_isOpen)
        {
            if (_healthArmourSystem != null)
            {
                if (_healthArmourSystem.CurrentHealth <= _healthCost)
                {
                    Debug.Log("Not enough health to open chest!");
                    return false;  // Prevent opening if not enough health
                }
                
                // Apply health cost using TakeDamage method
                _healthArmourSystem.TakeDamage(_healthCost);
            }
            else
            {
                Debug.LogWarning("No HealtharmourSystem found, opening chest without health cost");
            }
        }

        // Toggle chest state
        _isOpen = !_isOpen;
        
        // Update animation if animator exists
        if (_animator != null)
        {
            _animator.SetBool("IsOpen", _isOpen);
        }
        
        return true;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Manages player interaction with objects implementing IInteractable interface
/// Detects nearby interactable objects and handles interaction input
public class Interactor : MonoBehaviour
{
    [SerializeField] private Transform _interactionPoint;          // Point from which to check for interactables
    [SerializeField] private float _interactionPointRadius = 0.5f; // Radius of interaction detection sphere
    [SerializeField] private LayerMask _interactableMask;         // Layer mask for interactable objects
    [SerializeField] private InteractionPromtUI[] _interactionPromtUIs; // Array of UI prompts to update

    private readonly Collider[] _colliders = new Collider[3];     // Buffer for overlap sphere results
    [SerializeField] private int _numFound;                       // Number of colliders found in overlap check

    private IInteractable _interactable;                          // Currently focused interactable object
    private KeyCode _lastInteractionKey = KeyCode.None;           // Track the last interaction key to prevent log spam

    /// Updates interaction detection and handles player input each frame
    private void Update()
    {
        // Skip all input processing if game is paused
        if (PauseManager.IsPaused())
        {
            // Hide any currently shown prompts when paused
            if (_interactable != null)
            {
                foreach (var promptUI in _interactionPromtUIs)
                {
                    if (promptUI != null && promptUI.IsDisplayed)
                        promptUI.Close();
                }
            }
            return;
        }
        
        // Debug ray to visualize interaction direction
        Debug.DrawRay(_interactionPoint.position, transform.forward * _interactionPointRadius, Color.yellow);

        // Check for interactable objects within radius
        _numFound = Physics.OverlapSphereNonAlloc(_interactionPoint.position, 
            _interactionPointRadius, _colliders, _interactableMask);

        if (_numFound > 0)
        {
            // Find closest interactable object
            float closestDistance = float.MaxValue;
            IInteractable nearestInteractable = null;

            for (int i = 0; i < _numFound; i++)
            {
                if (_colliders[i] != null)
                {
                    var currentInteractable = _colliders[i].GetComponent<IInteractable>();
                    if (currentInteractable != null)
                    {
                        // Calculate distance to interactable
                        float distance = Vector3.Distance(_interactionPoint.position, 
                            _colliders[i].transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            nearestInteractable = currentInteractable;
                        }
                    }
                }
            }

            // Update UI and handle interaction for nearest object
            if (nearestInteractable != null)
            {
                _interactable = nearestInteractable;
                // Update all UI prompts with interaction text
                foreach (var promptUI in _interactionPromtUIs)
                {
                    if (promptUI != null)
                        promptUI.SetUp(_interactable.InteractionPrompt);
                }
                
                // Get the specific interaction key for this interactable
                KeyCode interactionKey = KeyCode.E; // Default
                
                // Try to get custom key if the interactable specifies one
                if (_interactable is IInteractable customKeyInteractable)
                {
                    try {
                        // This uses reflection since we added InteractionKey as a default implementation
                        // This code explicitly checks for the custom key
                        var property = typeof(IInteractable).GetProperty("InteractionKey");
                        if (property != null)
                        {
                            interactionKey = (KeyCode)property.GetValue(_interactable);
                            
                            // Only log when the key changes or when interacting with a new object
                            if (_lastInteractionKey != interactionKey)
                            {
                                _lastInteractionKey = interactionKey;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                
                // Handle interaction input - Check for the specific key
                if (Input.GetKeyDown(interactionKey))
                {
                    bool interactionResult = _interactable.Interact(this);
                }
            }
            else
            {
                // Clear interaction target if none found
                if (_interactable != null) 
                {
                    _interactable = null;
                    _lastInteractionKey = KeyCode.None; // Reset the last key when no longer interacting
                }
                
                // Close all UI prompts
                foreach (var promptUI in _interactionPromtUIs)
                {
                    if (promptUI != null && promptUI.IsDisplayed)
                        promptUI.Close();
                }
            }
        }
        else
        {
            // Clear interaction target if none in range
            if (_interactable != null)
            {
                _interactable = null;
                _lastInteractionKey = KeyCode.None; // Reset the last key when no longer interacting
            }
            
            // Close all UI prompts
            foreach (var promptUI in _interactionPromtUIs)
            {
                if (promptUI != null && promptUI.IsDisplayed)
                    promptUI.Close();
            }
        }
    }

    /// Draws interaction radius visualization in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_interactionPoint.position, _interactionPointRadius);
    }
}
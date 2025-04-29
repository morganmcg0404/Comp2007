using UnityEngine;

/// <summary>
/// Interface for all objects that can be interacted with by the player
/// Provides a standard contract for the interaction system
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Gets the text prompt to display when player is near the interactable object
    /// </summary>
    /// <remarks>
    /// This should include instructions for the player on how to interact and any associated costs
    /// Example: "Press E to open door" or "Press E to buy Ammo (50 points)"
    /// </remarks>
    string InteractionPrompt { get; }
    
    /// <summary>
    /// Called when the player interacts with this object
    /// </summary>
    /// <param name="interactor">Reference to the Interactor component that initiated the interaction</param>
    /// <returns>True if the interaction was successful, false otherwise</returns>
    bool Interact(Interactor interactor);
    
    /// <summary>
    /// The key that needs to be pressed to interact with this object
    /// </summary>
    /// <remarks>
    /// Default implementation returns KeyCode.E
    /// Override this property to customize the interaction key for specific objects
    /// </remarks>
    KeyCode InteractionKey => KeyCode.E;
}
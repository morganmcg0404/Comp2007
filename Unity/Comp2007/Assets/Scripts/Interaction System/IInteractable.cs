using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    bool Interact(Interactor interactor);
    
    // Default implementation for interaction key (defaults to E)
    KeyCode InteractionKey => KeyCode.E;
}